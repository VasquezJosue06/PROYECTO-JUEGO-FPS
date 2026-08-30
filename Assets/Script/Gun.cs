using UnityEngine;
using System.Collections;

public class Gun : MonoBehaviour
{
    [Header("Disparo")]
    public float reloadTime = 1f;
    public float fireRate = 0.15f;
    public int maxSize = 20;

    public AudioClip shootingSFX;

    public GameObject bullet;
    public Transform bulletSpawnPoint;

    [Header("Efectos")]
    public GameObject weaponFlash;
    public GameObject droppedWeapon;

    public float recoilDistance = 0.1f;
    public float recoilSpeed = 15f;

    // Estado interno del cargador, la recarga y la cadencia de disparo.
    private int currentAmmo;
    private bool isReloading = false;
    private float nextTimetoFire = 0f;

    private Quaternion initialRotation;
    private Vector3 initialPosition;
    private Vector3 reloadRotationOffset = new Vector3(66, 50, 50);

    void Start()
    {
        // Se registra la posición inicial para restaurarla después de recargar o retroceder.
        currentAmmo = maxSize;
        initialRotation = transform.localRotation;
        initialPosition = transform.localPosition;
        UiManager.Instance.ammoText.text = currentAmmo.ToString();
    }

    public void Shoot()
    {
        // No permite disparar durante una recarga ni antes de cumplir la cadencia.
        if(isReloading) return;
        if(Time.time < nextTimetoFire) return;

        if(currentAmmo <= 0)
        {
            // La recarga automática evita disparos vacíos al mantener el botón presionado.
            StartCoroutine(Reload());
            return;
        }

        nextTimetoFire = Time.time + fireRate;
        currentAmmo--;
        UiManager.Instance.ammoText.text = currentAmmo.ToString();


        AudioManager.Instance.PlaySFX(shootingSFX, 0.25f);

        // Ajuste de orientación requerido por el eje local del prefab de bala.
        Quaternion adjustedRotation = bulletSpawnPoint.rotation * Quaternion.Euler(-1f, -1f, 0f);

        Instantiate(bullet, bulletSpawnPoint.position, adjustedRotation);
        Instantiate(weaponFlash, bulletSpawnPoint.position, bulletSpawnPoint.rotation);

        StopCoroutine(nameof(Recoil));
        StartCoroutine(nameof(Recoil));
    }

    IEnumerator Reload()
    {
        isReloading = true;

        // La animación manual gira el arma hacia abajo y después la devuelve a su posición.
        Quaternion targetRotation = Quaternion.Euler(initialRotation.eulerAngles + reloadRotationOffset);
        float halfReload = reloadTime / 2f;
        float t = 0f;

        while(t < halfReload)
        {
            t += Time.deltaTime;
            transform.localRotation = Quaternion.Slerp(initialRotation, targetRotation, t / halfReload);
            yield return null;
        }

        t = 0f;

        while(t < halfReload)
        {
            t += Time.deltaTime;
            transform.localRotation = Quaternion.Slerp(targetRotation, initialRotation, t / halfReload);
            yield return null;
        }

        currentAmmo = maxSize;
        UiManager.Instance.ammoText.text = currentAmmo.ToString();
        isReloading = false;
    }

    public void TryReload()
    {
        // Evita reiniciar una recarga ya activa o recargar un cargador completo.
        if (isReloading) return;
        if (currentAmmo == maxSize) return;

        StartCoroutine(Reload());
    }

    private IEnumerator Recoil()
    {
        // Retroceso visual local; no modifica la posición global del jugador ni la cámara.
        Vector3 recoilTarget = initialPosition + new Vector3(recoilDistance, 0, 0);
        float t = 0f;

        while(t < 1f)
        {
            t += Time.deltaTime * recoilSpeed;
            transform.localPosition = Vector3.Lerp(initialPosition, recoilTarget, t);
            yield return null;
        }

        t = 0f;

        while(t < 1f)
        {
            t += Time.deltaTime * recoilSpeed;
            transform.localPosition = Vector3.Lerp(recoilTarget, initialPosition, t);
            yield return null;
        }

        transform.localPosition = initialPosition;
    }

    public void Drop()
    {
        // Convierte el arma equipada en un pickup del mundo y limpia el contador de munición.
        UiManager.Instance.ammoText.text = "";
        Instantiate(droppedWeapon, transform.position, transform.rotation);
        Destroy(gameObject);
    }
}
