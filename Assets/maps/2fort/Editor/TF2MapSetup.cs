using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
public static class TF2MapSetup
{
    const string Output = "Assets/maps/2fort";
    const string ScenePath = "Assets/maps/2fort/Scenes/2Fort Playable.unity";
    [MenuItem("Tools/TF2/2Fort/Create Playable Scene")]
    public static void Create()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (File.Exists(ScenePath)) { EditorSceneManager.OpenScene(ScenePath); return; }
        Build();
    }
    public static void Build()
    {
        if (File.Exists(ScenePath)) throw new InvalidOperationException("La escena ya existe; no se sobrescribe.");
        var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/maps/2fort/ctf_2fort_Unity_ExportTest.fbx");
        if (model == null) throw new InvalidOperationException("Falta FBX 2Fort.");
        Directory.CreateDirectory(Output + "/Collision");
        Directory.CreateDirectory(Output + "/Prefabs");
        Directory.CreateDirectory(Output + "/Materials/Gameplay");
        Directory.CreateDirectory(Output + "/Scenes");
        AssetDatabase.Refresh();
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Escena de prueba.unity");
        var player = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PlayerMove>(true)).FirstOrDefault();
        if (player == null) throw new InvalidOperationException("Falta jugador en Escena de prueba.");
        foreach (var root in scene.GetRootGameObjects())
            if (root != player.transform.root.gameObject && root.GetComponentInChildren<UiManager>(true) == null && root.GetComponentInChildren<AudioManager>(true) == null && root.GetComponentInChildren<UnityEngine.EventSystems.EventSystem>(true) == null)
                UnityEngine.Object.DestroyImmediate(root);
        var map = (GameObject)PrefabUtility.InstantiatePrefab(model, scene);
        map.name = "2Fort_Geometry";
        map.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        int layer = LayerMask.NameToLayer("Ground");
        if (layer < 0) throw new InvalidOperationException("Falta capa Ground.");
        int count = RebuildColliders(map, layer);
        Physics.SyncTransforms();
        var spawn = new GameObject("Spawn_Exploration");
        spawn.transform.SetParent(new GameObject("2Fort_Gameplay").transform);
        spawn.transform.position = FindSpawn(map, player, layer);
        player.transform.position = spawn.transform.position;
        player.groundMask = 1 << layer;
        var selector = player.GetComponent<TF2ExplorationClasses>();
        if (selector == null) selector = player.gameObject.AddComponent<TF2ExplorationClasses>();
        selector.scout = AssetDatabase.LoadAssetAtPath<Classes>("Assets/Classes/Scout.asset");
        selector.heavy = AssetDatabase.LoadAssetAtPath<Classes>("Assets/Classes/Heavy.asset");
        player.GetComponent<Rigidbody>().constraints |= RigidbodyConstraints.FreezeRotation;
        var respawn = player.gameObject.AddComponent<TF2MapRespawn>();
        respawn.spawn = spawn.transform;
        respawn.minimumHeight = map.GetComponentsInChildren<Renderer>().Min(r => r.bounds.min.y) - 15;
        Lighting();
        Specials(map);
        ForceBackfaceCulling(map);
        HideSourceHelpers(map);
        PrefabUtility.SaveAsPrefabAsset(map, Output + "/Prefabs/2Fort_Map.prefab");
        PrefabUtility.SaveAsPrefabAsset(player.transform.root.gameObject, Output + "/Prefabs/2Fort_Player.prefab");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Audit(scene);
        Debug.Log("2Fort creada con " + count + " colliders. Revisar spawn, puertas, agua y rendimiento en Play.");
    }
    static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
    static bool NonSolid(Material m)
    {
        if (m == null) return false;
        string n = m.name.ToLowerInvariant();
        return n.StartsWith("water_") || n.Contains("toolssky");
    }

    static bool IsGameplayGeometry(string hierarchy)
    {
        // The imported FBX does not preserve every walkable displacement under the
        // expected SourceIO collection names. Keep visible solid meshes, while dropping
        // only entity helpers and non-playable export artifacts.
        return !hierarchy.Contains("skybox") && !hierarchy.Contains("template") &&
               !hierarchy.Contains("physics") && !hierarchy.Contains("/lights/") &&
               !hierarchy.Contains("/environment/") && !hierarchy.Contains("/logic/") &&
               !hierarchy.Contains("sky_camera");
    }

    static int RebuildColliders(GameObject map, int layer)
    {
        foreach (var collider in map.GetComponentsInChildren<MeshCollider>(true))
            UnityEngine.Object.DestroyImmediate(collider);

        int count = 0;
        foreach (var filter in map.GetComponentsInChildren<MeshFilter>(true))
        {
            var renderer = filter.GetComponent<MeshRenderer>();
            var mesh = filter.sharedMesh;
            if (mesh == null || renderer == null || !IsGameplayGeometry(PathOf(filter.transform).ToLowerInvariant()))
                continue;

            var materials = renderer.sharedMaterials;
            var solid = Enumerable.Range(0, mesh.subMeshCount)
                .Where(i => i >= materials.Length || !NonSolid(materials[i])).ToArray();
            if (solid.Length == 0) continue;
            if (solid.Length != mesh.subMeshCount)
            {
                var clone = UnityEngine.Object.Instantiate(mesh);
                clone.subMeshCount = 1;
                clone.SetTriangles(solid.SelectMany(i => mesh.GetTriangles(i)).ToArray(), 0);
                string path = Output + "/Collision/Solid_" + count + ".asset";
                var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (existing != null)
                {
                    EditorUtility.CopySerialized(clone, existing);
                    UnityEngine.Object.DestroyImmediate(clone);
                    mesh = existing;
                }
                else
                {
                    AssetDatabase.CreateAsset(clone, path);
                    mesh = clone;
                }
            }
            var collider = filter.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            collider.convex = false;
            filter.gameObject.layer = layer;
            count++;
        }
        if (count == 0) throw new InvalidOperationException("No se encontró geometría sólida para la colisión.");
        return count;
    }
    static Vector3 FindSpawn(GameObject map, PlayerMove player, int layer)
    {
        var renderers = map.GetComponentsInChildren<Renderer>();
        var bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        var c = player.GetComponent<CapsuleCollider>();
        if (c == null || c.direction != 1) throw new InvalidOperationException("Falta cápsula vertical del jugador.");
        float radius = c.radius * Mathf.Max(Mathf.Abs(player.transform.lossyScale.x), Mathf.Abs(player.transform.lossyScale.z));
        float half = Mathf.Max(radius, c.height * Mathf.Abs(player.transform.lossyScale.y) * 0.5f);
        Vector3 centerOffset = player.transform.TransformVector(c.center);
        for (int ring = 0; ring < 40; ring++) for (int step = 0; step < 16; step++)
        {
            float angle = step * Mathf.PI / 8;
            var origin = new Vector3(bounds.center.x + Mathf.Cos(angle) * ring * 2, bounds.max.y + 5, bounds.center.z + Mathf.Sin(angle) * ring * 2);
            foreach (var hit in Physics.RaycastAll(origin, Vector3.down, bounds.size.y + 10, 1 << layer, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance))
            {
                if (hit.normal.y < 0.85f) continue;
                Vector3 center = hit.point + Vector3.up * (half + 0.08f), offset = Vector3.up * (half - radius);
                if (!Physics.CheckCapsule(center - offset, center + offset, radius, 1 << layer, QueryTriggerInteraction.Ignore)) return center - centerOffset;
            }
        }
        throw new InvalidOperationException("No se encontró spawn libre. Revisar orientación/escala del modelo.");
    }
    static void Lighting()
    {
        var old = GameObject.Find("2Fort_Lighting");
        if (old != null) UnityEngine.Object.DestroyImmediate(old);
        Lightmapping.Clear();
        var root = new GameObject("2Fort_Lighting");
        var sun = new GameObject("Sun_Approximation").AddComponent<Light>();
        sun.transform.SetParent(root.transform);
        sun.type = LightType.Directional;
        sun.transform.rotation = Quaternion.Euler(48, -35, 0);
        sun.color = new Color(1, 0.91f, 0.76f);
        sun.intensity = 1.1f;
        sun.shadows = LightShadows.Soft;
        RenderSettings.sun = sun;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.48f, 0.56f, 0.65f);
        RenderSettings.ambientEquatorColor = new Color(0.34f, 0.31f, 0.26f);
        RenderSettings.ambientGroundColor = new Color(0.18f, 0.16f, 0.13f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.63f, 0.69f, 0.72f);
        RenderSettings.fogStartDistance = 250;
        RenderSettings.fogEndDistance = 800;
    }
    static void Specials(GameObject map)
    {
        var replacements = new Dictionary<Material, Material>();
        foreach (var renderer in map.GetComponentsInChildren<Renderer>())
        {
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                var original = materials[i];
                if (original == null) continue;
                string n = original.name.ToLowerInvariant();
                bool water = n.StartsWith("water_2fort");
                bool sky = n.Contains("toolsskybox");
                if (!water && !sky && !n.StartsWith("reflectivity_")) continue;
                if (!replacements.TryGetValue(original, out var material))
                {
                    string path = Output + "/Materials/Gameplay/" + original.name + ".mat";
                    material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (material == null)
                    {
                        var shader = Shader.Find(water ? "TF2/2Fort Water" : "Universal Render Pipeline/Unlit");
                        if (shader == null) throw new InvalidOperationException("Shader especial no importado.");
                        material = new Material(shader);
                        if (water)
                        {
                            material.SetColor("_WaterColor", n.Contains("beneath") ? new Color(0.36f, 0.39f, 0.31f, 0.8f) : new Color(0.20f, 0.17f, 0.05f, 0.65f));
                            material.SetTexture("_NormalMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/maps/Textures/water/tfwater001_normal.png"));
                        }
                        else if (sky)
                        {
                            material.SetColor("_BaseColor", new Color(0f, 0f, 0f, 0f));
                            material.SetFloat("_Surface", 1f);
                            material.renderQueue = (int)RenderQueue.Transparent;
                        }
                        else
                        {
                            var match = System.Text.RegularExpressions.Regex.Match(n, @"reflectivity_(\d+)");
                            float value = match.Success ? int.Parse(match.Groups[1].Value) / 100f : 0.3f;
                            material.SetColor("_BaseColor", new Color(value, value, value));
                            material.SetFloat("_Smoothness", 0);
                        }
                        AssetDatabase.CreateAsset(material, path);
                    }
                    replacements.Add(original, material);
                }
                materials[i] = material;
            }
            renderer.sharedMaterials = materials;
        }
    }

    static void ForceBackfaceCulling(GameObject map)
    {
        foreach (var material in map.GetComponentsInChildren<Renderer>(true)
                     .SelectMany(renderer => renderer.sharedMaterials).Where(material => material != null).Distinct())
        {
            string name = material.name.ToLowerInvariant();
            if (name.StartsWith("water_") || name.Contains("toolsskybox")) continue;
            if (material.HasProperty("_Cull")) material.SetFloat("_Cull", 2f);
            EditorUtility.SetDirty(material);
        }
    }

    static void HideSourceHelpers(GameObject map)
    {
        foreach (var renderer in map.GetComponentsInChildren<Renderer>(true))
        {
            string hierarchy = PathOf(renderer.transform).ToLowerInvariant();
            // These are SourceIO entity helpers, not the visible lamps or props in static_props.
            if (hierarchy.Contains("/lights/") || hierarchy.Contains("/environment/") ||
                hierarchy.Contains("/logic/") || hierarchy.Contains("sky_camera"))
                renderer.enabled = false;
        }
    }

    [MenuItem("Tools/TF2/2Fort/Refresh Playable Scene")]
    public static void RefreshPlayableScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene previousActive = SceneManager.GetActiveScene();
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool openedForRefresh = !scene.IsValid() || !scene.isLoaded;
        if (openedForRefresh) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
        var map = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "2Fort_Geometry");
        if (map == null) throw new InvalidOperationException("Falta 2Fort_Geometry en la escena jugable.");
        int layer = LayerMask.NameToLayer("Ground");
        if (layer < 0) throw new InvalidOperationException("Falta capa Ground.");
        int count = RebuildColliders(map, layer);
        Lighting();
        Specials(map);
        ForceBackfaceCulling(map);
        HideSourceHelpers(map);
        foreach (var player in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PlayerMove>(true)))
        {
            var body = player.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.constraints |= RigidbodyConstraints.FreezeRotation;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
        }
        PrefabUtility.SaveAsPrefabAsset(map, Output + "/Prefabs/2Fort_Map.prefab");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Audit(scene);
        Debug.Log("2Fort actualizada con " + count + " colliders sólidos.");
        }
        finally
        {
            if (previousActive.IsValid() && previousActive.isLoaded) SceneManager.SetActiveScene(previousActive);
            if (openedForRefresh) EditorSceneManager.CloseScene(scene, true);
        }
    }
    [MenuItem("Tools/TF2/2Fort/Add Sliding Door To Selected Panel")]
    public static void Door()
    {
        var panel = Selection.activeGameObject;
        if (panel == null || EditorUtility.IsPersistent(panel) || panel.GetComponentInParent<TF2SlidingDoor>() != null) return;
        var prefab = PrefabUtility.GetOutermostPrefabInstanceRoot(panel);
        if (prefab != null) PrefabUtility.UnpackPrefabInstance(prefab, PrefabUnpackMode.Completely, InteractionMode.UserAction);
        Undo.IncrementCurrentGroup();
        var root = new GameObject(panel.name + "_Trigger");
        Undo.RegisterCreatedObjectUndo(root, "Crear puerta");
        root.transform.SetParent(panel.transform.parent, false);
        root.transform.SetPositionAndRotation(panel.transform.position, panel.transform.rotation);
        Undo.SetTransformParent(panel.transform, root.transform, "Mover panel");
        var door = Undo.AddComponent<TF2SlidingDoor>(root);
        door.panel = panel.transform;
        var trigger = Undo.AddComponent<BoxCollider>(root);
        trigger.isTrigger = true;
        var renderers = panel.GetComponentsInChildren<Renderer>();
        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
        foreach (var renderer in renderers)
        {
            Bounds b = renderer.bounds;
            for (int i = 0; i < 8; i++) bounds.Encapsulate(root.transform.InverseTransformPoint(b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1))));
        }
        trigger.center = bounds.center;
        trigger.size = bounds.size + Vector3.one * 2;
        door.localTravel = Vector3.up * (bounds.size.y + 0.1f);
        foreach (var t in panel.GetComponentsInChildren<Transform>()) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(root.scene);
    }
    [MenuItem("Tools/TF2/2Fort/Audit Active Scene")]
    public static void Audit()
    {
        Audit(SceneManager.GetActiveScene());
    }

    static void Audit(Scene scene)
    {
        var roots = scene.GetRootGameObjects();
        var colliders = roots.SelectMany(r => r.GetComponentsInChildren<MeshCollider>(true)).ToArray();
        var players = roots.SelectMany(r => r.GetComponentsInChildren<PlayerMove>(true)).ToArray();
        string report = "Escena: " + scene.path + "\nJugadores: " + players.Length + "\nMesh colliders: " + colliders.Length + "\nPuertas: " + roots.Sum(r => r.GetComponentsInChildren<TF2SlidingDoor>(true).Length) + "\n";
        foreach (var c in colliders) if (c.sharedMesh == null) report += "ERROR collider vacío: " + PathOf(c.transform) + "\n";
        foreach (var p in players) if (p.groundchek == null || p.groundMask.value == 0) report += "ERROR ground probe/mask: " + p.name + "\n";
        Directory.CreateDirectory(Output);
        File.WriteAllText(Output + "/2Fort-audit.txt", report);
        Debug.Log(report);
    }
}
