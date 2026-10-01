using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Authoring tools for the Heavy class and its imported viewmodels.
public static class HeavySetup
{
    private const string Root = "Assets/WeaponsTF2/Heavy/";
    [MenuItem("Tools/Heavy/Open Demo")]
    public static void OpenDemo()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene("Assets/Scenes/Heavy Demo.unity");
    }

    [MenuItem("Tools/Heavy/Configure Heavy")]
    public static void Configure()
    {
        Directory.CreateDirectory("Assets/Prefabs/TF2/Heavy");
        AssetDatabase.Refresh();
        var mini = MakeWeapon("Minigun", true);
        var shotgun = MakeWeapon("Shootgun", false);
        var heavy = AssetDatabase.LoadAssetAtPath<Classes>("Assets/Classes/Heavy.asset");
        if (heavy == null)
        {
            heavy = ScriptableObject.CreateInstance<Classes>();
            AssetDatabase.CreateAsset(heavy, "Assets/Classes/Heavy.asset");
        }
        heavy.displayName = "Heavy";
        heavy.maxHealth = 300;
        // Scout 400 HU/s maps to 10 units/s in this project; Heavy uses 230 HU/s.
        heavy.moveSpeed = 5.75f;
        heavy.jumpForce = 7.4f;
        heavy.airJumps = 0;
        heavy.airJumpSpeed = 0;
        heavy.gravity = 20.3f;
        heavy.backwardSpeedMultiplier = 0.9f;
        heavy.primaryWeapon = mini;
        heavy.secondaryWeapon = shotgun;
        EditorUtility.SetDirty(heavy);
        AssetDatabase.SaveAssets();
        const string demo = "Assets/Scenes/Heavy Demo.unity";
        if (!File.Exists(demo))
        {
            if (!AssetDatabase.CopyAsset("Assets/Scenes/Escena de prueba.unity", demo))
                throw new Exception("Could not copy the gameplay scene.");
            Scene scene = EditorSceneManager.OpenScene(demo, OpenSceneMode.Additive);
            try
            {
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var player in root.GetComponentsInChildren<PlayerClass>(true))
                    {
                        var serialized = new SerializedObject(player);
                        serialized.FindProperty("selectedClass").objectReferenceValue = heavy;
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                    }
                EditorSceneManager.SaveScene(scene);
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
        Debug.Log("Heavy ready: open Assets/Scenes/Heavy Demo.unity to play.");
    }

    private static Gun MakeWeapon(string name, bool minigun)
    {
        string folder = Root + name + "/";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(folder + "Animations/" + name + "Animator.controller");
        if (controller == null) throw new Exception("Missing controller: " + name);
        ConfigureController(controller, minigun);
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(folder + "Models/" + name + ".fbx");
        if (model == null) throw new Exception("Missing model: " + name);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
        try
        {
            instance.name = minigun ? "HeavyMinigun" : "HeavyShotgun";
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0, 90, 0));
            var animator = instance.GetComponent<Animator>();
            if (animator == null) animator = instance.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var gun = instance.GetComponent<Gun>();
            if (gun == null) gun = instance.AddComponent<Gun>();
            var template = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TF2/Scout/ScatterGun.prefab").GetComponent<Gun>();
            EditorUtility.CopySerialized(template, gun);
            gun.weaponAnimator = animator;
            gun.animatedMinigun = minigun;
            gun.barrelLocalAxis = Vector3.right;
            gun.animatedShotgun = !minigun;
            gun.animatedMagazine = false;
            gun.maxSize = minigun ? 200 : 6;
            gun.reserveSize = minigun ? -1 : 32;
            gun.fireRate = minigun ? 0.1f : 0.625f;
            gun.pelletsPerShot = minigun ? 4 : 10;
            gun.hitscan = true;
            gun.visibleTracers = true;
            gun.tracerPrefab = Resources.Load<GameObject>("WeaponTracer");
            gun.fireAnimationDuration = minigun ? 0f : 0.625f;
            gun.pelletDamage = minigun ? 9f : 6f;
            gun.spreadDegrees = minigun ? 3.5f : 4f;
            gun.spunMoveMultiplier = 110f / 230f;
            gun.equippedPosition = Vector3.zero;
            gun.equippedEulerAngles = new Vector3(0, 90, 0);
            gun.bulletSpawnPoint = instance.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "muzzle");
            if (minigun) gun.shootingSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Ak-47 Gunshot.mp3");
            string path = "Assets/Prefabs/TF2/Heavy/" + instance.name + ".prefab";
            return PrefabUtility.SaveAsPrefabAsset(instance, path).GetComponent<Gun>();
        }
        finally { UnityEngine.Object.DestroyImmediate(instance); }
    }

    private static void ConfigureController(AnimatorController controller, bool minigun)
    {
        var machine = controller.layers[0].stateMachine;
        var states = machine.states.ToDictionary(s => s.state.name, s => s.state);
        foreach (string required in minigun
            ? new[] { "@draw", "@idle", "@spool_up", "@spool_idle", "@fire", "@spool_down" }
            : new[] { "@draw", "@idle", "@fire", "@reload_start", "@reload_loop", "@reload_end" })
            if (!states.ContainsKey(required) || states[required].motion == null)
                throw new Exception("Missing animation " + controller.name + "/" + required);
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
        EditorUtility.SetDirty(controller);
    }
}
