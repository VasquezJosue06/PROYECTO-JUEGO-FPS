using UnityEngine;

// Giro y disparo continuo exclusivos de la minigun.
// Es parte del mismo componente Gun; no se agrega como otro componente.
public partial class Gun
{
    // El Animator termina de acelerar antes de permitir disparos.
    public void UpdateMinigunInput(bool fire, bool spin)
    {
        if (!animatedMinigun || !initialized || !isActiveAndEnabled || weaponAnimator == null) return;
        bool requested = Time.time >= readyAt && (fire || spin);
        weaponAnimator.SetBool("Spin", requested);
        weaponAnimator.SetBool("Fire", requested && fire && currentAmmo > 0);
        AnimatorStateInfo state = weaponAnimator.GetCurrentAnimatorStateInfo(0);
        bool fullySpun = state.IsName("@spool_idle") || state.IsName("@fire");
        IsSpinning = requested || fullySpun || state.IsName("@spool_up") || state.IsName("@spool_down");
        if (fullySpun && !wasFullySpun) fullSpinAt = Time.time;
        wasFullySpun = fullySpun;
        if (Time.timeScale <= 0f || !fire || !requested || !state.IsName("@fire") ||
            currentAmmo <= 0 || Time.time < nextTimetoFire || aimCamera == null) return;
        currentAmmo--;
        nextTimetoFire = Time.time + Mathf.Max(0.01f, fireRate);
        UpdateAmmoUI();
        if (shootingSFX != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(shootingSFX, 0.25f);
        SpawnAnimatedShot();
    }

    private void FindMinigunBarrel()
    {
        if (minigunBarrel == null)
            foreach (SkinnedMeshRenderer skin in GetComponentsInChildren<SkinnedMeshRenderer>(true))
                foreach (Transform bone in skin.bones)
                    if (bone != null && bone.name == "v_minigun_barrel") minigunBarrel = bone;
        if (minigunBarrel == null)
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
                if (child.name == "v_minigun_barrel") { minigunBarrel = child; break; }
        if (minigunBarrel == null)
        {
            Debug.LogWarning("No se encontro v_minigun_barrel para el giro de la minigun.", this);
            return;
        }
        barrelRestRotation = minigunBarrel.localRotation;
        // El hueso _end es una marca de exportacion: no indica la direccion de los cañones.
        barrelAxis = barrelLocalAxis.sqrMagnitude > 0.000001f
            ? barrelLocalAxis.normalized : Vector3.right;
    }

    private void AnimateMinigunBarrel(float deltaTime)
    {
        if (!initialized || !animatedMinigun || minigunBarrel == null || weaponAnimator == null) return;
        AnimatorStateInfo state = weaponAnimator.GetCurrentAnimatorStateInfo(0);
        bool spinning = state.IsName("@spool_up") || state.IsName("@spool_idle") || state.IsName("@fire");
        float target = spinning ? barrelDegreesPerSecond : 0f;
        float duration = spinning ? barrelSpinUpTime : barrelSpinDownTime;
        barrelSpeed = Mathf.MoveTowards(barrelSpeed, target,
            barrelDegreesPerSecond / Mathf.Max(0.01f, duration) * deltaTime);
        barrelAngle = Mathf.Repeat(barrelAngle + barrelSpeed * deltaTime, 360f);
        // Se aplica despues del Animator para que el clip no borre el giro.
        minigunBarrel.localRotation = barrelRestRotation * Quaternion.AngleAxis(barrelAngle, barrelAxis);
    }
}
