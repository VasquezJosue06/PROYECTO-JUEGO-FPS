using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DosierRecogida))]
public sealed class EditorDosierRecogida : Editor
{
    private static readonly (string etiqueta, string ruta)[] OpcionesMateriales =
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

        foreach (var option in OpcionesMateriales)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(option.ruta);
            if (material == null)
            {
                EditorGUILayout.HelpBox($"No se encontró el material: {option.ruta}", MessageType.Warning);
                continue;
            }

            if (GUILayout.Button(option.etiqueta, GUILayout.Height(28f)))
                AplicarMaterial(material);
        }
    }

    private void AplicarMaterial(Material material)
    {
        DosierRecogida pickup = (DosierRecogida)target;
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
