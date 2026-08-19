# Koloni Çekirdeği — Uygulama Planı (Plan 1)

> **Agent'lar için:** Bu plan görev görev uygulanır. Adımlar `- [ ]` kutucuk sözdizimi ile takip edilir.

**Hedef:** Kandırma mekaniğinin matematiksel çekirdeğini (feromon, tehdit, yem, rota maliyeti) ve doğrulanmış prosedürel harita üretimini, MonoBehaviour'suz saf C# olarak ve tam birim testli biçimde kurmak.

**Mimari:** `Assets/Colony/Core/` altında Unity'ye bağımlı olmayan yeni bir assembly (`Colony.Core`). Hiçbir sınıf MonoBehaviour değildir, hiçbiri sahneye bağlı değildir — bu sayede EditMode testlerinde saniyeler içinde koşar. Mevcut `Assets/Scripts/` dokunulmadan yerinde kalır; eski ve yeni kod bir süre yan yana yaşar.

**Tech Stack:** Unity 6000.3.21f1, C#, Unity Test Framework 1.6.0 (NUnit), Assembly Definition dosyaları.

## Global Constraints

- Unity sürümü: 6000.3.21f1
- Hedef platform: Android, **dikey tutuş**
- Assembly: `Colony.Core` hiçbir MonoBehaviour içermez ve `Assembly-CSharp`'a referans vermez
- `Colony.Core` asmdef'inde `noEngineReferences: true` — dolayısıyla `Vector2Int`, `Mathf` gibi Unity tipleri **kullanılamaz**
- Namespace: tüm çekirdek sınıflar `Colony.Core` altındadır
- Dil: yorumlar Türkçe, tanımlayıcılar İngilizce
- **Rota maliyeti asla negatif olamaz** (A* doğruluğu için; bkz. Görev 5)
- Feromon *çeker*, tehdit *iter* — ikisi aynı maliyet fonksiyonunda birleşir
- Branch: `tasarim/koloni-reboot`
- Her görev kendi testleriyle biter ve ayrı commit alır

---

### Görev 1: Test altyapısı ve ColonyGrid

**Dosyalar:**
- Oluştur: `Assets/Colony/Core/Colony.Core.asmdef`
- Oluştur: `Assets/Colony/Core/Grid/CellKind.cs`
- Oluştur: `Assets/Colony/Core/Grid/ColonyGrid.cs`
- Oluştur: `Assets/Colony/Tests/EditMode/Colony.Tests.EditMode.asmdef`
- Oluştur: `Assets/Colony/Tests/EditMode/ColonyGridTests.cs`

**Arayüzler:**
- Üretir: `Colony.Core.CellKind` enum; `Colony.Core.ColonyGrid` sınıfı — `Width`, `Height`, `CellCount`, `Index(int,int)`, `X(int)`, `Y(int)`, `InBounds(int,int)`, `Get(int)`, `Set(int,CellKind)`, `IsWalkable(int)`, `GetNeighbors(int,int[])`

- [ ] **Adım 1: Assembly tanımlarını oluştur**

`Assets/Colony/Core/Colony.Core.asmdef`:
```json
{
    "name": "Colony.Core",
    "rootNamespace": "Colony.Core",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": true
}
```

`Assets/Colony/Tests/EditMode/Colony.Tests.EditMode.asmdef`:
```json
{
    "name": "Colony.Tests.EditMode",
    "rootNamespace": "Colony.Tests",
    "references": [
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner",
        "Colony.Core"
    ],
    "includePlatforms": [ "Editor" ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [ "nunit.framework.dll" ],
    "autoReferenced": false,
    "defineConstraints": [ "UNITY_INCLUDE_TESTS" ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

`noEngineReferences: true` kritiktir: çekirdek UnityEngine'e bağımlı olmaz, bu yüzden koordinatlar düz `int` indeks ve `(x, y)` çiftleri ile taşınır.

- [ ] **Adım 2: Başarısız testi yaz**

`Assets/Colony/Tests/EditMode/ColonyGridTests.cs`:
```csharp
using NUnit.Framework;
using Colony.Core;

namespace Colony.Tests
{
    public class ColonyGridTests
    {
        [Test]
        public void Index_And_Coordinates_RoundTrip()
        {
            var grid = new ColonyGrid(11, 20);
            int index = grid.Index(7, 13);
            Assert.AreEqual(7, grid.X(index));
            Assert.AreEqual(13, grid.Y(index));
        }

        [Test]
        public void NewGrid_AllCellsBlocked()
        {
            var grid = new ColonyGrid(4, 4);
            for (int i = 0; i < grid.CellCount; i++)
                Assert.AreEqual(CellKind.Blocked, grid.Get(i));
        }

        [Test]
        public void IsWalkable_OnlyCorridorCounts()
        {
            var grid = new ColonyGrid(3, 3);
            grid.Set(grid.Index(1, 1), CellKind.Corridor);
            grid.Set(grid.Index(2, 2), CellKind.BuildStrip);

            Assert.IsTrue(grid.IsWalkable(grid.Index(1, 1)));
            Assert.IsFalse(grid.IsWalkable(grid.Index(2, 2)));
            Assert.IsFalse(grid.IsWalkable(grid.Index(0, 0)));
        }

        [Test]
        public void GetNeighbors_CornerCell_ReturnsTwo()
        {
            var grid = new ColonyGrid(5, 5);
            var buffer = new int[4];
            Assert.AreEqual(2, grid.GetNeighbors(grid.Index(0, 0), buffer));
        }

        [Test]
        public void GetNeighbors_CenterCell_ReturnsFour()
        {
            var grid = new ColonyGrid(5, 5);
            var buffer = new int[4];
            Assert.AreEqual(4, grid.GetNeighbors(grid.Index(2, 2), buffer));
        }

        [Test]
        public void Constructor_RejectsNonPositiveSize()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new ColonyGrid(0, 5));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new ColonyGrid(5, -1));
        }
    }
}
```

- [ ] **Adım 3: Testin başarısız olduğunu doğrula**

Unity MCP `run_tests` aracı, `mode: "EditMode"`, `testFilter: "Colony.Tests.ColonyGridTests"`.
Beklenen: derleme hatası — `ColonyGrid` tipi bulunamıyor.

- [ ] **Adım 4: CellKind ve ColonyGrid'i yaz**

`Assets/Colony/Core/Grid/CellKind.cs`:
```csharp
namespace Colony.Core
{
    /// <summary>Bir grid hücresinin türü.</summary>
    public enum CellKind
    {
        /// <summary>Geçilemez ve üzerine kule kurulamaz.</summary>
        Blocked = 0,
        /// <summary>Böceklerin yürüdüğü koridor hücresi.</summary>
        Corridor = 1,
        /// <summary>Kule kurulabilen şerit. Böcekler buradan geçmez.</summary>
        BuildStrip = 2
    }
}
```

`Assets/Colony/Core/Grid/ColonyGrid.cs`:
```csharp
using System;

namespace Colony.Core
{
    /// <summary>
    /// Oyunun harita ızgarası. Dikey tutuş için tasarlanmıştır:
    /// y = Height - 1 üst kenar (böceklerin girişi), y = 0 alt kenar (yuva).
    /// Unity'ye bağımlı değildir; koordinatlar düz int indeks olarak taşınır.
    /// </summary>
    public sealed class ColonyGrid
    {
        private readonly CellKind[] cells;

        public int Width { get; }
        public int Height { get; }
        public int CellCount => cells.Length;

        public ColonyGrid(int width, int height)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), "Genişlik pozitif olmalı.");
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height), "Yükseklik pozitif olmalı.");

            Width = width;
            Height = height;
            cells = new CellKind[width * height];
        }

        public int Index(int x, int y) => y * Width + x;
        public int X(int index) => index % Width;
        public int Y(int index) => index / Width;

        public bool InBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

        public CellKind Get(int index) => cells[index];
        public void Set(int index, CellKind kind) => cells[index] = kind;

        /// <summary>Yalnızca koridor hücreleri yürünebilir.</summary>
        public bool IsWalkable(int index) => cells[index] == CellKind.Corridor;

        /// <summary>
        /// 4 yönlü komşuları buffer'a yazar ve sayısını döndürür.
        /// Buffer en az 4 elemanlı olmalıdır. Yürünebilirlik kontrolü yapmaz.
        /// </summary>
        public int GetNeighbors(int index, int[] buffer)
        {
            if (buffer == null || buffer.Length < 4)
                throw new ArgumentException("Buffer en az 4 elemanlı olmalı.", nameof(buffer));

            int x = X(index);
            int y = Y(index);
            int count = 0;

            if (y + 1 < Height) buffer[count++] = Index(x, y + 1);
            if (y - 1 >= 0)     buffer[count++] = Index(x, y - 1);
            if (x - 1 >= 0)     buffer[count++] = Index(x - 1, y);
            if (x + 1 < Width)  buffer[count++] = Index(x + 1, y);

            return count;
        }
    }
}
```

- [ ] **Adım 5: Testlerin geçtiğini doğrula**

Unity MCP `run_tests`, filtre `Colony.Tests.ColonyGridTests`.
Beklenen: 6 test PASS.

- [ ] **Adım 6: Commit**

```bash
git add Assets/Colony
git commit -m "feat: Colony.Core assembly ve ColonyGrid"
```

---

### Görev 2: PheromoneMap — koloni hafızası

**Dosyalar:**
- Oluştur: `Assets/Colony/Core/Fields/PheromoneMap.cs`
- Oluştur: `Assets/Colony/Tests/EditMode/PheromoneMapTests.cs`

**Arayüzler:**
- Kullanır: `ColonyGrid` (Görev 1)
- Üretir: `PheromoneMap` — `Max`, `DecayRate`, `Get(int)`, `Deposit(int,float)`, `Weaken(int,float)`, `DepositTrail(IReadOnlyList<int>,float)`, `WeakenTrail(IReadOnlyList<int>,float)`, `Decay()`, `Clear()`

Tasarım kaynağı: spec bölüm 3 — "işe yarayan rotalar güçlenir, ölümle biten rotalar sönümlenir".

- [ ] **Adım 1: Başarısız testi yaz**

`Assets/Colony/Tests/EditMode/PheromoneMapTests.cs`:
```csharp
using System.Collections.Generic;
using NUnit.Framework;
using Colony.Core;

namespace Colony.Tests
{
    public class PheromoneMapTests
    {
        private static ColonyGrid NewGrid() => new ColonyGrid(5, 5);

        [Test]
        public void Deposit_IncreasesValue()
        {
            var map = new PheromoneMap(NewGrid(), max: 100f, decayRate: 0.1f);
            map.Deposit(3, 10f);
            Assert.AreEqual(10f, map.Get(3), 0.001f);
        }

        [Test]
        public void Deposit_ClampsAtMax()
        {
            var map = new PheromoneMap(NewGrid(), max: 20f, decayRate: 0.1f);
            map.Deposit(3, 500f);
            Assert.AreEqual(20f, map.Get(3), 0.001f);
        }

        [Test]
        public void Weaken_NeverGoesBelowZero()
        {
            var map = new PheromoneMap(NewGrid(), max: 100f, decayRate: 0.1f);
            map.Deposit(3, 5f);
            map.Weaken(3, 50f);
            Assert.AreEqual(0f, map.Get(3), 0.001f);
        }

        [Test]
        public void DepositTrail_AffectsEveryCellOnPath()
        {
            var map = new PheromoneMap(NewGrid(), max: 100f, decayRate: 0.1f);
            map.DepositTrail(new List<int> { 1, 2, 3 }, 7f);

            Assert.AreEqual(7f, map.Get(1), 0.001f);
            Assert.AreEqual(7f, map.Get(2), 0.001f);
            Assert.AreEqual(7f, map.Get(3), 0.001f);
            Assert.AreEqual(0f, map.Get(4), 0.001f);
        }

        [Test]
        public void WeakenTrail_ReducesEveryCellOnPath()
        {
            var map = new PheromoneMap(NewGrid(), max: 100f, decayRate: 0.1f);
            map.DepositTrail(new List<int> { 1, 2 }, 10f);
            map.WeakenTrail(new List<int> { 1, 2 }, 4f);

            Assert.AreEqual(6f, map.Get(1), 0.001f);
            Assert.AreEqual(6f, map.Get(2), 0.001f);
        }

        [Test]
        public void Decay_ReducesAllValuesProportionally()
        {
            var map = new PheromoneMap(NewGrid(), max: 100f, decayRate: 0.25f);
            map.Deposit(0, 100f);
            map.Decay();
            Assert.AreEqual(75f, map.Get(0), 0.001f);
        }

        [Test]
        public void Decay_RepeatedlyApproachesZero_NeverNegative()
        {
            var map = new PheromoneMap(NewGrid(), max: 100f, decayRate: 0.5f);
            map.Deposit(0, 100f);
            for (int i = 0; i < 50; i++) map.Decay();

            Assert.GreaterOrEqual(map.Get(0), 0f);
            Assert.Less(map.Get(0), 0.001f);
        }

        [Test]
        public void Constructor_RejectsInvalidParameters()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => new PheromoneMap(NewGrid(), max: 0f, decayRate: 0.1f));
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => new PheromoneMap(NewGrid(), max: 10f, decayRate: 1.5f));
        }
    }
}
```

- [ ] **Adım 2: Testin başarısız olduğunu doğrula**

Unity MCP `run_tests`, filtre `Colony.Tests.PheromoneMapTests`.
Beklenen: derleme hatası — `PheromoneMap` bulunamıyor.

- [ ] **Adım 3: PheromoneMap'i yaz**

`Assets/Colony/Core/Fields/PheromoneMap.cs`:
```csharp
using System;
using System.Collections.Generic;

namespace Colony.Core
{
    /// <summary>
    /// Kolonin öğrenilmiş rota hafızası. Böcekler geçtikleri hücrelere feromon
    /// bırakır; yuvaya ulaşan rota güçlenir, ölümle biten rota sönümlenir.
    /// Yüksek feromon = tercih edilen rota (çeker, itmez).
    /// </summary>
    public sealed class PheromoneMap
    {
        private readonly float[] values;

        /// <summary>Bir hücrenin taşıyabileceği en yüksek feromon değeri.</summary>
        public float Max { get; }

        /// <summary>Her Decay() çağrısında kaybedilen oran (0-1).</summary>
        public float DecayRate { get; }

        public PheromoneMap(ColonyGrid grid, float max, float decayRate)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (max <= 0f)
                throw new ArgumentOutOfRangeException(nameof(max), "Max pozitif olmalı.");
            if (decayRate < 0f || decayRate > 1f)
                throw new ArgumentOutOfRangeException(nameof(decayRate), "DecayRate 0-1 aralığında olmalı.");

            values = new float[grid.CellCount];
            Max = max;
            DecayRate = decayRate;
        }

        public float Get(int index) => values[index];

        public void Deposit(int index, float amount)
        {
            float v = values[index] + amount;
            values[index] = v > Max ? Max : v;
        }

        public void Weaken(int index, float amount)
        {
            float v = values[index] - amount;
            values[index] = v < 0f ? 0f : v;
        }

        public void DepositTrail(IReadOnlyList<int> path, float amountPerCell)
        {
            if (path == null) return;
            for (int i = 0; i < path.Count; i++)
                Deposit(path[i], amountPerCell);
        }

        public void WeakenTrail(IReadOnlyList<int> path, float amountPerCell)
        {
            if (path == null) return;
            for (int i = 0; i < path.Count; i++)
                Weaken(path[i], amountPerCell);
        }

        /// <summary>Dalga sonunda çağrılır: tüm izler orantılı olarak sönümlenir.</summary>
        public void Decay()
        {
            float keep = 1f - DecayRate;
            for (int i = 0; i < values.Length; i++)
                values[i] *= keep;
        }

        public void Clear() => Array.Clear(values, 0, values.Length);
    }
}
```

- [ ] **Adım 4: Testlerin geçtiğini doğrula**

Unity MCP `run_tests`, filtre `Colony.Tests.PheromoneMapTests`.
Beklenen: 8 test PASS.

- [ ] **Adım 5: Commit**

```bash
git add Assets/Colony
git commit -m "feat: PheromoneMap — koloni rota hafızası"
```

---

### Görev 3: LureField — sahte feromon ve azalan getiri

**Dosyalar:**
- Oluştur: `Assets/Colony/Core/Fields/LureField.cs`
- Oluştur: `Assets/Colony/Tests/EditMode/LureFieldTests.cs`

**Arayüzler:**
- Kullanır: `ColonyGrid` (Görev 1)
- Üretir: `LureField` — `BaseStrength`, `DiminishFactor`, `Get(int)`, `Apply(int)`, `ApplyCount(int)`, `Clear()`

Tasarım kaynağı: spec bölüm 9.2. **Sabit ekleme, oransal değil** — güçlü izleri tek başına çeviremesin. Aynı hücrede **azalan getiri**.

- [ ] **Adım 1: Başarısız testi yaz**

`Assets/Colony/Tests/EditMode/LureFieldTests.cs`:
```csharp
using NUnit.Framework;
using Colony.Core;

namespace Colony.Tests
{
    public class LureFieldTests
    {
        private static ColonyGrid NewGrid() => new ColonyGrid(5, 5);

        [Test]
        public void Apply_AddsBaseStrength_FirstTime()
        {
            var lure = new LureField(NewGrid(), baseStrength: 10f, diminishFactor: 0.5f);
            lure.Apply(4);
            Assert.AreEqual(10f, lure.Get(4), 0.001f);
        }

        [Test]
        public void Apply_SecondTime_AddsDiminishedAmount()
        {
            var lure = new LureField(NewGrid(), baseStrength: 10f, diminishFactor: 0.5f);
            lure.Apply(4);
            lure.Apply(4);
            // 10 + (10 * 0.5) = 15
            Assert.AreEqual(15f, lure.Get(4), 0.001f);
        }

        [Test]
        public void Apply_ThirdTime_KeepsDiminishing()
        {
            var lure = new LureField(NewGrid(), baseStrength: 10f, diminishFactor: 0.5f);
            lure.Apply(4);
            lure.Apply(4);
            lure.Apply(4);
            // 10 + 5 + 2.5 = 17.5
            Assert.AreEqual(17.5f, lure.Get(4), 0.001f);
        }

        [Test]
        public void Apply_ManyTimes_ConvergesToFiniteCeiling()
        {
            var lure = new LureField(NewGrid(), baseStrength: 10f, diminishFactor: 0.5f);
            for (int i = 0; i < 200; i++) lure.Apply(4);
            // Geometrik seri limiti: 10 / (1 - 0.5) = 20
            Assert.Less(lure.Get(4), 20.001f);
            Assert.Greater(lure.Get(4), 19.9f);
        }

        [Test]
        public void Apply_IsPerCell_NotGlobal()
        {
            var lure = new LureField(NewGrid(), baseStrength: 10f, diminishFactor: 0.5f);
            lure.Apply(4);
            lure.Apply(4);
            lure.Apply(7);
            Assert.AreEqual(10f, lure.Get(7), 0.001f);
        }

        [Test]
        public void Clear_ResetsValuesAndCounts()
        {
            var lure = new LureField(NewGrid(), baseStrength: 10f, diminishFactor: 0.5f);
            lure.Apply(4);
            lure.Apply(4);
            lure.Clear();

            Assert.AreEqual(0f, lure.Get(4), 0.001f);
            Assert.AreEqual(0, lure.ApplyCount(4));

            lure.Apply(4);
            Assert.AreEqual(10f, lure.Get(4), 0.001f);
        }

        [Test]
        public void Constructor_RejectsInvalidParameters()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => new LureField(NewGrid(), baseStrength: -1f, diminishFactor: 0.5f));
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => new LureField(NewGrid(), baseStrength: 10f, diminishFactor: 1f));
        }
    }
}
```

- [ ] **Adım 2: Testin başarısız olduğunu doğrula**

Unity MCP `run_tests`, filtre `Colony.Tests.LureFieldTests`.
Beklenen: derleme hatası — `LureField` bulunamıyor.

- [ ] **Adım 3: LureField'i yaz**

`Assets/Colony/Core/Fields/LureField.cs`:
```csharp
using System;

namespace Colony.Core
{
    /// <summary>
    /// Sahte Feromon Yayıcı'nın ürettiği yapay çekim alanı.
    /// Gerçek feromondan ayrı tutulur çünkü kuralları farklıdır:
    /// sabit miktar ekler (oransal değil) ve aynı hücrede azalan getiri verir.
    /// Böylece yem zayıf izleri kolayca yönlendirir, oturmuş güçlü izi çeviremez.
    /// </summary>
    public sealed class LureField
    {
        private readonly float[] values;
        private readonly int[] applyCounts;

        /// <summary>İlk uygulamada eklenen miktar. Tüm yem dengesi bu sayıya bağlıdır.</summary>
        public float BaseStrength { get; }

        /// <summary>Her tekrarda önceki eklemenin çarpanı. [0, 1) aralığında.</summary>
        public float DiminishFactor { get; }

        public LureField(ColonyGrid grid, float baseStrength, float diminishFactor)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (baseStrength < 0f)
                throw new ArgumentOutOfRangeException(nameof(baseStrength), "BaseStrength negatif olamaz.");
            if (diminishFactor < 0f || diminishFactor >= 1f)
                throw new ArgumentOutOfRangeException(nameof(diminishFactor), "DiminishFactor [0, 1) aralığında olmalı.");

            values = new float[grid.CellCount];
            applyCounts = new int[grid.CellCount];
            BaseStrength = baseStrength;
            DiminishFactor = diminishFactor;
        }

        public float Get(int index) => values[index];

        public int ApplyCount(int index) => applyCounts[index];

        /// <summary>
        /// Hücreye yem uygular. n'inci uygulama BaseStrength * DiminishFactor^(n-1) ekler.
        /// Toplam, geometrik seri limiti olan BaseStrength / (1 - DiminishFactor) değerini aşmaz.
        /// </summary>
        public void Apply(int index)
        {
            float amount = BaseStrength * Pow(DiminishFactor, applyCounts[index]);
            values[index] += amount;
            applyCounts[index]++;
        }

        public void Clear()
        {
            Array.Clear(values, 0, values.Length);
            Array.Clear(applyCounts, 0, applyCounts.Length);
        }

        /// <summary>
        /// Assembly noEngineReferences olduğu için Mathf kullanılamaz.
        /// Üs tam sayı olduğundan basit döngü, Math.Pow'un double dönüşümünden ucuzdur.
        /// </summary>
        private static float Pow(float baseValue, int exponent)
        {
            float result = 1f;
            for (int i = 0; i < exponent; i++)
                result *= baseValue;
            return result;
        }
    }
}
```

- [ ] **Adım 4: Testlerin geçtiğini doğrula**

Unity MCP `run_tests`, filtre `Colony.Tests.LureFieldTests`.
Beklenen: 7 test PASS.

- [ ] **Adım 5: Commit**

```bash
git add Assets/Colony
git commit -m "feat: LureField — sahte feromon, azalan getiri"
```

---

### Görev 4: ThreatField — yalnızca gürültücü kuleler

**Dosyalar:**
- Oluştur: `Assets/Colony/Core/Fields/ThreatField.cs`
- Oluştur: `Assets/Colony/Tests/EditMode/ThreatFieldTests.cs`

**Arayüzler:**
- Kullanır: `ColonyGrid` (Görev 1)
- Üretir: `ThreatField` — `Get(int)`, `AddSource(int,float,float)`, `Clear()`

Tasarım kaynağı: spec bölüm 3.1. Sessiz kuleler bu alana **hiç yazmaz** — bu, sınıf ayrımının tek teknik karşılığıdır. Kulenin sessiz mi gürültücü mü olduğu bilgisi burada değil, çağıran tarafta yaşar: sessiz kule `AddSource` çağırmaz.

- [ ] **Adım 1: Başarısız testi yaz**

`Assets/Colony/Tests/EditMode/ThreatFieldTests.cs`:
```csharp
using NUnit.Framework;
using Colony.Core;

namespace Colony.Tests
{
    public class ThreatFieldTests
    {
        private static ColonyGrid NewGrid() => new ColonyGrid(9, 9);

        [Test]
        public void AddSource_CenterHasFullStrength()
        {
            var grid = NewGrid();
            var threat = new ThreatField(grid);
            int center = grid.Index(4, 4);

            threat.AddSource(center, radius: 2f, strength: 10f);

            Assert.AreEqual(10f, threat.Get(center), 0.001f);
        }

        [Test]
        public void AddSource_FallsOffWithDistance()
        {
            var grid = NewGrid();
            var threat = new ThreatField(grid);
            threat.AddSource(grid.Index(4, 4), radius: 2f, strength: 10f);

            float near = threat.Get(grid.Index(5, 4));
            float far  = threat.Get(grid.Index(6, 4));

            Assert.Greater(near, far);
            Assert.Greater(far, 0f);
        }

        [Test]
        public void AddSource_OutsideRadius_IsZero()
        {
            var grid = NewGrid();
            var threat = new ThreatField(grid);
            threat.AddSource(grid.Index(4, 4), radius: 2f, strength: 10f);

            Assert.AreEqual(0f, threat.Get(grid.Index(8, 4)), 0.001f);
        }

        [Test]
        public void AddSource_MultipleSources_Accumulate()
        {
            var grid = NewGrid();
            var threat = new ThreatField(grid);
            int cell = grid.Index(4, 4);

            threat.AddSource(cell, radius: 1f, strength: 5f);
            threat.AddSource(cell, radius: 1f, strength: 3f);

            Assert.AreEqual(8f, threat.Get(cell), 0.001f);
        }

        [Test]
        public void Clear_RemovesAllThreat()
        {
            var grid = NewGrid();
            var threat = new ThreatField(grid);
            int cell = grid.Index(4, 4);

            threat.AddSource(cell, radius: 2f, strength: 10f);
            threat.Clear();

            Assert.AreEqual(0f, threat.Get(cell), 0.001f);
        }

        [Test]
        public void AddSource_NeverProducesNegativeValues()
        {
            var grid = NewGrid();
            var threat = new ThreatField(grid);
            threat.AddSource(grid.Index(0, 0), radius: 3f, strength: 10f);

            for (int i = 0; i < grid.CellCount; i++)
                Assert.GreaterOrEqual(threat.Get(i), 0f);
        }

        [Test]
        public void AddSource_IgnoresNonPositiveRadiusOrStrength()
        {
            var grid = NewGrid();
            var threat = new ThreatField(grid);
            int cell = grid.Index(4, 4);

            threat.AddSource(cell, radius: 0f, strength: 10f);
            threat.AddSource(cell, radius: 2f, strength: 0f);

            Assert.AreEqual(0f, threat.Get(cell), 0.001f);
        }
    }
}
```

- [ ] **Adım 2: Testin başarısız olduğunu doğrula**

Unity MCP `run_tests`, filtre `Colony.Tests.ThreatFieldTests`.
Beklenen: derleme hatası — `ThreatField` bulunamıyor.

- [ ] **Adım 3: ThreatField'i yaz**

`Assets/Colony/Core/Fields/ThreatField.cs`:
```csharp
using System;

namespace Colony.Core
{
    /// <summary>
    /// Böceklerin algıladığı tehlike haritası. YALNIZCA gürültücü kuleler buraya yazar.
    /// Sessiz kuleler hiç AddSource çağırmaz; koloni onları öğrenemez.
    /// Bu ayrım, oyunun merkezi mekaniğinin tek teknik karşılığıdır.
    /// </summary>
    public sealed class ThreatField
    {
        private readonly ColonyGrid grid;
        private readonly float[] values;

        public ThreatField(ColonyGrid grid)
        {
            this.grid = grid ?? throw new ArgumentNullException(nameof(grid));
            values = new float[grid.CellCount];
        }

        public float Get(int index) => values[index];

        /// <summary>
        /// Bir gürültücü kulenin tehdidini alana ekler.
        /// Merkezde tam güç, yarıçapın kenarında sıfıra doğrusal düşer.
        /// Yarıçap dışına hiç yazmaz. Değerler birikimlidir.
        /// </summary>
        public void AddSource(int centerIndex, float radius, float strength)
        {
            if (radius <= 0f || strength <= 0f) return;

            int cx = grid.X(centerIndex);
            int cy = grid.Y(centerIndex);
            int reach = (int)radius;

            int minX = cx - reach; if (minX < 0) minX = 0;
            int maxX = cx + reach; if (maxX > grid.Width - 1) maxX = grid.Width - 1;
            int minY = cy - reach; if (minY < 0) minY = 0;
            int maxY = cy + reach; if (maxY > grid.Height - 1) maxY = grid.Height - 1;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                    if (distance > radius) continue;

                    float falloff = 1f - (distance / radius);
                    values[grid.Index(x, y)] += strength * falloff;
                }
            }
        }

        public void Clear() => Array.Clear(values, 0, values.Length);
    }
}
```

- [ ] **Adım 4: Testlerin geçtiğini doğrula**

Unity MCP `run_tests`, filtre `Colony.Tests.ThreatFieldTests`.
Beklenen: 7 test PASS.

- [ ] **Adım 5: Commit**

```bash
git add Assets/Colony
git commit -m "feat: ThreatField — gürültücü kule tehdit alanı"
```

---

### Görev 5: ColonyPathfinder — birleşik maliyet modeli

**Dosyalar:**
- Oluştur: `Assets/Colony/Core/Pathing/IndexedMinHeap.cs`
- Oluştur: `Assets/Colony/Core/Pathing/RouteCostModel.cs`
- Oluştur: `Assets/Colony/Core/Pathing/ColonyPathfinder.cs`
- Oluştur: `Assets/Colony/Tests/EditMode/RouteCostModelTests.cs`
- Oluştur: `Assets/Colony/Tests/EditMode/ColonyPathfinderTests.cs`

**Arayüzler:**
- Kullanır: `ColonyGrid`, `PheromoneMap`, `LureField`, `ThreatField`
- Üretir: `IndexedMinHeap` — `Count`, `Push(int,float)`, `Pop()`, `Contains(int)`, `Update(int,float)`, `Clear()`
- Üretir: `RouteCostModel` — `ThreatWeight`, `AttractionWeight`, `StepCost(int)`
- Üretir: `ColonyPathfinder` — `FindPath(int,int,List<int>)`, `bool` döner

**Kritik doğruluk kuralı:** Feromon *çeker*, yani maliyeti düşürmelidir. Ancak negatif kenar maliyeti A*'ı bozar. Çözüm: çekim, sabit bir cezanın **azaltılması** olarak modellenir.

```
stepCost = 1
         + ThreatWeight * threat
         + AttractionWeight * (1 - normalizedAttraction)
```

`normalizedAttraction` [0,1] aralığındadır, dolayısıyla çekim terimi [0, AttractionWeight] aralığında kalır ve maliyet asla 1'in altına inmez. Manhattan sezgiseli bu sayede kabul edilebilir (admissible) kalır ve A* en kısa yol garantisini korur.

- [ ] **Adım 1: RouteCostModel testini yaz**

`Assets/Colony/Tests/EditMode/RouteCostModelTests.cs`:
```csharp
using NUnit.Framework;
using Colony.Core;

namespace Colony.Tests
{
    public class RouteCostModelTests
    {
        private ColonyGrid grid;
        private PheromoneMap pheromone;
        private LureField lure;
        private ThreatField threat;
        private RouteCostModel model;

        [SetUp]
        public void SetUp()
        {
            grid = new ColonyGrid(5, 5);
            pheromone = new PheromoneMap(grid, max: 100f, decayRate: 0.1f);
            lure = new LureField(grid, baseStrength: 10f, diminishFactor: 0.5f);
            threat = new ThreatField(grid);
            model = new RouteCostModel(pheromone, lure, threat)
            {
                ThreatWeight = 2f,
                AttractionWeight = 5f
            };
        }

        [Test]
        public void StepCost_EmptyCell_IsBasePlusFullAttractionPenalty()
        {
            // Çekim yok => 1 + 0 + (1 - 0) * 5 = 6
            Assert.AreEqual(6f, model.StepCost(0), 0.001f);
        }

        [Test]
        public void StepCost_MaxPheromone_RemovesAttractionPenalty()
        {
            pheromone.Deposit(0, 100f);
            Assert.AreEqual(1f, model.StepCost(0), 0.001f);
        }

        [Test]
        public void StepCost_NeverBelowOne()
        {
            pheromone.Deposit(0, 100f);
            for (int i = 0; i < 50; i++) lure.Apply(0);
            Assert.GreaterOrEqual(model.StepCost(0), 1f);
        }

        [Test]
        public void StepCost_ThreatIncreasesCost()
        {
            int cell = grid.Index(2, 2);
            float before = model.StepCost(cell);
            threat.AddSource(cell, radius: 1f, strength: 3f);
            float after = model.StepCost(cell);

            Assert.Greater(after, before);
            Assert.AreEqual(before + 6f, after, 0.001f); // 3 * ThreatWeight(2)
        }

        [Test]
        public void StepCost_LureCountsAsAttraction()
        {
            float before = model.StepCost(0);
            lure.Apply(0);
            float after = model.StepCost(0);
            Assert.Less(after, before);
        }

        [Test]
        public void StepCost_AttractionSaturates_DoesNotOvershoot()
        {
            // Feromon zaten tavanda; yem eklemek maliyeti 1'in altına indirmemeli
            pheromone.Deposit(3, 100f);
            lure.Apply(3);
            lure.Apply(3);
            Assert.AreEqual(1f, model.StepCost(3), 0.001f);
        }
    }
}
```

- [ ] **Adım 2: Testin başarısız olduğunu doğrula**

Unity MCP `run_tests`, filtre `Colony.Tests.RouteCostModelTests`.
Beklenen: derleme hatası — `RouteCostModel` bulunamıyor.

- [ ] **Adım 3: RouteCostModel'i yaz**

`Assets/Colony/Core/Pathing/RouteCostModel.cs`:
```csharp
using System;

namespace Colony.Core
{
    /// <summary>
    /// Üç alanı tek bir adım maliyetinde birleştirir.
    ///
    /// stepCost = 1
    ///          + ThreatWeight * threat
    ///          + AttractionWeight * (1 - normalizedAttraction)
    ///
    /// Çekim (feromon + yem) maliyeti DÜŞÜRÜR ama bunu negatif değer üreterek
    /// değil, sabit bir cezayı azaltarak yapar. Maliyet asla 1'in altına inmez;
    /// bu, A* sezgiselinin kabul edilebilir kalması için zorunludur.
    /// </summary>
    public sealed class RouteCostModel
    {
        private readonly PheromoneMap pheromone;
        private readonly LureField lure;
        private readonly ThreatField threat;

        /// <summary>Çekimin doygunluğa ulaştığı toplam değer. Feromon tavanı ile aynıdır.</summary>
        private readonly float attractionSaturation;

        public float ThreatWeight { get; set; } = 1f;
        public float AttractionWeight { get; set; } = 1f;

        public RouteCostModel(PheromoneMap pheromone, LureField lure, ThreatField threat)
        {
            this.pheromone = pheromone ?? throw new ArgumentNullException(nameof(pheromone));
            this.lure = lure ?? throw new ArgumentNullException(nameof(lure));
            this.threat = threat ?? throw new ArgumentNullException(nameof(threat));
            attractionSaturation = pheromone.Max;
        }

        /// <summary>Bir hücreye adım atmanın maliyeti. Her zaman &gt;= 1.</summary>
        public float StepCost(int index)
        {
            float attraction = pheromone.Get(index) + lure.Get(index);
            float normalized = attraction / attractionSaturation;
            if (normalized > 1f) normalized = 1f;

            float attractionPenalty = AttractionWeight * (1f - normalized);
            float threatPenalty = ThreatWeight * threat.Get(index);

            return 1f + threatPenalty + attractionPenalty;
        }
    }
}
```

- [ ] **Adım 4: RouteCostModel testlerinin geçtiğini doğrula**

Unity MCP `run_tests`, filtre `Colony.Tests.RouteCostModelTests`.
Beklenen: 6 test PASS.

- [ ] **Adım 5: ColonyPathfinder testini yaz**

`Assets/Colony/Tests/EditMode/ColonyPathfinderTests.cs`:
```csharp
using System.Collections.Generic;
using NUnit.Framework;
using Colony.Core;

namespace Colony.Tests
{
    public class ColonyPathfinderTests
    {
        private ColonyGrid grid;
        private PheromoneMap pheromone;
        private LureField lure;
        private ThreatField threat;
        private RouteCostModel model;
        private ColonyPathfinder finder;
        private List<int> path;

        /// <summary>
        /// 5x5 harita. İki dikey koridor: x=0 ve x=4.
        /// Üst satır (y=4) ve alt satır (y=0) baştan sona koridordur,
        /// böylece iki koridor birbirine bağlanır ve gerçek bir seçim doğar.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            grid = new ColonyGrid(5, 5);

            for (int y = 0; y < 5; y++)
            {
                grid.Set(grid.Index(0, y), CellKind.Corridor);
                grid.Set(grid.Index(4, y), CellKind.Corridor);
            }
            for (int x = 0; x < 5; x++)
            {
                grid.Set(grid.Index(x, 4), CellKind.Corridor);
                grid.Set(grid.Index(x, 0), CellKind.Corridor);
            }

            pheromone = new PheromoneMap(grid, max: 100f, decayRate: 0.1f);
            lure = new LureField(grid, baseStrength: 10f, diminishFactor: 0.5f);
            threat = new ThreatField(grid);
            model = new RouteCostModel(pheromone, lure, threat)
            {
                ThreatWeight = 3f,
                AttractionWeight = 4f
            };
            finder = new ColonyPathfinder(grid, model);
            path = new List<int>();
        }

        [Test]
        public void FindPath_ReturnsTrue_ForConnectedCells()
        {
            Assert.IsTrue(finder.FindPath(grid.Index(0, 4), grid.Index(0, 0), path));
            Assert.Greater(path.Count, 0);
        }

        [Test]
        public void FindPath_StartsAtStart_EndsAtGoal()
        {
            int start = grid.Index(0, 4);
            int goal = grid.Index(4, 0);
            finder.FindPath(start, goal, path);

            Assert.AreEqual(start, path[0]);
            Assert.AreEqual(goal, path[path.Count - 1]);
        }

        [Test]
        public void FindPath_ReturnsFalse_WhenGoalUnreachable()
        {
            var isolated = new ColonyGrid(5, 5);
            isolated.Set(isolated.Index(0, 0), CellKind.Corridor);
            isolated.Set(isolated.Index(4, 4), CellKind.Corridor);

            var f = new ColonyPathfinder(isolated,
                new RouteCostModel(
                    new PheromoneMap(isolated, 100f, 0.1f),
                    new LureField(isolated, 10f, 0.5f),
                    new ThreatField(isolated)));

            Assert.IsFalse(f.FindPath(isolated.Index(0, 0), isolated.Index(4, 4), path));
            Assert.AreEqual(0, path.Count);
        }

        [Test]
        public void FindPath_ReturnsFalse_WhenStartNotWalkable()
        {
            Assert.IsFalse(finder.FindPath(grid.Index(2, 2), grid.Index(0, 0), path));
        }

        [Test]
        public void FindPath_PrefersCorridorWithPheromone()
        {
            int start = grid.Index(2, 4);
            int goal  = grid.Index(2, 0);

            for (int y = 0; y < 5; y++)
                pheromone.Deposit(grid.Index(4, y), 100f);

            finder.FindPath(start, goal, path);

            Assert.IsTrue(path.Contains(grid.Index(4, 2)), "Feromonlu sağ koridor tercih edilmeliydi.");
            Assert.IsFalse(path.Contains(grid.Index(0, 2)));
        }

        [Test]
        public void FindPath_AvoidsCorridorWithThreat()
        {
            int start = grid.Index(2, 4);
            int goal  = grid.Index(2, 0);

            for (int y = 0; y < 5; y++)
                threat.AddSource(grid.Index(0, y), radius: 1f, strength: 20f);

            finder.FindPath(start, goal, path);

            Assert.IsFalse(path.Contains(grid.Index(0, 2)), "Tehditli sol koridordan kaçınılmalıydı.");
        }

        [Test]
        public void FindPath_LureOverridesWeakPheromone()
        {
            int start = grid.Index(2, 4);
            int goal  = grid.Index(2, 0);

            for (int y = 0; y < 5; y++)
                pheromone.Deposit(grid.Index(0, y), 5f);

            for (int y = 0; y < 5; y++)
                for (int i = 0; i < 4; i++)
                    lure.Apply(grid.Index(4, y));

            finder.FindPath(start, goal, path);

            Assert.IsTrue(path.Contains(grid.Index(4, 2)), "Yem, zayıf feromonu yenmeliydi.");
        }

        [Test]
        public void FindPath_SameStartAndGoal_ReturnsSingleCell()
        {
            int cell = grid.Index(0, 0);
            Assert.IsTrue(finder.FindPath(cell, cell, path));
            Assert.AreEqual(1, path.Count);
            Assert.AreEqual(cell, path[0]);
        }
    }
}
```

- [ ] **Adım 6: Testin başarısız olduğunu doğrula**

Unity MCP `run_tests`, filtre `Colony.Tests.ColonyPathfinderTests`.
Beklenen: derleme hatası — `ColonyPathfinder` bulunamıyor.

- [ ] **Adım 7: IndexedMinHeap'i yaz**

`Assets/Colony/Core/Pathing/IndexedMinHeap.cs`:
```csharp
using System;

namespace Colony.Core
{
    /// <summary>
    /// Hücre indeksleri için float öncelikli ikili min-heap.
    /// Mevcut Utils/MinHeap'ten farkı: Node nesnesi yerine int indeks tutar,
    /// bu sayede Colony.Core hiçbir Unity tipine bağımlı olmaz ve
    /// her hücrenin heap konumu O(1) bulunabilir.
    /// </summary>
    public sealed class IndexedMinHeap
    {
        private readonly int[] heap;        // heap pozisyonu -> hücre indeksi
        private readonly int[] positions;   // hücre indeksi -> heap pozisyonu (-1 = heap'te yok)
        private readonly float[] priorities;
        private int count;

        public int Count => count;

        public IndexedMinHeap(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            heap = new int[capacity];
            positions = new int[capacity];
            priorities = new float[capacity];
            Clear();
        }

        public void Clear()
        {
            count = 0;
            for (int i = 0; i < positions.Length; i++)
                positions[i] = -1;
        }

        public bool Contains(int cell) => positions[cell] >= 0;

        public void Push(int cell, float priority)
        {
            priorities[cell] = priority;
            heap[count] = cell;
            positions[cell] = count;
            count++;
            BubbleUp(count - 1);
        }

        /// <summary>Heap'te olan bir hücrenin önceliği DÜŞTÜĞÜNDE çağrılır.</summary>
        public void Update(int cell, float priority)
        {
            priorities[cell] = priority;
            BubbleUp(positions[cell]);
        }

        public int Pop()
        {
            int top = heap[0];
            positions[top] = -1;
            count--;

            if (count > 0)
            {
                heap[0] = heap[count];
                positions[heap[0]] = 0;
                BubbleDown(0);
            }

            return top;
        }

        private void BubbleUp(int position)
        {
            while (position > 0)
            {
                int parent = (position - 1) / 2;
                if (priorities[heap[position]] >= priorities[heap[parent]]) break;
                Swap(position, parent);
                position = parent;
            }
        }

        private void BubbleDown(int position)
        {
            while (true)
            {
                int left = position * 2 + 1;
                int right = left + 1;
                int smallest = position;

                if (left < count && priorities[heap[left]] < priorities[heap[smallest]])
                    smallest = left;
                if (right < count && priorities[heap[right]] < priorities[heap[smallest]])
                    smallest = right;

                if (smallest == position) break;

                Swap(position, smallest);
                position = smallest;
            }
        }

        private void Swap(int a, int b)
        {
            int cellA = heap[a];
            int cellB = heap[b];
            heap[a] = cellB;
            heap[b] = cellA;
            positions[cellB] = a;
            positions[cellA] = b;
        }
    }
}
```

- [ ] **Adım 8: ColonyPathfinder'ı yaz**

`Assets/Colony/Core/Pathing/ColonyPathfinder.cs`:
```csharp
using System;
using System.Collections.Generic;

namespace Colony.Core
{
    /// <summary>
    /// Koridor ağı üzerinde A*. Adım maliyetini RouteCostModel belirler,
    /// böylece feromon, yem ve tehdit tek bir rota kararında birleşir.
    /// Sezgisel Manhattan mesafesidir; minimum adım maliyeti 1 olduğu için
    /// kabul edilebilirdir ve en kısa yol garantisi korunur.
    /// </summary>
    public sealed class ColonyPathfinder
    {
        private readonly ColonyGrid grid;
        private readonly RouteCostModel costModel;

        private readonly float[] gCost;
        private readonly int[] cameFrom;
        private readonly bool[] closed;
        private readonly IndexedMinHeap open;
        private readonly int[] neighborBuffer = new int[4];

        public ColonyPathfinder(ColonyGrid grid, RouteCostModel costModel)
        {
            this.grid = grid ?? throw new ArgumentNullException(nameof(grid));
            this.costModel = costModel ?? throw new ArgumentNullException(nameof(costModel));

            gCost = new float[grid.CellCount];
            cameFrom = new int[grid.CellCount];
            closed = new bool[grid.CellCount];
            open = new IndexedMinHeap(grid.CellCount);
        }

        /// <summary>
        /// start'tan goal'e yolu bulur ve result'a yazar (start ve goal dahil).
        /// Yol yoksa false döner ve result boş kalır.
        /// </summary>
        public bool FindPath(int start, int goal, List<int> result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            result.Clear();

            if (!grid.IsWalkable(start) || !grid.IsWalkable(goal))
                return false;

            for (int i = 0; i < gCost.Length; i++)
            {
                gCost[i] = float.PositiveInfinity;
                cameFrom[i] = -1;
                closed[i] = false;
            }
            open.Clear();

            gCost[start] = 0f;
            open.Push(start, Heuristic(start, goal));

            while (open.Count > 0)
            {
                int current = open.Pop();

                if (current == goal)
                {
                    Retrace(start, goal, result);
                    return true;
                }

                closed[current] = true;

                int neighborCount = grid.GetNeighbors(current, neighborBuffer);
                for (int i = 0; i < neighborCount; i++)
                {
                    int neighbor = neighborBuffer[i];

                    if (closed[neighbor]) continue;
                    if (!grid.IsWalkable(neighbor)) continue;

                    float tentative = gCost[current] + costModel.StepCost(neighbor);
                    if (tentative >= gCost[neighbor]) continue;

                    gCost[neighbor] = tentative;
                    cameFrom[neighbor] = current;

                    float priority = tentative + Heuristic(neighbor, goal);
                    if (open.Contains(neighbor))
                        open.Update(neighbor, priority);
                    else
                        open.Push(neighbor, priority);
                }
            }

            return false;
        }

        /// <summary>Manhattan mesafesi. Minimum adım maliyeti 1 olduğu için kabul edilebilirdir.</summary>
        private float Heuristic(int from, int to)
        {
            int dx = grid.X(from) - grid.X(to);
            int dy = grid.Y(from) - grid.Y(to);
            if (dx < 0) dx = -dx;
            if (dy < 0) dy = -dy;
            return dx + dy;
        }

        private void Retrace(int start, int goal, List<int> result)
        {
            int current = goal;
            while (current != start)
            {
                result.Add(current);
                current = cameFrom[current];
            }
            result.Add(start);
            result.Reverse();
        }
    }
}
```

- [ ] **Adım 9: Tüm testlerin geçtiğini doğrula**

Unity MCP `run_tests`, filtre `Colony.Tests`.
Beklenen: Görev 1-5 arası tüm testler PASS (42 test).

- [ ] **Adım 10: Commit**

```bash
git add Assets/Colony
git commit -m "feat: ColonyPathfinder ve birleşik rota maliyet modeli"
```

---

### Görev 6: Harita üretimi ve doğrulama

**Dosyalar:**
- Oluştur: `Assets/Colony/Core/MapGen/MapTemplate.cs`
- Oluştur: `Assets/Colony/Core/MapGen/MapGenerator.cs`
- Oluştur: `Assets/Colony/Core/MapGen/MapValidator.cs`
- Oluştur: `Assets/Colony/Tests/EditMode/MapGeneratorTests.cs`

**Arayüzler:**
- Kullanır: `ColonyGrid`, `CellKind`, `RouteCostModel`, `PheromoneMap`, `LureField`, `ThreatField`
- Üretir: `MapTemplate` struct — `Width`, `Height`, `CorridorCount`, `CorridorWidth`
- Üretir: `MapGenerator` — `Generate(MapTemplate, int seed)` → `ColonyGrid`
- Üretir: `MapValidationResult` struct — `IsValid`, `CorridorCount`, `CostGap`, `FailureReason`
- Üretir: `CorridorSpan` struct — `StartX`, `EndX`

> **Uygulama sirasinda duzeltildi:** Plan ilk halinde koridorlar tek sutunla temsil ediliyordu
> (`FindCorridorColumns` -> `List<int>`). Bu hataliydi: 2 hucre genisligindeki bir koridorda
> kurulum seridi cogu zaman yalnizca kenardaki sutunun komsusudur, ilk sutunun degil.
> Bu haliyle her gecerli harita reddedilirdi. Koridorlar `CorridorSpan` ile sutun araligi
> olarak temsil edilir ve dogrulama tum aralig tarar.
- Üretir: `MapValidator` — `Validate(ColonyGrid, float maxCostGap)` → `MapValidationResult`

Tasarım kaynağı: spec bölüm 9.4. **Rastgelelik topolojiyi değil dokuyu üretir.**

- [ ] **Adım 1: Başarısız testi yaz**

`Assets/Colony/Tests/EditMode/MapGeneratorTests.cs`:
```csharp
using NUnit.Framework;
using Colony.Core;

namespace Colony.Tests
{
    public class MapGeneratorTests
    {
        private static MapTemplate ThreeCorridorTemplate() => new MapTemplate
        {
            Width = 11,
            Height = 20,
            CorridorCount = 3,
            CorridorWidth = 2
        };

        [Test]
        public void Generate_ProducesGridOfTemplateSize()
        {
            var grid = new MapGenerator().Generate(ThreeCorridorTemplate(), seed: 1);
            Assert.AreEqual(11, grid.Width);
            Assert.AreEqual(20, grid.Height);
        }

        [Test]
        public void Generate_IsDeterministicForSameSeed()
        {
            var a = new MapGenerator().Generate(ThreeCorridorTemplate(), seed: 42);
            var b = new MapGenerator().Generate(ThreeCorridorTemplate(), seed: 42);

            for (int i = 0; i < a.CellCount; i++)
                Assert.AreEqual(a.Get(i), b.Get(i), $"Hücre {i} farklı.");
        }

        [Test]
        public void Generate_ProducesBuildStrips()
        {
            var grid = new MapGenerator().Generate(ThreeCorridorTemplate(), seed: 7);

            int buildStrips = 0;
            for (int i = 0; i < grid.CellCount; i++)
                if (grid.Get(i) == CellKind.BuildStrip) buildStrips++;

            Assert.Greater(buildStrips, 0, "Kurulum şeridi üretilmedi.");
        }

        [Test]
        public void Generate_RejectsInvalidTemplate()
        {
            var bad = new MapTemplate { Width = 0, Height = 20, CorridorCount = 3, CorridorWidth = 2 };
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => new MapGenerator().Generate(bad, seed: 1));
        }

        [Test]
        public void Validate_AcceptsGeneratedMap()
        {
            var grid = new MapGenerator().Generate(ThreeCorridorTemplate(), seed: 3);
            var result = new MapValidator().Validate(grid, maxCostGap: 10f);

            Assert.IsTrue(result.IsValid, result.FailureReason);
            Assert.AreEqual(3, result.CorridorCount);
        }

        [Test]
        public void Validate_RejectsMapWithTooFewCorridors()
        {
            var template = ThreeCorridorTemplate();
            template.CorridorCount = 2;

            var grid = new MapGenerator().Generate(template, seed: 3);
            var result = new MapValidator().Validate(grid, maxCostGap: 10f);

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains("koridor", result.FailureReason);
        }

        [Test]
        public void Validate_RejectsMapWhenCostGapExceedsThreshold()
        {
            var grid = new MapGenerator().Generate(ThreeCorridorTemplate(), seed: 3);
            // Eşiği negatife çekince hiçbir maliyet farkı kabul edilmez
            var result = new MapValidator().Validate(grid, maxCostGap: -1f);

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains("maliyet", result.FailureReason);
        }

        [Test]
        public void Validate_ReportsCostGap_ForValidMap()
        {
            var grid = new MapGenerator().Generate(ThreeCorridorTemplate(), seed: 11);
            var result = new MapValidator().Validate(grid, maxCostGap: 10f);

            Assert.IsTrue(result.IsValid, result.FailureReason);
            Assert.GreaterOrEqual(result.CostGap, 0f);
        }
    }
}
```

- [ ] **Adım 2: Testin başarısız olduğunu doğrula**

Unity MCP `run_tests`, filtre `Colony.Tests.MapGeneratorTests`.
Beklenen: derleme hatası — `MapTemplate` bulunamıyor.

- [ ] **Adım 3: MapTemplate'i yaz**

`Assets/Colony/Core/MapGen/MapTemplate.cs`:
```csharp
namespace Colony.Core
{
    /// <summary>
    /// Elle tasarlanmış bir harita topolojisi. Prosedürel üretim yalnızca
    /// bu şablonun içindeki dokuyu değiştirir, topolojiyi değiştirmez.
    /// Böylece her harita okunabilir kalır (spec 9.4).
    /// </summary>
    public struct MapTemplate
    {
        /// <summary>Grid genişliği (dikey tutuş: dar eksen).</summary>
        public int Width;

        /// <summary>Grid yüksekliği (dikey tutuş: uzun eksen).</summary>
        public int Height;

        /// <summary>Kaç dikey koridor üretilecek.</summary>
        public int CorridorCount;

        /// <summary>Her koridorun hücre cinsinden genişliği.</summary>
        public int CorridorWidth;
    }
}
```

- [ ] **Adım 4: MapGenerator'ı yaz**

`Assets/Colony/Core/MapGen/MapGenerator.cs`:
```csharp
using System;

namespace Colony.Core
{
    /// <summary>
    /// Şablondan harita üretir. Dikey tutuş düzeni: koridorlar yukarıdan
    /// aşağı uzanır, aralarında kurulum şeritleri kalır. Üst satır giriş
    /// bandı, alt satır yuva bandıdır; ikisi koridorları birbirine bağlar.
    /// </summary>
    public sealed class MapGenerator
    {
        /// <summary>Blocked kalma olasılığı (yüzde). Doku varyasyonunu üretir.</summary>
        private const int BlockedChancePercent = 15;

        /// <summary>Aynı seed her zaman aynı haritayı üretir.</summary>
        public ColonyGrid Generate(MapTemplate template, int seed)
        {
            if (template.Width <= 0 || template.Height <= 0)
                throw new ArgumentOutOfRangeException(nameof(template), "Şablon boyutları pozitif olmalı.");
            if (template.CorridorCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(template), "CorridorCount pozitif olmalı.");
            if (template.CorridorWidth <= 0)
                throw new ArgumentOutOfRangeException(nameof(template), "CorridorWidth pozitif olmalı.");

            var grid = new ColonyGrid(template.Width, template.Height);
            var random = new Random(seed);

            // Koridorları dar eksende eşit aralıklarla yerleştir.
            int span = template.Width / template.CorridorCount;

            for (int c = 0; c < template.CorridorCount; c++)
            {
                int corridorStart = c * span + (span - template.CorridorWidth) / 2;
                if (corridorStart < 0) corridorStart = 0;

                for (int w = 0; w < template.CorridorWidth; w++)
                {
                    int x = corridorStart + w;
                    if (x >= template.Width) break;

                    for (int y = 0; y < template.Height; y++)
                        grid.Set(grid.Index(x, y), CellKind.Corridor);
                }
            }

            // Üst ve alt bantlar koridorları birbirine bağlar.
            for (int x = 0; x < template.Width; x++)
            {
                grid.Set(grid.Index(x, template.Height - 1), CellKind.Corridor);
                grid.Set(grid.Index(x, 0), CellKind.Corridor);
            }

            // Kalan Blocked hücreler kurulum şeridi olur; bir kısmı doku için engel kalır.
            for (int i = 0; i < grid.CellCount; i++)
            {
                if (grid.Get(i) != CellKind.Blocked) continue;

                bool keepBlocked = random.Next(0, 100) < BlockedChancePercent;
                grid.Set(i, keepBlocked ? CellKind.Blocked : CellKind.BuildStrip);
            }

            return grid;
        }
    }
}
```

- [ ] **Adım 5: MapValidator'ı yaz**

`Assets/Colony/Core/MapGen/MapValidator.cs`:
```csharp
using System.Collections.Generic;

namespace Colony.Core
{
    public struct MapValidationResult
    {
        public bool IsValid;
        public int CorridorCount;
        public float CostGap;
        public string FailureReason;
    }

    /// <summary>
    /// Üretilen haritanın oynanabilir olduğunu doğrular (spec 9.4).
    /// Geçemeyen harita atılır ve yeniden üretilir.
    ///
    /// Kurallar:
    /// 1. En az 3 ayrık dikey koridor bulunmalı.
    /// 2. Her koridorun yanında en az bir kurulum şeridi bulunmalı.
    /// 3. En ucuz iki koridor arasındaki maliyet farkı eşiğin altında olmalı;
    ///    aksi halde bir koridor baştan galiptir ve seçim yanılsamaya döner.
    /// </summary>
    public sealed class MapValidator
    {
        public const int MinimumCorridors = 3;

        public MapValidationResult Validate(ColonyGrid grid, float maxCostGap)
        {
            var result = new MapValidationResult { FailureReason = string.Empty };

            List<int> corridorColumns = FindCorridorColumns(grid);
            result.CorridorCount = corridorColumns.Count;

            if (corridorColumns.Count < MinimumCorridors)
            {
                result.IsValid = false;
                result.FailureReason =
                    $"Yetersiz koridor sayısı: {corridorColumns.Count} (en az {MinimumCorridors} gerekli).";
                return result;
            }

            if (!EveryCorridorHasBuildStrip(grid, corridorColumns, out int badColumn))
            {
                result.IsValid = false;
                result.FailureReason = $"{badColumn}. sütundaki koridorun yanında kurulum şeridi yok.";
                return result;
            }

            result.CostGap = ComputeCostGap(grid, corridorColumns);
            if (result.CostGap > maxCostGap)
            {
                result.IsValid = false;
                result.FailureReason =
                    $"Koridor maliyet farkı çok yüksek: {result.CostGap:F2} > {maxCostGap:F2}.";
                return result;
            }

            result.IsValid = true;
            return result;
        }

        /// <summary>
        /// Baştan sona kesintisiz koridor olan sütunlar; bitişik olanlar tek
        /// koridor sayılarak gruplanır. Üst ve alt bağlantı bantları hariç tutulur.
        /// </summary>
        private static List<int> FindCorridorColumns(ColonyGrid grid)
        {
            var groups = new List<int>();
            bool previousWasCorridor = false;

            for (int x = 0; x < grid.Width; x++)
            {
                bool fullColumn = true;
                for (int y = 1; y < grid.Height - 1; y++)
                {
                    if (grid.Get(grid.Index(x, y)) != CellKind.Corridor)
                    {
                        fullColumn = false;
                        break;
                    }
                }

                if (fullColumn && !previousWasCorridor)
                    groups.Add(x);

                previousWasCorridor = fullColumn;
            }

            return groups;
        }

        private static bool EveryCorridorHasBuildStrip(ColonyGrid grid, List<int> columns, out int badColumn)
        {
            var buffer = new int[4];

            for (int c = 0; c < columns.Count; c++)
            {
                int x = columns[c];
                bool found = false;

                for (int y = 1; y < grid.Height - 1 && !found; y++)
                {
                    int count = grid.GetNeighbors(grid.Index(x, y), buffer);
                    for (int i = 0; i < count; i++)
                    {
                        if (grid.Get(buffer[i]) == CellKind.BuildStrip)
                        {
                            found = true;
                            break;
                        }
                    }
                }

                if (!found)
                {
                    badColumn = x;
                    return false;
                }
            }

            badColumn = -1;
            return true;
        }

        /// <summary>
        /// Boş harita üzerinde (feromon, yem, tehdit sıfır) her koridorun baştan
        /// sona maliyetini ölçer ve en ucuz iki koridor arasındaki farkı döner.
        /// </summary>
        private static float ComputeCostGap(ColonyGrid grid, List<int> columns)
        {
            var pheromone = new PheromoneMap(grid, 100f, 0.1f);
            var lure = new LureField(grid, 10f, 0.5f);
            var threat = new ThreatField(grid);
            var model = new RouteCostModel(pheromone, lure, threat)
            {
                ThreatWeight = 1f,
                AttractionWeight = 1f
            };

            var costs = new List<float>();
            for (int c = 0; c < columns.Count; c++)
            {
                int x = columns[c];
                float total = 0f;
                for (int y = 1; y < grid.Height - 1; y++)
                    total += model.StepCost(grid.Index(x, y));
                costs.Add(total);
            }

            costs.Sort();
            return costs.Count >= 2 ? costs[1] - costs[0] : 0f;
        }
    }
}
```

- [ ] **Adım 6: Testlerin geçtiğini doğrula**

Unity MCP `run_tests`, filtre `Colony.Tests.MapGeneratorTests`.
Beklenen: 8 test PASS.

- [ ] **Adım 7: Tüm çekirdek testlerini koştur**

Unity MCP `run_tests`, filtre `Colony.Tests`.
Beklenen: 50 test PASS, 0 FAIL.

- [ ] **Adım 8: Commit**

```bash
git add Assets/Colony
git commit -m "feat: şablon tabanlı harita üretimi ve doğrulama"
```

---

## Bu Planın Kapsamadıkları

Bilerek dışarıda bırakıldı, sonraki planlara aittir:

- **Plan 2 — Unity entegrasyonu ve görselleştirme:** MonoBehaviour köprüleri, feromon izlerinin ekranda çizimi, gürültücü kule titreşim halkası, yerleştirme ön izlemesi, dokunmatik giriş.
- **Plan 3 — Oynanış katmanı:** kuleler, düşmanlar, dalga akışı, draft sistemi, ekonomi.
- **Plan 4 — Meta ilerleme ve kayıt:** biyokütle, yetenek ağacı, kalıcı kayıt.
- **Plan 5 — Blender asset pipeline ve ses.**

Eski `Assets/Scripts/` bu planda **silinmez**. Yeni çekirdek çalışır hale gelip Plan 2'de sahneye bağlandıktan sonra kaldırılacaktır.

## Spec Kapsam Kontrolü

Bu plan spec'in şu bölümlerini karşılar: 3.1 (gürültücü/sessiz ayrımı → ThreatField), 3.3 (yem dengesi → LureField), 3.4 (feromon verisi → PheromoneMap), 3.6 (dikey düzen → MapGenerator), 4 (Q-Learning katmanı → PheromoneMap + RouteCostModel), 9.2 (yem kuralları), 9.4 (harita doğrulama).

Spec'in şu bölümleri sonraki planlara bırakıldı: 2 (draft, run akışı), 3.2 ve 3.5 (görsel sunum), 5 (kule ve düşman içeriği), 6 (meta), 8 (oynanabilir run), 9.1 (görsel okunabilirlik), 9.3 (nemesis evrimi).
