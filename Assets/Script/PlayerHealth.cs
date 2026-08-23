using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int health = 100;

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

        if(health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Time.timeScale = 0f;
    }
}
