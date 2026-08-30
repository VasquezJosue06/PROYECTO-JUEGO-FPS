using UnityEngine;

public class PlayerShoothing : MonoBehaviour
{
    [Header("Arma equipada")]
    public Gun gun;
    public Transform gunHolder;

    // Se mantiene mientras el jugador sostiene el botón de disparo.
    private bool isHoldingShoot = false;

    // Evento del Input System al presionar disparar.
    void OnShoot()
    {
        isHoldingShoot = true;
    }

    void OnShootRelease()
    {
        isHoldingShoot = false;
    }

    void OnReload()
    {
        if (gun != null)
        {
            gun.TryReload();
        }
    }

    void Update()
    {
        // Gun controla la cadencia; este script solo comunica la intención del jugador.
        if (isHoldingShoot && gun != null)
        {
            gun.Shoot();
        }
    }

    public void OnDrop()
    {
        if (gun != null)
        {
            gun.Drop();
            gun = null;
        }
    }
}
