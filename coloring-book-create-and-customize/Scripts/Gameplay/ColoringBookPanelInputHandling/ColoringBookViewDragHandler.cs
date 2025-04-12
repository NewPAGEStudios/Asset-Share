using HootyBird.ColoringBook.Services;
using HootyBird.ColoringBook.Tools;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HootyBird.ColoringBook.Gameplay.BookInputHandling
{
    public class ColoringBookViewDragHandler : MonoBehaviour, IDragHandler, IPointerDownHandler
    {
        [SerializeField]
        private float step = 18f;

        private Color brushColor = Color.white;
        private Vector2 lastPos;
        private bool complete;
        private ColoringBookView coloringBookView;

        private IDragPointsModifier DragPointsModifier { get; set; }

        private void Awake()
        {
            coloringBookView = GetComponent<ColoringBookView>();

            coloringBookView.OnDataSet += OnColoringBookDataSet;
            coloringBookView.OnCompletePercentUpdated += OnColoringBookCompletePercentUpdate;
            coloringBookView.FillColorUpdated += FillColorUpdated;
        }

        /// <summary>
        /// Enables toogle in editor.
        /// </summary>
        private void OnEnable() { }

        private void OnDisable()
        {
            DragPointsModifier.Clear();
        }

        private void Start()
        {
            // Assign default one if none assigned.
            DragPointsModifier = GetComponent<IDragPointsModifier>() ?? new DefaultDragPointsModifier();
        }

        private void Update()
        {
            if (!DragPointsModifier.Rects.Any())
            {
                return;
            }

            DragPointsModifier.UpdateRects(Time.deltaTime);
            DragPointsModifier.UpdateAffectingRegions(coloringBookView);

            if (DragPointsModifier.HaveUpdates)
            {
                foreach (RegionDataView region in DragPointsModifier.AffectedRegions)
                {
                    // Update mask.
                    TextureDrawService.Instance.DrawBrush(
                        region.MaskTexture,
                        brushColor,
                        DragPointsModifier.Rects,
                        region.WorldRect);

                    // Update region texture.
                    region.UpdateRegionTexture();

                    // Update filled pixels count.
                    region.UpdateFilledPixelsFromRegionTexture();

                    if (complete)
                    {
                        break;
                    }
                }

                DragPointsModifier.Use();
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            // No reason to paint on a texture with a clear color :(
            if (coloringBookView.FillColor == Color.clear)
            {
                return;
            }

            float stepScaled = coloringBookView.ScaleValue * step;
            if (Vector2.Distance(lastPos, eventData.position) < stepScaled)
            {
                return;
            }

            List<Vector2> positions = new List<Vector2>();
            // Lerp quads positions across delta.
            float progress = 0f;
            do
            {
                Vector2 position = Vector2.Lerp(
                    eventData.position - eventData.delta,
                    eventData.position,
                    progress / eventData.delta.magnitude);
                positions.Add(position);

                progress += stepScaled;
            } while (progress < eventData.delta.magnitude);

            IEnumerable<Rect> brushRects = TextureDrawService.Instance
                .BrushRectAtPositions(positions, coloringBookView.ScaleValue)
                // Only use rects that affect at least 1 region with correct fillColor.
                .Where(rect => coloringBookView
                    .RegionsOverlappingRect(rect)
                    .Any(region => region.RegionData.MainColor.Compare(coloringBookView.FillColor)));

            // Get all brush rects.
            DragPointsModifier.AddRects(brushRects);

            lastPos = eventData.position;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            lastPos = eventData.position;
        }

        private void OnColoringBookDataSet()
        {
            complete = false;
        }

        private void OnColoringBookCompletePercentUpdate(float precentage)
        {
            if (complete)
            {
                return;
            }

            if (precentage == 1f)
            {
                complete = true;

                DragPointsModifier.Clear();
            }
        }

        private void FillColorUpdated(Color fillColor)
        {
            DragPointsModifier.Clear();
        }
    }
}
