using UnityEngine;

/// <summary>
/// Clock Tower'ýn kapsama alanýna giren enemy'nin ANÝMASYONUNU yavaþlatýr.
/// (Hareket hýzýný deðiþtirmez.)
/// Bu script'i Clock Tower'ýn range objesine (CircleCollider2D + IsTrigger) ekle.
/// </summary>
public class ClockAura : MonoBehaviour
{
    [Range(0.05f, 1f)]
    public float animationSpeedMultiplier = 0.5f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Collider child'da olabilir, bu yüzden parent'tan da bakýyoruz.
        EnemyMover enemy = other.GetComponent<EnemyMover>();
        if (enemy == null) enemy = other.GetComponentInParent<EnemyMover>();

        if (enemy != null)
        {
            enemy.SetClockAura(true, animationSpeedMultiplier);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        EnemyMover enemy = other.GetComponent<EnemyMover>();
        if (enemy == null) enemy = other.GetComponentInParent<EnemyMover>();

        if (enemy != null)
        {
            enemy.SetClockAura(false);
        }
    }
}
