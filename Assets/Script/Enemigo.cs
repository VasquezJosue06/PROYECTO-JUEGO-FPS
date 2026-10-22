using UnityEngine;
using System.Collections;
using UnityEngine.AI;
using System.Linq;

public class Enemigo : MonoBehaviour
{
    public int vida = 100;

    [SerializeField] private GameObject prefabricadoProyectil;
    public Transform puntoGeneracionProyectil;
    [SerializeField] private GameObject fogonazoArma;
    [SerializeField] private float dispersion;
    public float frecuenciaDisparo;
    private float ultimoDisparoEn = 0f;
    [SerializeField] private Material materialImpacto;

    public AudioClip sonidoDisparo;
    private Renderer renderizador;
    private Material materialOriginal;

    //AI Setings
    [SerializeField] private int indicePuntoActual = 0;
    [SerializeField] private Vector3 objetivoActual;
    [SerializeField] private float distanciaCambioObjetivo;
    [SerializeField] private float tiempoInactivo = 5f;
    [SerializeField] private float distanciaAtaque = 5f;
    [SerializeField] private float distanciaMaximaVision = 20f;
    [SerializeField] private float vidaMinimaPersecucion = 30f;

    [SerializeField] private Transform[] puntosPatrulla;
    private float contadorTiempoInactivo;
    private Transform transformacionJugador;
    private bool puedeVerJugador;
    private Vector3 ultimaPosicionConocidaJugador;

    private NavMeshAgent agenteNavegacion;

    public enum EstadoEnemigo { EsperarInactivo, Patrullar, Perseguir, Atacar }
    [SerializeField] private EstadoEnemigo estado = EstadoEnemigo.EsperarInactivo;

    private Rigidbody cuerpoRigido;

    void Start()
    {
        cuerpoRigido = GetComponent<Rigidbody>();
        renderizador = GetComponentInChildren<Renderer>();
        if (renderizador != null) materialOriginal = renderizador.material;

        agenteNavegacion = GetComponent<NavMeshAgent>();
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null) transformacionJugador = playerObject.transform;

        GameObject patrolPointParent = GameObject.FindWithTag("PatrolPoint");
        puntosPatrulla = patrolPointParent != null
            ? patrolPointParent.GetComponentsInChildren<Transform>().Where(t => t != patrolPointParent.transform).ToArray()
            : new Transform[0];
        if (puntosPatrulla.Length > 0)
        {
            indicePuntoActual = Mathf.Clamp(indicePuntoActual, 0, puntosPatrulla.Length - 1);
            objetivoActual = puntosPatrulla[indicePuntoActual].position;
        }
        if (agenteNavegacion == null) Debug.LogWarning($"Enemy '{name}' has no NavMeshAgent; AI movement is disabled.", this);
        if (transformacionJugador == null) Debug.LogWarning($"Enemy '{name}' could not find an object tagged Player.", this);
        // Patrol routes are optional; without one this enemy guards its spawn position.
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag == "Damage")
        {
            RecibirDanio(10);
        }
    }

    public void RecibirDanio(int cantidad)
    {
        if (vida <= 0 || cantidad <= 0) return;
        vida -= cantidad;
        if (vida <= 0) Morir();
        else if (renderizador != null && materialImpacto != null) StartCoroutine(Parpadear());
    }

    void Morir()
    {
        if (GestorInterfaz.Instancia != null) GestorInterfaz.Instancia.RegistrarEnemigoEliminado();
        Destroy(gameObject);
    }

    IEnumerator Parpadear()
    {
        renderizador.material = materialImpacto;
        yield return new WaitForSeconds(0.1f);
        renderizador.material = materialOriginal;
    }

    void Update()
    {
        if (transformacionJugador == null) return;
        BuscarJugador();

        switch (estado)
        {
            case EstadoEnemigo.EsperarInactivo:
                EsperarInactivo();
                break;
            case EstadoEnemigo.Patrullar:
                Patrullar();
                break;
            case EstadoEnemigo.Atacar:
                Atacar();
                break;
            case EstadoEnemigo.Perseguir:
                Perseguir();
                break;
        }

        if (cuerpoRigido != null) cuerpoRigido.linearVelocity = Vector3.zero;

        MirarAlJugador();
        GuardarUltimaPosicionJugador();
    }

    private void BuscarJugador()
    {
        Vector3 direccionAlJugador = transformacionJugador.position - transform.position;

        if(Physics.Raycast(transform.position, direccionAlJugador, out RaycastHit impacto, distanciaMaximaVision))
        {
            puedeVerJugador = impacto.transform == transformacionJugador;

            if(puedeVerJugador && estado != EstadoEnemigo.Atacar)
            {
                estado = EstadoEnemigo.Perseguir;
            }
        }
    }

    private void EsperarInactivo()
    {
        if (agenteNavegacion != null && agenteNavegacion.isOnNavMesh) agenteNavegacion.ResetPath();

        contadorTiempoInactivo -= Time.deltaTime;

        if(contadorTiempoInactivo < 0)
        {
            estado = EstadoEnemigo.Patrullar;
            contadorTiempoInactivo = tiempoInactivo;
        }
    }
    private void Patrullar()
    {
        if (puntosPatrulla == null || puntosPatrulla.Length == 0 || agenteNavegacion == null || !agenteNavegacion.isOnNavMesh) return;
        if(Vector3.Distance(objetivoActual, transform.position) < distanciaCambioObjetivo)
        {
            float chance = Random.Range(0, 100);

            if(chance < 10)
            {
                estado = EstadoEnemigo.EsperarInactivo;
                return;
            }

            indicePuntoActual++;
            objetivoActual = puntosPatrulla[indicePuntoActual % puntosPatrulla.Length].position;
        }
        else
        {
            agenteNavegacion.SetDestination(objetivoActual);
        }
    }
    private void Atacar()
    {
        contadorTiempoInactivo = tiempoInactivo;
        if (agenteNavegacion != null && agenteNavegacion.isOnNavMesh) agenteNavegacion.ResetPath();

        Disparar();

        if(Vector3.Distance(transform.position, transformacionJugador.position) > distanciaAtaque || !puedeVerJugador)
        {
            if(vida < vidaMinimaPersecucion)
            {
                estado = EstadoEnemigo.Patrullar; //Cautios
            }
            else
            {
                estado = EstadoEnemigo.Perseguir;
            }
        } 
    }
    private void Perseguir()
    {
        contadorTiempoInactivo = tiempoInactivo;
        if (agenteNavegacion == null || !agenteNavegacion.isOnNavMesh) return;
        agenteNavegacion.SetDestination(ultimaPosicionConocidaJugador);

        if(vida < vidaMinimaPersecucion)
        {
            estado = EstadoEnemigo.Patrullar;
        }
        else if(Vector3.Distance(transform.position, transformacionJugador.position) <= distanciaAtaque && puedeVerJugador)
        {
            estado = EstadoEnemigo.Atacar;
        }
        else if(Vector3.Distance(transform.position, transformacionJugador.position) < distanciaMaximaVision)
        {
            estado = EstadoEnemigo.Patrullar;
        }
        else if(Vector3.Distance(transform.position, transformacionJugador.position) > distanciaCambioObjetivo && !puedeVerJugador)
        {
            estado = EstadoEnemigo.Patrullar;
        }
    }

    private void MirarAlJugador()
    {
        if(puedeVerJugador)
        {
            transform.LookAt(new Vector3(transformacionJugador.position.x, transform.position.y, transformacionJugador.position.z));
        }
    }
    private void GuardarUltimaPosicionJugador()
    {
        if(puedeVerJugador)
        {
            ultimaPosicionConocidaJugador = transformacionJugador.position;
        }
    }

    private void Disparar()
    {
        if (prefabricadoProyectil == null || puntoGeneracionProyectil == null) return;
        if(Time.time > ultimoDisparoEn + frecuenciaDisparo)
        {
            Vector3 direccionAlJugador = transformacionJugador.position - transform.position;
            direccionAlJugador.Normalize();

            Quaternion rotacionProyectil = Quaternion.LookRotation(direccionAlJugador);

            float dispersionMaxima = 10f;
            float dispersionActual = dispersion + dispersionMaxima;
            float giroAleatorio = Random.Range(-dispersionActual, dispersionActual);
            float inclinacionAleatoria = Random.Range(-dispersionActual, dispersionActual);

            rotacionProyectil *= Quaternion.Euler(inclinacionAleatoria, giroAleatorio + 90, 0f);

            GestorAudio.Instancia.ReproducirEfectoSonoro(sonidoDisparo, 0.5f);

            Instantiate(prefabricadoProyectil, puntoGeneracionProyectil.position, rotacionProyectil);
            Instantiate(fogonazoArma, puntoGeneracionProyectil.position, puntoGeneracionProyectil.rotation);
            ultimoDisparoEn = Time.time;
        }
    }

}
