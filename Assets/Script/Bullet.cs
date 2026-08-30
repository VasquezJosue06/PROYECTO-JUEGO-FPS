using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Movimiento")]
    public float speed = 15f;
    public float lifeTime = 4f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // El prefab está orientado con su eje derecho inverso hacia delante.
        rb.linearVelocity = -transform.right * speed;

        // Limpieza de seguridad si no golpea ningún objeto.
        Destroy(gameObject, lifeTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        // El daño lo procesa el objeto impactado; la bala solo se elimina.
        Destroy(gameObject);
    }
}
