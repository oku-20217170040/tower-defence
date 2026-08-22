using UnityEngine;

namespace Frontline.Runtime
{
    /// <summary>
    /// Harita icinde gezinme: surukle-kaydir ve yakinlastir.
    ///
    /// Neden gerekli: harita 20'den 30 satira cikti (23 Agu) ve artik
    /// ekrana sigmiyor. Hepsini kadraja sokmak da bir secenekti ama
    /// telefonda hucreler okunamayacak kadar kuculuyordu - dikey tutuslu
    /// bir oyunda GENISLIK ekrani belirler, yukseklik gezilir.
    ///
    /// KAYDIRMA ILE YERLESTIRME AYRIMI (bu sinifin asil zorlugu):
    /// tek parmakla suruklemek kaydirir, dokunup birakmak kule kurar.
    /// Ikisi ayni parmakla yapildigi icin esik gerekiyor - basildigi
    /// yerden `dragThreshold` pikselden fazla uzaklasan hareket
    /// KAYDIRMADIR ve TowerPlacer o dokunusu yok sayar (bkz.
    /// <see cref="DraggedSincePress"/>). Esik olmadan haritayi her
    /// kaydirisinda istemedigin yere kule kuruluyor.
    ///
    /// Kamera sinirlari haritanin disina TASMAZ: bos zemine bakmak
    /// oyuncuya nerede oldugunu unutturuyor. Bir eksende gorunum
    /// haritadan buyukse o eksende ortalanir.
    /// </summary>
    public sealed class MapCamera : MonoBehaviour
    {
        public Battlefield world;
        public Camera targetCamera;

        [Tooltip("Kadrajda en az kac hucre YUKSEKLIK gorunecek (en yakin " +
                 "zoom). Cok kucultmek oyuncuyu haritada kaybediyor.")]
        public float minVisibleCells = 10f;

        [Tooltip("Acilista kadrajin genisligi kac hucre olsun. Dikey " +
                 "tutuslu oyunda genislik sabittir; 0 ise haritanin tam " +
                 "genisligi kullanilir.")]
        public float startVisibleWidthCells;

        [Tooltip("Bir dokunusun kaydirma sayilmasi icin gereken piksel. " +
                 "Bunun altindaki hareket kule kurma dokunusudur.")]
        public float dragThreshold = 12f;

        [Tooltip("Fare tekerlegi hassasiyeti.")]
        public float wheelZoomSpeed = 3f;

        [Tooltip("Kenarlarda birakilacak pay (dunya birimi).")]
        public float padding = 0.25f;

        /// <summary>
        /// Sahnedeki ornek. TowerPlacer bunu bulur; bulamazsa
        /// yerlestirme eskisi gibi calisir - gezinme katmani oyunun
        /// calismasi icin ZORUNLU olmamali.
        /// </summary>
        public static MapCamera Instance { get; private set; }

        /// <summary>
        /// Bu dokunus basildigindan beri esigi asti mi. TowerPlacer
        /// buna bakar: kaydirma biten parmak kule kurmamali.
        /// </summary>
        public bool DraggedSincePress { get; private set; }

        private Vector3 grabWorld;
        private Vector2 pressScreen;
        private bool dragging;
        private float pinchStartDistance;
        private float pinchStartSize;

        private void Awake()
        {
            Instance = this;

            if (world == null) world = FindFirstObjectByType<Battlefield>();
            if (targetCamera == null) targetCamera = GetComponent<Camera>();
            if (targetCamera == null) targetCamera = Camera.main;

            if (world != null)
            {
                world.WorldRebuilt += ResetView;
                if (world.Grid != null) ResetView();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (world != null) world.WorldRebuilt -= ResetView;
        }

        /// <summary>
        /// Acilis kadraji: genislik haritayi doldurur ve kamera USUN
        /// oldugu alta oturur. Oyuncu kendi ussunu gormeden basliyorsa
        /// ilk dalgada nereye bakacagini bilemiyor.
        /// </summary>
        public void ResetView()
        {
            if (world == null || world.Grid == null || targetCamera == null) return;

            targetCamera.orthographic = true;

            float aspect = Aspect();
            float widthCells = startVisibleWidthCells > 0f
                ? startVisibleWidthCells
                : world.Grid.Width;

            float size = widthCells * world.Space.CellSize * 0.5f / aspect;
            targetCamera.orthographicSize = ClampSize(size);

            Vector2 bottom = new Vector2(0f, world.Space.Origin.y);
            MoveTo(bottom);
        }

        private float Aspect()
        {
            float aspect = targetCamera.aspect;
            return aspect > 0f ? aspect : 9f / 16f;
        }

        private void Update()
        {
            if (world == null || world.Grid == null || targetCamera == null) return;

            if (Input.touchCount >= 2) HandlePinch();
            else HandleDragAndWheel();
        }

        private void HandleDragAndWheel()
        {
            pinchStartDistance = 0f;

            float wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) > 0.01f)
                Zoom(targetCamera.orthographicSize - wheel * wheelZoomSpeed);

            if (Input.GetMouseButtonDown(0))
            {
                pressScreen = Input.mousePosition;
                grabWorld = ScreenToWorld(Input.mousePosition);
                dragging = true;
                DraggedSincePress = false;
                return;
            }

            if (Input.GetMouseButtonUp(0))
            {
                dragging = false;
                return;
            }

            if (!dragging || !Input.GetMouseButton(0)) return;

            if (!DraggedSincePress &&
                Vector2.Distance(pressScreen, Input.mousePosition) > dragThreshold)
                DraggedSincePress = true;

            if (!DraggedSincePress) return;

            // Kaydirma noktayi PARMAGA yapistirir: basilan dunya noktasi
            // parmagin altinda kalir. Sabit hizli kaydirma daha basit
            // olurdu ama harita parmagin altindan kayiyormus gibi durur.
            Vector3 now = ScreenToWorld(Input.mousePosition);
            MoveTo((Vector2)transform.position - (Vector2)(now - grabWorld));
        }

        private void HandlePinch()
        {
            Touch a = Input.GetTouch(0);
            Touch b = Input.GetTouch(1);

            // Iki parmak degdigi anda dokunus artik kule kurmaz.
            DraggedSincePress = true;
            dragging = false;

            float distance = Vector2.Distance(a.position, b.position);

            if (pinchStartDistance <= 0f || a.phase == TouchPhase.Began || b.phase == TouchPhase.Began)
            {
                pinchStartDistance = distance;
                pinchStartSize = targetCamera.orthographicSize;
                return;
            }

            if (distance <= 1f) return;

            Zoom(pinchStartSize * (pinchStartDistance / distance));
        }

        private void Zoom(float size)
        {
            targetCamera.orthographicSize = ClampSize(size);
            MoveTo(transform.position);
        }

        private Vector3 ScreenToWorld(Vector3 screen)
        {
            Vector3 world = targetCamera.ScreenToWorldPoint(screen);
            world.z = 0f;
            return world;
        }

        /// <summary>Kamerayi harita sinirlari icinde kalacak sekilde tasir.</summary>
        private void MoveTo(Vector2 center)
        {
            float halfHeight = targetCamera.orthographicSize;
            float halfWidth = halfHeight * Aspect();

            Vector2 clamped = ClampCenter(center, world.Space.Origin,
                                          world.Space.WorldWidth, world.Space.WorldHeight,
                                          halfWidth - padding, halfHeight - padding);

            transform.position = new Vector3(clamped.x, clamped.y, transform.position.z);
        }

        private float ClampSize(float size)
        {
            float aspect = Aspect();

            // Ust sinir: haritanin tamami. Daha genise acmak bos zemin
            // gostermekten baska bir sey yapmaz.
            float max = world.Space.RequiredOrthographicSize(aspect);
            float min = minVisibleCells * world.Space.CellSize * 0.5f;

            if (min > max) min = max;
            return Mathf.Clamp(size, min, max);
        }

        /// <summary>
        /// Kamera merkezini harita dikdortgeninin icinde tutar.
        /// Gorunum bir eksende haritadan genisse o eksende ORTALANIR -
        /// aksi halde kamera kenara yapisip haritanin yarisini bos
        /// birakirdi.
        ///
        /// Saf fonksiyon: kamerasiz test edilebilsin diye disarida
        /// (bkz. MapCameraTests).
        /// </summary>
        public static Vector2 ClampCenter(Vector2 center, Vector2 origin,
                                          float worldWidth, float worldHeight,
                                          float halfWidth, float halfHeight)
        {
            float minX = origin.x + halfWidth;
            float maxX = origin.x + worldWidth - halfWidth;
            float minY = origin.y + halfHeight;
            float maxY = origin.y + worldHeight - halfHeight;

            float x = minX > maxX ? origin.x + worldWidth * 0.5f : Mathf.Clamp(center.x, minX, maxX);
            float y = minY > maxY ? origin.y + worldHeight * 0.5f : Mathf.Clamp(center.y, minY, maxY);

            return new Vector2(x, y);
        }
    }
}
