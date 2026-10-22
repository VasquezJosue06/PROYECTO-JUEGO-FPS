using UnityEngine;

public class VidaJugador : MonoBehaviour
{
    public int vida = 100;
    public int VidaMaxima { get; private set; } = 100;

    public void AplicarVidaMaxima(int valor)
    {
        VidaMaxima = Mathf.Max(1, valor);
        vida = VidaMaxima;
        if (GestorInterfaz.Instancia != null) GestorInterfaz.Instancia.ActualizarVida(vida, VidaMaxima);
    }

    [SerializeField] private AudioClip sonidoImpacto;

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag == "Damage")
        {
            RestarVida(10);
        }
    }

    private void RestarVida(int cantidadARestar)
    {
        vida -= cantidadARestar;
        MiradaJugador.Instancia.AplicarTemblor(0.1f, 0.25f);
        GestorInterfaz.Instancia.MostrarIndicadorImpacto();
        GestorAudio.Instancia.ReproducirEfectoSonoro(sonidoImpacto);
        GestorInterfaz.Instancia.ActualizarVida(vida, VidaMaxima);

        if(vida <= 0)
        {
            Morir();
        }
    }

    private void Morir()
    {
        Time.timeScale = 0f;
        GestorInterfaz.Instancia.MostrarInterfazMuerte();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
