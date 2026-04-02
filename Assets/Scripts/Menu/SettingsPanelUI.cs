using UnityEngine;
using UnityEngine.UI;

public class SettingsPanelUI : MonoBehaviour
{
    [Header("Sliders")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    private void OnEnable()
    {
        if (SettingsManager.Instance == null) return;

        // Panel açýlýnca mevcut deðerleri göster
        if (masterSlider != null) masterSlider.SetValueWithoutNotify(SettingsManager.Instance.GetMaster());
        if (musicSlider != null) musicSlider.SetValueWithoutNotify(SettingsManager.Instance.GetMusic());
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(SettingsManager.Instance.GetSfx());
    }
}
