using UnityEngine;

public class UiManager : MonoBehaviour
{
    public static UiManager Instance;

    public GameObject hitUi;

    private void Awake()
    {
        Instance = this;
    }

    public void InstatiateHitUi()
    {
        Instantiate(hitUi, transform);
    }
}
