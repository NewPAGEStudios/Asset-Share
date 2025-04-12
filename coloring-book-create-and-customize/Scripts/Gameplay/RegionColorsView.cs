using System;
using System.Collections.Generic;
using System.Linq;
using HootyBird.ColoringBook.Data;
using HootyBird.ColoringBook.Menu.Widgets;
using HootyBird.ColoringBook.Tools;
using UnityEngine;

namespace HootyBird.ColoringBook.Gameplay
{
    public class RegionColorsView : MonoBehaviour
    {
        [SerializeField]
        private ColoringBookView coloringBookView;
        [SerializeField]
        private RegionColorWidget colorWidgetPrefab;

        [SerializeField]
        private RegionColorViewType regionColorViewType = RegionColorViewType.IndexValue;
        [SerializeField]
        [Range(0f, 1f)]
        private float regionColorViewScaleEffect = .7f;
        [SerializeField]
        private int referenceSize = 1024;
        [SerializeField]
        private RegionColorViewOnColoredAction onRegionColoredAction = RegionColorViewOnColoredAction.HideColorView;

        private List<RegionColorWidget> colorWidgets = new List<RegionColorWidget>();
        private Dictionary<RegionDataView, RegionColorWidget> regionColorViewDictionary;
        private float colorViewsReferenceScale;

        private void Awake()
        {
            coloringBookView ??= GetComponent<ColoringBookView>();

            if (!coloringBookView)
            {
                Debug.LogWarning("ColoringBookView required for this component.");
                gameObject.SetActive(false);

                return;
            }

            coloringBookView.OnDataSet += OnDataSet;
            coloringBookView.ViewScaleUpdated += ViewScaleUpdated;
            coloringBookView.OnColorFilled += OnColorFilled;
        }

        private void OnDataSet()
        {
            // Update scale.
            colorViewsReferenceScale =
                Mathf.Min(coloringBookView.ColoringBookData.Texture.width, coloringBookView.ColoringBookData.Texture.height) / (float)referenceSize;

            // Initialize color widgets.
            regionColorViewDictionary = new Dictionary<RegionDataView, RegionColorWidget>();
            if (regionColorViewType == RegionColorViewType.None)
            {
                // Hide all.
                foreach (RegionColorWidget colorWidget in colorWidgets)
                {
                    colorWidget.gameObject.SetActive(false);
                }
            }
            else
            {
                while (colorWidgets.Count > coloringBookView.ColoringBookData.Regions.Count())
                {
                    Destroy(colorWidgets[0].gameObject);
                    colorWidgets.RemoveAt(0);
                }

                // Load region colors.
                for (int index = 0; index < coloringBookView.ColoringBookData.Regions.Count(); index++)
                {
                    IRegionData regionData = coloringBookView.ColoringBookData.Regions.ElementAt(index);

                    if (index >= colorWidgets.Count)
                    {
                        RegionColorWidget regionColorWidget = Instantiate(colorWidgetPrefab, coloringBookView.RectTransform);
                        regionColorWidget.transform.SetAsLastSibling();

                        colorWidgets.Add(regionColorWidget);
                    }

                    colorWidgets[index].gameObject.SetActive(true);

                    Color widgetColor = Color.white;
                    string widgetText = "";
                    switch (regionColorViewType)
                    {
                        case RegionColorViewType.Color:
                            widgetColor = regionData.MainColor;

                            break;

                        case RegionColorViewType.IndexValue:
                            widgetText = $"{coloringBookView.ColoringBookData.ColorIndex(regionData.MainColor) + 1}";

                            break;

                        case RegionColorViewType.ColorAndIndexValue:
                            widgetColor = regionData.MainColor;
                            widgetText = $"{coloringBookView.ColoringBookData.ColorIndex(regionData.MainColor) + 1}";

                            break;
                    }

                    colorWidgets[index].Color = widgetColor;
                    colorWidgets[index].SetText(widgetText);
                    colorWidgets[index].SetPosition(regionData.ColorIndexLocation);
                    colorWidgets[index].Show();

                    // Add to region/colorWidget dictionary.
                    regionColorViewDictionary.Add(
                        coloringBookView.Regions.Find(regionView => regionView.RegionData == regionData),
                        colorWidgets[index]);
                }

                // Subscribe to regions colored event.
                foreach (RegionDataView regionDataView in coloringBookView.Regions)
                {
                    regionDataView.OnColored += OnRegionColored;
                }
            }
        }

        private void ViewScaleUpdated(float scaleValue)
        {
            float newValue = Mathf.Lerp(1f, 1f / scaleValue, regionColorViewScaleEffect);
            foreach (RegionColorWidget regionColorWidget in colorWidgets)
            {
                regionColorWidget.transform.localScale =
                    new Vector3(newValue * colorViewsReferenceScale, newValue * colorViewsReferenceScale, 1f);
            }
        }

        private void OnColorFilled(Color color)
        {
            switch (onRegionColoredAction)
            {
                case RegionColorViewOnColoredAction.HideWhenAllRegionsColored:
                    // Hide all color views.
                    foreach (RegionColorWidget colorWidget in colorWidgets.Where(w => w.Color.Compare(color)))
                    {
                        colorWidget.Hide(.2f);
                    }

                    break;
            }
        }

        private void OnRegionColored(RegionDataView regionView)
        {
            switch (onRegionColoredAction)
            {
                case RegionColorViewOnColoredAction.HideColorView:
                    regionColorViewDictionary[regionView].Hide(.2f);

                    break;
            }
        }

        /// <summary>
        /// What color views look like.
        /// </summary>
        public enum RegionColorViewType
        {
            /// <summary>
            /// Not visible.
            /// </summary>
            None,
            /// <summary>
            /// Just a color.
            /// </summary>
            Color,
            /// <summary>
            /// Color index value.
            /// </summary>
            IndexValue,
            /// <summary>
            /// Both color and index value.
            /// </summary>
            ColorAndIndexValue,
        }

        /// <summary>
        /// What happens to color view when region is filled.
        /// </summary>
        public enum RegionColorViewOnColoredAction
        {
            /// <summary>
            /// Nothing.
            /// </summary>
            None,

            /// <summary>
            /// Hide only corresponding view.
            /// </summary>
            HideColorView,

            /// <summary>
            /// Hide all views of this color when all regions of color are colored.
            /// </summary>
            HideWhenAllRegionsColored,
        }
    }
}
