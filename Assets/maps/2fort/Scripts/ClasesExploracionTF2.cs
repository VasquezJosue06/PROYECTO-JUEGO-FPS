using UnityEngine;
using UnityEngine.InputSystem;
// Usa el controlador de clase y los assets existentes para explorar el mapa.
[RequireComponent(typeof(ClaseJugador))]
[DisallowMultipleComponent]
public sealed class ClasesExploracionTF2 : MonoBehaviour
{
    public ClasesJugador claseScout;
    public ClasesJugador claseHeavy;
    [SerializeField] private bool mostrarControles = true;
    ClaseJugador controladorClase;
    void Awake() { controladorClase = GetComponent<ClaseJugador>(); }
    void Update()
    {
        if (Time.timeScale <= 0 || Keyboard.current == null) return;
        if (Keyboard.current.f1Key.wasPressedThisFrame && claseScout != null) controladorClase.SeleccionarClase(claseScout);
        if (Keyboard.current.f2Key.wasPressedThisFrame && claseHeavy != null) controladorClase.SeleccionarClase(claseHeavy);
    }
    void OnGUI()
    {
        if (!mostrarControles) return;
        string seleccionada = controladorClase != null && controladorClase.ClaseActual != null ? controladorClase.ClaseActual.nombreVisible : "...";
        GUI.Box(new Rect(12, 12, 330, 52), "Explorar 2Fort | Clase: " + seleccionada + "\nF1: Scout   F2: Heavy | Cambiar reinicia vida y armas");
    }
}
