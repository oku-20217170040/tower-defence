using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BaseHitFlash : MonoBehaviour
{
    [Header("Flash")]
    public Color flashColor = new Color(1f, 0.3f, 0.3f, 1f);
    public float flashDuration = 0.08f;
    public int flashes = 2;

    private SpriteRenderer sr;
    private Color baseColor;      // ✅ gerçek orijinal renk (sabit)
    private Coroutine running;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        baseColor = sr.color;     // ✅ sadece 1 kere al
    }

    // Eğer Inspector’dan runtime’da renk değiştirirsen çağırabilirsin
    public void RefreshBaseColor()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        baseColor = sr.color;
    }

    public void Play()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();

        // ✅ her seferinde güvenli şekilde resetle
        sr.color = baseColor;

        if (running != null) StopCoroutine(running);
        running = StartCoroutine(CoFlash());
    }

    private IEnumerator CoFlash()
    {
        for (int i = 0; i < flashes; i++)
        {
            sr.color = flashColor;
            yield return new WaitForSeconds(flashDuration);

            sr.color = baseColor;
            yield return new WaitForSeconds(flashDuration);
        }

        sr.color = baseColor; // ✅ ne olursa olsun sonunda base rengine dön
        running = null;
    }

    private void OnDisable()
    {
        // ✅ sahne kapanır / obje disable olursa takılı kalmasın
        if (sr != null) sr.color = baseColor;
        running = null;
    }
}
