using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UiManager : MonoBehaviour
{
    public static UiManager Instance;

    public GameObject hitUi;

    public GameObject deathUi;
    [Tooltip("Se crea automáticamente copiando la pantalla de Game Over para reutilizar su diseño y botón de reinicio.")]
    public GameObject victoryUi;

    public TextMeshProUGUI ammoText;

    public Image healthBar;
    public Gradient healthGradiant;

    [Header("Dosier")]
    public Image dosierIcon;
    public TextMeshProUGUI dosierMessage;

    private void Awake()
    {
        Time.timeScale = 1.0f;

        Instance = this;
        CreateVictoryUiFromDeathScreen();
        if (dosierIcon != null) dosierIcon.gameObject.SetActive(false);
        if (dosierMessage != null) dosierMessage.gameObject.SetActive(false);
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
        HideGameplayHud();
        if (deathUi != null) deathUi.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void EnableVictoryUi()
    {
        HideGameplayHud();
        if (victoryUi == null) CreateVictoryUiFromDeathScreen();
        if (victoryUi != null) victoryUi.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void HideGameplayHud()
    {
        if (healthBar != null)
        {
            // In the current HUD the bar is inside its own background panel.
            GameObject healthRoot = healthBar.transform.parent != null
                ? healthBar.transform.parent.gameObject
                : healthBar.gameObject;
            healthRoot.SetActive(false);
        }
        if (ammoText != null) ammoText.gameObject.SetActive(false);
    }

    private void CreateVictoryUiFromDeathScreen()
    {
        if (victoryUi != null || deathUi == null) return;
        Transform parent = deathUi.transform.parent;
        victoryUi = Instantiate(deathUi, parent);
        victoryUi.name = "VictoryUi";
        victoryUi.SetActive(false);
        TMP_Text title = victoryUi.GetComponentInChildren<TMP_Text>(true);
        if (title != null) title.text = "VICTORIA";
    }

    public void SetHealthValue(int health, int maxHealth = 100)
    {
        if (healthBar == null) return;
        float floatHealth = Mathf.Clamp01((float)health / Mathf.Max(1, maxHealth));
        if (healthGradiant == null) return;
        healthBar.color = healthGradiant.Evaluate(floatHealth);
        healthBar.fillAmount = floatHealth;

    }

    public bool ShowDosierMessage(Sprite icon, string message, float duration)
    {
        bool displayed = false;
        if (dosierIcon != null)
        {
            dosierIcon.sprite = icon;
            dosierIcon.gameObject.SetActive(icon != null);
            displayed = icon != null;
        }
        if (dosierMessage != null)
        {
            dosierMessage.text = message;
            dosierMessage.gameObject.SetActive(!string.IsNullOrWhiteSpace(message));
            CancelInvoke(nameof(HideDosierMessage));
            if (duration > 0f) Invoke(nameof(HideDosierMessage), duration);
            displayed |= !string.IsNullOrWhiteSpace(message);
        }
        return displayed;
    }

    private void HideDosierMessage()
    {
        if (dosierMessage != null) dosierMessage.gameObject.SetActive(false);
    }
}
