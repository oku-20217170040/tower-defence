using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// EnemyHealth değerini world-space bir Slider üzerinde gösterir.
/// Smooth lerp ile animasyonlu HP düşüşü.
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    public EnemyHealth enemyHealth;
    public Slider hpSlider;

    [Header("Smooth Fill")]
    [Tooltip("HP bar animasyon hızı. Yüksek = daha hızlı.")]
    public float lerpSpeed = 8f;

    [Header("Color Gradient")]
    [Tooltip("Dolu HP rengi (sağlıklı).")]
    public Color fullColor  = Color.green;
    [Tooltip("Boş HP rengi (kritik).")]
    public Color emptyColor = Color.red;

    private Image fillImage;
    private float displayValue;

    void Start()
    {
        if (enemyHealth == null || hpSlider == null) return;

        hpSlider.minValue = 0;
        hpSlider.maxValue = enemyHealth.MaxHp;
        hpSlider.value    = enemyHealth.MaxHp;
        displayValue      = enemyHealth.MaxHp;

        // Fill objesi varsa renk kontrolü için al
        if (hpSlider.fillRect != null)
            fillImage = hpSlider.fillRect.GetComponent<Image>();
    }

    void OnEnable()
    {
        // Havuzdan geri alındığında bar'ı anında full'a sıfırla
        if (enemyHealth != null && hpSlider != null)
        {
            hpSlider.maxValue = enemyHealth.MaxHp;
            hpSlider.value    = enemyHealth.MaxHp;
            displayValue      = enemyHealth.MaxHp;
        }
    }

    void Update()
    {
        if (enemyHealth == null || hpSlider == null) return;

        // maxHp wave multiplier ile değişebilir — güncel tut
        hpSlider.maxValue = enemyHealth.MaxHp;

        // Smooth lerp
        displayValue = Mathf.Lerp(displayValue, enemyHealth.CurrentHp, lerpSpeed * Time.deltaTime);
        hpSlider.value = displayValue;

        // Renk gradyanı
        if (fillImage != null)
        {
            float t = (enemyHealth.MaxHp > 0)
                ? (float)enemyHealth.CurrentHp / enemyHealth.MaxHp
                : 0f;
            fillImage.color = Color.Lerp(emptyColor, fullColor, t);
        }
    }
}
