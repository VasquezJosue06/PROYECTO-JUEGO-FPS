using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
<<<<<<< HEAD
    [Header("Salud")]
    public int health = 100;

    [Header("Audio")]
=======
    public int health = 100;

>>>>>>> Develop
    public AudioClip hitSFX;

    void OnCollisionEnter(Collision collision)
    {
<<<<<<< HEAD
        if (collision.gameObject.CompareTag("Damage"))
=======
        if(collision.gameObject.tag == "Damage")
>>>>>>> Develop
        {
            DecreaseHealth(10);
        }
    }

    private void DecreaseHealth(int decreaseAmount)
    {
<<<<<<< HEAD
        // Evita que el valor de vida y la barra de UI sean negativos.
        health = Mathf.Max(health - decreaseAmount, 0);
=======
        health -= decreaseAmount;
>>>>>>> Develop
        PlayerLoock.Instance.AddShake(0.1f, 0.25f);
        UiManager.Instance.InstatiateHitUi();
        AudioManager.Instance.PlaySFX(hitSFX);
        UiManager.Instance.SetHealthValue(health);

<<<<<<< HEAD
        if (health <= 0)
=======
        if(health <= 0)
>>>>>>> Develop
        {
            Die();
        }
    }

    private void Die()
    {
<<<<<<< HEAD
        // Pausa el juego, muestra el menú y libera el cursor para usar la interfaz.
=======
>>>>>>> Develop
        Time.timeScale = 0f;
        UiManager.Instance.EnableDeathUi();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
