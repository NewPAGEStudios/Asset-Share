using HootyBird.ColoringBook.Gameplay;
using HootyBird.ColoringBook.Gameplay.BookInputHandling;
using UnityEngine;
using UnityEngine.UI;

namespace HootyBird.ColoringBook.Menu.Widgets
{
    public class ColoringBookPanel : MonoBehaviour
    {
        [SerializeField]
        private ColoringBookView coloringBookView;
        [SerializeField]
        private float zoomSpeed = .1f;
        [SerializeField]
        private float maxZoomMultiplier = 4f;
        [SerializeField]
        private Vector2 padding = new Vector2(.1f, .1f);

        private PanelInteraction panelInteraction;
        private RectTransform parentRect;
        private ScrollRect scrollRect;
        private RectTransform rectTransform;
        private RectTransform scrollRectTransform;

        private float minZoom;
        private float maxZoom;
        private Vector2 zoomPoint;
        private bool zoomStarted;
        private float screenPercent = (Screen.width + Screen.height) * .5f;

        private void Awake()
        {
            parentRect = transform.parent.GetComponent<RectTransform>();
            rectTransform = GetComponent<RectTransform>();
            scrollRect = GetComponentInParent<ScrollRect>();
            scrollRectTransform = scrollRect.GetComponent<RectTransform>();

            panelInteraction = GetComponent<PanelInteraction>();
            panelInteraction.OnZoomStarted += OnZoomStarted;
            panelInteraction.OnZoom += ZoomPanel;
            panelInteraction.OnZoomStopped += OnZoomStopped;

            if (coloringBookView == null)
            {
                enabled = false;
                Debug.LogWarning("Coloring book reference not set.");

                return;
            }

            panelInteraction.SetState(true);
            coloringBookView.OnDataSet += OnColoringBookDataSet;
        }

        /// <summary>
        /// Invoked when pinch start event is triggered.
        /// </summary>
        /// <param name="pos"></param>
        private void OnZoomStarted(Vector2 pos)
        {
            // Can't start outside panel rect.
            if (!RectTransformUtility.RectangleContainsScreenPoint(rectTransform, pos, Camera.main))
            {
                return;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, pos, Camera.main, out Vector2 onRect))
            {
                // Remember pinch point, used for zooming into specific point in panel.
                zoomPoint = onRect;
            }

            zoomStarted = true;

            SetScrollRectState(false);
            // Block coloringBook click events.
            coloringBookView.SetInputHandlerState(false);
        }


        private void ZoomPanel(float zoomDelta, Vector2 posDelta)
        {
            if (!zoomStarted)
            {
                return;
            }

            float zoomDeltaAdjusted = zoomDelta / screenPercent * zoomSpeed;
            float prevZoom = rectTransform.localScale.x;
            float zoomValue = Mathf.Clamp(rectTransform.localScale.x + zoomDeltaAdjusted, minZoom, maxZoom);
            rectTransform.localScale = new Vector3(zoomValue, zoomValue, 1f);

            // Since we're zooming into specific point, move panel towards it.
            rectTransform.anchoredPosition += -zoomPoint * (rectTransform.localScale.x - prevZoom) + posDelta;
        }

        /// <summary>
        /// Invoked when zooming is done.
        /// </summary>
        private void OnZoomStopped()
        {
            if (!zoomStarted)
            {
                return;
            }

            zoomStarted = false;
            SetScrollRectState(true);

            // Unblock coloringBook click events.
            coloringBookView.SetInputHandlerState(true);
        }

        /// <summary>
        /// Set scroll rect state.
        /// </summary>
        /// <param name="state"></param>
        private void SetScrollRectState(bool state)
        {
            if (state)
            {
                scrollRect.enabled = true;
            }
            else
            {
                scrollRect.StopMovement();
                scrollRect.enabled = false;
            }
        }

        private void OnColoringBookDataSet()
        {
            Vector2 bookSize = new Vector2(
                coloringBookView.Size.x + (padding.x * coloringBookView.Size.x),
                coloringBookView.Size.y + (padding.y * coloringBookView.Size.y));

            float aspectRatio = bookSize.x / bookSize.y;
            float scrollRectAspectRatio = scrollRectTransform.rect.width / scrollRectTransform.rect.height;
            // Fit width.
            if (aspectRatio > scrollRectAspectRatio)
            {
                minZoom = scrollRectTransform.rect.width / bookSize.x;

                // Fit panel height to view.
                bookSize.y = parentRect.rect.height / minZoom;
            }
            // Fit height.
            else
            {
                minZoom = scrollRectTransform.rect.height / bookSize.y;

                // Fit panel width to view.
                bookSize.x = parentRect.rect.width / minZoom;
            }

            // Update panel size.
            rectTransform.sizeDelta = bookSize;
            rectTransform.anchoredPosition = Vector3.zero;

            maxZoom = minZoom * maxZoomMultiplier;
            rectTransform.localScale = new Vector3(minZoom, minZoom, 1f);
        }
    }
}
