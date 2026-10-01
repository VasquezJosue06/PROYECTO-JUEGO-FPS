using UnityEngine;
using UnityEngine.Rendering;

// El prefab define el aspecto. El pool controla duracion y reutilizacion.
[DisallowMultipleComponent]
public class WeaponTracerVisual : MonoBehaviour
{
    public Material material;
    public Color color = new Color(1f, 0.72f, 0.25f, 1f);

    [Tooltip("Velocidad visual en unidades por segundo; no cambia el instante del daño.")]
    [Min(1f)] public float travelSpeed = 160f;
    [Tooltip("Longitud maxima de la cola, nunca de todo el disparo.")]
    [Min(0.01f)] public float tailLength = 1.4f;

    public LineRenderer Initialize()
    {
        var line = GetComponent<LineRenderer>();
        if (line == null) line = gameObject.AddComponent<LineRenderer>();
        line.sharedMaterial = material;
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.textureMode = LineTextureMode.Stretch;
        line.numCapVertices = 3;
        line.alignment = LineAlignment.View;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.lightProbeUsage = LightProbeUsage.Off;
        line.reflectionProbeUsage = ReflectionProbeUsage.Off;
        line.startColor = color;
        line.endColor = color;
        return line;
    }
}
