using UnityEngine;
using System.Collections.Generic;
[DisallowMultipleComponent]
public sealed class PuertaCorredizaTF2 : MonoBehaviour
{
    public Transform panel;
    public Vector3 recorridoLocal = Vector3.up * 2.5f;
    [SerializeField] private float velocidad = 2f, demoraCierre = 1f;
    Vector3 posicionCerrada;
    float ultimaOcupacion = float.NegativeInfinity;
    readonly HashSet<Collider> ocupantes = new HashSet<Collider>();
    void Awake() { if (panel != null) posicionCerrada = panel.localPosition; }
    void OnTriggerEnter(Collider colisionador) { if (colisionador.GetComponentInParent<MovimientoJugador>() != null) ocupantes.Add(colisionador); }
    void OnTriggerExit(Collider colisionador) { ocupantes.Remove(colisionador); }
    void FixedUpdate()
    {
        if (panel == null) return;
        ocupantes.RemoveWhere(colisionador => colisionador == null || !colisionador.enabled || !colisionador.gameObject.activeInHierarchy);
        if (ocupantes.Count > 0) ultimaOcupacion = Time.time;
        bool abierta = ocupantes.Count > 0 || Time.time - ultimaOcupacion < demoraCierre;
        panel.localPosition = Vector3.MoveTowards(panel.localPosition, posicionCerrada + (abierta ? recorridoLocal : Vector3.zero), Mathf.Max(0, velocidad) * Time.fixedDeltaTime);
    }
}
