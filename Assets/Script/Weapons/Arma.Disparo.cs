using UnityEngine;

// Impactos, dispersion y efectos del disparo.
// Es parte del mismo componente Gun; no se agrega como otro componente.
public partial class Arma
{
    private void DispararConAnimacion()
    {
        if (Time.time < siguienteDisparoEn || (!impactoInstantaneo && proyectil == null) || camaraApuntado == null) return;
        if (cargadorAnimado && estaRecargando) return;
        if (municionActual == 0)
        {
            IntentarRecargar();
            return;
        }
        if (recargaCartuchos != null) StopCoroutine(recargaCartuchos);
        recargaCartuchos = null;
        estaRecargando = false;
        municionActual--;
        ActualizarInterfazMunicion();
        siguienteDisparoEn = Time.time + (cargadorAnimado ? frecuenciaDisparo : Mathf.Max(frecuenciaDisparo, DuracionClip("@fire")));
        ReproducirAnimacion("@fire");
        // Aplica la pose de disparo antes de leer la boca al interrumpir una recarga.
        if (animadorArma != null && animadorArma.runtimeAnimatorController != null)
            animadorArma.Update(0f);
        if (sonidoDisparo != null && GestorAudio.Instancia != null)
            GestorAudio.Instancia.ReproducirEfectoSonoro(sonidoDisparo, 0.25f);

        CrearDisparoAnimado();
    }

    private void CrearDisparoAnimado()
    {
        Transform origin = puntoGeneracionProyectil != null ? puntoGeneracionProyectil : transform;
        Quaternion aim = impactoInstantaneo ? camaraApuntado.transform.rotation
            : Quaternion.LookRotation((ObtenerPuntoApuntado() - origin.position).normalized);
        colisionadoresGenerados.Clear();
        for (int i = 0; i < Mathf.Max(1, perdigonesPorDisparo); i++)
        {
            float warmupSpread = minigunAnimada ? Mathf.Lerp(2f, 1f, Mathf.Clamp01(Time.time - giroCompletoEn)) : 1f;
            Vector2 spread = Random.insideUnitCircle * Mathf.Tan(gradosDispersion * warmupSpread * Mathf.Deg2Rad);
            Vector3 direccion = aim * new Vector3(spread.x, spread.y, 1f).normalized;
            // Los proyectiles antiguos avanzan por su eje local -X.
            if (impactoInstantaneo)
            {
                Vector3 fin = DispararImpactoInstantaneo(direccion);
                // El dano es inmediato; la salida visual espera la pose final del fotograma.
                if (trazadoras != null) finalesTrazadorasPendientes.Add(fin);
                continue;
            }
            GameObject pellet = Instantiate(proyectil, origin.position,
                Quaternion.LookRotation(direccion) * Quaternion.Euler(0, 90, 0));
            colisionadoresPerdigones.Clear();
            pellet.GetComponentsInChildren<Collider>(colisionadoresPerdigones);
            foreach (Collider pelletCollider in colisionadoresPerdigones)
            {
                foreach (Collider ownerCollider in colisionadoresPropietario)
                    if (ownerCollider != null) Physics.IgnoreCollision(pelletCollider, ownerCollider);
                foreach (Collider previous in colisionadoresGenerados)
                    Physics.IgnoreCollision(pelletCollider, previous);
            }
            colisionadoresGenerados.AddRange(colisionadoresPerdigones);
        }
        if (fogonazoArma != null)
        {
            GameObject flash = Instantiate(fogonazoArma, origin.position, aim);
            var muzzleFlash = flash.GetComponent<FogonazoArma>();
            if (muzzleFlash != null)
                muzzleFlash.Inicializar(origin, camaraArma != null ? LayerMask.NameToLayer("Viewmodel") : gameObject.layer);
            else if (camaraArma != null)
            {
                flash.transform.SetParent(origin, true);
                foreach (Transform child in flash.GetComponentsInChildren<Transform>(true))
                    child.gameObject.layer = LayerMask.NameToLayer("Viewmodel");
            }
        }
    }

    private void EmitirTrazadorasPendientes()
    {
        if (finalesTrazadorasPendientes.Count == 0) return;
        if (trazadoras != null && camaraApuntado != null)
        {
            Transform bocaArma = puntoGeneracionProyectil != null ? puntoGeneracionProyectil : transform;
            Vector3 inicio = bocaArma.position;
            // Leer la boca despues del Animator evita usar la pose anterior de recarga.
            if (camaraArma != null)
                inicio = camaraApuntado.ViewportToWorldPoint(camaraArma.WorldToViewportPoint(inicio));
            foreach (Vector3 fin in finalesTrazadorasPendientes)
                trazadoras.Emitir(inicio, fin, anchoTrazadora, duracionTrazadora);
        }
        finalesTrazadorasPendientes.Clear();
    }

    private Vector3 DispararImpactoInstantaneo(Vector3 direccion)
    {
        // El impacto parte de la camara para no atravesar paredes cercanas.
        int cantidad = Physics.RaycastNonAlloc(camaraApuntado.transform.position, direccion, impactosApuntado,
            alcanceImpactoInstantaneo, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        RaycastHit[] hits = impactosApuntado;
        if (cantidad == impactosApuntado.Length)
        {
            hits = Physics.RaycastAll(camaraApuntado.transform.position, direccion,
                alcanceImpactoInstantaneo, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            cantidad = hits.Length;
        }
        int nearest = -1;
        for (int i = 0; i < cantidad; i++)
            if (!hits[i].collider.transform.IsChildOf(propietario) &&
                (nearest < 0 || hits[i].distance < hits[nearest].distance)) nearest = i;
        if (nearest >= 0)
        {
            RaycastHit impacto = hits[nearest];
            Enemigo enemy = impacto.collider.GetComponentInParent<Enemigo>();
            if (enemy != null)
            {
                float distanceScale = impacto.distance < 13f
                    ? Mathf.Lerp(1.5f, 1f, impacto.distance / 13f)
                    : Mathf.Lerp(1f, 0.5f, Mathf.Clamp01((impacto.distance - 13f) / 13f));
                if (!caidaDanioDistancia) distanceScale = 1f;
                float warmup = minigunAnimada ? Mathf.Lerp(0.5f, 1f, Mathf.Clamp01(Time.time - giroCompletoEn)) : 1f;
                enemy.RecibirDanio(Mathf.Max(1, Mathf.RoundToInt(danioPerdigon * distanceScale * warmup)));
            }
            return impacto.point;
        }
        return camaraApuntado.transform.position + direccion * alcanceImpactoInstantaneo;
    }

    private Vector3 ObtenerPuntoApuntado()
    {
        Vector3 origin = camaraApuntado.transform.position;
        Vector3 direccion = camaraApuntado.transform.forward;
        Vector3 objetivo = origin + direccion * 100f;
        int cantidad = Physics.RaycastNonAlloc(origin, direccion, impactosApuntado, 100f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        // Si el buffer se llena, revisar todos los impactos para no perder el mas cercano.
        RaycastHit[] hits = impactosApuntado;
        if (cantidad == impactosApuntado.Length)
        {
            hits = Physics.RaycastAll(origin, direccion, 100f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            cantidad = hits.Length;
        }

        float nearest = 100f;
        for (int i = 0; i < cantidad; i++)
        {
            RaycastHit impacto = hits[i];
            if (impacto.collider.transform.IsChildOf(propietario) || impacto.distance >= nearest) continue;
            nearest = impacto.distance;
            objetivo = impacto.point;
        }
        return objetivo;
    }
}
