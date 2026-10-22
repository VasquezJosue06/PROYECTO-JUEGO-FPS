using UnityEngine;
using System.Collections;

// Recarga, duracion de los clips y retroceso.
// Es parte del mismo componente Gun; no se agrega como otro componente.
public partial class Arma
{
    IEnumerator Reload()
    {
        estaRecargando = true;

        Quaternion targetRotation = Quaternion.Euler(rotacionInicial.eulerAngles + desplazamientoRotacionRecarga);
        float halfReload = tiempoRecarga / 2f;
        float t = 0f;

        while(t < halfReload)
        {
            t += Time.deltaTime;
            transform.localRotation = Quaternion.Slerp(rotacionInicial, targetRotation, t / halfReload);
            yield return null;
        }

        t = 0f;

        while(t < halfReload)
        {
            t += Time.deltaTime;
            transform.localRotation = Quaternion.Slerp(targetRotation, rotacionInicial, t / halfReload);
            yield return null;
        }

        LlenarCargadorDesdeReserva();
        ActualizarInterfazMunicion();
        estaRecargando = false;
    }

    public void IntentarRecargar()
    {
        if (minigunAnimada || municionReserva == 0 || Time.timeScale <= 0f) return;
        if (!inicializado || !isActiveAndEnabled || estaRecargando) return;
        if (municionActual == capacidadMaxima) return;
        if (UsaAnimaciones)
        {
            if (Time.time < siguienteDisparoEn) return;
            recargaCartuchos = StartCoroutine(cargadorAnimado ? RecargarCargador() : RecargarCartuchos());
            return;
        }

        StartCoroutine(Reload());
    }

    private IEnumerator Retroceso()
    {
        Vector3 recoilTarget = posicionInicial + new Vector3(distanciaRetroceso, 0, 0);
        float t = 0f;

        while(t < 1f)
        {
            t += Time.deltaTime * velocidadRetroceso;
            transform.localPosition = Vector3.Lerp(posicionInicial, recoilTarget, t);
            yield return null;
        }

        t = 0f;

        while(t < 1f)
        {
            t += Time.deltaTime * velocidadRetroceso;
            transform.localPosition = Vector3.Lerp(recoilTarget, posicionInicial, t);
            yield return null;
        }

        transform.localPosition = posicionInicial;
    }

    private void GuardarDuracionesAnimacion()
    {
        duracionesClips.Clear();
        if (animadorArma == null || animadorArma.runtimeAnimatorController == null) return;

        foreach (AnimationClip clip in animadorArma.runtimeAnimatorController.animationClips)
            duracionesClips[clip.name] = clip.length;
    }

    private float DuracionClip(string clipName)
    {
        if (clipName == "@fire" && duracionAnimacionDisparo > 0f) return duracionAnimacionDisparo;
        return duracionesClips.TryGetValue(clipName, out float duracion) ? duracion : tiempoRecarga;
    }

    private void ReproducirAnimacion(string estado)
    {
        if (animadorArma != null && animadorArma.runtimeAnimatorController != null)
            animadorArma.Play("Base Layer." + estado, 0, 0f);
    }

    // -1 significa reserva ilimitada; las reservas finitas nunca crean municion.
    private void LlenarCargadorDesdeReserva()
    {
        int missing = Mathf.Max(0, capacidadMaxima - municionActual);
        int loaded = municionReserva < 0 ? missing : Mathf.Min(missing, municionReserva);
        municionActual += loaded;
        if (municionReserva >= 0) municionReserva -= loaded;
    }

    private IEnumerator RecargarCargador()
    {
        estaRecargando = true;
        ReproducirAnimacion("@reload");
        yield return new WaitForSeconds(DuracionClip("@reload"));
        LlenarCargadorDesdeReserva();
        ActualizarInterfazMunicion();
        ReproducirAnimacion("@idle");
        estaRecargando = false;
        recargaCartuchos = null;
    }

    private IEnumerator RecargarCartuchos()
    {
        estaRecargando = true;
        ReproducirAnimacion("@reload_start");
        yield return new WaitForSeconds(DuracionClip("@reload_start"));
        while (municionActual < capacidadMaxima && municionReserva != 0)
        {
            ReproducirAnimacion("@reload_loop");
            yield return new WaitForSeconds(DuracionClip("@reload_loop"));
            municionActual++;
            if (municionReserva > 0) municionReserva--;
            ActualizarInterfazMunicion();
        }
        ReproducirAnimacion("@reload_end");
        yield return new WaitForSeconds(DuracionClip("@reload_end"));
        ReproducirAnimacion("@idle");
        estaRecargando = false;
        recargaCartuchos = null;
    }
}
