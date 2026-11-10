using UnityEngine;
using UnityEngine.UI;
using Frontline.Runtime.Audio;

namespace Frontline.Runtime.UI
{
    /// <summary>
    /// Ayarlar ekrani: muzik ac/kapa, efekt ac/kapa. Referans oyunun ayarlar
    /// ekraninda gozlenen kapsam bununla sinirli (bkz.
    /// docs/notes/2026-09-03-referans-oyun-tam-analiz.md) - titresim/dil
    /// gibi fazladan secenek icat edilmedi.
    ///
    /// Hem duraklatma menusunden hem ana menuden acilabilir; kod ile kurulur
    /// (UiKit uslubu), Close() cagrildiginda kendi kendini yok eder.
    /// </summary>
    public sealed class SettingsOverlay : MonoBehaviour
    {
        private RectTransform root;
        private Text musicText;
        private Text sfxText;

        public static SettingsOverlay Open(RectTransform parentCanvas)
        {
            var go = new GameObject("AyarlarOverlay", typeof(RectTransform));
            go.transform.SetParent(parentCanvas, false);
            var overlay = go.AddComponent<SettingsOverlay>();
            overlay.Build();
            return overlay;
        }

        private void Build()
        {
            root = (RectTransform)transform;
            UiKit.Stretch(root);

            UiKit.Panel("Karartma", root, new Color(0f, 0f, 0f, 0.75f));

            var panel = UiKit.Container("Panel", root);
            UiKit.SetAnchors(panel, 0.14f, 0.32f, 0.86f, 0.68f);
            UiKit.Panel("Zemin", panel, new Color(0.1f, 0.12f, 0.11f, 0.97f));
            UiKit.Stretch((RectTransform)panel.GetChild(0));

            var title = UiKit.Container("Baslik", panel);
            UiKit.SetAnchors(title, 0.05f, 0.78f, 0.95f, 0.96f);
            UiKit.Label("Metin", title, 34, TextAnchor.MiddleCenter,
                new Color(0.92f, 0.9f, 0.85f)).text = "AYARLAR";

            var musicRow = UiKit.Button("Muzik", panel, new Color(0.18f, 0.22f, 0.26f, 0.95f));
            UiKit.SetAnchors(musicRow.GetComponent<RectTransform>(), 0.1f, 0.48f, 0.9f, 0.7f);
            musicText = UiKit.Label("Metin", musicRow.transform, 26, TextAnchor.MiddleCenter,
                new Color(0.92f, 0.9f, 0.85f));
            musicRow.onClick.AddListener(ToggleMusic);

            var sfxRow = UiKit.Button("Efekt", panel, new Color(0.18f, 0.22f, 0.26f, 0.95f));
            UiKit.SetAnchors(sfxRow.GetComponent<RectTransform>(), 0.1f, 0.24f, 0.9f, 0.46f);
            sfxText = UiKit.Label("Metin", sfxRow.transform, 26, TextAnchor.MiddleCenter,
                new Color(0.92f, 0.9f, 0.85f));
            sfxRow.onClick.AddListener(ToggleSfx);

            var closeButton = UiKit.Button("Kapat", panel, new Color(0.22f, 0.4f, 0.24f, 0.95f));
            UiKit.SetAnchors(closeButton.GetComponent<RectTransform>(), 0.3f, 0.04f, 0.7f, 0.2f);
            UiKit.Label("Metin", closeButton.transform, 24, TextAnchor.MiddleCenter,
                new Color(0.92f, 0.9f, 0.85f)).text = "KAPAT";
            closeButton.onClick.AddListener(Close);

            Refresh();
        }

        private void Refresh()
        {
            musicText.text = "MUZIK: " + (AudioManager.Instance.MusicOn ? "ACIK" : "KAPALI");
            sfxText.text = "EFEKT: " + (AudioManager.Instance.SfxOn ? "ACIK" : "KAPALI");
        }

        private void ToggleMusic()
        {
            AudioManager.Instance.SetMusicOn(!AudioManager.Instance.MusicOn);
            Refresh();
        }

        private void ToggleSfx()
        {
            AudioManager.Instance.SetSfxOn(!AudioManager.Instance.SfxOn);
            Refresh();
        }

        private void Close()
        {
            Destroy(gameObject);
        }
    }
}
