using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShoothing : MonoBehaviour
{
    // PlayerClass aplica la clase; este script no modifica su asset.
    private Classes currentClass;
    [Header("Arma equipada")]
    public Gun gun;
    public Transform gunHolder;
    // Compatibilidad con escenas que aun no tienen una clase asignada.
    [SerializeField, HideInInspector] private Gun startingWeapon;
    [SerializeField, HideInInspector] private Gun secondaryWeapon;
    private bool started;
    private readonly Gun[] weapons = new Gun[2];
    private int activeSlot;
    private int previousSlot = -1;
    private bool isHoldingShoot;
    private PlayerHealth playerHealth;
    private bool CanUseWeapons => isActiveAndEnabled &&
        (playerHealth == null || playerHealth.health > 0);
    private InputAction keyboardChange;
    private InputAction mouseChange;
    private InputAction previousChange;
    private InputAction spinAction;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
    }

    void Start()
    {
        started = true;
        // Si hay una clase, PlayerClass se encarga de equipar las armas.
        PlayerClass controller = GetComponent<PlayerClass>();
        if (controller != null && controller.isActiveAndEnabled && controller.SelectedClass != null)
        {
            BindSwitchInputs();
            return;
        }
        weapons[0] = gun;
        if (weapons[0] == null && startingWeapon != null && gunHolder != null)
            weapons[0] = Instantiate(startingWeapon, gunHolder);
        gun = weapons[0];
        if (gun == null) SelectWeapon(1);
        BindSwitchInputs();
    }

    private void BindSwitchInputs()
    {
        UnbindSwitchInputs();
        UnityEngine.InputSystem.PlayerInput input = GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (input == null || input.actions == null) return;
        keyboardChange = input.actions.FindAction("OnFoot/Change_K", false);
        mouseChange = input.actions.FindAction("OnFoot/change_M", false);
        previousChange = input.actions.FindAction("OnFoot/Change_Q", false);
        if (previousChange != null) previousChange.performed += ChangeToPrevious;
        if (keyboardChange != null) keyboardChange.performed += ChangeByKeyboard;
        if (mouseChange != null) mouseChange.performed += ChangeByMouse;
        spinAction = new InputAction("MinigunSpin", InputActionType.Button, "<Mouse>/rightButton");
        spinAction.AddBinding("<Gamepad>/leftTrigger");
        spinAction.Enable();
    }

    public void ApplyClass(Classes configuration)
    {
        isHoldingShoot = false;
        // Guarda el arma anterior y cancela su recarga antes de cambiar de clase.
        if (gun != null && gun != weapons[0] && gun != weapons[1])
        {
            gun.gameObject.SetActive(false);
            Destroy(gun.gameObject);
        }
        for (int slot = 0; slot < weapons.Length; slot++)
        {
            if (weapons[slot] != null)
            {
                weapons[slot].gameObject.SetActive(false);
                Destroy(weapons[slot].gameObject);
                weapons[slot] = null;
            }
        }
        gun = null;
        activeSlot = 0;
        previousSlot = -1;
        currentClass = configuration;
        startingWeapon = configuration.primaryWeapon;
        secondaryWeapon = configuration.secondaryWeapon;
        SelectWeapon(startingWeapon != null ? 0 : 1);
        if (gun == null && UiManager.Instance != null && UiManager.Instance.ammoText != null)
            UiManager.Instance.ammoText.text = "";
    }

    private void UnbindSwitchInputs()
    {
        spinAction?.Dispose();
        spinAction = null;
        if (previousChange != null) previousChange.performed -= ChangeToPrevious;
        if (keyboardChange != null) keyboardChange.performed -= ChangeByKeyboard;
        if (mouseChange != null) mouseChange.performed -= ChangeByMouse;
    }

    private void OnEnable()
    {
        // La primera vez se enlazan los controles en Start.
        if (started) BindSwitchInputs();
    }

    private void OnDisable()
    {
        if (gun != null) gun.UpdateMinigunInput(false, false);
        UnbindSwitchInputs();
        isHoldingShoot = false;
    }

    private void ChangeByKeyboard(InputAction.CallbackContext context)
    {
        if (context.control.name == "1") SelectWeapon(0);
        else if (context.control.name == "2") SelectWeapon(1);
    }

    private void ChangeByMouse(InputAction.CallbackContext context)
    {
        float scroll = context.ReadValue<float>();
        if (Mathf.Approximately(scroll, 0f)) return;
        int direction = scroll > 0f ? -1 : 1;
        for (int step = 1; step <= weapons.Length; step++)
        {
            int slot = (activeSlot + direction * step + weapons.Length) % weapons.Length;
            if (HasWeapon(slot))
            {
                SelectWeapon(slot);
                return;
            }
        }
    }

    private void ChangeToPrevious(InputAction.CallbackContext context)
    {
        if (previousSlot >= 0) SelectWeapon(previousSlot);
    }

    private bool HasWeapon(int slot)
    {
        return weapons[slot] != null || (slot == 0 ? startingWeapon != null : secondaryWeapon != null);
    }

    public void SelectWeapon(int slot)
    {
        if (!CanUseWeapons) return;
        if (gun != null && !gun.CanSwitch) return;
        if (slot < 0 || slot >= weapons.Length || !HasWeapon(slot)) return;
        if (gun != null && gun == weapons[slot]) return;
        if (weapons[slot] == null)
        {
            if (gunHolder == null) return;
            // Start se ejecuta antes del primer disparo; no hace falta un objeto temporal.
            Gun prefab = slot == 0 ? startingWeapon : secondaryWeapon;
            weapons[slot] = Instantiate(prefab, gunHolder);
        }
        // Guardar el arma limpia su camara, efectos y recarga pendiente.
        if (gun != null)
        {
            previousSlot = activeSlot;
            gun.gameObject.SetActive(false);
        }
        activeSlot = slot;
        gun = weapons[slot];
        gun.gameObject.SetActive(true);
    }

    void OnShoot()
    {
        isHoldingShoot = CanUseWeapons;
    }
    void OnShootRelease()
    {
        isHoldingShoot = false;
    }
    void OnReload()
    {
        if (!CanUseWeapons) return;
        if (gun != null && !gun.CanSwitch) return;
        if (gun != null) gun.TryReload();
    }

    void Update()
    {
        // Al morir se ignoran los controles aunque Update siga ejecutandose.
        if (!CanUseWeapons)
        {
            isHoldingShoot = false;
            if (gun != null) gun.UpdateMinigunInput(false, false);
            return;
        }
        if (gun != null) gun.UpdateMinigunInput(isHoldingShoot, spinAction != null && spinAction.IsPressed());
        if (isHoldingShoot && gun != null) gun.Shoot();
    }

    public void OnDrop()
    {
        if (!CanUseWeapons) return;
        if (gun != null && !gun.CanSwitch) return;
        if (gun == null || !gun.Drop()) return;
        weapons[activeSlot] = null;
        if (activeSlot == 0) startingWeapon = null;
        else secondaryWeapon = null;
        gun = null;
    }

    public bool CanPickUpWeapon(Gun prefab)
    {
        return CanUseWeapons && gunHolder != null && prefab != null &&
            (currentClass == null || currentClass.AllowsWeapon(prefab));
    }

    public bool EquipPickedUpWeapon(Gun newWeapon, Gun sourcePrefab)
    {
        if (newWeapon == null || !CanPickUpWeapon(sourcePrefab)) return false;
        weapons[activeSlot] = newWeapon;
        gun = newWeapon;
        return true;
    }
}
