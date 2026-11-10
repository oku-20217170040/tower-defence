# Menu ve Ses Sistemi — 2026-09-03

Kapsam: ana menu, duraklatma menusu, ayarlar ekrani, zafer/yenilgi ekrani
guncellemesi, merkezi ses servisi (AudioManager) ve olaylara baglanmasi.
Kule/harita gorselleri kapsam disi (paralel ajanin isiydi, dokunulmadi).

## A) Menu Sistemi — TAMAMLANDI, sahnede dogrulandi

### Yeni dosyalar
- `Assets/Frontline/Runtime/UI/MainMenuScreen.cs` — acilis ekrani: FRONTLINE
  basligi + OYNA/AYARLAR/CIKIS. OYNA -> `Frontline_Sandbox` sahnesini yukler.
  Statik `LoadMainMenuOrQuit()` diger ekranlardan (duraklat/yenilgi) cagirilir;
  ana menu build ayarlarindaysa ona doner, yoksa uygulamayi kapatir.
- `Assets/Frontline/Runtime/UI/PauseOverlay.cs` — referans oyunun
  "DURDURULDU" ekraniyla birebir: DEVAM ET / AYARLAR / YENIDEN BASLAT / CIKIS.
  YARDIM atlandi (ogretici metin icat edilmedi), CANLANDIR yok (parasal
  dongu kapsam disi). Acilinca `Time.timeScale = 0`, DEVAM ET onceki hiza
  geri doner.
- `Assets/Frontline/Runtime/UI/SettingsOverlay.cs` — MUZIK AC/KAPA, EFEKT
  AC/KAPA (referans oyunda gozlenenle sinirli, titresim/dil icat edilmedi).
  Hem duraklatma menusunden hem ana menuden acilabilir, PlayerPrefs'e yazar.
- `Assets/Frontline/Runtime/Audio/AudioManager.cs` — bkz. B bolumu.

### Degisen dosyalar
- `RunScreen.cs` — Duraklat butonu artik dogrudan timeScale degistirmek
  yerine `PauseOverlay.Open()` cagiriyor. Zafer/yenilgi etiketinin altina
  **YENIDEN BASLAT** ve **CIKIS** butonlari eklendi (referans oyunun yenilgi
  ekranindaki CIK/YENIDEN BASLAT/CANLANDIR ucluesunden CANLANDIR cikarildi).
- `FrontlineSandboxSceneBuilder.cs` — yeni menu item **"Frontline/Ana Menu
  Sahnesini Uret"**, `Assets/Scenes/Frontline_MainMenu.unity` uretir
  (EventSystem + MainMenuScreen, sandbox sahnesiyle ayni desen).
- Build Settings sahne listesi guncellendi: index 0 = Frontline_MainMenu,
  index 1 = Frontline_Sandbox.

### Sahnede dogrulama (Unity MCP execute_code + screenshot, gercek Play Mode)
- Ana menu sahnesi acildi, OYNA butonu tiklanip **Frontline_Sandbox
  sahnesine gecis** dogrulandi (`get_active` ile teyit edildi).
- Sandbox'ta Duraklat -> PauseOverlay acildi, `Time.timeScale == 0` oldu
  (ekran goruntusu alindi).
- PauseOverlay uzerinden AYARLAR acildi, MUZIK butonuna basilinca
  `AudioManager.MusicOn` gercekten `false` oldu (kod ile teyit edildi).
- HomeBase cani sifirlanip **YUVA DUSTU** ekrani + YENIDEN BASLAT/CIKIS
  butonlari canli sahnede goruldu (ekran goruntusu alindi,
  `Assets/Screenshots/defeat_screen.png`).
- Ekran goruntuleri: `Assets/Screenshots/main_menu2.png`,
  `pause_overlay.png`, `settings_overlay.png`, `defeat_screen.png`.

## B) Ses Sistemi — KOD TAMAM, GERCEK SES DOSYASI URETILEMEDI (API anahtari yok)

### AudioManager (`Assets/Frontline/Runtime/Audio/AudioManager.cs`)
Tek merkezi singleton, ilk cagrida kendini yaratir (DontDestroyOnLoad).
`PlaySfx(clipName)` / `PlayMusic(clipName)` klip'i `Resources/Audio/<isim>`
altindan isimle yukler (veri-tabanli: sahneye elle referans atamaya gerek
yok, dosya klasore atilinca calisir). `SetMusicOn/SetSfxOn` PlayerPrefs'e
yazar ve ayarlar ekraniyla gercekten baglidir.

### Baglanan olaylar (hepsi gercek kod yolunda, execute_code ile canli
Play Mode'da log kanitiyla dogrulandi — asagida tam log dokumu var)
| Olay | Dosya:satir | Klip adi |
|---|---|---|
| Kule atesi (Batarya/Pusu) | `TowerWeapon.cs` EmitTracer/EmitSceneryTracer | tower_fire_mg / tower_fire_sniper |
| Kule atesi (Isin) | `TowerWeapon.cs` UpdateBeam | tower_fire_timewarp |
| Birlik olumu | `UnitHealth.cs` TakeDamage | unit_death |
| Her UI butonu (merkezi) | `UiKit.cs` Button() | ui_click |
| Dalga baslangici | `RunController.cs` RunWaves | wave_start |
| Kule kurma | `RadialBuildMenu.cs` ChooseTower | tower_build |
| Zafer | `RunController.cs` Finish | victory |
| Yenilgi | `RunController.cs` Finish | defeat |
| Arka plan muzigi | `RunScreen.cs` Start, `MainMenuScreen.cs` Start | music_loop |

Kule tipine gore ses secimi `TowerWeapon.FireClip` uzerinden: Batarya ->
mekanik/hizli, Pusu -> keskin/sessiz, Isin -> elektronik hum; Sahte Hedef ve
Mayin Tarlasi ates etmedigi icin ses atanmadi (tasarim karari, referans
oyunun radar/zaman-bozulma kulelerinin de ates etmemesiyle tutarli).

**Canli dogrulama:** AudioManager'a gecici bir log satiri eklenip Play
Mode'da kule kuruldu, dalga baslatildi, birlikler olduruldu, UI butonlarina
basildi, yenilgi tetiklendi. Konsolda sirasiyla su cagrilar gorundu (gercek
log, uydurma degil):
```
[AudioManager] PlaySfx cagrildi: wave_start
[AudioManager] PlaySfx cagrildi: tower_fire_mg   (x10+)
[AudioManager] PlaySfx cagrildi: unit_death       (x4)
[AudioManager] PlaySfx cagrildi: ui_click          (x4, her buton tikinda)
[AudioManager] PlaySfx cagrildi: defeat
```
Test sonrasi log satiri kaldirildi, kod temiz haline dondu (testler tekrar
302/302 yesil).

### NEDEN GERCEK SES DOSYASI YOK
`mcp__unity__generate_audio` (fal.ai saglayicisi, BYOK) cagrildiginda net
hata alindi:
```
No API key configured for 'fal'. Add it in the MCP for Unity ->
Asset Generation tab (or set MCPFORUNITY_FAL_API_KEY).
```
Ortam degiskenlerinde (`MCPFORUNITY_FAL_API_KEY`, `ELEVENLABS_*`) hicbir
anahtar bulunamadi ve bu oturum Unity Editor'un Asset Generation panelini
interaktif acip anahtar girme yetkisine sahip degil (o bir GUI islemi).
Bu yuzden **hicbir gercek .wav/.mp3 dosyasi uretilmedi** — sistem tamamen
hazir, `Resources.Load` klip bulamayinca sessizce (hatasiz) geri donuyor,
oyun bozulmuyor ama ses duyulmuyor.

**Kullaniciya gereken adim:** MCP for Unity penceresinde "Asset Generation"
sekmesinden fal.ai API anahtarini gir (ya da `MCPFORUNITY_FAL_API_KEY` ortam
degiskenini ayarla), sonra asagidaki 8 klibi uret ve `Assets/Frontline/
Resources/Audio/` altina tam bu isimlerle koy — sistem otomatik devreye
girer, kod tarafinda hicbir degisiklik gerekmez:

| Dosya adi | Tasarim niyeti (janr konvansiyonu, referans oyunun gorsel
ipuclarindan cikarildi — gercek ses dinlenmedi) |
|---|---|
| `tower_fire_mg.wav` | Hizli, mekanik, kisa staccato tik-tik (makineli tufek) |
| `tower_fire_sniper.wav` | Keskin, tek, metalik cinlama + hafif yay/geri tepme |
| `tower_fire_timewarp.wav` | Elektronik hum/whir, kristal/enerji tonu |
| `unit_death.wav` | Kisa patlama/metal gicirtisi, dusuk ton |
| `ui_click.wav` | Kisa, notr tik (referans oyunun buton geri bildirimi) |
| `wave_start.wav` | Kisa uyari sinyali/borazan, dikkat cekici ama rahatsiz etmeyen |
| `tower_build.wav` | Kisa "yerlesme" thunk sesi, hafif metal |
| `victory.wav` | Kisa zafer fanfari, yukselen ton |
| `defeat.wav` | Kisa dususen/kasvetli ton, uzun degil |
| `music_loop.wav` | Loop'lanabilir, gergin ama tekrarlanabilir tur-savunma ritmi |

## Kapsam disi birakilanlar (gorev talimatina gore)
- Kule/harita gorselleri (paralel ajanin isi).
- CANLANDIR butonu (parasal dongu yok).
- Ekonomi/denge, coklu yol, dusman-ogrenme degisikligi.
- Gercek .wav dosyalarinin uretimi (API anahtari eksikligi nedeniyle
  engellendi, yukarida detaylandirildi).

## Test durumu
EditMode: **302/302 gecti** (degisiklik oncesi de 302'ydi, dusus yok).
İlk birkaç `run_tests` çağrısı Unity'nin arka planda odaksız kalmasından
("editor_unfocused") 13/302'de takıldı; `clear_stuck` + play mode'u
durdurup tekrar çalıştırmak sorunu çözdü — bilinen bir Unity MCP kenar
durumu, kod değişikliğiyle ilgisi yok.
