using UnityEngine;
using UnityEngine.InputSystem;
// Uses the existing class controller and class assets; intended for map exploration.
[RequireComponent(typeof(PlayerClass))]
[DisallowMultipleComponent]
public sealed class TF2ExplorationClasses : MonoBehaviour
{
    public Classes scout;
    public Classes heavy;
    public bool showControls = true;
    PlayerClass controller;
    void Awake() { controller = GetComponent<PlayerClass>(); }
    void Update()
    {
        if (Time.timeScale <= 0 || Keyboard.current == null) return;
        if (Keyboard.current.f1Key.wasPressedThisFrame && scout != null) controller.SetClass(scout);
        if (Keyboard.current.f2Key.wasPressedThisFrame && heavy != null) controller.SetClass(heavy);
    }
    void OnGUI()
    {
        if (!showControls) return;
        string selected = controller != null && controller.CurrentClass != null ? controller.CurrentClass.displayName : "...";
        GUI.Box(new Rect(12, 12, 330, 52), "Explorar 2Fort | Clase: " + selected + "\nF1: Scout   F2: Heavy | Cambiar reinicia vida y armas");
    }
}
