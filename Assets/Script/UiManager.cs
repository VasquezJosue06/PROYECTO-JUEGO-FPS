using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UiManager : MonoBehaviour
{
    // Instancia compartida para que los demás sistemas actualicen la interfaz.
    public static UiManager Instance;

    [Header("Feedback visual")]
    public GameObject hitUi;
    public GameObject deathUi;

    [Header("Estado del jugador")]
    public TextMeshProUGUI ammoText;
    public Image healthBar;
    public Gradient healthGradiant;

    private void Awake()
    {
        // Cada carga de escena comienza con el juego activo.
        Time.timeScale = 1.0f;
        Instance = this;
    }

    public void InstatiateHitUi()
    {
        // Crea un indicador breve cuando el jugador recibe daño.
        Instantiate(hitUi, transform);
    }

    public void Restart()
    {
        // Reinicia la escena que está actualmente activa.
        Time.timeScale = 1.0f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void EnableDeathUi()
    {
        deathUi.SetActive(true);
    }

    public void SetHealthValue(int health)
    {
        // Mantiene el valor de la interfaz dentro del rango válido de 0 a 100.
        float floatHealth = Mathf.Clamp01((float)health / 100);
        healthBar.color = healthGradiant.Evaluate(floatHealth);
        healthBar.fillAmount = floatHealth;
    }
}
