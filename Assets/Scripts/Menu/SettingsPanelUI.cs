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

        // Panel açılınca mevcut değerleri göster
        if (masterSlider != null)
        {
            masterSlider.SetValueWithoutNotify(SettingsManager.Instance.GetMaster());
            masterSlider.onValueChanged.RemoveAllListeners();
            masterSlider.onValueChanged.AddListener(SettingsManager.Instance.SetMaster);
        }
        if (musicSlider != null)
        {
            musicSlider.SetValueWithoutNotify(SettingsManager.Instance.GetMusic());
            musicSlider.onValueChanged.RemoveAllListeners();
            musicSlider.onValueChanged.AddListener(SettingsManager.Instance.SetMusic);
        }
        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(SettingsManager.Instance.GetSfx());
            sfxSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.onValueChanged.AddListener(SettingsManager.Instance.SetSfx);
        }
    }
}
