using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Salud")]
    public int health = 100;

    [Header("Audio")]
    public AudioClip hitSFX;

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Damage"))
        {
            DecreaseHealth(10);
        }
    }

    private void DecreaseHealth(int decreaseAmount)
    {
        // Evita que el valor de vida y la barra de UI sean negativos.
        health = Mathf.Max(health - decreaseAmount, 0);
        PlayerLoock.Instance.AddShake(0.1f, 0.25f);
        UiManager.Instance.InstatiateHitUi();
        AudioManager.Instance.PlaySFX(hitSFX);
        UiManager.Instance.SetHealthValue(health);

        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // Pausa el juego, muestra el menú y libera el cursor para usar la interfaz.
        Time.timeScale = 0f;
        UiManager.Instance.EnableDeathUi();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
