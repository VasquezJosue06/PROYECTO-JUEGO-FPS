using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerHealth), typeof(PlayerMove), typeof(PlayerShoothing))]
public class PlayerClass : MonoBehaviour
{
    [SerializeField, Tooltip("Estadisticas y armas del jugador. Cambiar durante Play reinicia vida y arsenal.")]
    private Classes selectedClass;

    public Classes SelectedClass => selectedClass;
    public Classes CurrentClass { get; private set; }
    private PlayerHealth health;
    private PlayerMove movement;
    private PlayerShoothing shooting;

    private void Awake()
    {
        health = GetComponent<PlayerHealth>();
        movement = GetComponent<PlayerMove>();
        shooting = GetComponent<PlayerShoothing>();
    }

    private void Start()
    {
        if (selectedClass != null) SetClass(selectedClass);
    }

    private void Update()
    {
        // Apply Inspector changes on the main thread, never inside OnValidate.
        if (selectedClass != CurrentClass && !SetClass(selectedClass))
            selectedClass = CurrentClass;
    }

    public bool SetClass(Classes newClass)
    {
        if (newClass == null || !isActiveAndEnabled || health == null || health.health <= 0)
            return false;
        if ((newClass.primaryWeapon != null || newClass.secondaryWeapon != null) && shooting.gunHolder == null)
        {
            Debug.LogError("Asigna Gun Holder antes de seleccionar una clase.", this);
            return false;
        }
        if (newClass == CurrentClass)
        {
            selectedClass = newClass;
            return true;
        }

        health.ApplyMaxHealth(newClass.maxHealth);
        movement.ApplyClass(newClass);
        shooting.ApplyClass(newClass);
        CurrentClass = newClass;
        selectedClass = newClass;
        return true;
    }
}
