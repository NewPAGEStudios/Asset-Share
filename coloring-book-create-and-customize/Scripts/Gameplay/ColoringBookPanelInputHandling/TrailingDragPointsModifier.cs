using HootyBird.ColoringBook.Tools;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HootyBird.ColoringBook.Gameplay.BookInputHandling
{
    public class TrailingDragPointsModifier : MonoBehaviour, IDragPointsModifier
    {
        [SerializeField]
        private int maxPointsAlive = 20;
        [SerializeField]
        private float maxPointLifetime = 1f;
        [SerializeField]
        private float pointsGrowSpeed = .2f;

        private bool haveUpdates;
        private IEnumerable<RegionDataView> affectedRegions;
        private List<RectWithTime> rects = new List<RectWithTime>();

        public IEnumerable<Rect> Rects => rects.Select(entry => entry.rect);

        public bool HaveUpdates => haveUpdates;

        public IEnumerable<RegionDataView> AffectedRegions => affectedRegions;

        public void AddRects(IEnumerable<Rect> newRects)
        {
            foreach (Rect rect in newRects)
            {
                rects.Add(new RectWithTime() { rect = rect });
            }

            // Make sure size is MaxPointsAlive
            while (rects.Count > maxPointsAlive)
            {
                rects.RemoveAt(0);
            }
        }

        public void Clear()
        {
            rects.Clear();
        }

        public void UpdateRects(float timeDelta)
        {
            if (rects.Count == 0) 
            { 
                haveUpdates = false;
                return;
            }

            haveUpdates = true;

            for (int index = 0; index < rects.Count; index++)
            {
                rects[index].time += timeDelta;

                Vector2 originalCenter = rects[index].rect.center;
                rects[index].rect.size *= 1f + (timeDelta * pointsGrowSpeed);
                rects[index].rect.center = originalCenter;
            }

            // Remove expired points.
            rects.RemoveAll(entry => entry.time > maxPointLifetime);
        }

        public void UpdateAffectingRegions(ColoringBookView view)
        {
            affectedRegions = Rects
                .SelectMany(brushRect => view.RegionsOverlappingRect(brushRect))
                .Distinct()
                .Where(region => region.RegionData.MainColor.Compare(view.FillColor));
        }

        public void Use()
        {
            haveUpdates = false;
        }

        private class RectWithTime
        {
            public Rect rect;
            public float time;
        }
    }
}
