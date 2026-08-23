using UnityEngine;
using System.Collections;
using Unity.Mathematics;

public class Enemy : MonoBehaviour
{
    public int health = 100;

    public Material hitMat;
    private Renderer rend;
    private Material originalMaterial;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rend = GetComponent<Renderer>();
        originalMaterial = rend.material;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag == "Damage")
        {
            health -= 10;
            if(health <= 0)
            {
                Die();
            }

            else
            {
                StartCoroutine(Blink());
            }
        }
    }

    void Die()
    {
        if(!this.enabled) return;
        
        rb.freezeRotation = false;
        transform.rotation = quaternion.Euler(transform.rotation.x, transform.rotation.y, transform.rotation.z + 5f);
        this.enabled = false;
    }

    IEnumerator Blink()
    {
        rend.material = hitMat;
        yield return new WaitForSeconds(0.1f);
        rend.material = originalMaterial;
    }

}
