using UnityEngine;

public class CameraKeyboardPan2D : MonoBehaviour
{
    [Header("Refs")]
    public GridManager gridManager;

    [Header("Movement")]
    public float moveSpeed = 10f;
    public float fastMultiplier = 1.8f;

    [Header("Clamp")]
    [Tooltip("Kameranýn kenarlara tam yapýþmamasý için küçük boþluk")]
    public float padding = 0.5f;

    [Header("Zoom (3 Steps)")]
    [Tooltip("1=Yakýn, 2=Orta, 3=Uzak")]
    [Range(1, 3)] public int zoomStep = 2;

    [Tooltip("Orthographic size deðerleri: [0]=Yakýn, [1]=Orta, [2]=Uzak")]
    public float[] zoomSizes = new float[3] { 4f, 6f, 8f };

    [Tooltip("Mouse wheel hassasiyeti. 0.1-0.3 genelde iyi.")]
    public float wheelThreshold = 0.12f;

    private float minX, maxX, minY, maxY;
    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;

        if (gridManager == null)
            gridManager = FindObjectOfType<GridManager>();

        ApplyZoomStep(zoomStep);
        RecalculateBounds();
    }

    void OnValidate()
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();

        // zoomSizes güvenliði
        if (zoomSizes == null || zoomSizes.Length != 3)
            zoomSizes = new float[3] { 4f, 6f, 8f };

        if (cam != null)
            ApplyZoomStep(zoomStep);

        if (cam != null && gridManager != null)
            RecalculateBounds();
    }

    void Update()
    {
        HandleZoomWheel();
        HandleKeyboardPan();
    }

    void HandleZoomWheel()
    {
        float wheel = Input.mouseScrollDelta.y; // + yukarý, - aþaðý

        if (Mathf.Abs(wheel) < wheelThreshold) return;

        // yukarý -> yakýn (step 1'e doðru), aþaðý -> uzak (step 3'e doðru)
        if (wheel > 0f) zoomStep = Mathf.Max(1, zoomStep - 1);
        else zoomStep = Mathf.Min(3, zoomStep + 1);

        ApplyZoomStep(zoomStep);
        RecalculateBounds();
    }

    void ApplyZoomStep(int step)
    {
        if (cam == null) return;
        step = Mathf.Clamp(step, 1, 3);
        zoomStep = step;

        // 1->index0, 2->index1, 3->index2
        cam.orthographicSize = zoomSizes[zoomStep - 1];
    }

    void HandleKeyboardPan()
    {
        float x = 0f;
        float y = 0f;

        // WASD
        if (Input.GetKey(KeyCode.A)) x -= 1f;
        if (Input.GetKey(KeyCode.D)) x += 1f;
        if (Input.GetKey(KeyCode.W)) y += 1f;
        if (Input.GetKey(KeyCode.S)) y -= 1f;

        // Arrow keys
        if (Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
        if (Input.GetKey(KeyCode.RightArrow)) x += 1f;
        if (Input.GetKey(KeyCode.UpArrow)) y += 1f;
        if (Input.GetKey(KeyCode.DownArrow)) y -= 1f;

        Vector3 dir = new Vector3(x, y, 0f);
        if (dir.sqrMagnitude > 1f) dir.Normalize();

        float speed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            speed *= fastMultiplier;

        Vector3 newPos = transform.position + dir * speed * Time.deltaTime;

        // Grid sýnýrýna göre kýsýtla
        newPos.x = Mathf.Clamp(newPos.x, minX, maxX);
        newPos.y = Mathf.Clamp(newPos.y, minY, maxY);

        transform.position = newPos;
    }

    /// <summary>
    /// GridManager(width/height/cellSize/origin) + camera ortho size/aspect ile
    /// kameranýn gidebileceði dünya sýnýrlarýný hesaplar.
    /// </summary>
    public void RecalculateBounds()
    {
        if (gridManager == null || cam == null) return;

        float gridWidthWorld = gridManager.width * gridManager.cellSize;
        float gridHeightWorld = gridManager.height * gridManager.cellSize;

        Vector3 origin = gridManager.origin;

        float camHalfHeight = cam.orthographicSize;
        float camHalfWidth = camHalfHeight * cam.aspect;

        minX = origin.x + camHalfWidth - padding;
        maxX = origin.x + gridWidthWorld - camHalfWidth + padding;

        minY = origin.y + camHalfHeight - padding;
        maxY = origin.y + gridHeightWorld - camHalfHeight + padding;

        if (minX > maxX) { float mid = (minX + maxX) * 0.5f; minX = maxX = mid; }
        if (minY > maxY) { float mid = (minY + maxY) * 0.5f; minY = maxY = mid; }

        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, minX, maxX);
        p.y = Mathf.Clamp(p.y, minY, maxY);
        transform.position = p;
    }
}
