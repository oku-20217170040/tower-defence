using UnityEngine;

public class RadarGlowPulse : MonoBehaviour
{
    [Header("Pulse Ayarlarý")]
    [Tooltip("Yanýp sönme hýzý")]
    public float pulseSpeed = 3f;

    [Tooltip("Alpha minimum (0=þeffaf, 1=tam opak). 0.6-0.9 iyi.")]
    [Range(0f, 1f)] public float minAlpha = 0.65f;

    [Tooltip("Alpha maksimum (genelde 1 býrak).")]
    [Range(0f, 1f)] public float maxAlpha = 1f;

    [Tooltip("Ýstersen hafif renk tonu (beyaz býrakma, örn açýk mavi).")]
    public Color tint = new Color(0.7f, 0.95f, 1f, 1f);

    [Tooltip("Tint etkisi (0=kapalý, 1=tam tint).")]
    [Range(0f, 1f)] public float tintStrength = 0.35f;

    private SpriteRenderer[] _renderers;
    private Color[] _baseColors;

    private int _refCount = 0;
    private bool _active = false;

    private void Awake()
    {
        _renderers = GetComponentsInChildren<SpriteRenderer>(true);
        _baseColors = new Color[_renderers.Length];

        for (int i = 0; i < _renderers.Length; i++)
            _baseColors[i] = _renderers[i].color;
    }

    private void Update()
    {
        if (!_active) return;

        float s = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f; // 0..1

        float a = Mathf.Lerp(minAlpha, maxAlpha, s);

        for (int i = 0; i < _renderers.Length; i++)
        {
            var baseC = _baseColors[i];

            // tint uygula (RGB)
            Color rgb = Color.Lerp(baseC, tint, tintStrength);

            // alpha pulse
            rgb.a = baseC.a * a;

            _renderers[i].color = rgb;
        }
    }

    public void AddSource()
    {
        _refCount++;
        _active = true;
    }

    public void RemoveSource()
    {
        _refCount = Mathf.Max(0, _refCount - 1);
        if (_refCount == 0)
        {
            _active = false;
            Restore();
        }
    }

    private void Restore()
    {
        if (_renderers == null || _baseColors == null) return;
        for (int i = 0; i < _renderers.Length; i++)
            if (_renderers[i] != null) _renderers[i].color = _baseColors[i];
    }

    private void OnDisable()
    {
        _refCount = 0;
        _active = false;
        Restore();
    }
}
