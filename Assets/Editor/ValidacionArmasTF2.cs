using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

// Comprueba referencias reales sin modificar la escena abierta ni los prefabs.
public static class ValidacionArmasTF2
{
    [MenuItem("Tools/TF2/Validate Weapons")]
    public static void Ejecutar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Sal de Play antes de validar los prefabs de TF2.");
            return;
        }
        var escena = EditorSceneManager.NewPreviewScene();
        string informe = "";
        try
        {
            string[] paths = {
                "Assets/Prefabs/TF2/Scout/ScatterGun.prefab",
                "Assets/Prefabs/TF2/Scout/ScoutPistol.prefab",
                "Assets/Prefabs/TF2/Heavy/HeavyMinigun.prefab",
                "Assets/Prefabs/TF2/Heavy/HeavyShotgun.prefab"
            };
            foreach (string ruta in paths)
            {
                var prefabricado = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
                Exigir(prefabricado != null, "Falta prefab: " + ruta);
                var instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefabricado, escena);
                var armaActual = instancia.GetComponent<Arma>();
                Exigir(armaActual != null, "Falta componente Gun: " + ruta);
                Exigir(armaActual.impactoInstantaneo && armaActual.trazadorasVisibles, "Faltan impactos o trazadoras: " + ruta);
                Exigir(armaActual.prefabricadoTrazadora != null, "Falta prefab de trazadora: " + ruta);
                var visual = armaActual.prefabricadoTrazadora.GetComponent<VisualTrazadoraArma>();
                Exigir(visual != null && visual.material != null, "Falta configuracion visual: " + ruta);
                Exigir(!ShaderUtil.ShaderHasError(visual.material.shader), "Shader de trazadora invalido");
                var controladorClase = armaActual.animadorArma != null ? armaActual.animadorArma.runtimeAnimatorController as AnimatorController : null;
                Exigir(controladorClase != null, "Falta Animator Controller: " + ruta);
                var states = controladorClase.layers[0].stateMachine.states.Select(s => s.state).ToArray();
                foreach (string nombre in new[] { "@draw", "@idle", "@fire" })
                    Exigir(states.Any(s => s.name == nombre && s.motion != null), "Falta clip " + nombre + ": " + ruta);
                Exigir(instancia.GetComponentsInChildren<Transform>(true).Any(t => t == armaActual.puntoGeneracionProyectil || t.name == "muzzle"), "Falta boca de disparo: " + ruta);
                informe += "PASS: " + ruta + "\n";
            }
            foreach (string nombre in new[] { "Scout", "Heavy" })
            {
                var configuracion = AssetDatabase.LoadAssetAtPath<ClasesJugador>("Assets/Classes/" + nombre + ".asset");
                Exigir(configuracion != null && configuracion.armaPrincipal != null && configuracion.armaSecundaria != null, "Clase sin armas: " + nombre);
                Exigir(configuracion.PermiteArma(configuracion.armaPrincipal) && configuracion.PermiteArma(configuracion.armaSecundaria), "Armas no permitidas: " + nombre);
                informe += "PASS: clase " + nombre + "\n";
            }
            // Prueba el caso que antes rellenaba el cargador sin descontar reserva.
            var pistol = escena.GetRootGameObjects().Select(g => g.GetComponent<Arma>()).First(g => g.cargadorAnimado);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var campoMunicionActual = typeof(Arma).GetField("municionActual", flags);
            var campoMunicionReserva = typeof(Arma).GetField("municionReserva", flags);
            var fill = typeof(Arma).GetMethod("LlenarCargadorDesdeReserva", flags);
            pistol.capacidadMaxima = 12;
            campoMunicionActual.SetValue(pistol, 3); campoMunicionReserva.SetValue(pistol, 2);
            fill.Invoke(pistol, null);
            Exigir(pistol.MunicionActual == 5 && pistol.MunicionReserva == 0, "La recarga no respeta la reserva finita");
            fill.Invoke(pistol, null);
            Exigir(pistol.MunicionActual == 5, "La reserva vacia crea municion");
            campoMunicionReserva.SetValue(pistol, -1); fill.Invoke(pistol, null);
            Exigir(pistol.MunicionActual == 12 && pistol.MunicionReserva == -1, "Se perdio la reserva ilimitada existente");
            informe += "PASS: reserva finita, vacia e ilimitada\n";
            Debug.Log(informe);
        }
        catch (Exception error)
        {
            informe += "FAIL: " + error;
            Debug.LogException(error);
        }
        finally
        {
            Directory.CreateDirectory("Temp");
            File.WriteAllText("Temp/TF2WeaponsValidation.result", informe);
            EditorSceneManager.ClosePreviewScene(escena);
        }
    }

    private static void Exigir(bool condition, string mensaje)
    {
        if (!condition) throw new Exception(mensaje);
    }
}
