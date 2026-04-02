using UnityEngine;
using UnityEngine.Audio;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    public const string MASTER_KEY = "vol_master";
    public const string SFX_KEY = "vol_sfx";
    public const string MUSIC_KEY = "vol_music";

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer mixer;

    private const string MASTER_PARAM = "MasterVol";
    private const string SFX_PARAM = "SfxVol";
    private const string MUSIC_PARAM = "MusicVol";

    private void Awake()
    {
        // ✅ Tek kopya + sahneler arası kalıcı
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        ApplyAll();
    }

    // ---------------- PUBLIC (Slider'dan çağrılır) ----------------

    public void SetMaster(float v)
    {
        PlayerPrefs.SetFloat(MASTER_KEY, v);
        PlayerPrefs.Save();
        ApplyVolume(MASTER_PARAM, v);
    }

    public void SetSfx(float v)
    {
        PlayerPrefs.SetFloat(SFX_KEY, v);
        PlayerPrefs.Save();
        ApplyVolume(SFX_PARAM, v);
    }

    public void SetMusic(float v)
    {
        PlayerPrefs.SetFloat(MUSIC_KEY, v);
        PlayerPrefs.Save();
        ApplyVolume(MUSIC_PARAM, v);
    }

    // ✅ Sliderları doldurmak için getter'lar
    public float GetMaster() => PlayerPrefs.GetFloat(MASTER_KEY, 1f);
    public float GetSfx() => PlayerPrefs.GetFloat(SFX_KEY, 1f);
    public float GetMusic() => PlayerPrefs.GetFloat(MUSIC_KEY, 1f);

    // ---------------- INTERNAL ----------------

    public void ApplyAll()
    {
        ApplyVolume(MASTER_PARAM, GetMaster());
        ApplyVolume(SFX_PARAM, GetSfx());
        ApplyVolume(MUSIC_PARAM, GetMusic());
    }

    private void ApplyVolume(string param, float linearValue)
    {
        float dB = linearValue <= 0.0001f
            ? -80f
            : Mathf.Log10(linearValue) * 20f;

        mixer.SetFloat(param, dB);
    }
}
