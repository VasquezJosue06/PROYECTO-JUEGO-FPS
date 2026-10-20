using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
// One-shot local authoring request. Never runs without Temp/TF2Build.request.
[InitializeOnLoad]
public static class TF2MapBuildRequest
{
    static TF2MapBuildRequest() { EditorApplication.update += Tick; }
    static void Tick()
    {
        const string request = "Temp/TF2Build.request";
        if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        string command = File.ReadAllText(request).Trim();
        File.Move(request, "Temp/TF2Build.consumed." + DateTime.UtcNow.Ticks);
        try
        {
            if (string.Equals(command, "refresh", StringComparison.OrdinalIgnoreCase))
                TF2MapSetup.RefreshPlayableScene();
            else
                TF2MapSetup.Build();
            File.WriteAllText("Temp/TF2Build.result", "SUCCESS");
        }
        catch (Exception e) { File.WriteAllText("Temp/TF2Build.result", e.ToString()); UnityEngine.Debug.LogException(e); }
    }
}
