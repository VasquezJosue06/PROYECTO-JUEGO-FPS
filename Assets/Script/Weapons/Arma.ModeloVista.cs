using UnityEngine;
using UnityEngine.Rendering.Universal;

// Camara del arma y restauracion de capas al guardarla.
// Es parte del mismo componente Gun; no se agrega como otro componente.
public partial class Arma
{
    private void ConfigurarCamaraArma()
    {
        if (!UsaAnimaciones || !camaraArmaSeparada || camaraApuntado == null || camaraAdjunta)
            return;
        int capa = LayerMask.NameToLayer("Viewmodel");
        if (capa < 0)
        {
            Debug.LogError("Create the Viewmodel layer before enabling the weapon camera.", this);
            return;
        }
        var stack = camaraApuntado.GetUniversalAdditionalCameraData().cameraStack;
        if (stack == null) return;

        if (camaraArma == null)
        {
            GameObject cameraObject = new GameObject("Viewmodel Camera");
            cameraObject.transform.SetParent(camaraApuntado.transform, false);
            camaraArma = cameraObject.AddComponent<Camera>();
            camaraArma.fieldOfView = campoVisionArma;
            camaraArma.nearClipPlane = 0.001f;
            camaraArma.farClipPlane = 20f;
            camaraArma.cullingMask = 1 << capa;
            camaraArma.useOcclusionCulling = false;
            camaraArma.allowHDR = camaraApuntado.allowHDR;
            camaraArma.allowMSAA = camaraApuntado.allowMSAA;
            var overlay = camaraArma.GetUniversalAdditionalCameraData();
            overlay.renderType = CameraRenderType.Overlay;
            overlay.renderPostProcessing = false;
            overlay.renderShadows = false;
        }
        camaraArma.enabled = true;
        camaraPrincipalIncluyeArma = (camaraApuntado.cullingMask & (1 << capa)) != 0;
        camaraApuntado.cullingMask &= ~(1 << capa);
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            capasOriginales[child.gameObject] = child.gameObject.layer;
            child.gameObject.layer = capa;
        }
        if (!stack.Contains(camaraArma)) stack.Add(camaraArma);
        camaraAdjunta = true;
    }

    private void QuitarCamaraArma()
    {
        if (camaraArma == null || !camaraAdjunta) return;
        if (camaraApuntado != null)
        {
            camaraApuntado.GetUniversalAdditionalCameraData().cameraStack?.Remove(camaraArma);
            int capa = LayerMask.NameToLayer("Viewmodel");
            if (capa >= 0 && camaraPrincipalIncluyeArma) camaraApuntado.cullingMask |= 1 << capa;
        }
        foreach (var entry in capasOriginales)
            if (entry.Key != null) entry.Key.layer = entry.Value;
        capasOriginales.Clear();
        camaraArma.enabled = false;
        camaraAdjunta = false;
        Destroy(camaraArma.gameObject);
        camaraArma = null;
    }
}
