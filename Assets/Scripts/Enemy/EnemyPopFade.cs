using System.Collections;
using UnityEngine;

public class EnemyPopFade : MonoBehaviour
{
    [Header("Pop/Fade Settings")]
    public float duration = 0.15f;
    public float popScale = 1.15f;

    SpriteRenderer sr;
    Collider2D col;
    Rigidbody2D rb;

    void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
    }

    public void PlayAndDestroy()
    {
        // double çaðrýlmasýn
        StopAllCoroutines();
        StartCoroutine(Co());
    }

    IEnumerator Co()
    {
        // çarpýþma/physics kapat
        if (col) col.enabled = false;
        if (rb) rb.simulated = false;

        // EnemyMover gibi hareket scriptlerini kapat (varsa)
        var mover = GetComponent<EnemyMover>();
        if (mover) mover.enabled = false;

        if (!sr)
        {
            Destroy(gameObject);
            yield break;
        }

        Color startC = sr.color;
        Vector3 startS = transform.localScale;
        Vector3 endS = startS * popScale;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / duration);

            // scale pop
            transform.localScale = Vector3.Lerp(startS, endS, a);

            // fade out
            sr.color = new Color(startC.r, startC.g, startC.b, Mathf.Lerp(startC.a, 0f, a));

            yield return null;
        }

        Destroy(gameObject);
    }
}
