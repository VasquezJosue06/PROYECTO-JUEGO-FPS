using UnityEngine;

/// <summary>On-screen pickup message fallback for scenes without a configured UiManager.</summary>
public sealed class DosierPickupNotice : MonoBehaviour
{
    private static DosierPickupNotice instance;
    private string message;
    private Sprite icon;
    private float hideAt;

    public static void Show(Sprite pickupIcon, string pickupMessage, float duration)
    {
        if (instance == null)
        {
            var host = new GameObject("DosierPickupNotice");
            DontDestroyOnLoad(host);
            instance = host.AddComponent<DosierPickupNotice>();
        }
        instance.icon = pickupIcon;
        instance.message = pickupMessage;
        instance.hideAt = Time.unscaledTime + Mathf.Max(0.1f, duration);
    }

    private void OnGUI()
    {
        if (string.IsNullOrEmpty(message) || Time.unscaledTime >= hideAt) return;

        const float width = 380f;
        const float height = 86f;
        Rect panel = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.78f, width, height);
        GUI.Box(panel, GUIContent.none);
        if (icon != null)
            GUI.DrawTexture(new Rect(panel.x + 14f, panel.y + 13f, 60f, 60f), icon.texture, ScaleMode.ScaleToFit, true);

        var style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            wordWrap = true
        };
        style.normal.textColor = Color.white;
        float left = icon != null ? 88f : 20f;
        GUI.Label(new Rect(panel.x + left, panel.y + 8f, width - left - 14f, height - 16f), message, style);
    }
}
