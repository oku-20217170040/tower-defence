using UnityEngine;

public class BaseHealth : MonoBehaviour
{
    public int hp = 20;
    public bool isDestroyed = false;

    // UI için sadece okunabilir HP property
    public int CurrentHp => hp;

    // UIManager bu eventi subscribe eder
    public event System.Action<int> OnHpChanged;

    public void TakeDamage(int dmg)
    {
        if (isDestroyed) return;

        hp -= dmg;
        OnHpChanged?.Invoke(hp);

        // ✅ Base'in kendisinde şok/flash efekti (BaseHitFlash scripti varsa)
        BaseHitFlash flash = GetComponent<BaseHitFlash>();
        if (flash != null) flash.Play();

        if (hp <= 0)
        {
            hp = 0;
            isDestroyed = true;
            Debug.Log("💀 Base destroyed! Game over!");
        }
    }

    /// <summary>
    /// ✅ Enemy base'e DEĞER DEĞMEZ hasar ver + enemy'yi kaldır.
    /// Base objesinde Collider2D olmalı ve IsTrigger açık olmalı.
    /// Enemy tarafında Rigidbody2D var olmalı.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDestroyed) return;

        EnemyMover enemy = other.GetComponent<EnemyMover>();
        if (enemy == null) enemy = other.GetComponentInParent<EnemyMover>();
        if (enemy == null) return;

        // Aynı enemy birden fazla collider ile tetikleyebilir → tek sefer vur
        if (enemy.HasHitBase) return;

        // 1) Hasar ver (burada flash otomatik tetiklenir)
        TakeDamage(enemy.damageToBase);

        // 2) Enemy'yi tekrar tetiklememesi için hemen pasifleştir
        Collider2D[] cols = enemy.GetComponentsInChildren<Collider2D>();
        foreach (var c in cols) c.enabled = false;

        Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
        if (rb != null) rb.simulated = false;

        enemy.enabled = false;

        // 3) Pop/Fade varsa onu oynat, yoksa direkt yok et
        EnemyPopFade pop = enemy.GetComponent<EnemyPopFade>();
        if (pop == null) pop = enemy.GetComponentInChildren<EnemyPopFade>();

        if (pop != null)
        {
            pop.PlayAndDestroy();
        }
        else
        {
            Destroy(enemy.gameObject);
        }
    }
}
