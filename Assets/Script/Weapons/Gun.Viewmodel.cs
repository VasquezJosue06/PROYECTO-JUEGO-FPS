using UnityEngine;
using UnityEngine.Rendering.Universal;

// Camara del arma y restauracion de capas al guardarla.
// Es parte del mismo componente Gun; no se agrega como otro componente.
public partial class Gun
{
    private void SetupViewmodelCamera()
    {
        if (!UsesAnimations || !separateViewmodelCamera || aimCamera == null || cameraAttached)
            return;
        int layer = LayerMask.NameToLayer("Viewmodel");
        if (layer < 0)
        {
            Debug.LogError("Create the Viewmodel layer before enabling the weapon camera.", this);
            return;
        }
        var stack = aimCamera.GetUniversalAdditionalCameraData().cameraStack;
        if (stack == null) return;

        if (viewmodelCamera == null)
        {
            GameObject cameraObject = new GameObject("Viewmodel Camera");
            cameraObject.transform.SetParent(aimCamera.transform, false);
            viewmodelCamera = cameraObject.AddComponent<Camera>();
            viewmodelCamera.fieldOfView = viewmodelFieldOfView;
            viewmodelCamera.nearClipPlane = 0.001f;
            viewmodelCamera.farClipPlane = 20f;
            viewmodelCamera.cullingMask = 1 << layer;
            viewmodelCamera.useOcclusionCulling = false;
            viewmodelCamera.allowHDR = aimCamera.allowHDR;
            viewmodelCamera.allowMSAA = aimCamera.allowMSAA;
            var overlay = viewmodelCamera.GetUniversalAdditionalCameraData();
            overlay.renderType = CameraRenderType.Overlay;
            overlay.renderPostProcessing = false;
            overlay.renderShadows = false;
        }
        viewmodelCamera.enabled = true;
        mainCameraIncludedViewmodel = (aimCamera.cullingMask & (1 << layer)) != 0;
        aimCamera.cullingMask &= ~(1 << layer);
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            originalLayers[child.gameObject] = child.gameObject.layer;
            child.gameObject.layer = layer;
        }
        if (!stack.Contains(viewmodelCamera)) stack.Add(viewmodelCamera);
        cameraAttached = true;
    }

    private void RemoveViewmodelCamera()
    {
        if (viewmodelCamera == null || !cameraAttached) return;
        if (aimCamera != null)
        {
            aimCamera.GetUniversalAdditionalCameraData().cameraStack?.Remove(viewmodelCamera);
            int layer = LayerMask.NameToLayer("Viewmodel");
            if (layer >= 0 && mainCameraIncludedViewmodel) aimCamera.cullingMask |= 1 << layer;
        }
        foreach (var entry in originalLayers)
            if (entry.Key != null) entry.Key.layer = entry.Value;
        originalLayers.Clear();
        viewmodelCamera.enabled = false;
        cameraAttached = false;
        Destroy(viewmodelCamera.gameObject);
        viewmodelCamera = null;
    }
}
