using UnityEngine;
using UnityEngine.UI;

namespace Frontline.Runtime.UI
{
    /// <summary>
    /// uGUI parcalarini KOD ile kuran kucuk fabrika. Sahne YAML'i elle
    /// duzenlenmez (bkz. devir notu), o yuzden arayuz de kodla dogar.
    ///
    /// Bilerek TextMeshPro KULLANILMAZ: TMP bir font asset'i ve
    /// "TMP Essentials" import'u ister; ikisi de projede garanti degil ve
    /// eksikse arayuz bos ekran olarak acilir. UnityEngine.UI.Text +
    /// dahili font her kurulumda calisir. TMP'ye gecis bir sonraki isin
    /// isi; o zaman yalnizca bu dosya degisir.
    /// </summary>
    public static class UiKit
    {
        private static Font cachedFont;

        /// <summary>Dahili font. Unity surumune gore adi degisti, ikisi de denenir.</summary>
        public static Font DefaultFont()
        {
            if (cachedFont != null) return cachedFont;

            cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (cachedFont == null)
                cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            return cachedFont;
        }

        public static RectTransform Panel(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.color = color;

            // Saydam panel dokunusu YINE de yutmali: alt bantta bosluga
            // basmak haritaya kule kurmamali.
            image.raycastTarget = true;
            return rect;
        }

        public static RectTransform Container(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        public static Text Label(string name, Transform parent, int fontSize,
                                 TextAnchor anchor, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            Stretch(rect);

            var text = go.GetComponent<Text>();
            text.font = DefaultFont();
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            // Etiketler dokunusu YUTMAZ; yutan altlarindaki paneldir.
            // Yoksa yazinin ustune basmak dugmeyi calistirmaz.
            text.raycastTarget = false;
            return text;
        }

        public static Button Button(string name, Transform parent, Color color)
        {
            var rect = Panel(name, parent, color);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();

            // Her buton merkezi bir tik sesi calar - tek tek her cagiran
            // yerde ayri ayri baglamak yerine (2026-09-03 ses gorevi).
            button.onClick.AddListener(() =>
                Frontline.Runtime.Audio.AudioManager.Instance.PlaySfx(
                    Frontline.Runtime.Audio.AudioManager.UiClick));
            return button;
        }

        /// <summary>Ebeveynin tamamini kaplayacak sekilde gerer.</summary>
        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Normalize edilmis (0-1) ankraj dikdortgenine oturtur. Piksel
        /// yerine oran kullanmak, farkli ekran oranlarinda yerlesimi
        /// kendiliginden dogru tutar.
        /// </summary>
        public static void SetAnchors(RectTransform rect, float xMin, float yMin,
                                      float xMax, float yMax)
        {
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        public static void Inset(RectTransform rect, float left, float bottom,
                                 float right, float top)
        {
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }
    }
}
