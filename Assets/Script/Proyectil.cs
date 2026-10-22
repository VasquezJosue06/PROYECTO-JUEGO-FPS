using UnityEngine;

public class Proyectil : MonoBehaviour
{
    [SerializeField] private float velocidad = 15f;
    [SerializeField] private float duracion = 4f;

    private Rigidbody cuerpoRigido;
    void Start()
    {
        cuerpoRigido = GetComponent<Rigidbody>();
        cuerpoRigido.linearVelocity = -transform.right * velocidad;
        Destroy(gameObject, duracion);
    }

    void OnCollisionEnter(Collision collision)
    {
        Destroy(gameObject);
    }
}
