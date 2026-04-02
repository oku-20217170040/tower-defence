using UnityEngine;

public class ClockSlowTint : MonoBehaviour
{
    public Color slowTintColor = new Color(1f, 0.9f, 0.2f, 1f);
    [Range(0f, 1f)] public float tintStrength = 0.6f;

    private SpriteRenderer[] _renderers;
    private Color[] _baseColors;
    private int _sourceCount = 0;

    private void Awake()
    {
        _renderers = GetComponentsInChildren<SpriteRenderer>(true);
        _baseColors = new Color[_renderers.Length];

        for (int i = 0; i < _renderers.Length; i++)
            _baseColors[i] = _renderers[i].color;
    }

    public void AddSlowSource()
    {
        _sourceCount++;
        ApplyTint();
    }

    public void RemoveSlowSource()
    {
        _sourceCount = Mathf.Max(0, _sourceCount - 1);
        if (_sourceCount == 0) Restore();
        else ApplyTint();
    }

    private void ApplyTint()
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            var baseC = _baseColors[i];
            Color tinted = Color.Lerp(baseC, slowTintColor, tintStrength);
            tinted.a = baseC.a;
            if (_renderers[i] != null) _renderers[i].color = tinted;
        }
    }

    private void Restore()
    {
        for (int i = 0; i < _renderers.Length; i++)
            if (_renderers[i] != null) _renderers[i].color = _baseColors[i];
    }

    private void OnDisable()
    {
        _sourceCount = 0;
        Restore();
    }
}
