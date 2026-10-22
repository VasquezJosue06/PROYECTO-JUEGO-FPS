using UnityEngine;

// Giro y disparo continuo exclusivos de la minigun.
// Es parte del mismo componente Gun; no se agrega como otro componente.
public partial class Arma
{
    // El Animator termina de acelerar antes de permitir disparos.
    public void ActualizarEntradaMinigun(bool fire, bool spin)
    {
        if (!minigunAnimada || !inicializado || !isActiveAndEnabled || animadorArma == null) return;
        bool requested = Time.time >= disponibleEn && (fire || spin);
        animadorArma.SetBool("Spin", requested);
        animadorArma.SetBool("Fire", requested && fire && municionActual > 0);
        AnimatorStateInfo estado = animadorArma.GetCurrentAnimatorStateInfo(0);
        bool fullySpun = estado.IsName("@spool_idle") || estado.IsName("@fire");
        EstaGirando = requested || fullySpun || estado.IsName("@spool_up") || estado.IsName("@spool_down");
        if (fullySpun && !giroEstabaCompleto) giroCompletoEn = Time.time;
        giroEstabaCompleto = fullySpun;
        if (Time.timeScale <= 0f || !fire || !requested || !estado.IsName("@fire") ||
            municionActual <= 0 || Time.time < siguienteDisparoEn || camaraApuntado == null) return;
        municionActual--;
        siguienteDisparoEn = Time.time + Mathf.Max(0.01f, frecuenciaDisparo);
        ActualizarInterfazMunicion();
        if (sonidoDisparo != null && GestorAudio.Instancia != null)
            GestorAudio.Instancia.ReproducirEfectoSonoro(sonidoDisparo, 0.25f);
        CrearDisparoAnimado();
    }

    private void BuscarTamborMinigun()
    {
        if (tamborMinigun == null)
            foreach (SkinnedMeshRenderer skin in GetComponentsInChildren<SkinnedMeshRenderer>(true))
                foreach (Transform bone in skin.bones)
                    if (bone != null && bone.name == "v_minigun_barrel") tamborMinigun = bone;
        if (tamborMinigun == null)
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
                if (child.name == "v_minigun_barrel") { tamborMinigun = child; break; }
        if (tamborMinigun == null)
        {
            Debug.LogWarning("No se encontro v_minigun_barrel para el giro de la minigun.", this);
            return;
        }
        rotacionReposoTambor = tamborMinigun.localRotation;
        // El hueso _end es una marca de exportacion: no indica la direccion de los cañones.
        ejeTambor = ejeLocalTambor.sqrMagnitude > 0.000001f
            ? ejeLocalTambor.normalized : Vector3.right;
    }

    private void AnimarTamborMinigun(float tiempoTranscurrido)
    {
        if (!inicializado || !minigunAnimada || tamborMinigun == null || animadorArma == null) return;
        AnimatorStateInfo estado = animadorArma.GetCurrentAnimatorStateInfo(0);
        bool spinning = estado.IsName("@spool_up") || estado.IsName("@spool_idle") || estado.IsName("@fire");
        float objetivo = spinning ? gradosTamborPorSegundo : 0f;
        float duracion = spinning ? tiempoAceleracionTambor : tiempoFrenadoTambor;
        velocidadTambor = Mathf.MoveTowards(velocidadTambor, objetivo,
            gradosTamborPorSegundo / Mathf.Max(0.01f, duracion) * tiempoTranscurrido);
        anguloTambor = Mathf.Repeat(anguloTambor + velocidadTambor * tiempoTranscurrido, 360f);
        // Se aplica despues del Animator para que el clip no borre el giro.
        tamborMinigun.localRotation = rotacionReposoTambor * Quaternion.AngleAxis(anguloTambor, ejeTambor);
    }
}
