using UnityEngine;
using System.Collections.Generic;
[DisallowMultipleComponent]
public sealed class TF2SlidingDoor : MonoBehaviour
{
    public Transform panel;
    public Vector3 localTravel = Vector3.up * 2.5f;
    public float speed = 2f, closeDelay = 1f;
    Vector3 closed;
    float lastOccupied = float.NegativeInfinity;
    readonly HashSet<Collider> occupants = new HashSet<Collider>();
    void Awake() { if (panel != null) closed = panel.localPosition; }
    void OnTriggerEnter(Collider c) { if (c.GetComponentInParent<PlayerMove>() != null) occupants.Add(c); }
    void OnTriggerExit(Collider c) { occupants.Remove(c); }
    void FixedUpdate()
    {
        if (panel == null) return;
        occupants.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);
        if (occupants.Count > 0) lastOccupied = Time.time;
        bool open = occupants.Count > 0 || Time.time - lastOccupied < closeDelay;
        panel.localPosition = Vector3.MoveTowards(panel.localPosition, closed + (open ? localTravel : Vector3.zero), Mathf.Max(0, speed) * Time.fixedDeltaTime);
    }
}
