using UnityEngine;
using UnityEngine.EventSystems;
using static HootyBird.ColoringBook.Gameplay.ColoringBookView;

namespace HootyBird.ColoringBook.Gameplay.BookInputHandling
{
    public class ColoringBookViewClickHandler : MonoBehaviour, IPointerClickHandler
    {
        public ColoringBookView ColoringBookView { get; private set; }

        private void Awake()
        {
            ColoringBookView = GetComponent<ColoringBookView>();
        }

        private void OnEnable() { }

        public void OnPointerClick(PointerEventData eventData)
        {
            switch (ColoringBookView.ColoringStyle)
            {
                // Ignore click events for next coloring styles...
                case RegionColoringStyle.FreeDrawing:
                    return;
            }

            ColoringBookView.CheckClickAtScreenPoint(eventData.position);
        }
    }
}