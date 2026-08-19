using System;
using UnityEngine;

namespace Colony.Runtime
{
    /// <summary>
    /// Bocegin cani. Sifirlaninca BugAgent.Kill() cagrilir; o da rotayi
    /// zayiflatir - olumle biten yol sonumlenir (spec 3.3).
    ///
    /// Can dalga ilerledikce artar. Bunun sebebi olcumle bulundu: sabit canli
    /// boceklerde tek kule YA HEPSINI oldurur YA HICBIRINI - arada gradyan yok.
    /// Bir bocek 4 yaricapli kulenin menzilinde ~3.2 saniye gecirir; dps 3'te
    /// toplam hasar 9.6 (sag kurtulur), dps 4'te 12.8 (olur). Esik keskin.
    /// Can dalga dalga artinca sabit bir tuzak zamanla yetersiz kalir ve
    /// oyuncu genisletmek, tasimak ya da yukseltmek zorunda kalir.
    /// </summary>
    public sealed class BugHealth : MonoBehaviour
    {
        public float MaxHp { get; private set; }
        public float CurrentHp { get; private set; }
        public bool IsAlive => CurrentHp > 0f;

        /// <summary>Bocek oldu. Argument olen bocektir.</summary>
        public event Action<BugHealth> Died;

        private BugAgent agent;
        private SpriteRenderer spriteRenderer;
        private Color baseColor;

        public void Initialize(float maxHp)
        {
            MaxHp = Mathf.Max(1f, maxHp);
            CurrentHp = MaxHp;

            agent = GetComponent<BugAgent>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null) baseColor = spriteRenderer.color;
        }

        public void TakeDamage(float amount)
        {
            if (amount <= 0f || !IsAlive) return;

            CurrentHp -= amount;

            // Hasar gorsel olarak okunmali; yoksa oyuncu tuzagin ise yarayip
            // yaramadigini goremez. Bocek yaralandikca kararir.
            if (spriteRenderer != null)
            {
                float t = Mathf.Clamp01(CurrentHp / MaxHp);
                spriteRenderer.color = Color.Lerp(new Color(0.5f, 0.1f, 0.1f), baseColor, t);
            }

            if (CurrentHp <= 0f)
            {
                CurrentHp = 0f;
                if (Died != null) Died(this);
                if (agent != null) agent.Kill();
            }
        }
    }
}
