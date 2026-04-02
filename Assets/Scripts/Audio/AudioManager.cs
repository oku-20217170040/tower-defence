using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Mixer Groups")]
    [SerializeField] private AudioMixerGroup musicGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;

    [Header("Music Clips")]
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip gameMusic;

    [Header("UI SFX")]
    [SerializeField] private AudioClip uiClick;

    [Header("Tower SFX (by TowerType)")]
    public TowerSfxEntry[] towerSfx;

    private AudioSource musicSource;
    private AudioSource sfxSource;

    [System.Serializable]
    public struct TowerSfxEntry
    {
        public TowerType towerType;
        public AudioClip shotClip;
        [Range(0f, 1f)] public float volume;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureSources();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        // Oyun ilk açıldığında hangi sahnedeyse ona göre müzik başlat
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void EnsureSources()
    {
        // Music Source
        if (musicSource == null)
        {
            musicSource = gameObject.GetComponent<AudioSource>();
            if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
        }
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.enabled = true;
        if (musicGroup != null) musicSource.outputAudioMixerGroup = musicGroup;

        // SFX Source (UI vb.)
        if (sfxSource == null)
        {
            // İkinci bir AudioSource varsa onu kullan, yoksa ekle
            AudioSource[] sources = gameObject.GetComponents<AudioSource>();
            if (sources.Length >= 2) sfxSource = sources[1];
            else sfxSource = gameObject.AddComponent<AudioSource>();
        }
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.enabled = true;
        if (sfxGroup != null) sfxSource.outputAudioMixerGroup = sfxGroup;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Sahne adına göre otomatik müzik
        if (scene.name.Contains("MainMenu"))
            PlayMusic(menuMusic);
        else if (scene.name.Contains("Game") || scene.name.Contains("Level"))
            PlayMusic(gameMusic);
    }

    // ---------------- MUSIC ----------------
    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;

        // Disabled audio source hatasını engelle
        if (!gameObject.activeInHierarchy)
            gameObject.SetActive(true);

        EnsureSources();

        if (!musicSource.enabled) musicSource.enabled = true;

        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.clip = clip;
        musicSource.Play();
    }

    // ---------------- UI SFX ----------------
    public void PlayUIClick()
    {
        if (uiClick == null) return;

        if (!gameObject.activeInHierarchy)
            gameObject.SetActive(true);

        EnsureSources();

        if (!sfxSource.enabled) sfxSource.enabled = true;

        sfxSource.PlayOneShot(uiClick, 1f);
    }

    // ---------------- TOWER SFX ----------------
    public void PlayTowerShot(TowerType type, Vector3 worldPos)
    {
        AudioClip clip = null;
        float vol = 1f;

        for (int i = 0; i < towerSfx.Length; i++)
        {
            if (towerSfx[i].towerType == type)
            {
                clip = towerSfx[i].shotClip;
                vol = towerSfx[i].volume <= 0f ? 1f : towerSfx[i].volume;
                break;
            }
        }

        if (clip == null) return;

        AudioSource.PlayClipAtPoint(clip, worldPos, vol);
    }
}
