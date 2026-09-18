using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int health = 100;
    public int MaxHealth { get; private set; } = 100;

    public void ApplyMaxHealth(int value)
    {
        MaxHealth = Mathf.Max(1, value);
        health = MaxHealth;
        if (UiManager.Instance != null) UiManager.Instance.SetHealthValue(health, MaxHealth);
    }

    public AudioClip hitSFX;

    void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag == "Damage")
        {
            DecreaseHealth(10);
        }
    }

    private void DecreaseHealth(int decreaseAmount)
    {
        health -= decreaseAmount;
        PlayerLoock.Instance.AddShake(0.1f, 0.25f);
        UiManager.Instance.InstatiateHitUi();
        AudioManager.Instance.PlaySFX(hitSFX);
        UiManager.Instance.SetHealthValue(health, MaxHealth);

        if(health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Time.timeScale = 0f;
        UiManager.Instance.EnableDeathUi();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
