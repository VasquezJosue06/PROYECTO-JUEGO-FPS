using UnityEngine;
<<<<<<< HEAD

public class PlayerShoothing : MonoBehaviour
{
    [Header("Arma equipada")]
    public Gun gun;
    public Transform gunHolder;

    // Se mantiene mientras el jugador sostiene el botón de disparo.
    private bool isHoldingShoot = false;

    // Evento del Input System al presionar disparar.
=======
using UnityEngine.InputSystem;

public class PlayerShoothing : MonoBehaviour
{
    public Gun gun;
    public Transform gunHolder;
    private bool isHoldingShoot = false;

>>>>>>> Develop
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
<<<<<<< HEAD
            gun.TryReload();
=======
           gun.TryReload(); 
>>>>>>> Develop
        }
    }

    void Update()
    {
<<<<<<< HEAD
        // Gun controla la cadencia; este script solo comunica la intención del jugador.
=======
>>>>>>> Develop
        if (isHoldingShoot && gun != null)
        {
            gun.Shoot();
        }
    }

    public void OnDrop()
    {
<<<<<<< HEAD
        if (gun != null)
=======
        if(gun != null)
>>>>>>> Develop
        {
            gun.Drop();
            gun = null;
        }
    }
}
