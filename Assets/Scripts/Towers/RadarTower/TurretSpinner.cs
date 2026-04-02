using UnityEngine;

public class TurretSpinner : MonoBehaviour
{
    [SerializeField] private float speed = 120f;   // derece/saniye
    [SerializeField] private bool clockwise = true;

    void Update()
    {
        float dir = clockwise ? -1f : 1f;
        transform.Rotate(0f, 0f, dir * speed * Time.deltaTime);
    }
}
