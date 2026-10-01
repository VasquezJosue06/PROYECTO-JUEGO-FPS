using UnityEngine;
using System.Collections.Generic;

// Conserva este componente y sus campos: los prefabs y las clases ya los usan.
public partial class Gun : MonoBehaviour
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
    [Header("Minigun")]
    public bool animatedMinigun;
    [Range(0f, 1f)] public float spunMoveMultiplier = 0.4782609f;
    [Header("Reserva de municion")]
    [Tooltip("-1 conserva la reserva ilimitada de las armas existentes.")]
    public int reserveSize = -1;
    [Header("Tipo de impacto")]
    [Tooltip("Impacto instantaneo. La trazadora solo muestra el recorrido.")]
    public bool hitscan;
    public bool distanceDamageFalloff = true;
    [Tooltip("0 usa la duracion del clip; debe coincidir con la velocidad del estado fire.")]
    [Min(0)] public float fireAnimationDuration;
    [Min(0)] public float pelletDamage = 6f;
    [Min(1)] public float hitscanRange = 100f;
    [Header("Trazadoras compartidas")]
    public bool visibleTracers;
    public GameObject tracerPrefab;
    [Min(0.001f)] public float tracerWidth = 0.035f;
    [Min(0.01f)] public float tracerLifetime = 0.075f;
    [Header("Giro de la minigun")]
    public Transform minigunBarrel;
    [Tooltip("Eje longitudinal del tambor en el modelo exportado. Esta minigun usa X.")]
    public Vector3 barrelLocalAxis = Vector3.right;
    [Min(0)] public float barrelDegreesPerSecond = 1440f;
    [Min(0.01f)] public float barrelSpinUpTime = 0.87f;
    [Min(0.01f)] public float barrelSpinDownTime = 0.6f;
    private Quaternion barrelRestRotation;
    private Vector3 barrelAxis = Vector3.right;
    private float barrelSpeed;
    private float barrelAngle;
    private HitscanTracerPool tracers;
    private readonly List<Vector3> pendingTracerEnds = new List<Vector3>(16);
    private int reserveAmmo;
    private float readyAt;
    private float fullSpinAt;
    private bool wasFullySpun;
    public int ReserveAmmo => reserveAmmo;
    public bool IsSpinning { get; private set; }
    public float MovementMultiplier => IsSpinning ? spunMoveMultiplier : 1f;
    public bool CanSwitch => !IsSpinning;
    private bool initialized;
    private bool UsesAnimations => animatedShotgun || animatedMagazine || animatedMinigun;
    [Header("Animator del arma")]
    public Animator weaponAnimator;
    [Header("Perdigones y dispersion")]
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
        reserveAmmo = reserveSize;
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
            readyAt = nextTimetoFire;
        }
        if (animatedMinigun) FindMinigunBarrel();
        if (hitscan && visibleTracers) tracers = new HitscanTracerPool(tracerPrefab);
        initialized = true;
        initialRotation = transform.localRotation;
        initialPosition = transform.localPosition;
        UpdateAmmoUI();
    }

    public void Shoot()
    {
        if (!initialized || !isActiveAndEnabled || animatedMinigun || Time.timeScale <= 0f) return;
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
            UiManager.Instance.ammoText.text = reserveSize >= 0
                ? currentAmmo + " / " + reserveAmmo : currentAmmo.ToString();
    }

    private void OnDisable()
    {
        IsSpinning = false;
        wasFullySpun = false;
        barrelSpeed = 0;
        barrelAngle = 0;
        if (initialized && minigunBarrel != null) minigunBarrel.localRotation = barrelRestRotation;
        pendingTracerEnds.Clear();
        tracers?.Clear();
        if (animatedMinigun && weaponAnimator != null && weaponAnimator.runtimeAnimatorController != null)
        {
            weaponAnimator.SetBool("Spin", false);
            weaponAnimator.SetBool("Fire", false);
        }
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
        // Al volver a equipar, recupera la camara y reproduce la animacion de sacar el arma.
        if (!initialized) return;
        if (UsesAnimations)
        {
            SetupViewmodelCamera();
            PlayAnimation("@draw");
            nextTimetoFire = Mathf.Max(nextTimetoFire, Time.time + ClipDuration("@draw"));
            readyAt = nextTimetoFire;
        }
        UpdateAmmoUI();
    }

    private void LateUpdate()
    {
        AnimateMinigunBarrel(Time.deltaTime);
        tracers?.Tick(Time.deltaTime);
        if (viewmodelCamera != null && aimCamera != null)
        {
            // Sincroniza la proyeccion antes de calcular la salida de las trazadoras.
            viewmodelCamera.fieldOfView = Mathf.Clamp(viewmodelFieldOfView, 20f, 100f);
            viewmodelCamera.rect = aimCamera.rect;
            viewmodelCamera.aspect = aimCamera.aspect;
            viewmodelCamera.enabled = aimCamera.enabled;
        }
        EmitPendingTracers();
    }

    private void OnDestroy()
    {
        tracers?.Dispose();
        // Limpia los efectos y cualquier camara que aun exista.
        RemoveViewmodelCamera();
        if (viewmodelCamera != null) Destroy(viewmodelCamera.gameObject);
    }

}
