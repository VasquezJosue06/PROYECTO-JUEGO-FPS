using UnityEngine;
using System.Collections;

// Recarga, duracion de los clips y retroceso.
// Es parte del mismo componente Gun; no se agrega como otro componente.
public partial class Gun
{
    IEnumerator Reload()
    {
        isReloading = true;

        Quaternion targetRotation = Quaternion.Euler(initialRotation.eulerAngles + reloadRotationOffset);
        float halfReload = reloadTime / 2f;
        float t = 0f;

        while(t < halfReload)
        {
            t += Time.deltaTime;
            transform.localRotation = Quaternion.Slerp(initialRotation, targetRotation, t / halfReload);
            yield return null;
        }

        t = 0f;

        while(t < halfReload)
        {
            t += Time.deltaTime;
            transform.localRotation = Quaternion.Slerp(targetRotation, initialRotation, t / halfReload);
            yield return null;
        }

        currentAmmo = maxSize;
        UpdateAmmoUI();
        isReloading = false;
    }

    public void TryReload()
    {
        if (animatedMinigun || reserveAmmo == 0 || Time.timeScale <= 0f) return;
        if (!initialized || !isActiveAndEnabled || isReloading) return;
        if (currentAmmo == maxSize) return;
        if (UsesAnimations)
        {
            if (Time.time < nextTimetoFire) return;
            shellReload = StartCoroutine(animatedMagazine ? ReloadMagazine() : ReloadShells());
            return;
        }

        StartCoroutine(Reload());
    }

    private IEnumerator Recoil()
    {
        Vector3 recoilTarget = initialPosition + new Vector3(recoilDistance, 0, 0);
        float t = 0f;

        while(t < 1f)
        {
            t += Time.deltaTime * recoilSpeed;
            transform.localPosition = Vector3.Lerp(initialPosition, recoilTarget, t);
            yield return null;
        }

        t = 0f;

        while(t < 1f)
        {
            t += Time.deltaTime * recoilSpeed;
            transform.localPosition = Vector3.Lerp(recoilTarget, initialPosition, t);
            yield return null;
        }

        transform.localPosition = initialPosition;
    }

    private void CacheAnimationDurations()
    {
        clipDurations.Clear();
        if (weaponAnimator == null || weaponAnimator.runtimeAnimatorController == null) return;

        foreach (AnimationClip clip in weaponAnimator.runtimeAnimatorController.animationClips)
            clipDurations[clip.name] = clip.length;
    }

    private float ClipDuration(string clipName)
    {
        if (clipName == "@fire" && fireAnimationDuration > 0f) return fireAnimationDuration;
        return clipDurations.TryGetValue(clipName, out float duration) ? duration : reloadTime;
    }

    private void PlayAnimation(string state)
    {
        if (weaponAnimator != null && weaponAnimator.runtimeAnimatorController != null)
            weaponAnimator.Play("Base Layer." + state, 0, 0f);
    }

    // -1 significa reserva ilimitada; las reservas finitas nunca crean municion.
    private void FillMagazineFromReserve()
    {
        int missing = Mathf.Max(0, maxSize - currentAmmo);
        int loaded = reserveAmmo < 0 ? missing : Mathf.Min(missing, reserveAmmo);
        currentAmmo += loaded;
        if (reserveAmmo >= 0) reserveAmmo -= loaded;
    }

    private IEnumerator ReloadMagazine()
    {
        isReloading = true;
        PlayAnimation("@reload");
        yield return new WaitForSeconds(ClipDuration("@reload"));
        FillMagazineFromReserve();
        UpdateAmmoUI();
        PlayAnimation("@idle");
        isReloading = false;
        shellReload = null;
    }

    private IEnumerator ReloadShells()
    {
        isReloading = true;
        PlayAnimation("@reload_start");
        yield return new WaitForSeconds(ClipDuration("@reload_start"));
        while (currentAmmo < maxSize && reserveAmmo != 0)
        {
            PlayAnimation("@reload_loop");
            yield return new WaitForSeconds(ClipDuration("@reload_loop"));
            currentAmmo++;
            if (reserveAmmo > 0) reserveAmmo--;
            UpdateAmmoUI();
        }
        PlayAnimation("@reload_end");
        yield return new WaitForSeconds(ClipDuration("@reload_end"));
        PlayAnimation("@idle");
        isReloading = false;
        shellReload = null;
    }
}
