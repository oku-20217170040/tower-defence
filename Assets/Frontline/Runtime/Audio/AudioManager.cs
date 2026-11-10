using UnityEngine;

namespace Frontline.Runtime.Audio
{
    /// <summary>
    /// Tek merkezi ses servisi. Sahneye elle eklenmez - ilk cagrida kendini
    /// yaratir (DontDestroyOnLoad), tipki diger Frontline sistemlerinin kod
    /// ile kurulmasi gibi (bkz. UiKit, RunScreen).
    ///
    /// Klip'ler Resources/Audio altindan isimle yuklenir: bu projede
    /// sahneler/objeler kodla kuruluyor, o yuzden Inspector'dan tek tek
    /// referans atamak yerine Resources.Load ile veri-tabanli bir yaklasim
    /// secildi - yeni bir ses eklemek icin dosyayi klasore atmak yeterli.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        private const string ResourceFolder = "Audio/";
        private const string PrefMusic = "frontline_music_on";
        private const string PrefSfx = "frontline_sfx_on";

        // Klip adlari (Resources/Audio/<isim>.wav olarak beklenir).
        public const string TowerFireMg = "tower_fire_mg";
        public const string TowerFireCannon = "tower_fire_cannon";
        public const string TowerFireSniper = "tower_fire_sniper";
        public const string TowerFireTimeWarp = "tower_fire_timewarp";
        public const string UnitDeath = "unit_death";
        public const string UiClick = "ui_click";
        public const string WaveStart = "wave_start";
        public const string TowerBuild = "tower_build";
        public const string Victory = "victory";
        public const string Defeat = "defeat";
        public const string Music = "music_loop";

        private static AudioManager instance;
        public static AudioManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("AudioManager");
                    instance = go.AddComponent<AudioManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private AudioSource sfxSource;
        private AudioSource musicSource;

        public bool MusicOn { get; private set; } = true;
        public bool SfxOn { get; private set; } = true;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;

            MusicOn = PlayerPrefs.GetInt(PrefMusic, 1) == 1;
            SfxOn = PlayerPrefs.GetInt(PrefSfx, 1) == 1;
            ApplyVolumes();
        }

        private void ApplyVolumes()
        {
            sfxSource.volume = SfxOn ? 1f : 0f;
            musicSource.volume = MusicOn ? 0.5f : 0f;
        }

        public void SetMusicOn(bool on)
        {
            MusicOn = on;
            PlayerPrefs.SetInt(PrefMusic, on ? 1 : 0);
            ApplyVolumes();
        }

        public void SetSfxOn(bool on)
        {
            SfxOn = on;
            PlayerPrefs.SetInt(PrefSfx, on ? 1 : 0);
            ApplyVolumes();
        }

        /// <summary>Isimle klip yukler ve tek seferlik calar. Klip yoksa sessizce gecer.</summary>
        public void PlaySfx(string clipName)
        {
            if (!SfxOn || string.IsNullOrEmpty(clipName)) return;
            var clip = Resources.Load<AudioClip>(ResourceFolder + clipName);
            if (clip == null) return;
            sfxSource.PlayOneShot(clip);
        }

        public void PlayMusic(string clipName)
        {
            var clip = Resources.Load<AudioClip>(ResourceFolder + clipName);
            if (clip == null) return;
            if (musicSource.clip == clip && musicSource.isPlaying) return;
            musicSource.clip = clip;
            musicSource.volume = MusicOn ? 0.5f : 0f;
            musicSource.Play();
        }

        public void StopMusic()
        {
            musicSource.Stop();
        }
    }
}
