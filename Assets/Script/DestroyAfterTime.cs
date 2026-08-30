using UnityEngine;

public class DestroyAfterTime : MonoBehaviour
{
    public float time;
<<<<<<< HEAD

    void Start()
    {
        // Útil para efectos temporales, como fogonazos o partículas de impacto.
=======
    void Start()
    {
>>>>>>> Develop
        Destroy(gameObject, time);
    }

}
