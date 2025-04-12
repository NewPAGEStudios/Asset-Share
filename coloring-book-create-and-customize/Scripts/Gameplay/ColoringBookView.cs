using HootyBird.ColoringBook.Data;
using HootyBird.ColoringBook.Gameplay.BookInputHandling;
using HootyBird.ColoringBook.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace HootyBird.ColoringBook.Gameplay
{
    [RequireComponent(typeof(RectTransform))]
    public class ColoringBookView : MonoBehaviour
    {
        [SerializeField]
        private RegionDataView regionDataViewPrefab;
        [SerializeField]
        private RawImage background;
        [SerializeField]
        private Color fillColor;
        [Header("Assign to override default value.")]
        [SerializeField]
        private Material overrideRegionTextureRenderMaterial;

        [Space(10)]
        [Header("Region View Settings")]
        [SerializeField]
        private RegionColoringStyle regionColoringStyle = RegionColoringStyle.CircleFillAnimation;

        private ColoringBookViewDragHandler dragHandler;
        private ColoringBookViewClickHandler clickHandler;

        private Material regionTextureRenderMaterial;
        private List<RegionDataView> regionWidgets;
        private float previousScale;
        private float originalScale;
        private Vector3 previousPosition;
        private int totalPixelCount;

        public RectTransform RectTransform { get; private set; }
        public List<RegionDataView> Regions => regionWidgets;
        public IColoringBookData ColoringBookData { get; private set; }
        public Vector2 Size { get; private set; }
        public float ScaleValue => previousScale / originalScale;
        public float CompletePercent { get; private set; }
        public Color FillColor => fillColor;
        public RegionColoringStyle ColoringStyle
        {
            get => regionColoringStyle;
            set => regionColoringStyle = value;
        }
        /// <summary>
        /// Shared material for all regions.
        /// </summary>
        public Material RegionTextureUpdateMaterial => regionTextureRenderMaterial;

        public Action OnDataSet { get; set; }
        public Action<float> ViewScaleUpdated { get; set; }
        public Action<Color> OnColorFilled { get; set; }
        public Action<float> OnCompletePercentUpdated { get; set; }
        public Action<Color> FillColorUpdated { get; set; }

        private void Awake()
        {
            RectTransform = GetComponent<RectTransform>();
            regionWidgets = new List<RegionDataView>();

            clickHandler = GetComponent<ColoringBookViewClickHandler>();
            dragHandler = GetComponent<ColoringBookViewDragHandler>();
        }

        private void FixedUpdate()
        {
            bool rectsUpdated = false;
            if (previousScale != transform.lossyScale.x)
            {
                previousScale = transform.lossyScale.x;

                rectsUpdated = true;
                ViewScaleUpdate(previousScale / originalScale);
            }

            if (previousPosition != transform.position)
            {
                previousPosition = transform.position;

                if (!rectsUpdated)
                {
                    UpdateRegionsWorldRect();
                }
            }
        }

        private void OnDestroy()
        {
            ReleaseCurrentColoringBookAssets();
        }

        public void LoadColoringBook(IColoringBookData coloringBookData)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            if (!background)
            {
                Debug.LogWarning("No texture target assigned.");    
                return;
            }

            SetFillColor(Color.clear);
            ColoringBookData = coloringBookData;

            // Udpate background texture.
            background.texture = coloringBookData.Texture;
            Size = new Vector2(coloringBookData.Texture.width, coloringBookData.Texture.height);
            RectTransform.sizeDelta = Size;

            // Update regions texture-update material.
            if (overrideRegionTextureRenderMaterial)
            {
                regionTextureRenderMaterial = overrideRegionTextureRenderMaterial;
            }
            else
            {
                switch (coloringBookData.ColoringStyle)
                {
                    case Data.ColoringStyle.FillRegion:
                        regionTextureRenderMaterial = new Material(Resources.Load<Shader>("Shaders/ColoringBook-Default"));

                        break;

                    case Data.ColoringStyle.RemoveRegion:
                        regionTextureRenderMaterial = new Material(Resources.Load<Shader>("Shaders/ColoringBook-Deduct Mask"));

                        break;
                }
            }

            UpdateRegionColoringStyle(regionColoringStyle);

            // Instantiate coloring book regions.
            InstantiateRegions(coloringBookData);

            OnDataSet?.Invoke();

            ViewScaleUpdate(1f);

            // Update transform values.
            originalScale = transform.lossyScale.x;
            previousScale = transform.lossyScale.x;
            previousPosition = transform.position;

            Debug.Log($"Coloring book initialize time: {sw.ElapsedMilliseconds}ms");
            sw.Stop();
        }
        
        public void SetInputHandlerState(bool state)
        {
            if (state)
            {
                EnableCorrectInputHandler();
            }
            else
            {
                clickHandler.enabled = false;
                dragHandler.enabled = false;
            }
        }

        public void SetFillColor(Color color)
        {
            fillColor = color;

            FillColorUpdated?.Invoke(fillColor);
        }

        /// <summary>
        /// Get all regions of selected region which overlay target rect.
        /// </summary>
        /// <param name="target">World rect.</param>
        /// <returns></returns>
        public IEnumerable<RegionDataView> RegionsOverlappingRect(Rect target)
        {
            return regionWidgets.Where(regionDataView => regionDataView.WorldRect.Overlaps(target));
        }

        public void UpdateRegionColoringStyle(RegionColoringStyle newValue)
        {
            regionColoringStyle = newValue;

            EnableCorrectInputHandler();
        }

        /// <summary>
        /// Stops any active updates. When there is a need to terminate any ongoing animations on region views.
        /// </summary>
        public void StopColoringBookUpdates()
        {
            // Stop all (any) fill animations.
            foreach (RegionDataView region in regionWidgets)
            {
                region.StopFillRegionAnimation();
            }

            // Stop drag handler.
            if (dragHandler != null)
            {
                dragHandler.enabled = false;
            }
        }

        public void CheckClickAtScreenPoint(Vector2 screenPosition)
        {
            // Do nothing if fill color is Clear.
            if (fillColor == Color.clear)
            {
                return;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                RectTransform, 
                screenPosition,
                Camera.main, 
                out Vector2 local))
            {
                Vector2 localNormalized = Rect.PointToNormalized(RectTransform.rect, local);
                int2 pixelLocation = new int2(
                    Mathf.FloorToInt(localNormalized.x * ColoringBookData.Texture.width),
                    Mathf.FloorToInt(localNormalized.y * ColoringBookData.Texture.height)
                );

                foreach (RegionDataView regionDataView in regionWidgets)
                {
                    // Skip all that are outside of region rect.
                    if (pixelLocation.x < regionDataView.RegionData.Location.x ||
                        pixelLocation.x > regionDataView.RegionData.Location.x + regionDataView.RegionData.TextureSize.x ||
                        pixelLocation.y < regionDataView.RegionData.Location.y ||
                        pixelLocation.y > regionDataView.RegionData.Location.y + regionDataView.RegionData.TextureSize.y)
                    {
                        continue;
                    }

                    // Check region pixel.
                    int2 regionPixelLocation = pixelLocation - regionDataView.RegionData.Location;
                    if (!ColoringBookTools.TextureHavePixelAt(regionDataView.RegionData.Texture, regionPixelLocation))
                    {
                        continue;
                    }

                    bool regionColored = false;
                    // Check region.
                    switch (regionColoringStyle)
                    {
                        case RegionColoringStyle.None:
                            regionColored = regionDataView.TryColor(
                                fillColor, 
                                RegionDataView.ClearMaskAnimation.None,
                                screenPosition);

                            break;

                        case RegionColoringStyle.CircleFillAnimation:
                            regionColored = regionDataView.TryColor(
                                fillColor, 
                                RegionDataView.ClearMaskAnimation.CircleClear,
                                screenPosition);

                            break;
                    }

                    if (regionColored)
                    {
                        break;
                    }
                }
            }
        }

        public void ReleaseCurrentColoringBookAssets()
        {
            if (ColoringBookData != null)
            {
                ColoringBookData.ReleaseAssets();
            }
        }

        private void EnableCorrectInputHandler()
        {
            // Update drag handler.
            switch (regionColoringStyle)
            {
                case RegionColoringStyle.FreeDrawing:
                    dragHandler.enabled = true;
                    clickHandler.enabled = false;

                    break;

                default:
                    dragHandler.enabled = false;
                    clickHandler.enabled = true;

                    break;
            }
        }

        private void OnRegionFillPixelsUpdated(RegionDataView regionView)
        {
            // Update coloring book complete percentage.
            float percentSum = regionWidgets.Sum(view => view.FilledPixelsCount);
            CompletePercent = percentSum / totalPixelCount;

            OnCompletePercentUpdated?.Invoke(CompletePercent);
        }

        private void OnRegionColored(RegionDataView regionView)
        {
            // Find all views of color.
            IEnumerable<RegionDataView> ofColor = regionWidgets
                .Where(widget => widget.RegionData.MainColor.Compare(regionView.RegionData.MainColor));

            // If all colored, invoke an event.
            if (ofColor.All(widget => widget.Colored))
            {
                OnColorFilled?.Invoke(regionView.RegionData.MainColor);
            }
        }

        private void ViewScaleUpdate(float scaleValue)
        {
            UpdateRegionsWorldRect();

            ViewScaleUpdated?.Invoke(scaleValue);
        }

        private void UpdateRegionsWorldRect()
        {
            foreach (RegionDataView region in regionWidgets)
            {
                region.UpdateWorldRect();
            }
        }

        #region Initializing.

        private void InstantiateRegions(IColoringBookData coloringBookData)
        {
            while (regionWidgets.Count > coloringBookData.Regions.Count())
            {
                // Destroying region will also release all textures used by it.
                Destroy(regionWidgets[0].gameObject);
                regionWidgets.RemoveAt(0);
            }

            // Load regions.
            for (int index = 0; index < coloringBookData.Regions.Count(); index++)
            {
                IRegionData regionData = coloringBookData.Regions.ElementAt(index);

                if (index >= regionWidgets.Count)
                {
                    RegionDataView regionDataView = Instantiate(regionDataViewPrefab, transform);
                    regionDataView.OnFilledPixelCountUpdated += OnRegionFillPixelsUpdated;
                    regionDataView.OnColored += OnRegionColored;

                    regionWidgets.Add(regionDataView);
                }

                switch (coloringBookData.ColoringStyle)
                {
                    // For fillRegion coloring books, region color value, so only the pixels of this color can be used when counting pixels.
                    case Data.ColoringStyle.FillRegion:
                        regionWidgets[index].SetFillColor(regionData.MainColor);

                        break;

                    // for removeRegion, color should be set to white.
                    case Data.ColoringStyle.RemoveRegion:
                        regionWidgets[index].SetFillColor(Color.white);

                        break;
                }
                regionWidgets[index].SetRegionData(regionData);
            }

            totalPixelCount = regionWidgets.Sum(widget => widget.MaxPixelCount);
        }

        #endregion

        /// <summary>
        /// Style of region filling.
        /// </summary>
        public enum RegionColoringStyle
        {
            /// <summary>
            /// Immediately fills region with selected <see cref="RegionFillColorStyle"/>
            /// </summary>
            None,

            /// <summary>
            /// Fill region using circle-fill animation.
            /// </summary>
            CircleFillAnimation,

            /// <summary>
            /// player can freely fill regions with their finger if correct color is selected.
            /// </summary>
            FreeDrawing,
        }
    }
}
