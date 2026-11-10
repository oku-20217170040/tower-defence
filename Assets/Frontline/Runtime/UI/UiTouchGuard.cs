using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Frontline.Runtime.UI
{
    /// <summary>
    /// Arayuz uzerindeki dokunusun haritaya gecmesini engeller.
    ///
    /// Sorun somut: alt bant bas parmagin altindadir ve tam da orasi
    /// haritanin kaydirildigi yerdir. Kule dugmesine basan oyuncu ayni
    /// dokunusla haritaya kule kurmus olursa besinini kaybeder ve bunu
    /// bir hata sanir - mobil arayuzde en sik yapilan hata budur.
    ///
    /// Iki katmanli koruma:
    ///   1. EventSystem - Graphic Raycaster'i olan Canvas icin dogru cevap.
    ///   2. Kayitli dikdortgenler - EventSystem sahnede yoksa ya da
    ///      raycast hedefi kapaliysa yedek. Bant dikdortgenleri
    ///      RunScreen tarafindan kaydedilir.
    ///
    /// Statiktir cunku cagiran taraf (TowerPlacer, MapCamera) bir
    /// referans tasimadan sorabilmeli.
    /// </summary>
    public static class UiTouchGuard
    {
        private static readonly List<Rect> blockers = new List<Rect>();

        /// <summary>
        /// Arayuzun kapladigi bir ekran dikdortgenini kaydeder (piksel,
        /// koken sol alt). Ayni dikdortgen iki kez kaydedilmez.
        /// </summary>
        public static void Register(Rect screenRect)
        {
            if (screenRect.width <= 0f || screenRect.height <= 0f) return;
            if (!blockers.Contains(screenRect)) blockers.Add(screenRect);
        }

        public static void Unregister(Rect screenRect)
        {
            blockers.Remove(screenRect);
        }

        /// <summary>Sahne degisiminde eski dikdortgenler kalmasin.</summary>
        public static void Clear()
        {
            blockers.Clear();
        }

        /// <summary>
        /// Su anki isaretci (fare ya da ilk parmak) arayuzun uzerinde mi?
        /// Yerlestirme parmak KALKINCA oldugu icin cagri o anda yapilir.
        /// </summary>
        public static bool IsPointerOverUi()
        {
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.GetTouch(i);
                    if (IsOverUi(touch.position, touch.fingerId)) return true;
                }

                return false;
            }

            return IsOverUi(Input.mousePosition, -1);
        }

        /// <summary>Belirli bir ekran noktasi arayuzun uzerinde mi?</summary>
        public static bool IsOverUi(Vector2 screenPosition, int pointerId)
        {
            EventSystem events = EventSystem.current;
            if (events != null)
            {
                if (pointerId >= 0)
                {
                    if (events.IsPointerOverGameObject(pointerId)) return true;
                }
                else if (events.IsPointerOverGameObject())
                {
                    return true;
                }
            }

            for (int i = 0; i < blockers.Count; i++)
                if (blockers[i].Contains(screenPosition)) return true;

            return false;
        }
    }
}
