using UnityEngine;

/// <summary>Invisible trigger that accepts and drops the carried dosier.</summary>
[RequireComponent(typeof(BoxCollider))]
public sealed class DosierDeliveryZone : MonoBehaviour
{
    [Header("Configuración del nivel")]
    [Range(0, 6)]
    [Tooltip("Cantidad de dosieres que hay en el nivel y que se deben entregar para ganar.")]
    public int dosiersRequired = 4;
    [Tooltip("Cada punto es el espacio donde se coloca el siguiente dosier entregado.")]
    public Transform[] dropPoints = new Transform[4];
    [TextArea] public string deliveryMessage = "Dosier entregado";
    [Min(0f)] public float messageDuration = 4f;
    [Min(0.1f)] public float markerSize = 0.75f;
    private int nextDropPoint;
    private bool victoryTriggered;

    private void Start()
    {
        dosiersRequired = Mathf.Clamp(dosiersRequired, 0, 6);
    }

    private void Reset()
    {
        BoxCollider trigger = GetComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(6f, 4f, 6f);
    }

    private void OnTriggerEnter(Collider other) => TryDeliver(other);
    private void OnTriggerStay(Collider other) => TryDeliver(other);

    private void TryDeliver(Collider other)
    {
        if (dosiersRequired < 1) return;
        if (other.GetComponentInParent<PlayerMove>() == null) return;
        DosierPickup dosier = DosierPickup.CarriedDosier;
        if (dosier == null || !dosier.IsCarried) return;
        if (dropPoints == null || nextDropPoint >= dosiersRequired || nextDropPoint >= dropPoints.Length) return;
        Transform point = dropPoints[nextDropPoint];
        if (point == null) return;

        dosier.DeliverAt(point.position);
        nextDropPoint++;
        Sprite icon = DosierIcon.Create();
        bool displayedByHud = UiManager.Instance != null &&
            UiManager.Instance.ShowDosierMessage(icon, deliveryMessage, messageDuration);
        if (!displayedByHud) DosierPickupNotice.Show(icon, deliveryMessage, messageDuration);
        if (nextDropPoint >= dosiersRequired) CompleteLevel();
    }

    private void CompleteLevel()
    {
        if (dosiersRequired < 1 || victoryTriggered || nextDropPoint < dosiersRequired) return;
        victoryTriggered = true;
        if (UiManager.Instance != null) UiManager.Instance.EnableVictoryUi();
        else Debug.LogWarning("No se encontró UiManager para mostrar la pantalla de victoria.", this);
    }

    private void OnDrawGizmosSelected()
    {
        if (dropPoints == null) return;
        Gizmos.color = new Color(0.1f, 0.85f, 1f, 0.9f);
        Vector3 size = Vector3.one * markerSize;
        foreach (Transform point in dropPoints)
            if (point != null) Gizmos.DrawWireCube(point.position, size);
    }
}
