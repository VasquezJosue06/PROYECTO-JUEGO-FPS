using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PuntoExtraccion))]
public sealed class EditorDosierZonaEntrega : Editor
{
    private SerializedProperty enemigosRequeridos;
    private SerializedProperty dosieresRequeridos;
    private SerializedProperty puntosEntrega;

    private void OnEnable()
    {
        enemigosRequeridos = serializedObject.FindProperty("enemigosRequeridos");
        dosieresRequeridos = serializedObject.FindProperty("dosieresRequeridos");
        puntosEntrega = serializedObject.FindProperty("puntosEntrega");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.LabelField("Punto de extracción", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Configura los objetivos de la misión. Se crearán espacios para colocar cada dosier entregado.", MessageType.Info);
        enemigosRequeridos.intValue = EditorGUILayout.IntSlider("Enemigos a eliminar", enemigosRequeridos.intValue, 0, 50);
        int requestedCount = EditorGUILayout.IntSlider("Dosieres necesarios", dosieresRequeridos.intValue, 0, 6);
        dosieresRequeridos.intValue = requestedCount;
        EditorGUILayout.Space(4f);
        DrawPropertiesExcluding(serializedObject, "m_Script", "enemigosRequeridos", "dosieresRequeridos", "puntosEntrega");

        bool countChanged = serializedObject.ApplyModifiedProperties();
        PuntoExtraccion zone = (PuntoExtraccion)target;
        if (zone.puntosEntrega == null || zone.puntosEntrega.Length != requestedCount)
            AjustarEspacios(zone, requestedCount);

        serializedObject.Update();
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField($"Espacios del punto de extracción ({requestedCount})", EditorStyles.boldLabel);
        if (puntosEntrega != null)
        {
            for (int i = 0; i < puntosEntrega.arraySize; i++)
                EditorGUILayout.PropertyField(puntosEntrega.GetArrayElementAtIndex(i), new GUIContent($"Posición de dosier {i + 1}"));
        }
        bool slotsChanged = serializedObject.ApplyModifiedProperties();
        if (countChanged || slotsChanged)
        {
            EditorUtility.SetDirty(zone);
            PrefabUtility.RecordPrefabInstancePropertyModifications(zone);
        }

        if (countChanged && Application.isPlaying && GestorInterfaz.Instancia != null)
            GestorInterfaz.Instancia.ConfigurarObjetivos(zone.enemigosRequeridos, zone.dosieresRequeridos);
    }

    private static void AjustarEspacios(PuntoExtraccion zone, int cantidad)
    {
        cantidad = Mathf.Clamp(cantidad, 0, 6);
        Transform[] oldSlots = zone.puntosEntrega ?? Array.Empty<Transform>();
        Transform[] newSlots = new Transform[cantidad];
        int keepCount = Mathf.Min(oldSlots.Length, cantidad);
        Array.Copy(oldSlots, newSlots, keepCount);

        Undo.RecordObject(zone, "Cambiar posiciones de dosier del punto de extracción");
        for (int i = cantidad; i < oldSlots.Length; i++)
        {
            Transform oldSlot = oldSlots[i];
            if (oldSlot != null && oldSlot.parent == zone.transform && oldSlot.name.StartsWith("PuntoDosier", StringComparison.Ordinal))
            {
                Undo.RecordObject(oldSlot.gameObject, "Ocultar posición de dosier sobrante");
                oldSlot.gameObject.SetActive(false);
                PrefabUtility.RecordPrefabInstancePropertyModifications(oldSlot.gameObject);
            }
        }

        List<Transform> childTransforms = new List<Transform>(zone.GetComponentsInChildren<Transform>(true));
        for (int i = 0; i < cantidad; i++)
        {
            if (newSlots[i] == null)
            {
                Transform reusableSlot = BuscarEspacioInactivo(childTransforms, newSlots, zone.transform);
                if (reusableSlot != null)
                {
                    Undo.RecordObject(reusableSlot.gameObject, "Reactivar posición de dosier");
                    reusableSlot.gameObject.SetActive(true);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(reusableSlot.gameObject);
                    newSlots[i] = reusableSlot;
                }
                else
                {
                    GameObject posicionArreglo = new GameObject($"PuntoDosier{i + 1}");
                    Undo.RegisterCreatedObjectUndo(posicionArreglo, "Crear posición de dosier del punto de extracción");
                    posicionArreglo.transform.SetParent(zone.transform, false);
                    posicionArreglo.transform.localPosition = PosicionPredeterminada(i);
                    newSlots[i] = posicionArreglo.transform;
                    childTransforms.Add(posicionArreglo.transform);
                }
            }
            else if (newSlots[i].parent == zone.transform)
            {
                newSlots[i].name = $"PuntoDosier{i + 1}";
            }
        }

        zone.dosieresRequeridos = cantidad;
        zone.puntosEntrega = newSlots;
        EditorUtility.SetDirty(zone);
        PrefabUtility.RecordPrefabInstancePropertyModifications(zone);
    }

    private static Transform BuscarEspacioInactivo(List<Transform> hijos, Transform[] espaciosAsignados, Transform transformacionZona)
    {
        foreach (Transform child in hijos)
        {
            if (child == null || child.parent != transformacionZona || !child.name.StartsWith("PuntoDosier", StringComparison.Ordinal))
                continue;
            if (child.gameObject.activeSelf) continue;

            bool alreadyAssigned = false;
            foreach (Transform assigned in espaciosAsignados)
            {
                if (assigned == child)
                {
                    alreadyAssigned = true;
                    break;
                }
            }
            if (!alreadyAssigned) return child;
        }
        return null;
    }

    private static Vector3 PosicionPredeterminada(int indice)
    {
        int column = indice % 3;
        int row = indice / 3;
        float x = (column - 1) * 1.2f;
        float z = row == 0 ? -1.2f : 1.2f;
        return new Vector3(x, 0.5f, z);
    }
}
