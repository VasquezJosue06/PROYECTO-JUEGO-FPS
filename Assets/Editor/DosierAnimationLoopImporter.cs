using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>Imports the dosier's @spin take as a looping clip and binds it to its controller.</summary>
public sealed class DosierAnimationLoopImporter : AssetPostprocessor
{
    private const string ModelPath = "Assets/dosier/Models/Dosier.fbx";
    private const string ControllerPath = "Assets/dosier/Animations/DosierAnimatior.controller";

    [InitializeOnLoadMethod]
    private static void EnsureLoopingClipIsImported()
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
                if (!IsSpin(clip.name) && !IsSpin(clip.takeName)) continue;
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
                BindImportedSpinClip();
            }
        };
    }

    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
        string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (string path in importedAssets)
            if (string.Equals(path, ModelPath, StringComparison.OrdinalIgnoreCase))
                BindImportedSpinClip();
    }

    private static void BindImportedSpinClip()
    {
        AnimationClip spinClip = null;
        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(ModelPath))
        {
            if (asset is AnimationClip clip && IsSpin(clip.name))
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

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null) return;
        bool changed = false;
        foreach (AnimatorControllerLayer layer in controller.layers)
        {
            foreach (ChildAnimatorState child in layer.stateMachine.states)
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
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
        }
    }

    private static bool IsSpin(string value) =>
        !string.IsNullOrEmpty(value) && value.IndexOf("spin", StringComparison.OrdinalIgnoreCase) >= 0;
}
