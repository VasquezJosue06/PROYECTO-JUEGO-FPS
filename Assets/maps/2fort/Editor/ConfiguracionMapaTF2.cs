using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
public static class ConfiguracionMapaTF2
{
    const string Output = "Assets/maps/2fort";
    const string ScenePath = "Assets/maps/2fort/Scenes/2Fort Playable.unity";
    [MenuItem("Tools/TF2/2Fort/Create Playable Scene")]
    public static void Crear()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (File.Exists(ScenePath)) { EditorSceneManager.OpenScene(ScenePath); return; }
        Construir();
    }
    public static void Construir()
    {
        if (File.Exists(ScenePath)) throw new InvalidOperationException("La escena ya existe; no se sobrescribe.");
        var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/maps/2fort/ctf_2fort_Unity_ExportTest.fbx");
        if (model == null) throw new InvalidOperationException("Falta FBX 2Fort.");
        Directory.CreateDirectory(Output + "/Collision");
        Directory.CreateDirectory(Output + "/Prefabs");
        Directory.CreateDirectory(Output + "/Materials/Gameplay");
        Directory.CreateDirectory(Output + "/Scenes");
        AssetDatabase.Refresh();
        var escena = EditorSceneManager.OpenScene("Assets/Scenes/Escena de prueba.unity");
        var jugador = escena.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MovimientoJugador>(true)).FirstOrDefault();
        if (jugador == null) throw new InvalidOperationException("Falta jugador en Escena de prueba.");
        foreach (var root in escena.GetRootGameObjects())
            if (root != jugador.transform.root.gameObject && root.GetComponentInChildren<GestorInterfaz>(true) == null && root.GetComponentInChildren<GestorAudio>(true) == null && root.GetComponentInChildren<UnityEngine.EventSystems.EventSystem>(true) == null)
                UnityEngine.Object.DestroyImmediate(root);
        var map = (GameObject)PrefabUtility.InstantiatePrefab(model, escena);
        map.name = "2Fort_Geometry";
        map.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        int capa = LayerMask.NameToLayer("Ground");
        if (capa < 0) throw new InvalidOperationException("Falta capa Ground.");
        int cantidad = RebuildColliders(map, capa);
        Physics.SyncTransforms();
        var puntoReaparicion = new GameObject("Spawn_Exploration");
        puntoReaparicion.transform.SetParent(new GameObject("2Fort_Gameplay").transform);
        puntoReaparicion.transform.position = FindSpawn(map, jugador, capa);
        jugador.transform.position = puntoReaparicion.transform.position;
        jugador.mascaraSuelo = 1 << capa;
        var selector = jugador.GetComponent<ClasesExploracionTF2>();
        if (selector == null) selector = jugador.gameObject.AddComponent<ClasesExploracionTF2>();
        selector.claseScout = AssetDatabase.LoadAssetAtPath<ClasesJugador>("Assets/Classes/Scout.asset");
        selector.claseHeavy = AssetDatabase.LoadAssetAtPath<ClasesJugador>("Assets/Classes/Heavy.asset");
        jugador.GetComponent<Rigidbody>().constraints |= RigidbodyConstraints.FreezeRotation;
        var respawn = jugador.gameObject.AddComponent<ReaparicionMapaTF2>();
        respawn.puntoReaparicion = puntoReaparicion.transform;
        respawn.alturaMinima = map.GetComponentsInChildren<Renderer>().Min(r => r.bounds.min.y) - 15;
        Lighting();
        Specials(map);
        ForceBackfaceCulling(map);
        HideSourceHelpers(map);
        PrefabUtility.SaveAsPrefabAsset(map, Output + "/Prefabs/2Fort_Map.prefab");
        PrefabUtility.SaveAsPrefabAsset(jugador.transform.root.gameObject, Output + "/Prefabs/2Fort_Player.prefab");
        EditorSceneManager.SaveScene(escena, ScenePath);
        AssetDatabase.SaveAssets();
        Revisar(escena);
        Debug.Log("2Fort creada con " + cantidad + " colliders. Revisar spawn, puertas, agua y rendimiento en Play.");
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

    static int RebuildColliders(GameObject map, int capa)
    {
        foreach (var collider in map.GetComponentsInChildren<MeshCollider>(true))
            UnityEngine.Object.DestroyImmediate(collider);

        int cantidad = 0;
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
                string ruta = Output + "/Collision/Solid_" + cantidad + ".asset";
                var existing = AssetDatabase.LoadAssetAtPath<Mesh>(ruta);
                if (existing != null)
                {
                    EditorUtility.CopySerialized(clone, existing);
                    UnityEngine.Object.DestroyImmediate(clone);
                    mesh = existing;
                }
                else
                {
                    AssetDatabase.CreateAsset(clone, ruta);
                    mesh = clone;
                }
            }
            var collider = filter.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            collider.convex = false;
            filter.gameObject.layer = capa;
            cantidad++;
        }
        if (cantidad == 0) throw new InvalidOperationException("No se encontró geometría sólida para la colisión.");
        return cantidad;
    }
    static Vector3 FindSpawn(GameObject map, MovimientoJugador jugador, int capa)
    {
        var renderers = map.GetComponentsInChildren<Renderer>();
        var bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        var colisionador = jugador.GetComponent<CapsuleCollider>();
        if (colisionador == null || colisionador.direction != 1) throw new InvalidOperationException("Falta cápsula vertical del jugador.");
        float radius = colisionador.radius * Mathf.Max(Mathf.Abs(jugador.transform.lossyScale.x), Mathf.Abs(jugador.transform.lossyScale.z));
        float half = Mathf.Max(radius, colisionador.height * Mathf.Abs(jugador.transform.lossyScale.y) * 0.5f);
        Vector3 centerOffset = jugador.transform.TransformVector(colisionador.center);
        for (int ring = 0; ring < 40; ring++) for (int step = 0; step < 16; step++)
        {
            float angle = step * Mathf.PI / 8;
            var origin = new Vector3(bounds.center.x + Mathf.Cos(angle) * ring * 2, bounds.max.y + 5, bounds.center.z + Mathf.Sin(angle) * ring * 2);
            foreach (var impacto in Physics.RaycastAll(origin, Vector3.down, bounds.size.y + 10, 1 << capa, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance))
            {
                if (impacto.normal.y < 0.85f) continue;
                Vector3 center = impacto.point + Vector3.up * (half + 0.08f), offset = Vector3.up * (half - radius);
                if (!Physics.CheckCapsule(center - offset, center + offset, radius, 1 << capa, QueryTriggerInteraction.Ignore)) return center - centerOffset;
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
                    string ruta = Output + "/Materials/Gameplay/" + original.name + ".mat";
                    material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
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
                            float valor = match.Success ? int.Parse(match.Groups[1].Value) / 100f : 0.3f;
                            material.SetColor("_BaseColor", new Color(valor, valor, valor));
                            material.SetFloat("_Smoothness", 0);
                        }
                        AssetDatabase.CreateAsset(material, ruta);
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
            string nombre = material.name.ToLowerInvariant();
            if (nombre.StartsWith("water_") || nombre.Contains("toolsskybox")) continue;
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
    public static void ActualizarEscenaJugable()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene previousActive = SceneManager.GetActiveScene();
        Scene escena = SceneManager.GetSceneByPath(ScenePath);
        bool openedForRefresh = !escena.IsValid() || !escena.isLoaded;
        if (openedForRefresh) escena = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        SceneManager.SetActiveScene(escena);
        try
        {
        var map = escena.GetRootGameObjects().FirstOrDefault(root => root.name == "2Fort_Geometry");
        if (map == null) throw new InvalidOperationException("Falta 2Fort_Geometry en la escena jugable.");
        int capa = LayerMask.NameToLayer("Ground");
        if (capa < 0) throw new InvalidOperationException("Falta capa Ground.");
        int cantidad = RebuildColliders(map, capa);
        Lighting();
        Specials(map);
        ForceBackfaceCulling(map);
        HideSourceHelpers(map);
        foreach (var jugador in escena.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MovimientoJugador>(true)))
        {
            var cuerpoRigido = jugador.GetComponent<Rigidbody>();
            if (cuerpoRigido != null)
            {
                cuerpoRigido.constraints |= RigidbodyConstraints.FreezeRotation;
                cuerpoRigido.interpolation = RigidbodyInterpolation.Interpolate;
                cuerpoRigido.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
        }
        PrefabUtility.SaveAsPrefabAsset(map, Output + "/Prefabs/2Fort_Map.prefab");
        EditorSceneManager.SaveScene(escena, ScenePath);
        AssetDatabase.SaveAssets();
        Revisar(escena);
        Debug.Log("2Fort actualizada con " + cantidad + " colliders sólidos.");
        }
        finally
        {
            if (previousActive.IsValid() && previousActive.isLoaded) SceneManager.SetActiveScene(previousActive);
            if (openedForRefresh) EditorSceneManager.CloseScene(escena, true);
        }
    }
    [MenuItem("Tools/TF2/2Fort/Add Sliding Door To Selected Panel")]
    public static void Puerta()
    {
        var panel = Selection.activeGameObject;
        if (panel == null || EditorUtility.IsPersistent(panel) || panel.GetComponentInParent<PuertaCorredizaTF2>() != null) return;
        var prefabricado = PrefabUtility.GetOutermostPrefabInstanceRoot(panel);
        if (prefabricado != null) PrefabUtility.UnpackPrefabInstance(prefabricado, PrefabUnpackMode.Completely, InteractionMode.UserAction);
        Undo.IncrementCurrentGroup();
        var root = new GameObject(panel.name + "_Trigger");
        Undo.RegisterCreatedObjectUndo(root, "Crear puerta");
        root.transform.SetParent(panel.transform.parent, false);
        root.transform.SetPositionAndRotation(panel.transform.position, panel.transform.rotation);
        Undo.SetTransformParent(panel.transform, root.transform, "Mover panel");
        var door = Undo.AddComponent<PuertaCorredizaTF2>(root);
        door.panel = panel.transform;
        var activador = Undo.AddComponent<BoxCollider>(root);
        activador.isTrigger = true;
        var renderers = panel.GetComponentsInChildren<Renderer>();
        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
        foreach (var renderer in renderers)
        {
            Bounds b = renderer.bounds;
            for (int i = 0; i < 8; i++) bounds.Encapsulate(root.transform.InverseTransformPoint(b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1))));
        }
        activador.center = bounds.center;
        activador.size = bounds.size + Vector3.one * 2;
        door.recorridoLocal = Vector3.up * (bounds.size.y + 0.1f);
        foreach (var t in panel.GetComponentsInChildren<Transform>()) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(root.scene);
    }
    [MenuItem("Tools/TF2/2Fort/Audit Active Scene")]
    public static void Revisar()
    {
        Revisar(SceneManager.GetActiveScene());
    }

    static void Revisar(Scene escena)
    {
        var raices = escena.GetRootGameObjects();
        var colisionadores = raices.SelectMany(r => r.GetComponentsInChildren<MeshCollider>(true)).ToArray();
        var jugadores = raices.SelectMany(r => r.GetComponentsInChildren<MovimientoJugador>(true)).ToArray();
        string informe = "Escena: " + escena.path + "\nJugadores: " + jugadores.Length + "\nMesh colliders: " + colisionadores.Length + "\nPuertas: " + raices.Sum(r => r.GetComponentsInChildren<PuertaCorredizaTF2>(true).Length) + "\n";
        foreach (var colisionador in colisionadores) if (colisionador.sharedMesh == null) informe += "ERROR collider vacío: " + PathOf(colisionador.transform) + "\n";
        foreach (var p in jugadores) if (p.puntoChequeoSuelo == null || p.mascaraSuelo.value == 0) informe += "ERROR ground probe/mask: " + p.name + "\n";
        Directory.CreateDirectory(Output);
        File.WriteAllText(Output + "/2Fort-audit.txt", informe);
        Debug.Log(informe);
    }
}
