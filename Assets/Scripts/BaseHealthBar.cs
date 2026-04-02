using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Base'in HP deðerini bir Slider üzerinden gösterir.
/// </summary>
public class BaseHealthBar : MonoBehaviour
{
    public BaseHealth baseHealth;
    public Slider hpSlider;

    private int maxHp;

    void Start()
    {
        if (baseHealth == null || hpSlider == null)
        {
            Debug.LogError("[BaseHealthBar] Referanslar eksik!");
            return;
        }

        maxHp = baseHealth.CurrentHp;
        hpSlider.minValue = 0;
        hpSlider.maxValue = maxHp;
        hpSlider.value = maxHp;
    }

    void Update()
    {
        if (baseHealth == null || hpSlider == null) return;

        hpSlider.value = baseHealth.CurrentHp;
    }
}
