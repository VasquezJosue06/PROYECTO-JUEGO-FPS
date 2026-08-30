using UnityEngine;

public class Bullet : MonoBehaviour
{
<<<<<<< HEAD
    [Header("Movimiento")]
=======
>>>>>>> Develop
    public float speed = 15f;
    public float lifeTime = 4f;

    private Rigidbody rb;
<<<<<<< HEAD

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // El prefab está orientado con su eje derecho inverso hacia delante.
        rb.linearVelocity = -transform.right * speed;

        // Limpieza de seguridad si no golpea ningún objeto.
=======
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.linearVelocity = -transform.right * speed;
>>>>>>> Develop
        Destroy(gameObject, lifeTime);
    }

    void OnCollisionEnter(Collision collision)
    {
<<<<<<< HEAD
        // El daño lo procesa el objeto impactado; la bala solo se elimina.
=======
>>>>>>> Develop
        Destroy(gameObject);
    }
}
