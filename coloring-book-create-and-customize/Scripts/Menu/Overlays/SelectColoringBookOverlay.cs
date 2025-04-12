using HootyBird.ColoringBook.Menu.Widgets;
using HootyBird.ColoringBook.Repositories;
using HootyBird.ColoringBook.Serialized;
using UnityEngine;

namespace HootyBird.ColoringBook.Menu.Overlays
{
    public class SelectColoringBookOverlay : MenuOverlay
    {
        [SerializeField]
        private ColoringBookWidget widgetPrefab;
        [SerializeField]
        private RectTransform widgetsParent;

        protected override void Start()
        {
            base.Awake();

            // Instantiate coloring books widgets.
            InitWidgets();
        }

        private void InitWidgets()
        {
            foreach (ColoringBookDataBase bookData in DataHandler.Instance.DefaultColoringBooks.ColoringBooks)
            {
                ColoringBookWidget widget = Instantiate(widgetPrefab, widgetsParent);
                widget.SetData(bookData);
            }
        }
    }
}
