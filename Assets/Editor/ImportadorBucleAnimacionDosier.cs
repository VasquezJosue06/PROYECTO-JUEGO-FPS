using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>Imports the dosier's @spin take as a looping clip and binds it to its controller.</summary>
public sealed class ImportadorBucleAnimacionDosier : AssetPostprocessor
{
    private const string ModelPath = "Assets/dosier/Models/Dosier.fbx";
    private const string ControllerPath = "Assets/dosier/Animations/DosierAnimatior.controller";

    [InitializeOnLoadMethod]
    private static void AsegurarImportacionClipBucle()
    {
        EditorApplication.delayCall += () =>
        {
            ModelImporter importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer == null) return;

            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            bool useDefaults = clips == null || clips.Length == 0;
            if (useDefaults) clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length == 0) return;

            bool foundSpin = false;
            bool changed = useDefaults;
            foreach (ModelImporterClipAnimation clip in clips)
            {
                if (!EsClipDeGiro(clip.name) && !EsClipDeGiro(clip.takeName)) continue;
                foundSpin = true;
                if (!clip.loopTime || !clip.loopPose || clip.wrapMode != WrapMode.Loop) changed = true;
                clip.loopTime = true;
                clip.loopPose = true;
                clip.wrapMode = WrapMode.Loop;
            }

            if (!foundSpin)
            {
                Debug.LogError("No se encontró el take @spin en Dosier.fbx. Revisa la pestaña Animations del modelo.");
                return;
            }

            if (changed)
            {
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
            }
            else
            {
                VincularClipGiroImportado();
            }
        };
    }

    private static void OnPostprocessAllAssets(string[] recursosImportados, string[] recursosEliminados,
        string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (string ruta in recursosImportados)
            if (string.Equals(ruta, ModelPath, StringComparison.OrdinalIgnoreCase))
                VincularClipGiroImportado();
    }

    private static void VincularClipGiroImportado()
    {
        AnimationClip spinClip = null;
        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(ModelPath))
        {
            if (asset is AnimationClip clip && EsClipDeGiro(clip.name))
            {
                spinClip = clip;
                if (string.Equals(clip.name, "@spin", StringComparison.OrdinalIgnoreCase)) break;
            }
        }
        if (spinClip == null)
        {
            Debug.LogError("Unity no generó un AnimationClip @spin desde Dosier.fbx.");
            return;
        }

        AnimatorController controladorClase = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controladorClase == null) return;
        bool changed = false;
        foreach (AnimatorControllerLayer capa in controladorClase.layers)
        {
            foreach (ChildAnimatorState child in capa.stateMachine.states)
            {
                if (!string.Equals(child.state.name, "@spin", StringComparison.OrdinalIgnoreCase)) continue;
                if (child.state.motion != spinClip)
                {
                    child.state.motion = spinClip;
                    changed = true;
                }
            }
        }
        if (changed)
        {
            EditorUtility.SetDirty(controladorClase);
            AssetDatabase.SaveAssets();
        }
    }

    private static bool EsClipDeGiro(string valor) =>
        !string.IsNullOrEmpty(valor) && valor.IndexOf("spin", StringComparison.OrdinalIgnoreCase) >= 0;
}
