using UnityEngine;

// Impactos, dispersion y efectos del disparo.
// Es parte del mismo componente Gun; no se agrega como otro componente.
public partial class Gun
{
    private void ShootAnimated()
    {
        if (Time.time < nextTimetoFire || (!hitscan && bullet == null) || aimCamera == null) return;
        if (animatedMagazine && isReloading) return;
        if (currentAmmo == 0)
        {
            TryReload();
            return;
        }
        if (shellReload != null) StopCoroutine(shellReload);
        shellReload = null;
        isReloading = false;
        currentAmmo--;
        UpdateAmmoUI();
        nextTimetoFire = Time.time + (animatedMagazine ? fireRate : Mathf.Max(fireRate, ClipDuration("@fire")));
        PlayAnimation("@fire");
        // Aplica la pose de disparo antes de leer la boca al interrumpir una recarga.
        if (weaponAnimator != null && weaponAnimator.runtimeAnimatorController != null)
            weaponAnimator.Update(0f);
        if (shootingSFX != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(shootingSFX, 0.25f);

        SpawnAnimatedShot();
    }

    private void SpawnAnimatedShot()
    {
        Transform origin = bulletSpawnPoint != null ? bulletSpawnPoint : transform;
        Quaternion aim = hitscan ? aimCamera.transform.rotation
            : Quaternion.LookRotation((GetAimPoint() - origin.position).normalized);
        spawnedColliders.Clear();
        for (int i = 0; i < Mathf.Max(1, pelletsPerShot); i++)
        {
            float warmupSpread = animatedMinigun ? Mathf.Lerp(2f, 1f, Mathf.Clamp01(Time.time - fullSpinAt)) : 1f;
            Vector2 spread = Random.insideUnitCircle * Mathf.Tan(spreadDegrees * warmupSpread * Mathf.Deg2Rad);
            Vector3 direction = aim * new Vector3(spread.x, spread.y, 1f).normalized;
            // Los proyectiles antiguos avanzan por su eje local -X.
            if (hitscan)
            {
                Vector3 end = FireHitscan(direction);
                // El dano es inmediato; la salida visual espera la pose final del fotograma.
                if (tracers != null) pendingTracerEnds.Add(end);
                continue;
            }
            GameObject pellet = Instantiate(bullet, origin.position,
                Quaternion.LookRotation(direction) * Quaternion.Euler(0, 90, 0));
            pelletColliders.Clear();
            pellet.GetComponentsInChildren<Collider>(pelletColliders);
            foreach (Collider pelletCollider in pelletColliders)
            {
                foreach (Collider ownerCollider in ownerColliders)
                    if (ownerCollider != null) Physics.IgnoreCollision(pelletCollider, ownerCollider);
                foreach (Collider previous in spawnedColliders)
                    Physics.IgnoreCollision(pelletCollider, previous);
            }
            spawnedColliders.AddRange(pelletColliders);
        }
        if (weaponFlash != null)
        {
            GameObject flash = Instantiate(weaponFlash, origin.position, aim);
            var muzzleFlash = flash.GetComponent<WeaponMuzzleFlash>();
            if (muzzleFlash != null)
                muzzleFlash.Initialize(origin, viewmodelCamera != null ? LayerMask.NameToLayer("Viewmodel") : gameObject.layer);
            else if (viewmodelCamera != null)
            {
                flash.transform.SetParent(origin, true);
                foreach (Transform child in flash.GetComponentsInChildren<Transform>(true))
                    child.gameObject.layer = LayerMask.NameToLayer("Viewmodel");
            }
        }
    }

    private void EmitPendingTracers()
    {
        if (pendingTracerEnds.Count == 0) return;
        if (tracers != null && aimCamera != null)
        {
            Transform muzzle = bulletSpawnPoint != null ? bulletSpawnPoint : transform;
            Vector3 start = muzzle.position;
            // Leer la boca despues del Animator evita usar la pose anterior de recarga.
            if (viewmodelCamera != null)
                start = aimCamera.ViewportToWorldPoint(viewmodelCamera.WorldToViewportPoint(start));
            foreach (Vector3 end in pendingTracerEnds)
                tracers.Emit(start, end, tracerWidth, tracerLifetime);
        }
        pendingTracerEnds.Clear();
    }

    private Vector3 FireHitscan(Vector3 direction)
    {
        // El impacto parte de la camara para no atravesar paredes cercanas.
        int count = Physics.RaycastNonAlloc(aimCamera.transform.position, direction, aimHits,
            hitscanRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        RaycastHit[] hits = aimHits;
        if (count == aimHits.Length)
        {
            hits = Physics.RaycastAll(aimCamera.transform.position, direction,
                hitscanRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            count = hits.Length;
        }
        int nearest = -1;
        for (int i = 0; i < count; i++)
            if (!hits[i].collider.transform.IsChildOf(owner) &&
                (nearest < 0 || hits[i].distance < hits[nearest].distance)) nearest = i;
        if (nearest >= 0)
        {
            RaycastHit hit = hits[nearest];
            Enemy enemy = hit.collider.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                float distanceScale = hit.distance < 13f
                    ? Mathf.Lerp(1.5f, 1f, hit.distance / 13f)
                    : Mathf.Lerp(1f, 0.5f, Mathf.Clamp01((hit.distance - 13f) / 13f));
                if (!distanceDamageFalloff) distanceScale = 1f;
                float warmup = animatedMinigun ? Mathf.Lerp(0.5f, 1f, Mathf.Clamp01(Time.time - fullSpinAt)) : 1f;
                enemy.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(pelletDamage * distanceScale * warmup)));
            }
            return hit.point;
        }
        return aimCamera.transform.position + direction * hitscanRange;
    }

    private Vector3 GetAimPoint()
    {
        Vector3 origin = aimCamera.transform.position;
        Vector3 direction = aimCamera.transform.forward;
        Vector3 target = origin + direction * 100f;
        int count = Physics.RaycastNonAlloc(origin, direction, aimHits, 100f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        // Si el buffer se llena, revisar todos los impactos para no perder el mas cercano.
        RaycastHit[] hits = aimHits;
        if (count == aimHits.Length)
        {
            hits = Physics.RaycastAll(origin, direction, 100f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            count = hits.Length;
        }

        float nearest = 100f;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider.transform.IsChildOf(owner) || hit.distance >= nearest) continue;
            nearest = hit.distance;
            target = hit.point;
        }
        return target;
    }
}
