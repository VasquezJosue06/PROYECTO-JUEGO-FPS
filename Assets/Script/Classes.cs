using UnityEngine;

// Define las estadisticas y armas de una clase. La vida y municion actuales pertenecen al jugador.
[CreateAssetMenu(fileName = "NewClass", menuName = "FPS/Classes")]
public class Classes : ScriptableObject
{
    public string displayName = "New class";
    [Header("Estadisticas")]
    [Min(1)] public int maxHealth = 100;
    [Min(0)] public float moveSpeed = 5f;
    [Min(0)] public float jumpForce = 5f;
    [Tooltip("Saltos adicionales antes de aterrizar. Scout: 1.")]
    [Min(0)] public int airJumps;
    [Min(0)] public float airJumpSpeed = 5f;
    [Tooltip("0 usa la gravedad del proyecto. Valor positivo: aceleracion hacia abajo.")]
    [Min(0)] public float gravity;
    [Range(0f, 1f)] public float backwardSpeedMultiplier = 1f;
    [Header("Armas iniciales y permitidas")]
    public Gun primaryWeapon;
    public Gun secondaryWeapon;
    [Tooltip("Otros prefabs que esta clase puede recoger.")]
    public Gun[] additionalWeapons = new Gun[0];

    public bool AllowsWeapon(Gun prefab)
    {
        if (prefab == null) return false;
        if (prefab == primaryWeapon || prefab == secondaryWeapon) return true;
        if (additionalWeapons != null)
            foreach (Gun allowed in additionalWeapons)
                if (allowed == prefab) return true;
        return false;
    }
}
