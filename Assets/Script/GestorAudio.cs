using UnityEngine;
using System.Collections;

public class GestorAudio : MonoBehaviour
{
    public static GestorAudio Instancia;

    void Awake()
    {
        Instancia = this;
    }

    public void ReproducirEfectoSonoro(AudioClip clipAudio, float volumen = 1f)
    {
        StartCoroutine(ReproducirEfectoSonoroCoroutine(clipAudio, volumen));
    }

    IEnumerator ReproducirEfectoSonoroCoroutine(AudioClip clipAudio, float volumen = 1f)
    {
        AudioSource fuenteAudio = gameObject.AddComponent<AudioSource>();
        fuenteAudio.clip = clipAudio;
        fuenteAudio.volume = volumen;
        fuenteAudio.Play();

        yield return new WaitForSeconds(clipAudio.length * 2f);

        Destroy(fuenteAudio);
    }
}
