using System.Collections.Generic;
using UnityEngine;
using Frontline.Core;

namespace Frontline.Runtime
{
    /// <summary>
    /// Kaya ve agaclari cizer, yikilinca yerlerine cukur birakir.
    ///
    /// Hucre basina bir SpriteRenderer kullanilir - zemin gibi tek dokuya
    /// yazilamaz, cunku ortu tek tek yikiliyor ve her yikilista butun atlasi
    /// yeniden uretmek atisin ortasinda kare dusurur. Ortu sayisi yuzlerle
    /// olculur, binlerle degil; bu maliyet kabul edilebilir.
    ///
    /// Hasar geri bildirimi renkten gelir: cani azalan ortu kararir ve
    /// kuculur. Oyuncu vurdugunun ise yaradigini gormeli, yoksa kayaya ates
    /// etmeyi bir daha denemez.
    /// </summary>
    public sealed class SceneryView : MonoBehaviour
    {
        [Tooltip("Bos birakilirsa sahnedeki Battlefield bulunur.")]
        public Battlefield world;

        [Header("Sprite'lar (bos birakilabilir)")]
        [Tooltip("Agac gorseli. Yoksa daire cizilir.")]
        public Sprite treeSprite;

        [Tooltip("Kaya gorseli. Yoksa daire cizilir.")]
        public Sprite rockSprite;

        [Tooltip("Yikilan ortunun yerinde kalan cukur. Yoksa daire cizilir.")]
        public Sprite craterSprite;

        [Header("Renkler")]
        public Color treeTint = new Color(0.33f, 0.52f, 0.28f);
        public Color rockTint = new Color(0.55f, 0.54f, 0.50f);

        [Tooltip("Cukur zemine gomulu durmali: koridordan koyu, seritten sicak.")]
        public Color craterTint = new Color(0.35f, 0.26f, 0.18f);

        [Header("Boyut")]
        [Tooltip("Ortunun hucreye orani. 1 = hucreyi tam doldurur.")]
        public float sceneryScale = 0.9f;

        [Tooltip("Cukurun hucreye orani. Ortuden kucuk olmali ki fark edilsin.")]
        public float craterScale = 0.75f;

        [Tooltip("Can bittikce gorselin kuculdugu oran. 0 = kuculmez.")]
        [Range(0f, 0.5f)]
        public float damageShrink = 0.25f;

        [Header("Cizim")]
        [Tooltip("Zeminin ustunde, birliklerin altinda olmali.")]
        public int sortingOrder = 2;

        public int craterSortingOrder = 1;

        private readonly Dictionary<int, SpriteRenderer> standing = new Dictionary<int, SpriteRenderer>();
        private readonly List<int> tracked = new List<int>();
        private Transform root;

        private void Awake()
        {
            if (world == null) world = FindFirstObjectByType<Battlefield>();
            if (world == null) return;

            world.WorldRebuilt += Rebuild;
            world.SceneryCleared += OnCleared;

            if (world.Grid != null) Rebuild();
        }

        private void OnDestroy()
        {
            if (world == null) return;

            world.WorldRebuilt -= Rebuild;
            world.SceneryCleared -= OnCleared;
        }

        /// <summary>Can azaldikca ortuyu karart ve kucult. Yalnizca kozmetik.</summary>
        private void Update()
        {
            if (world == null || world.Scenery == null || standing.Count == 0) return;

            for (int i = 0; i < tracked.Count; i++)
            {
                int cell = tracked[i];
                SpriteRenderer renderer;
                if (!standing.TryGetValue(cell, out renderer) || renderer == null) continue;

                float health = world.Scenery.HealthFraction(cell);
                if (health >= 0.999f) continue;

                Color tint = BaseTint(world.Scenery.KindAt(cell));
                // Tamamen karartma: yikilmak uzere olan ortu hala okunabilmeli.
                float shade = 0.45f + 0.55f * health;
                renderer.color = new Color(tint.r * shade, tint.g * shade, tint.b * shade, 1f);

                float scale = sceneryScale * (1f - damageShrink * (1f - health));
                SetSize(renderer, scale);
            }
        }

        /// <summary>Harita yeniden uretildi: butun ortuyu bastan ciz.</summary>
        private void Rebuild()
        {
            ClearAll();

            if (world == null || world.Scenery == null || world.Space == null) return;

            root = new GameObject("Scenery").transform;
            root.SetParent(transform, false);

            IReadOnlyList<int> cells = world.Scenery.Occupied;
            for (int i = 0; i < cells.Count; i++)
                Spawn(cells[i]);
        }

        private void Spawn(int cell)
        {
            SceneryKind kind = world.Scenery.KindAt(cell);
            if (kind == SceneryKind.None) return;

            Sprite sprite = kind == SceneryKind.Rock ? rockSprite : treeSprite;
            var renderer = Create(
                kind == SceneryKind.Rock ? "Rock" : "Tree",
                cell, sprite, BaseTint(kind), sceneryScale, sortingOrder);

            standing[cell] = renderer;
            tracked.Add(cell);
        }

        /// <summary>Ortu yikildi: gorseli kaldir, yerine cukur birak.</summary>
        private void OnCleared(int cell)
        {
            SpriteRenderer renderer;
            if (standing.TryGetValue(cell, out renderer))
            {
                if (renderer != null) Destroy(renderer.gameObject);
                standing.Remove(cell);
                tracked.Remove(cell);
            }

            if (root == null) return;

            Create("Crater", cell, craterSprite, craterTint, craterScale, craterSortingOrder);
        }

        private SpriteRenderer Create(
            string name, int cell, Sprite sprite, Color tint, float scale, int order)
        {
            var go = new GameObject(name + " " + cell);
            go.transform.SetParent(root, false);
            go.transform.position = world.Space.CellToWorld(cell);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite != null ? sprite : PrimitiveSprites.Circle();
            renderer.color = tint;
            renderer.sortingOrder = order;

            SetSize(renderer, scale);
            return renderer;
        }

        /// <summary>
        /// Sprite'i hucre boyutuna oturtur. Sprite'in kendi piksel yogunlugu
        /// bilinmedigi icin dunya boyutundan geri hesaplanir; boylece Blender'dan
        /// gelen farkli cozunurlukteki gorseller ayni boyu tutar.
        /// </summary>
        private void SetSize(SpriteRenderer renderer, float scale)
        {
            if (renderer.sprite == null) return;

            Vector2 size = renderer.sprite.bounds.size;
            if (size.x <= 0.0001f || size.y <= 0.0001f) return;

            float target = world.Space.CellSize * scale;
            renderer.transform.localScale = new Vector3(target / size.x, target / size.y, 1f);
        }

        private Color BaseTint(SceneryKind kind)
        {
            return kind == SceneryKind.Rock ? rockTint : treeTint;
        }

        private void ClearAll()
        {
            standing.Clear();
            tracked.Clear();

            if (root != null) Destroy(root.gameObject);
            root = null;
        }
    }
}
