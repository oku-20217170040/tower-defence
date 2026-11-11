using UnityEngine;

namespace Frontline.Runtime
{
    /// <summary>
    /// Birligin uzerindeki etki gorseli (11 Kasim 2026, Serhat): "zaman
    /// bozulmasina ugrayan dusmanin uzerine sanki bir agirlik cokmus gibi,
    /// kirmizi los bir isik dussun."
    ///
    ///   * Yavaslatma alaninda: birligin ustune los kirmizi bir ortu iner ve
    ///     birlik hafifce BASILIR (agirlik hissi).
    ///   * Kirilganlik alaninda (sv3): ayni ortu daha parlak ve nabiz gibi atar.
    ///
    /// Ucan birlik etkilenmez (UnitAgent.SlowFactorHere ucana 1 doner).
    /// </summary>
    public sealed class UnitStatusFx : MonoBehaviour
    {
        private UnitAgent agent;
        private SpriteRenderer body;
        private SpriteRenderer veil;
        private float amount;      // 0 = etki yok, 1 = tam
        private Vector3 restScale;
        private bool hasRestScale;

        private static readonly Color VeilColor = new Color(0.82f, 0.08f, 0.10f, 1f);

        /// <summary>Birlikte yoksa ekler. Havuzdan donen birlikte tekrar eklenmez.</summary>
        public static void Ensure(UnitAgent agent)
        {
            if (agent == null) return;

            var fx = agent.GetComponent<UnitStatusFx>();
            if (fx == null)
            {
                fx = agent.gameObject.AddComponent<UnitStatusFx>();
                fx.agent = agent;
                fx.body = agent.GetComponent<SpriteRenderer>();
                fx.BuildVeil();
            }

            fx.ResetState();
        }

        private void BuildVeil()
        {
            var go = new GameObject("StatusVeil");
            go.transform.SetParent(transform, false);

            veil = go.AddComponent<SpriteRenderer>();
            veil.sprite = PrimitiveSprites.Circle();
            veil.sortingOrder = body != null ? body.sortingOrder + 1 : 21;
            veil.color = Color.clear;

            float circle = veil.sprite.bounds.size.x;
            float unit = body != null && body.sprite != null ? body.sprite.bounds.size.x : 1f;
            float scale = circle > 0.0001f ? unit * 1.15f / circle : 1f;
            go.transform.localScale = new Vector3(scale, scale, 1f);
        }

        private void ResetState()
        {
            amount = 0f;
            if (veil != null) veil.color = Color.clear;
            if (hasRestScale) transform.localScale = restScale;
            hasRestScale = false;
        }

        private void LateUpdate()
        {
            if (agent == null || veil == null) return;

            bool slowed = agent.SlowFactorHere > 1.001f;
            bool fragile = agent.DamageTakenMultiplier > 1.001f;

            // Ani acilip kapanmasin: etki alana girerken ve cikarken yumusar.
            float target = slowed ? 1f : 0f;
            amount = Mathf.MoveTowards(amount, target, Time.deltaTime * 4f);

            if (amount <= 0f)
            {
                if (veil.color.a != 0f) veil.color = Color.clear;
                if (hasRestScale)
                {
                    transform.localScale = restScale;
                    hasRestScale = false;
                }
                return;
            }

            Color c = VeilColor;
            float pulse = fragile ? 0.5f + 0.5f * Mathf.Sin(Time.time * 9f) : 0f;
            c.a = amount * (fragile ? 0.30f + 0.14f * pulse : 0.26f);
            veil.color = c;

            // Agirlik: birlik gittigi yonde hafifce basilir.
            if (!hasRestScale)
            {
                restScale = transform.localScale;
                hasRestScale = true;
            }

            float squash = 1f - 0.12f * amount;
            transform.localScale = new Vector3(restScale.x * (1f + 0.05f * amount),
                                               restScale.y * squash, restScale.z);
        }
    }
}
