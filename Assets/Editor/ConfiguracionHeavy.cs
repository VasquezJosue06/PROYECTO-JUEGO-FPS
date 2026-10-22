using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Authoring tools for the Heavy class and its imported viewmodels.
public static class ConfiguracionHeavy
{
    private const string Root = "Assets/WeaponsTF2/Heavy/";
    [MenuItem("Tools/Heavy/Open Demo")]
    public static void AbrirDemostracion()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene("Assets/Scenes/Heavy Demo.unity");
    }

    [MenuItem("Tools/Heavy/Configure Heavy")]
    public static void Configurar()
    {
        Directory.CreateDirectory("Assets/Prefabs/TF2/Heavy");
        AssetDatabase.Refresh();
        var mini = CrearArma("Minigun", true);
        var shotgun = CrearArma("Shootgun", false);
        var claseHeavy = AssetDatabase.LoadAssetAtPath<ClasesJugador>("Assets/Classes/Heavy.asset");
        if (claseHeavy == null)
        {
            claseHeavy = ScriptableObject.CreateInstance<ClasesJugador>();
            AssetDatabase.CreateAsset(claseHeavy, "Assets/Classes/Heavy.asset");
        }
        claseHeavy.nombreVisible = "Heavy";
        claseHeavy.vidaMaxima = 300;
        // Scout 400 HU/s maps to 10 units/s in this project; Heavy uses 230 HU/s.
        claseHeavy.velocidadMovimiento = 5.75f;
        claseHeavy.fuerzaSalto = 7.4f;
        claseHeavy.saltosAereos = 0;
        claseHeavy.velocidadSaltoAereo = 0;
        claseHeavy.gravedad = 20.3f;
        claseHeavy.multiplicadorVelocidadAtras = 0.9f;
        claseHeavy.armaPrincipal = mini;
        claseHeavy.armaSecundaria = shotgun;
        EditorUtility.SetDirty(claseHeavy);
        AssetDatabase.SaveAssets();
        const string demo = "Assets/Scenes/Heavy Demo.unity";
        if (!File.Exists(demo))
        {
            if (!AssetDatabase.CopyAsset("Assets/Scenes/Escena de prueba.unity", demo))
                throw new Exception("Could not copy the gameplay scene.");
            Scene escena = EditorSceneManager.OpenScene(demo, OpenSceneMode.Additive);
            try
            {
                foreach (var root in escena.GetRootGameObjects())
                    foreach (var jugador in root.GetComponentsInChildren<ClaseJugador>(true))
                    {
                        var serialized = new SerializedObject(jugador);
                        serialized.FindProperty("claseSeleccionada").objectReferenceValue = claseHeavy;
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                    }
                EditorSceneManager.SaveScene(escena);
            }
            finally { EditorSceneManager.CloseScene(escena, true); }
        }
        Debug.Log("Heavy ready: open Assets/Scenes/Heavy Demo.unity to play.");
    }

    private static Arma CrearArma(string nombre, bool minigun)
    {
        string folder = Root + nombre + "/";
        var controladorClase = AssetDatabase.LoadAssetAtPath<AnimatorController>(folder + "Animations/" + nombre + "Animator.controller");
        if (controladorClase == null) throw new Exception("Missing controller: " + nombre);
        ConfigurarControlador(controladorClase, minigun);
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(folder + "Models/" + nombre + ".fbx");
        if (model == null) throw new Exception("Missing model: " + nombre);
        GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(model);
        try
        {
            instancia.name = minigun ? "HeavyMinigun" : "HeavyShotgun";
            instancia.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0, 90, 0));
            var animator = instancia.GetComponent<Animator>();
            if (animator == null) animator = instancia.AddComponent<Animator>();
            animator.runtimeAnimatorController = controladorClase;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var armaActual = instancia.GetComponent<Arma>();
            if (armaActual == null) armaActual = instancia.AddComponent<Arma>();
            var template = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TF2/Scout/ScatterGun.prefab").GetComponent<Arma>();
            EditorUtility.CopySerialized(template, armaActual);
            armaActual.animadorArma = animator;
            armaActual.minigunAnimada = minigun;
            armaActual.ejeLocalTambor = Vector3.right;
            armaActual.escopetaAnimada = !minigun;
            armaActual.cargadorAnimado = false;
            armaActual.capacidadMaxima = minigun ? 200 : 6;
            armaActual.capacidadReserva = minigun ? -1 : 32;
            armaActual.frecuenciaDisparo = minigun ? 0.1f : 0.625f;
            armaActual.perdigonesPorDisparo = minigun ? 4 : 10;
            armaActual.impactoInstantaneo = true;
            armaActual.trazadorasVisibles = true;
            armaActual.prefabricadoTrazadora = Resources.Load<GameObject>("WeaponTracer");
            armaActual.duracionAnimacionDisparo = minigun ? 0f : 0.625f;
            armaActual.danioPerdigon = minigun ? 9f : 6f;
            armaActual.gradosDispersion = minigun ? 3.5f : 4f;
            armaActual.multiplicadorMovimientoGirando = 110f / 230f;
            armaActual.posicionEquipada = Vector3.zero;
            armaActual.angulosEulerEquipada = new Vector3(0, 90, 0);
            armaActual.puntoGeneracionProyectil = instancia.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "muzzle");
            if (minigun) armaActual.sonidoDisparo = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Ak-47 Gunshot.mp3");
            string ruta = "Assets/Prefabs/TF2/Heavy/" + instancia.name + ".prefab";
            return PrefabUtility.SaveAsPrefabAsset(instancia, ruta).GetComponent<Arma>();
        }
        finally { UnityEngine.Object.DestroyImmediate(instancia); }
    }

    private static void ConfigurarControlador(AnimatorController controladorClase, bool minigun)
    {
        var machine = controladorClase.layers[0].stateMachine;
        var states = machine.states.ToDictionary(s => s.state.name, s => s.state);
        foreach (string required in minigun
            ? new[] { "@draw", "@idle", "@spool_up", "@spool_idle", "@fire", "@spool_down" }
            : new[] { "@draw", "@idle", "@fire", "@reload_start", "@reload_loop", "@reload_end" })
            if (!states.ContainsKey(required) || states[required].motion == null)
                throw new Exception("Missing animation " + controladorClase.name + "/" + required);
        if (minigun)
        {
            // Synchronize the stock spin-up delay with the actual imported clip.
            var up = states["@spool_up"];
            up.speed = ((AnimationClip)up.motion).length / 0.87f;
            if (!states["@fire"].transitions.Any(t => t.destinationState == states["@spool_down"]))
            {
                var stop = states["@fire"].AddTransition(states["@spool_down"]);
                stop.hasExitTime = false;
                stop.duration = 0;
                stop.AddCondition(AnimatorConditionMode.IfNot, 0, "Spin");
            }
        }
        else states["@fire"].speed = ((AnimationClip)states["@fire"].motion).length / 0.625f;
        // Only continuous poses loop. Reload cycles are restarted by Gun per shell.
        string modelPath = AssetDatabase.GetAssetPath(states["@idle"].motion);
        var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
        var clips = importer.clipAnimations;
        if (clips.Length == 0) clips = importer.defaultClipAnimations;
        bool changed = false;
        foreach (var clip in clips)
        {
            bool loop = clip.name == "@idle" || (minigun && (clip.name == "@fire" || clip.name == "@spool_idle"));
            if (clip.loopTime != loop) { clip.loopTime = loop; changed = true; }
        }
        if (changed) { importer.clipAnimations = clips; importer.SaveAndReimport(); }
        EditorUtility.SetDirty(controladorClase);
    }
}
