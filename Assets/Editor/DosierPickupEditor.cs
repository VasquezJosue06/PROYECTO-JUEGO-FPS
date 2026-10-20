using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DosierPickup))]
public sealed class DosierPickupEditor : Editor
{
    private static readonly (string label, string path)[] MaterialOptions =
    {
        ("Rojo", "Assets/dosier/Materials/Dosier_Mat.mat"),
        ("Azul", "Assets/dosier/Materials/DosierBlue_Mat.mat"),
        ("Gris", "Assets/dosier/Materials/DosierNtr_Mat.mat")
    };

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("Material del dosier", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Selecciona una opción para aplicarla a todos los renderers del modelo.", MessageType.Info);

        foreach (var option in MaterialOptions)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(option.path);
            if (material == null)
            {
                EditorGUILayout.HelpBox($"No se encontró el material: {option.path}", MessageType.Warning);
                continue;
            }

            if (GUILayout.Button(option.label, GUILayout.Height(28f)))
                ApplyMaterial(material);
        }
    }

    private void ApplyMaterial(Material material)
    {
        DosierPickup pickup = (DosierPickup)target;
        Renderer[] renderers = pickup.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            EditorUtility.DisplayDialog("Dosier sin modelo", "Agrega Dosier.fbx como hijo del objeto Dosier y vuelve a elegir el material.", "Aceptar");
            return;
        }

        Undo.RecordObjects(renderers, "Cambiar material del dosier");
        foreach (Renderer renderer in renderers)
        {
            Material[] slots = renderer.sharedMaterials;
            if (slots.Length == 0) slots = new Material[1];
            for (int i = 0; i < slots.Length; i++) slots[i] = material;
            renderer.sharedMaterials = slots;
            EditorUtility.SetDirty(renderer);
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        }
    }
}
