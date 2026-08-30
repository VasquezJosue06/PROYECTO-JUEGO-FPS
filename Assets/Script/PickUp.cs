using UnityEngine;

public class PickUp : MonoBehaviour
{
    [Header("Aspecto")]
    public Material highlightMaterial;
    private Material[] originalMaterials;
    private MeshRenderer[] meshRenderers;

    [Header("Arma que se recogerá")]
    public GameObject weaponPrefab;
    public float lookRange = 3f;

    private bool isLookedAt = false;
    private Camera playerCam;
    private PlayerShoothing player;

    void Start()
    {
        // Se guardan los materiales para restaurarlos al dejar de mirar el objeto.
        meshRenderers = GetComponentsInChildren<MeshRenderer>();
        originalMaterials = new Material[meshRenderers.Length];
        for(int i = 0; i< meshRenderers.Length; i++)
        {
            originalMaterials[i] = meshRenderers[i].material;
        }

        player = FindAnyObjectByType<PlayerShoothing>();
        playerCam = player.GetComponentInChildren<Camera>();
    }

    void Update()
    {
        // Un único raycast determina si este pickup es el que el jugador está mirando.
        Ray ray = new Ray(playerCam.transform.position, playerCam.transform.forward);
        if(Physics.Raycast(ray, out RaycastHit hit, lookRange))
        {
            if (hit.collider.GetComponentInParent<PickUp>() == this)
            {
                if (!isLookedAt)
                    SetLookedAt(true);

                return;
            }
        }

        if (isLookedAt)
            SetLookedAt(false);
    }

    void SetLookedAt(bool lookedAt)
    {
        isLookedAt = lookedAt;

        if (lookedAt)
        {
            foreach(MeshRenderer mr in meshRenderers)
            {
                mr.material = highlightMaterial;
            }
        }
        else
        {
            for (int i = 0; i < meshRenderers.Length; i++)
            {
                meshRenderers[i].material = originalMaterials[i];
            }
        }
    }

    public void OnPickUp()
    {
        // La acción solo se permite para el objeto actualmente resaltado.
        if(!isLookedAt) return;

        // El arma anterior se convierte en un pickup antes de equipar la nueva.
        player.OnDrop();

        GameObject newWeapon = Instantiate(weaponPrefab, player.gunHolder);
        newWeapon.transform.localPosition = Vector3.zero;
        newWeapon.transform.localRotation = Quaternion.identity;

        // La referencia permite a PlayerShoothing controlar el arma recién creada.
        player.gun = newWeapon.GetComponent<Gun>();

        Destroy(gameObject);

    }
}
