using System.Collections.Generic;
using UnityEngine;

namespace HootyBird.ColoringBook.Gameplay.BookInputHandling
{
    public interface IDragPointsModifier
    {
        IEnumerable<Rect> Rects { get; }
        IEnumerable<RegionDataView> AffectedRegions { get; }
        bool HaveUpdates { get; }
        void AddRects(IEnumerable<Rect> rects);
        void Clear();
        void UpdateRects(float timeDelta);
        void UpdateAffectingRegions(ColoringBookView view);
        void Use();
    }
}