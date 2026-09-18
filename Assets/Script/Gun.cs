using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;


public class Gun : MonoBehaviour
{
    [Header("Municion y cadencia")]
    public float reloadTime = 1f;
    public float fireRate = 0.15f;
    public int maxSize = 20;

    [Header("Sonido y efectos")]
    public AudioClip shootingSFX;

    public GameObject bullet;
    public Transform bulletSpawnPoint;

    public GameObject weaponFlash;
    public GameObject droppedWeapon;

    [Header("Retroceso sin animaciones")]
    public float recoilDistance = 0.1f;
    public float recoilSpeed = 15f;

    [Header("Animaciones y tipo de recarga")]
    [Tooltip("Recarga un cartucho por ciclo (Scattergun).")]
    public bool animatedShotgun;
    [Tooltip("Recarga el cargador completo (pistola). Tiene prioridad sobre cartuchos.")]
    public bool animatedMagazine;
    private bool initialized;
    private bool UsesAnimations => animatedShotgun || animatedMagazine;
    public Animator weaponAnimator;
    [Header("Proyectiles animados")]
    [Min(1)] public int pelletsPerShot = 10;
    [Range(0, 30)] public float spreadDegrees = 4f;
    [Header("Posicion respecto a la camara")]
    public Vector3 equippedPosition = new Vector3(0, 0, 0.01f);
    public Vector3 equippedEulerAngles = new Vector3(0, 90, 0);
    [Header("Camara del arma")]
    public bool separateViewmodelCamera = true;
    [Range(20f, 100f)] public float viewmodelFieldOfView = 50f;
    private Camera viewmodelCamera;
    private bool cameraAttached;
    private bool mainCameraIncludedViewmodel;
    private readonly Dictionary<GameObject, int> originalLayers =
        new Dictionary<GameObject, int>();
    // Referencias reutilizadas: no buscar colliders ni clips por cada disparo.
    private readonly Dictionary<string, float> clipDurations = new Dictionary<string, float>();
    private readonly List<Collider> spawnedColliders = new List<Collider>();
    private readonly List<Collider> pelletColliders = new List<Collider>();
    private readonly RaycastHit[] aimHits = new RaycastHit[32];
    private Collider[] ownerColliders;
    private Transform owner;
    private Coroutine shellReload;
    private Camera aimCamera;
    public int CurrentAmmo => currentAmmo;
    public bool IsReloading => isReloading;
    private int currentAmmo;
    private bool isReloading = false;
    private float nextTimetoFire = 0f;

    private Quaternion initialRotation;
    private Vector3 initialPosition;
    private Vector3 reloadRotationOffset = new Vector3(66, 50, 50);

    void Start()
    {
        currentAmmo = maxSize;
        owner = transform.root;
        ownerColliders = owner.GetComponentsInChildren<Collider>();
        if (UsesAnimations)
        {
            if (weaponAnimator == null) weaponAnimator = GetComponentInChildren<Animator>();
            CacheAnimationDurations();
            aimCamera = GetComponentInParent<Camera>();
            if (aimCamera == null) aimCamera = Camera.main;
            foreach (Transform child in GetComponentsInChildren<Transform>())
                if (bulletSpawnPoint == null && child.name == "muzzle") bulletSpawnPoint = child;
            if (GetComponentInParent<PlayerShoothing>() != null && aimCamera != null)
            {
                transform.position = aimCamera.transform.TransformPoint(equippedPosition);
                transform.rotation = aimCamera.transform.rotation * Quaternion.Euler(equippedEulerAngles);
            }
            SetupViewmodelCamera();
            PlayAnimation("@draw");
            nextTimetoFire = Time.time + ClipDuration("@draw");
        }
        initialized = true;
        initialRotation = transform.localRotation;
        initialPosition = transform.localPosition;
        UpdateAmmoUI();
    }

    public void Shoot()
    {
        if (!initialized || !isActiveAndEnabled) return;
        if (UsesAnimations)
        {
            ShootAnimated();
            return;
        }
        if(isReloading) return;
        if(Time.time < nextTimetoFire) return;

        if(currentAmmo <= 0)
        {
            StartCoroutine(Reload());
            return;
        }

        nextTimetoFire = Time.time + fireRate;
        currentAmmo--;
        UpdateAmmoUI();


        AudioManager.Instance.PlaySFX(shootingSFX, 0.25f);

        Quaternion adjustedRotation = bulletSpawnPoint.rotation * Quaternion.Euler(-1f, -1f, 0);

        Instantiate(bullet, bulletSpawnPoint.position, adjustedRotation);
        Instantiate(weaponFlash, bulletSpawnPoint.position, bulletSpawnPoint.rotation);

        StopCoroutine(nameof(Recoil));
        StartCoroutine(nameof(Recoil));
    }

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

    public bool Drop()
    {
        if (droppedWeapon == null) return false;
        if (UiManager.Instance != null && UiManager.Instance.ammoText != null)
            UiManager.Instance.ammoText.text = "";
        Instantiate(droppedWeapon, transform.position, transform.rotation);
        Destroy(gameObject);
        return true;
    }

    private void UpdateAmmoUI()
    {
        if (UiManager.Instance != null && UiManager.Instance.ammoText != null)
            UiManager.Instance.ammoText.text = currentAmmo.ToString();
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
        return clipDurations.TryGetValue(clipName, out float duration) ? duration : reloadTime;
    }
    private void PlayAnimation(string state)
    {
        if (weaponAnimator != null && weaponAnimator.runtimeAnimatorController != null)
            weaponAnimator.Play("Base Layer." + state, 0, 0f);
    }

    private void ShootAnimated()
    {
        if (Time.time < nextTimetoFire || bullet == null || aimCamera == null) return;
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
        if (shootingSFX != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(shootingSFX, 0.25f);

        SpawnAnimatedShot();
    }

    private void SpawnAnimatedShot()
    {
        Transform origin = bulletSpawnPoint != null ? bulletSpawnPoint : transform;
        Quaternion aim = Quaternion.LookRotation((GetAimPoint() - origin.position).normalized);
        spawnedColliders.Clear();
        for (int i = 0; i < Mathf.Max(1, pelletsPerShot); i++)
        {
            Vector2 spread = Random.insideUnitCircle * Mathf.Tan(spreadDegrees * Mathf.Deg2Rad);
            Vector3 direction = aim * new Vector3(spread.x, spread.y, 1f).normalized;
            // Existing Bullet travels along local -X.
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
            if (viewmodelCamera != null)
            {
                flash.transform.SetParent(origin, true);
                foreach (Transform child in flash.GetComponentsInChildren<Transform>(true))
                    child.gameObject.layer = LayerMask.NameToLayer("Viewmodel");
            }
        }
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
    private IEnumerator ReloadMagazine()
    {
        isReloading = true;
        PlayAnimation("@reload");
        yield return new WaitForSeconds(ClipDuration("@reload"));
        currentAmmo = maxSize;
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
        while (currentAmmo < maxSize)
        {
            PlayAnimation("@reload_loop");
            yield return new WaitForSeconds(ClipDuration("@reload_loop"));
            currentAmmo++;
            UpdateAmmoUI();
        }
        PlayAnimation("@reload_end");
        yield return new WaitForSeconds(ClipDuration("@reload_end"));
        PlayAnimation("@idle");
        isReloading = false;
        shellReload = null;
    }

    private void OnDisable()
    {
        if (initialized && !UsesAnimations)
        {
            transform.localPosition = initialPosition;
            transform.localRotation = initialRotation;
        }
        RemoveViewmodelCamera();
        StopAllCoroutines();
        shellReload = null;
        isReloading = false;
    }

    private void OnEnable()
    {
        // Start discovers the owner the first time; subsequent enables restore the camera.
        if (!initialized) return;
        if (UsesAnimations)
        {
            SetupViewmodelCamera();
            PlayAnimation("@draw");
            nextTimetoFire = Mathf.Max(nextTimetoFire, Time.time + ClipDuration("@draw"));
        }
        UpdateAmmoUI();
    }

    private void SetupViewmodelCamera()
    {
        if (!UsesAnimations || !separateViewmodelCamera || aimCamera == null || cameraAttached)
            return;
        int layer = LayerMask.NameToLayer("Viewmodel");
        if (layer < 0)
        {
            Debug.LogError("Create the Viewmodel layer before enabling the weapon camera.", this);
            return;
        }
        var stack = aimCamera.GetUniversalAdditionalCameraData().cameraStack;
        if (stack == null) return;

        if (viewmodelCamera == null)
        {
            GameObject cameraObject = new GameObject("Viewmodel Camera");
            cameraObject.transform.SetParent(aimCamera.transform, false);
            viewmodelCamera = cameraObject.AddComponent<Camera>();
            viewmodelCamera.fieldOfView = viewmodelFieldOfView;
            viewmodelCamera.nearClipPlane = 0.001f;
            viewmodelCamera.farClipPlane = 20f;
            viewmodelCamera.cullingMask = 1 << layer;
            viewmodelCamera.useOcclusionCulling = false;
            viewmodelCamera.allowHDR = aimCamera.allowHDR;
            viewmodelCamera.allowMSAA = aimCamera.allowMSAA;
            var overlay = viewmodelCamera.GetUniversalAdditionalCameraData();
            overlay.renderType = CameraRenderType.Overlay;
            overlay.renderPostProcessing = false;
            overlay.renderShadows = false;
    
            }
        viewmodelCamera.enabled = true;
        mainCameraIncludedViewmodel = (aimCamera.cullingMask & (1 << layer)) != 0;
        aimCamera.cullingMask &= ~(1 << layer);
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            originalLayers[child.gameObject] = child.gameObject.layer;
            child.gameObject.layer = layer;
        }
        if (!stack.Contains(viewmodelCamera)) stack.Add(viewmodelCamera);
        cameraAttached = true;
    }

    private void LateUpdate()
    {
        if (viewmodelCamera == null || aimCamera == null) return;
        // Never copy the world FOV: zooming or sprint FOV must not expose the arm ends.
        viewmodelCamera.fieldOfView = Mathf.Clamp(viewmodelFieldOfView, 20f, 100f);
        viewmodelCamera.rect = aimCamera.rect;
        viewmodelCamera.aspect = aimCamera.aspect;
        viewmodelCamera.enabled = aimCamera.enabled;
    }

    private void OnDestroy()
    {
        // La camara se conserva al guardar el arma y solo se destruye con ella.
        RemoveViewmodelCamera();
        if (viewmodelCamera != null) Destroy(viewmodelCamera.gameObject);
    }

    private void RemoveViewmodelCamera()
    {
        if (viewmodelCamera == null || !cameraAttached) return;
        if (aimCamera != null)
        {
            aimCamera.GetUniversalAdditionalCameraData().cameraStack?.Remove(viewmodelCamera);
            int layer = LayerMask.NameToLayer("Viewmodel");
            if (layer >= 0 && mainCameraIncludedViewmodel) aimCamera.cullingMask |= 1 << layer;
        }
        foreach (var entry in originalLayers)
            if (entry.Key != null) entry.Key.layer = entry.Value;
        originalLayers.Clear();
        viewmodelCamera.enabled = false;
        cameraAttached = false;
        Destroy(viewmodelCamera.gameObject);
        viewmodelCamera = null;
    }
}
