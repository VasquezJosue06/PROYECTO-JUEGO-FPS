using UnityEngine;

// Define las estadisticas y armas de una clase. La vida y municion actuales pertenecen al jugador.
[CreateAssetMenu(fileName = "NewClass", menuName = "FPS/Classes")]
public class ClasesJugador : ScriptableObject
{
    public string nombreVisible = "New class";
    [Header("Estadisticas")]
    [Min(1)] public int vidaMaxima = 100;
    [Min(0)] public float velocidadMovimiento = 5f;
    [Min(0)] public float fuerzaSalto = 5f;
    [Tooltip("Saltos adicionales antes de aterrizar. Scout: 1.")]
    [Min(0)] public int saltosAereos;
    [Min(0)] public float velocidadSaltoAereo = 5f;
    [Tooltip("0 usa la gravedad del proyecto. Valor positivo: aceleracion hacia abajo.")]
    [Min(0)] public float gravedad;
    [Range(0f, 1f)] public float multiplicadorVelocidadAtras = 1f;
    [Header("Armas iniciales y permitidas")]
    public Arma armaPrincipal;
    public Arma armaSecundaria;
    [Tooltip("Otros prefabs que esta clase puede recoger.")]
    [SerializeField] private Arma[] armasAdicionales = new Arma[0];

    public bool PermiteArma(Arma prefabricado)
    {
        if (prefabricado == null) return false;
        if (prefabricado == armaPrincipal || prefabricado == armaSecundaria) return true;
        if (armasAdicionales != null)
            foreach (Arma permitida in armasAdicionales)
                if (permitida == prefabricado) return true;
        return false;
    }
}
