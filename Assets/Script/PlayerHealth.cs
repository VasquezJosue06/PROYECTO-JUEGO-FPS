using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int health = 100;

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
        UiManager.Instance.SetHealthValue(health);

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
