using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Düşmana vurulan hasarı ekranda yukarı kayarak gösterir.
/// FloatingTextPool tarafından yönetilir.
/// </summary>
[RequireComponent(typeof(TextMeshPro))]
public class FloatingText : MonoBehaviour
{
    [Header("Animation")]
    public float riseSpeed   = 1.5f;
    public float duration    = 0.8f;
    public float fadeDelay   = 0.3f;

    private TextMeshPro tmp;
    private Coroutine anim;

    private void Awake()
    {
        tmp = GetComponent<TextMeshPro>();
    }

    public void Show(int damage, Vector3 worldPos, Color color)
    {
        transform.position = worldPos;
        tmp.text = damage.ToString();
        tmp.color = color;

        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        float elapsed = 0f;
        Color startColor = tmp.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position += Vector3.up * riseSpeed * Time.deltaTime;

            if (elapsed > fadeDelay)
            {
                float t = (elapsed - fadeDelay) / (duration - fadeDelay);
                tmp.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);
            }

            yield return null;
        }

        FloatingTextPool.Instance?.Return(this);
    }

    private void OnDisable()
    {
        if (anim != null)
        {
            StopCoroutine(anim);
            anim = null;
        }
    }
}
