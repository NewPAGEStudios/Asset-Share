using HootyBird.ColoringBook.Tools;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HootyBird.ColoringBook.Gameplay.BookInputHandling
{
    public class DefaultDragPointsModifier : IDragPointsModifier
    {
        private IEnumerable<Rect> rects = new List<Rect>();
        private IEnumerable<RegionDataView> affectedRegions;
        public IEnumerable<Rect> Rects => rects;

        private bool haveUpdates;

        public void AddRects(IEnumerable<Rect> rects)
        {
            this.rects = rects;
            haveUpdates = true;
        }

        public bool HaveUpdates => haveUpdates;

        public IEnumerable<RegionDataView> AffectedRegions => affectedRegions;

        public void UpdateRects(float timeDelta) { }

        public void Use()
        {
            haveUpdates = false;
        }

        public void Clear() { }

        public void UpdateAffectingRegions(ColoringBookView view)
        {
            affectedRegions = rects
                .SelectMany(brushRect => view.RegionsOverlappingRect(brushRect))
                .Distinct()
                .Where(region => region.RegionData.MainColor.Compare(view.FillColor));
        }
    }
}
