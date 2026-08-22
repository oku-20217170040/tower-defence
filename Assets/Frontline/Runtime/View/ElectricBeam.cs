using System.Collections.Generic;
using UnityEngine;

namespace Frontline.Runtime
{
    /// <summary>
    /// Isin kulesinin atisi: ALEVLI ELEKTRIK yayi. Kozmetiktir.
    ///
    /// Uc kural degismedi (22 Agu kullanici karari, korunuyor):
    ///   1. INCE. Gorsel kalinlik hasar kalinligindan AYRIDIR; kalin
    ///      cizgi ekrani kaplayan bir bant gibi duruyordu.
    ///   2. UCU EKRAN DISINDA biter - oyuncu isinin nerede durdugunu
    ///      gormez, uzunluk kameradan hesaplanir.
    ///   3. Vurdugu birlik sayisi arttikca PARLAR, kalinlasmaz.
    ///
    /// Duz cizginin yerine ne geldi: cizgi hala bastan sona kesiyor ama
    /// artik yolu KIRIKLI ve her karede yeniden kiriliyor - elektrik
    /// boyle okunur. Sapma genligi kalinliga baglidir (1,2 kat), yani
    /// yay kirikli oldugu halde INCE kalir; sabit bir sapma verilseydi
    /// yakinlastirmaya gore kah tel kah sel gorunurdu.
    ///
    /// Iki katman: disda mavi govde, icde daha ince beyaz-sicak cekirdek.
    /// Alev hissi bu ikiliden gelir - tek renkli yay "neon boru" gibi
    /// duruyordu.
    ///
    /// Havuzlanir; her atis bir GameObject yaratsaydi cop toplayici
    /// kalabalik dalgada nefes alamazdi.
    /// </summary>
    public sealed class ElectricBeam : MonoBehaviour
    {
        private static readonly Stack<ElectricBeam> pool = new Stack<ElectricBeam>();
        private static Transform root;
        private static Material sharedMaterial;

        /// <summary>Yayin kac parcaya bolunecegi. Az olursa duz cizgi,
        /// cok olursa titresen bir bulut olur.</summary>
        private const int Segments = 26;

        private LineRenderer glow;
        private LineRenderer core;
        private readonly Vector3[] points = new Vector3[Segments + 1];

        private Vector2 origin;
        private Vector2 direction;
        private float length;
        private float thickness;
        private float life;
        private float age;
        private Color color;
        private float jitterTimer;

        private static Material SharedMaterial()
        {
            if (sharedMaterial != null) return sharedMaterial;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");

            sharedMaterial = new Material(shader) { name = "ElectricBeam" };
            return sharedMaterial;
        }

        private static LineRenderer MakeLine(GameObject host, int sortingOrder)
        {
            var line = host.AddComponent<LineRenderer>();
            line.material = SharedMaterial();
            line.useWorldSpace = true;
            line.textureMode = LineTextureMode.Stretch;
            line.alignment = LineAlignment.View;
            line.numCapVertices = 0;
            line.numCornerVertices = 2;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sortingOrder = sortingOrder;
            line.positionCount = Segments + 1;
            return line;
        }

        private static ElectricBeam Rent()
        {
            if (pool.Count > 0)
            {
                ElectricBeam reused = pool.Pop();
                reused.gameObject.SetActive(true);
                return reused;
            }

            if (root == null) root = new GameObject("ElectricBeams").transform;

            var go = new GameObject("ElectricBeam");
            go.transform.SetParent(root, false);

            var beam = go.AddComponent<ElectricBeam>();
            beam.glow = MakeLine(go, 32);

            var coreGo = new GameObject("Core");
            coreGo.transform.SetParent(go.transform, false);
            beam.core = MakeLine(coreGo, 33);

            return beam;
        }

        /// <summary>
        /// Kaynaktan verilen dogrultuda, ekran disinda biten elektrik yayi.
        /// Kalinlik DUNYA birimindedir ve hasar hattiyla ilgisi yoktur.
        /// </summary>
        public static void Spawn(Vector2 origin, Vector2 direction, float length,
                                 float thickness, Color color, float seconds)
        {
            if (length <= 0f || seconds <= 0f) return;

            ElectricBeam beam = Rent();
            beam.origin = origin;
            beam.direction = direction.normalized;
            beam.length = length;
            beam.thickness = thickness;
            beam.color = color;
            beam.life = seconds;
            beam.age = 0f;
            beam.jitterTimer = 0f;

            beam.Rebuild(1f);
        }

        /// <summary>
        /// Yayi yeniden kirar. Sapma uclarda SIFIRA yaklasir: yay
        /// namludan tam namlu ucunda cikmali, yoksa kulenin yaninda
        /// baslayan kopuk bir cizgi gibi durur.
        /// </summary>
        private void Rebuild(float intensity)
        {
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);
            float amplitude = thickness * 1.2f * intensity;

            for (int i = 0; i <= Segments; i++)
            {
                float t = i / (float)Segments;

                // Uclarda sonen zarf: sin egrisi 0 ve 1'de sifirdir.
                float envelope = Mathf.Sin(t * Mathf.PI);
                float offset = i == 0 || i == Segments
                    ? 0f
                    : Random.Range(-amplitude, amplitude) * envelope;

                Vector2 p = origin + direction * (length * t) + perpendicular * offset;
                points[i] = new Vector3(p.x, p.y, 0f);
            }

            glow.SetPositions(points);
            core.SetPositions(points);
        }

        private void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / life);

            if (t >= 1f)
            {
                gameObject.SetActive(false);
                pool.Push(this);
                return;
            }

            // Elektrik SABIT duramaz: yay saniyede ~60 kez yeniden
            // kirilir. Her karede kirmak da olurdu ama kare hizina bagli
            // bir gorsel telefonda baska turlu titrer.
            jitterTimer -= Time.deltaTime;
            if (jitterTimer <= 0f)
            {
                jitterTimer += 1f / 60f;
                Rebuild(1f - t * 0.5f);
            }

            // Alev hissi: kalinlik hafifce titrer ama ORTALAMASI sabit
            // kalir - kalinlasma inceligi bozuyordu.
            float flicker = 0.85f + Mathf.PerlinNoise(Time.time * 40f, 0f) * 0.35f;
            float fade = 1f - t * t;

            float width = thickness * flicker;
            glow.startWidth = glow.endWidth = width;
            core.startWidth = core.endWidth = width * 0.45f;

            Color glowColor = color;
            glowColor.a = color.a * fade;
            glow.startColor = glow.endColor = glowColor;

            // Cekirdek beyaza kacar: sicak merkez + soguk kenar,
            // alevin ve elektrigin ortak imzasi.
            Color coreColor = Color.Lerp(color, Color.white, 0.75f);
            coreColor.a = color.a * fade;
            core.startColor = core.endColor = coreColor;
        }
    }
}
