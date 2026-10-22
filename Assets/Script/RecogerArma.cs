using Unity.Mathematics;

using UnityEngine;
using UnityEngine.InputSystem;

public class RecogerArma : MonoBehaviour
{
    [SerializeField] private Material materialResaltado;
    private Material[] materialesOriginales;
    private MeshRenderer[] renderizadoresMalla;

    [SerializeField] private GameObject prefabricadoArma;
    [SerializeField] private float alcanceMirada = 3f;

    private bool estaMirado = false;
    private Camera camaraJugador;
    private DisparoJugador jugador;

    void Start()
    {
        renderizadoresMalla = GetComponentsInChildren<MeshRenderer>();
        materialesOriginales = new Material[renderizadoresMalla.Length];
        for(int i = 0; i< renderizadoresMalla.Length; i++)
        {
            materialesOriginales[i] = renderizadoresMalla[i].material;
        }

        jugador = FindAnyObjectByType<DisparoJugador>();
        camaraJugador = jugador.GetComponentInChildren<Camera>();
    }

    void Update()
    {
        Ray rayo = new Ray(camaraJugador.transform.position, camaraJugador.transform.forward);
        if(Physics.Raycast(rayo, out RaycastHit impacto, alcanceMirada))
        {
            if(impacto.collider.GetComponentInParent<RecogerArma>() == this)
            {
                if(!estaMirado)
                    EstablecerMirado(true);

                return;
            }
        }

        if(estaMirado)
            EstablecerMirado(false);
    }

    void EstablecerMirado(bool estaMirado)
    {
        this.estaMirado = estaMirado;

        if(estaMirado)
        {
            foreach(MeshRenderer renderizadorMalla in renderizadoresMalla)
            {
                renderizadorMalla.material = materialResaltado;
            }
        }
        else
        {
            for (int i = 0; i < renderizadoresMalla.Length; i++)
            {
                renderizadoresMalla[i].material = materialesOriginales[i];
            }
        }
    }

    public void OnPickUp()
    {
        if(!estaMirado) return;

        Arma prefabricado = prefabricadoArma != null ? prefabricadoArma.GetComponent<Arma>() : null;
        if (jugador == null || !jugador.PuedeRecogerArma(prefabricado)) return;

        jugador.OnDrop();
        if (jugador.armaActual != null) return;

        GameObject armaNueva = Instantiate(prefabricadoArma, jugador.soporteArma);
        armaNueva.transform.localPosition = Vector3.zero;
        armaNueva.transform.localRotation = Quaternion.identity;

        if (!jugador.EquiparArmaRecogida(armaNueva.GetComponent<Arma>(), prefabricado))
        {
            Destroy(armaNueva);
            return;
        }

        Destroy(gameObject);

    }
}
