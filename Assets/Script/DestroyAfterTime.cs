using UnityEngine;

public class DestroyAfterTime : MonoBehaviour
{
    public float time;

    void Start()
    {
        // Útil para efectos temporales, como fogonazos o partículas de impacto.
        Destroy(gameObject, time);
    }

}
