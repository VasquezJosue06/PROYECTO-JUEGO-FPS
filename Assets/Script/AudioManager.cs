using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    // Instancia compartida para reproducir efectos desde cualquier sistema.
    public static AudioManager Instance;

    void Awake()
    {
        Instance = this;
    }

    public void PlaySFX(AudioClip audioClip, float volume = 1f)
    {
        // Una fuente temporal permite superponer efectos de sonido.
        StartCoroutine(PlaySFXCoroutine(audioClip, volume));
    }

    IEnumerator PlaySFXCoroutine(AudioClip audioClip, float volume = 1f)
    {
        AudioSource audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = audioClip;
        audioSource.volume = volume;
        audioSource.Play();

        // Se libera la fuente al terminar el clip; antes esperaba el doble de tiempo.
        yield return new WaitForSeconds(audioClip.length);

        Destroy(audioSource);
    }
}
