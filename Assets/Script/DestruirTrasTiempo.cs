using UnityEngine;

public class DestruirTrasTiempo : MonoBehaviour
{
    [SerializeField] private float tiempo;
    void Start()
    {
        Destroy(gameObject, tiempo);
    }

}
