using UnityEngine;

/// <summary>Collectible briefcase that is picked up automatically when the player approaches.</summary>
[DisallowMultipleComponent]
public class DosierPickup : MonoBehaviour
{
    [Tooltip("Animator Controller that contains the original @spin animation.")]
    public RuntimeAnimatorController spinController;
    [Tooltip("HUD icon shown when collected. If empty, a simple briefcase icon is generated.")]
    public Sprite hudIcon;
    [TextArea] public string pickupMessage = "Dosier recogido";
    [Min(0f)] public float messageDuration = 4f;
    [Min(0.1f)] public float pickupRadius = 1.5f;
    public Vector3 carriedLocalPosition = new Vector3(0f, 0.85f, 0.65f);

    private bool collected;
    private bool delivered;
    private Transform player;
    private Animator dosierAnimator;
    private Collider pickupCollider;
    private Renderer[] modelRenderers;
    private bool[] originalRendererStates;

    public static DosierPickup CarriedDosier { get; private set; }
    public bool IsCarried => collected && !delivered;
    private void Start()
    {
        pickupCollider = GetComponent<Collider>();
        modelRenderers = GetComponentsInChildren<Renderer>(true);
        originalRendererStates = new bool[modelRenderers.Length];
        for (int i = 0; i < modelRenderers.Length; i++)
            originalRendererStates[i] = modelRenderers[i] != null && modelRenderers[i].enabled;
        // The controller's curves are authored on the imported FBX hierarchy,
        // so prefer the Animator on the model child over one on the pickup wrapper.
        Animator[] animators = GetComponentsInChildren<Animator>(true);
        foreach (Animator candidate in animators)
        {
            if (candidate.transform != transform)
            {
                dosierAnimator = candidate;
                break;
            }
        }
        if (dosierAnimator == null) dosierAnimator = GetComponent<Animator>();
        if (dosierAnimator == null)
        {
            Transform modelRoot = transform.childCount > 0 ? transform.GetChild(0) : transform;
            dosierAnimator = modelRoot.gameObject.AddComponent<Animator>();
        }
        if (dosierAnimator == null)
        {
            Debug.LogWarning($"Dosier '{name}' has no Animator. Assign the DosierAnimatior controller to its model.", this);
            return;
        }
        PlaySpinFromStart();
    }

    private void Update()
    {
        if (collected) return;
        if (CarriedDosier != null) return;
        if (player == null)
        {
            PlayerMove found = FindAnyObjectByType<PlayerMove>();
            if (found == null) return;
            player = found.transform;
        }

        Vector3 offset = player.position - transform.position;
        if (offset.sqrMagnitude > pickupRadius * pickupRadius) return;

        if (CarriedDosier != null) return;
        collected = true;
        CarriedDosier = this;
        if (pickupCollider != null) pickupCollider.enabled = false;
        transform.SetParent(player, false);
        transform.localPosition = carriedLocalPosition;
        transform.localRotation = Quaternion.identity;
        SetModelVisible(false);
        Sprite icon = hudIcon != null ? hudIcon : DosierIcon.Create();
        bool displayedByHud = UiManager.Instance != null && UiManager.Instance.ShowDosierMessage(icon, pickupMessage, messageDuration);
        if (!displayedByHud) DosierPickupNotice.Show(icon, pickupMessage, messageDuration);
    }

    public void DeliverAt(Vector3 worldPosition)
    {
        if (!IsCarried) return;
        delivered = true;
        collected = true;
        transform.SetParent(null, true);
        transform.position = worldPosition;
        if (pickupCollider != null) pickupCollider.enabled = false;
        if (CarriedDosier == this) CarriedDosier = null;
        // Freeze the model in its delivered pose; the spin is only for uncollected dosiers.
        if (dosierAnimator != null) dosierAnimator.enabled = false;
        SetModelVisible(true);
    }

    private void SetModelVisible(bool visible)
    {
        if (modelRenderers == null) return;
        for (int i = 0; i < modelRenderers.Length; i++)
        {
            if (modelRenderers[i] != null)
                modelRenderers[i].enabled = visible && originalRendererStates[i];
        }
    }

    private void PlaySpinFromStart()
    {
        if (dosierAnimator == null) return;
        dosierAnimator.enabled = true;
        if (spinController != null) dosierAnimator.runtimeAnimatorController = spinController;
        dosierAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        dosierAnimator.applyRootMotion = false;
        dosierAnimator.Rebind();
        dosierAnimator.Play("Base Layer.@spin", 0, 0f);
        dosierAnimator.Update(0f);
    }

    private void OnDestroy()
    {
        if (CarriedDosier == this) CarriedDosier = null;
    }
}
