using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
// One-shot local authoring request. Never runs without Temp/TF2Build.request.
[InitializeOnLoad]
public static class SolicitudConstruccionMapaTF2
{
    static SolicitudConstruccionMapaTF2() { EditorApplication.update += ActualizarPaso; }
    static void ActualizarPaso()
    {
        const string solicitud = "Temp/TF2Build.request";
        if (!File.Exists(solicitud) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        string comando = File.ReadAllText(solicitud).Trim();
        File.Move(solicitud, "Temp/TF2Build.consumed." + DateTime.UtcNow.Ticks);
        try
        {
            if (string.Equals(comando, "refresh", StringComparison.OrdinalIgnoreCase))
                ConfiguracionMapaTF2.ActualizarEscenaJugable();
            else
                ConfiguracionMapaTF2.Construir();
            File.WriteAllText("Temp/TF2Build.result", "SUCCESS");
        }
        catch (Exception e) { File.WriteAllText("Temp/TF2Build.result", e.ToString()); UnityEngine.Debug.LogException(e); }
    }
}
