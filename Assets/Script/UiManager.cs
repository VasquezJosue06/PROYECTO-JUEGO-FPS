using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UiManager : MonoBehaviour
{
    public static UiManager Instance;

    public GameObject hitUi;

    public GameObject deathUi;

    public TextMeshProUGUI ammoText;

    public Image healthBar;
    public Gradient healthGradiant;

    private void Awake()
    {
        Time.timeScale = 1.0f;

        Instance = this;
    }

    public void InstatiateHitUi()
    {
        Instantiate(hitUi, transform);
    }

    public void Restart()
    {
        Time.timeScale = 1.0f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void EnableDeathUi()
    {
        deathUi.SetActive(true);
    }

    public void SetHealthValue(int health, int maxHealth = 100)
    {
        float floatHealth = Mathf.Clamp01((float)health / Mathf.Max(1, maxHealth));
        healthBar.color = healthGradiant.Evaluate(floatHealth);
        healthBar.fillAmount = floatHealth;

    }
}
