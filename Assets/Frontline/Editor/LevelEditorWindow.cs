using UnityEditor;
using UnityEngine;
using Frontline.Core;
using Frontline.Runtime;

namespace Frontline.EditorTools
{
    /// <summary>
    /// Seviye ureteci: harita + dalga parametrelerini tasiyan seviyeleri
    /// (LevelDefinition) ekler/duzenler/siler ve diske kaydeder (LevelStore).
    /// Is mantigi burada YOK - hepsi LevelCatalog'da (test edilebilir, saf
    /// C#); bu pencere yalnizca onu cagirip goruntuler (FrontlineSandboxSceneBuilder
    /// ile ayni ayrim: sahne/veri kurulumu kod ile, tek tikla tekrarlanabilir).
    ///
    /// "Bu seviyeyle oyna": LevelSelection.SelectedLevelName'i set eder.
    /// Bir sonraki Play'de Battlefield/RunController bunu okuyup kendi
    /// alanlarini uzerine yazar (bkz. Battlefield.ApplySelectedLevelIfAny).
    /// </summary>
    public sealed class LevelEditorWindow : EditorWindow
    {
        private LevelCatalog catalog;
        private Vector2 listScroll;
        private Vector2 formScroll;
        private string selectedName;
        private LevelDefinition editBuffer;
        private string statusMessage;
        private bool isNew;

        [MenuItem("Frontline/Seviye Ureteci")]
        public static void Open()
        {
            var window = GetWindow<LevelEditorWindow>("Seviye Ureteci");
            window.minSize = new Vector2(520f, 420f);
            window.Reload();
        }

        private void OnEnable()
        {
            Reload();
        }

        private void Reload()
        {
            catalog = LevelStore.Load();
            statusMessage = null;
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();

            DrawList();
            DrawForm();

            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(statusMessage))
                EditorGUILayout.HelpBox(statusMessage, MessageType.Info);
        }

        private void DrawList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(200f));
            EditorGUILayout.LabelField("Seviyeler (" + catalog.Levels.Count + ")", EditorStyles.boldLabel);

            listScroll = EditorGUILayout.BeginScrollView(listScroll, GUILayout.ExpandHeight(true));
            foreach (var level in catalog.Levels)
            {
                bool isSelected = level.Name == selectedName;
                var style = isSelected ? EditorStyles.boldLabel : EditorStyles.label;
                if (GUILayout.Button(level.Name, style))
                    SelectForEdit(level.Name);
            }
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("+ Yeni Seviye Ekle"))
                StartNew();

            EditorGUILayout.EndVertical();
        }

        private void DrawForm()
        {
            EditorGUILayout.BeginVertical();

            if (editBuffer == null)
            {
                EditorGUILayout.HelpBox("Soldan bir seviye secin ya da 'Yeni Seviye Ekle'ye basin.", MessageType.None);
                EditorGUILayout.EndVertical();
                return;
            }

            formScroll = EditorGUILayout.BeginScrollView(formScroll);

            EditorGUILayout.LabelField(isNew ? "Yeni Seviye" : "Duzenle: " + selectedName, EditorStyles.boldLabel);
            editBuffer.Name = EditorGUILayout.TextField("Ad", editBuffer.Name);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Harita", EditorStyles.boldLabel);
            editBuffer.Width = EditorGUILayout.IntField("Genislik", editBuffer.Width);
            editBuffer.Height = EditorGUILayout.IntField("Yukseklik", editBuffer.Height);
            editBuffer.CorridorCount = EditorGUILayout.IntField("Koridor Sayisi", editBuffer.CorridorCount);
            editBuffer.CorridorWidth = EditorGUILayout.IntField("Koridor Genisligi", editBuffer.CorridorWidth);
            editBuffer.RungCount = EditorGUILayout.IntField("Gecit Sayisi", editBuffer.RungCount);
            editBuffer.SceneryPercent = EditorGUILayout.IntSlider("Ortu Yuzdesi", editBuffer.SceneryPercent, 0, 90);
            editBuffer.TreeHealth = EditorGUILayout.FloatField("Agac Cani", editBuffer.TreeHealth);
            editBuffer.RockHealth = EditorGUILayout.FloatField("Kaya Cani", editBuffer.RockHealth);
            editBuffer.Seed = EditorGUILayout.IntField("Seed (0 = rastgele)", editBuffer.Seed);
            editBuffer.Theme = (LevelTheme)EditorGUILayout.EnumPopup("Tema", editBuffer.Theme);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Dalgalar / Zorluk", EditorStyles.boldLabel);
            editBuffer.TotalWaves = EditorGUILayout.IntField("Toplam Dalga", editBuffer.TotalWaves);
            editBuffer.BaseUnitsPerWave = EditorGUILayout.IntField("Ilk Dalga Birlik Sayisi", editBuffer.BaseUnitsPerWave);
            editBuffer.UnitsAddedPerWave = EditorGUILayout.FloatField("Dalga Basi Eklenen Birlik", editBuffer.UnitsAddedPerWave);
            editBuffer.BaseUnitHp = EditorGUILayout.FloatField("Ilk Dalga Birlik Cani", editBuffer.BaseUnitHp);
            editBuffer.HpGrowthPerWave = EditorGUILayout.FloatField("Dalga Basi Can Artis Orani", editBuffer.HpGrowthPerWave);

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Kaydet"))
                SaveBuffer();

            using (new EditorGUI.DisabledScope(isNew))
            {
                if (GUILayout.Button("Sil") &&
                    EditorUtility.DisplayDialog("Seviyeyi Sil",
                        "'" + selectedName + "' silinsin mi?", "Sil", "Vazgec"))
                {
                    DeleteSelected();
                }
            }

            using (new EditorGUI.DisabledScope(isNew))
            {
                if (GUILayout.Button("Bu Seviyeyle Oyna"))
                {
                    LevelSelection.SelectedLevelName = selectedName;
                    statusMessage = "'" + selectedName + "' secildi - bir sonraki Play bunu kullanir.";
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void SelectForEdit(string name)
        {
            var level = catalog.Find(name);
            if (level == null) return;

            selectedName = name;
            editBuffer = level.Clone();
            isNew = false;
            statusMessage = null;
        }

        private void StartNew()
        {
            selectedName = null;
            editBuffer = new LevelDefinition { Name = "Yeni Seviye " + (catalog.Levels.Count + 1) };
            isNew = true;
            statusMessage = null;
        }

        private void SaveBuffer()
        {
            string error;
            bool ok = isNew
                ? catalog.Add(editBuffer, out error)
                : catalog.Update(selectedName, editBuffer, out error);

            if (!ok)
            {
                statusMessage = "HATA: " + error;
                return;
            }

            LevelStore.Save(catalog);
            selectedName = editBuffer.Name;
            isNew = false;
            statusMessage = "Kaydedildi.";
            Repaint();
        }

        private void DeleteSelected()
        {
            if (string.IsNullOrEmpty(selectedName)) return;

            catalog.Remove(selectedName);
            LevelStore.Save(catalog);

            if (LevelSelection.SelectedLevelName == selectedName)
                LevelSelection.Clear();

            editBuffer = null;
            selectedName = null;
            statusMessage = "Silindi.";
            Repaint();
        }
    }
}
