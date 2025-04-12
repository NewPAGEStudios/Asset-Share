using HootyBird.ColoringBook.Gameplay;
using HootyBird.ColoringBook.Services;
using HootyBird.ColoringBook.Tools;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HootyBird.ColoringBook.Menu.Widgets
{
    public class ColorsPanel : MonoBehaviour
    {
        [SerializeField]
        private ColorWidget colorWidgetPrefab;
        [SerializeField]
        private ColoringBookView coloringBookView;
        [SerializeField]
        private Transform colorWidgetsParent;
        [SerializeField]
        private OnColorFilledAction colorFilledAction = OnColorFilledAction.Checkmark;

        private List<ColorWidget> colorWidgets;
        private ColorWidget current;

        public void Awake()
        {
            if (!coloringBookView)
            {
                gameObject.SetActive(false);
                return;
            }

            colorWidgets = new List<ColorWidget>();
            coloringBookView.OnDataSet += OnColoringBookDataSet;
            coloringBookView.OnColorFilled += OnColorFilled;
        }

        private void OnColoringBookDataSet()
        {
            // Reset current one.
            if (current)
            {
                current.SetSelectedState(false);
                current = null;
            }

            IEnumerable<Color> colors = coloringBookView.ColoringBookData.Colors;

            // Remove if needed.
            while (colorWidgets.Count > colors.Count())
            {
                Destroy(colorWidgets[0].gameObject);
                colorWidgets.RemoveAt(0);
            }

            // Reset and add new ones.
            for (int index = 0; index < colors.Count(); index++)
            {
                if (index > colorWidgets.Count - 1)
                {
                    colorWidgets.Add(AddNewColorWidget());
                }

                colorWidgets[index].OnReset();
                colorWidgets[index].SetColor(colors.ElementAt(index));
                colorWidgets[index].SetText($"{index + 1}");
            }

            // Shuffle.
            if (SettingsService.GetSettingValue(SettingsOptions.ShuffleColors))
            {
                foreach (ColorWidget colorWidget in colorWidgets.OrderBy(value => Random.value))
                {
                    colorWidget.transform.SetAsFirstSibling();
                }
            }
        }

        private ColorWidget AddNewColorWidget()
        {
            ColorWidget colorWidget = Instantiate(colorWidgetPrefab, colorWidgetsParent);
            colorWidget.OnClicked += OnColorWidgetClicked;

            return colorWidget;
        }

        private void OnColorFilled(Color color)
        {
            switch (colorFilledAction)
            {
                case OnColorFilledAction.Hide:
                    foreach (ColorWidget widget in colorWidgets)
                    {
                        if (widget.Color.Compare(color))
                        {
                            widget.gameObject.SetActive(false);
                        }
                    }

                    coloringBookView.SetFillColor(Color.clear);

                    break;

                case OnColorFilledAction.Checkmark:
                    foreach (ColorWidget widget in colorWidgets)
                    {
                        if (widget.Color.Compare(color))
                        {
                            widget.ShowCompleteIcon();
                        }
                    }

                    break;
            }
        }

        private void OnColorWidgetClicked(ColorWidget widget)
        {
            // If clicked color is a current one, return.
            if (current && current == widget)
            {
                return;
            }

            if (current)
            {
                current.SetSelectedState(false);
            }

            current = widget;
            current.SetSelectedState(true);
            coloringBookView.SetFillColor(current.Color);
        }

        /// <summary>
        /// Action invoked when certain color is filled.
        /// </summary>
        public enum OnColorFilledAction
        {
            /// <summary>
            /// Do nothing.
            /// </summary>
            None,

            /// <summary>
            /// Fade out animation.
            /// </summary>
            Hide,

            /// <summary>
            /// Show "complete" checkmark.
            /// </summary>
            Checkmark,
        }
    }
}
