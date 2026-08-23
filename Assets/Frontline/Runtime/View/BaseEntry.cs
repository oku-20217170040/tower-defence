using UnityEngine;

namespace Frontline.Runtime
{
    /// <summary>
    /// Usse sizan birligin "iceri giriyor" gorseli.
    ///
    /// Birlik usse vardiginda aninda yok olursa oyuncu ne oldugunu
    /// goremiyor: bir sey ekrandan siliniyor, o kadar. Oysa sizinti oyunun
    /// en pahali olayi ve gorunmesi gerekir.
    ///
    /// Illuzyonun tamami CIZIM SIRASINDA: birlik us sprite'inin ALTINDA
    /// (sortingOrder daha kucuk) cizilir ve kapiya dogru kayarken kucululup
    /// solar. Bunun calismasi icin USUN kendisi birliklerin USTUNDE olmali
    /// (HomeBaseView.sortingOrder = 24, birlikler 20); yoksa birlik kalenin
    /// catisinda yuruyup usten kayboluyor.
    ///
    /// Oyun mantigiyla ILGISI YOKTUR: sizinti sayaci birlik varir varmaz
    /// isler, bu yalnizca birakilan bir gorseldir.
    /// </summary>
    public sealed class BaseEntry : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private Vector2 from;
        private Vector2 to;
        private float startScale;
        private float seconds;
        private float elapsed;
        private Color tint;

        /// <summary>
        /// Kapidan iceri suzulen bir kopya birakir.
        /// </summary>
        /// <param name="source">Kopyalanacak birlik gorseli.</param>
        /// <param name="gateTarget">Usun merkezi; birlik oraya dogru cekilir.</param>
        /// <param name="baseSortingOrder">Us sprite'inin cizim sirasi; kopya bunun ALTINA konur.</param>
        public static void Spawn(SpriteRenderer source, Vector2 gateTarget,
                                 int baseSortingOrder, float seconds = 0.45f)
        {
            if (source == null || source.sprite == null) return;

            var go = new GameObject("BaseEntry");
            go.transform.position = source.transform.position;
            go.transform.rotation = source.transform.rotation;
            go.transform.localScale = source.transform.lossyScale;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = source.sprite;
            renderer.color = source.color;
            // Usun ALTINDA: "iceri girdi" hissi tamamen buradan geliyor.
            renderer.sortingOrder = baseSortingOrder - 2;

            var entry = go.AddComponent<BaseEntry>();
            entry.spriteRenderer = renderer;
            entry.from = source.transform.position;
            entry.to = gateTarget;
            entry.startScale = source.transform.lossyScale.x;
            entry.seconds = seconds > 0.01f ? seconds : 0.45f;
            entry.tint = source.color;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / seconds);

            // Yavaslayarak girer: kapidan iceri suzulen bir sey, duvara
            // carpan bir sey gibi degil, yumusak durmali.
            float eased = 1f - (1f - t) * (1f - t);

            transform.position = Vector2.Lerp(from, to, eased);

            // Kucululme ve solma birlikte "derinlige gitti" okumasi uretir;
            // yalnizca solmak hayalet gibi duruyor.
            float scale = startScale * Mathf.Lerp(1f, 0.45f, eased);
            transform.localScale = new Vector3(scale, scale, 1f);

            var color = tint;
            color.a = tint.a * (1f - eased * eased);
            spriteRenderer.color = color;

            if (t >= 1f) Destroy(gameObject);
        }
    }
}
