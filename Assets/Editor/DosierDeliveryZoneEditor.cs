using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DosierDeliveryZone))]
public sealed class DosierDeliveryZoneEditor : Editor
{
    private SerializedProperty dosiersRequired;
    private SerializedProperty dropPoints;

    private void OnEnable()
    {
        dosiersRequired = serializedObject.FindProperty("dosiersRequired");
        dropPoints = serializedObject.FindProperty("dropPoints");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.LabelField("Dosieres del nivel", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Define cuántos dosieres hay que entregar para ganar. Se crearán exactamente esa cantidad de espacios.", MessageType.Info);
        int requestedCount = EditorGUILayout.IntSlider("Dosieres necesarios", dosiersRequired.intValue, 0, 6);
        dosiersRequired.intValue = requestedCount;
        EditorGUILayout.Space(4f);
        DrawPropertiesExcluding(serializedObject, "m_Script", "dosiersRequired", "dropPoints");

        bool countChanged = serializedObject.ApplyModifiedProperties();
        DosierDeliveryZone zone = (DosierDeliveryZone)target;
        if (zone.dropPoints == null || zone.dropPoints.Length != requestedCount)
            ResizeSlots(zone, requestedCount);

        serializedObject.Update();
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField($"Espacios de entrega ({requestedCount})", EditorStyles.boldLabel);
        if (dropPoints != null)
        {
            for (int i = 0; i < dropPoints.arraySize; i++)
                EditorGUILayout.PropertyField(dropPoints.GetArrayElementAtIndex(i), new GUIContent($"Espacio {i + 1}"));
        }
        if (serializedObject.ApplyModifiedProperties() || countChanged)
        {
            EditorUtility.SetDirty(zone);
            PrefabUtility.RecordPrefabInstancePropertyModifications(zone);
        }
    }

    private static void ResizeSlots(DosierDeliveryZone zone, int count)
    {
        count = Mathf.Clamp(count, 0, 6);
        Transform[] oldSlots = zone.dropPoints ?? Array.Empty<Transform>();
        Transform[] newSlots = new Transform[count];
        int keepCount = Mathf.Min(oldSlots.Length, count);
        Array.Copy(oldSlots, newSlots, keepCount);

        Undo.RecordObject(zone, "Cambiar espacios de entrega del dosier");
        for (int i = count; i < oldSlots.Length; i++)
        {
            Transform oldSlot = oldSlots[i];
            if (oldSlot != null && oldSlot.parent == zone.transform && oldSlot.name.StartsWith("PuntoDosier", StringComparison.Ordinal))
            {
                Undo.RecordObject(oldSlot.gameObject, "Ocultar espacio de entrega sobrante");
                oldSlot.gameObject.SetActive(false);
                PrefabUtility.RecordPrefabInstancePropertyModifications(oldSlot.gameObject);
            }
        }

        List<Transform> childTransforms = new List<Transform>(zone.GetComponentsInChildren<Transform>(true));
        for (int i = 0; i < count; i++)
        {
            if (newSlots[i] == null)
            {
                Transform reusableSlot = FindInactiveSlot(childTransforms, newSlots, zone.transform);
                if (reusableSlot != null)
                {
                    Undo.RecordObject(reusableSlot.gameObject, "Reactivar espacio de entrega");
                    reusableSlot.gameObject.SetActive(true);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(reusableSlot.gameObject);
                    newSlots[i] = reusableSlot;
                }
                else
                {
                    GameObject slot = new GameObject($"PuntoDosier{i + 1}");
                    Undo.RegisterCreatedObjectUndo(slot, "Crear espacio de entrega del dosier");
                    slot.transform.SetParent(zone.transform, false);
                    slot.transform.localPosition = DefaultPosition(i);
                    newSlots[i] = slot.transform;
                    childTransforms.Add(slot.transform);
                }
            }
            else if (newSlots[i].parent == zone.transform)
            {
                newSlots[i].name = $"PuntoDosier{i + 1}";
            }
        }

        zone.dosiersRequired = count;
        zone.dropPoints = newSlots;
        EditorUtility.SetDirty(zone);
        PrefabUtility.RecordPrefabInstancePropertyModifications(zone);
    }

    private static Transform FindInactiveSlot(List<Transform> children, Transform[] assignedSlots, Transform zoneTransform)
    {
        foreach (Transform child in children)
        {
            if (child == null || child.parent != zoneTransform || !child.name.StartsWith("PuntoDosier", StringComparison.Ordinal))
                continue;
            if (child.gameObject.activeSelf) continue;

            bool alreadyAssigned = false;
            foreach (Transform assigned in assignedSlots)
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

    private static Vector3 DefaultPosition(int index)
    {
        int column = index % 3;
        int row = index / 3;
        float x = (column - 1) * 1.2f;
        float z = row == 0 ? -1.2f : 1.2f;
        return new Vector3(x, 0.5f, z);
    }
}
