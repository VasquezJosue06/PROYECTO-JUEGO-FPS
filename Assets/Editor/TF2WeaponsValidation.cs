using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

// Comprueba referencias reales sin modificar la escena abierta ni los prefabs.
public static class TF2WeaponsValidation
{
    [MenuItem("Tools/TF2/Validate Weapons")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Sal de Play antes de validar los prefabs de TF2.");
            return;
        }
        var scene = EditorSceneManager.NewPreviewScene();
        string report = "";
        try
        {
            string[] paths = {
                "Assets/Prefabs/TF2/Scout/ScatterGun.prefab",
                "Assets/Prefabs/TF2/Scout/ScoutPistol.prefab",
                "Assets/Prefabs/TF2/Heavy/HeavyMinigun.prefab",
                "Assets/Prefabs/TF2/Heavy/HeavyShotgun.prefab"
            };
            foreach (string path in paths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Require(prefab != null, "Falta prefab: " + path);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                var gun = instance.GetComponent<Gun>();
                Require(gun != null, "Falta componente Gun: " + path);
                Require(gun.hitscan && gun.visibleTracers, "Faltan impactos o trazadoras: " + path);
                Require(gun.tracerPrefab != null, "Falta prefab de trazadora: " + path);
                var visual = gun.tracerPrefab.GetComponent<WeaponTracerVisual>();
                Require(visual != null && visual.material != null, "Falta configuracion visual: " + path);
                Require(!ShaderUtil.ShaderHasError(visual.material.shader), "Shader de trazadora invalido");
                var controller = gun.weaponAnimator != null ? gun.weaponAnimator.runtimeAnimatorController as AnimatorController : null;
                Require(controller != null, "Falta Animator Controller: " + path);
                var states = controller.layers[0].stateMachine.states.Select(s => s.state).ToArray();
                foreach (string name in new[] { "@draw", "@idle", "@fire" })
                    Require(states.Any(s => s.name == name && s.motion != null), "Falta clip " + name + ": " + path);
                Require(instance.GetComponentsInChildren<Transform>(true).Any(t => t == gun.bulletSpawnPoint || t.name == "muzzle"), "Falta boca de disparo: " + path);
                report += "PASS: " + path + "\n";
            }
            foreach (string name in new[] { "Scout", "Heavy" })
            {
                var configuration = AssetDatabase.LoadAssetAtPath<Classes>("Assets/Classes/" + name + ".asset");
                Require(configuration != null && configuration.primaryWeapon != null && configuration.secondaryWeapon != null, "Clase sin armas: " + name);
                Require(configuration.AllowsWeapon(configuration.primaryWeapon) && configuration.AllowsWeapon(configuration.secondaryWeapon), "Armas no permitidas: " + name);
                report += "PASS: clase " + name + "\n";
            }
            // Prueba el caso que antes rellenaba el cargador sin descontar reserva.
            var pistol = scene.GetRootGameObjects().Select(g => g.GetComponent<Gun>()).First(g => g.animatedMagazine);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var current = typeof(Gun).GetField("currentAmmo", flags);
            var reserve = typeof(Gun).GetField("reserveAmmo", flags);
            var fill = typeof(Gun).GetMethod("FillMagazineFromReserve", flags);
            pistol.maxSize = 12;
            current.SetValue(pistol, 3); reserve.SetValue(pistol, 2);
            fill.Invoke(pistol, null);
            Require(pistol.CurrentAmmo == 5 && pistol.ReserveAmmo == 0, "La recarga no respeta la reserva finita");
            fill.Invoke(pistol, null);
            Require(pistol.CurrentAmmo == 5, "La reserva vacia crea municion");
            reserve.SetValue(pistol, -1); fill.Invoke(pistol, null);
            Require(pistol.CurrentAmmo == 12 && pistol.ReserveAmmo == -1, "Se perdio la reserva ilimitada existente");
            report += "PASS: reserva finita, vacia e ilimitada\n";
            Debug.Log(report);
        }
        catch (Exception error)
        {
            report += "FAIL: " + error;
            Debug.LogException(error);
        }
        finally
        {
            Directory.CreateDirectory("Temp");
            File.WriteAllText("Temp/TF2WeaponsValidation.result", report);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
