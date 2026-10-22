using UnityEngine;
[RequireComponent(typeof(Rigidbody))]
public sealed class ReaparicionMapaTF2 : MonoBehaviour
{
    public Transform puntoReaparicion;
    public float alturaMinima = -30;
    void FixedUpdate()
    {
        if (puntoReaparicion == null || transform.position.y >= alturaMinima) return;
        var cuerpoRigido = GetComponent<Rigidbody>();
        cuerpoRigido.position = puntoReaparicion.position;
        cuerpoRigido.rotation = puntoReaparicion.rotation;
        cuerpoRigido.linearVelocity = Vector3.zero;
        cuerpoRigido.angularVelocity = Vector3.zero;
    }
}
