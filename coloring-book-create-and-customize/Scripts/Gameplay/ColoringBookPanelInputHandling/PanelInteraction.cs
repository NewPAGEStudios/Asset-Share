using System;
using System.Collections;
using UnityEngine;

namespace HootyBird.ColoringBook.Gameplay.BookInputHandling
{
    /// <summary>
    /// Used for pinch-in/out support.
    /// </summary>
    public class PanelInteraction : MonoBehaviour
    {
        private TouchControls touchControls;
        private Coroutine zoomingRoutine;

        private bool secondaryCanceled;

        /// <summary>
        /// Zoom started action. Invoked with position between 2 touch points.
        /// </summary>
        public Action<Vector2> OnZoomStarted { get; set; }
        /// <summary>
        /// Invoked while zoom is active. Invoked with distance delta and center point delta.
        /// </summary>
        public Action<float, Vector2> OnZoom { get; set; }
        public Action OnZoomStopped { get; set; }

        private void Awake()
        {
            Initialize();
        }

        public void SetState(bool state)
        {
            Initialize();

            if (state)
            {
                touchControls.Enable();
            }
            else
            {
                touchControls.Disable();
            }
        }

        private void Initialize()
        {
            if (touchControls != null)
            {
                return;
            }

            touchControls = new TouchControls();

            touchControls.Touch.PrimaryFingerContact.canceled += _ => CancelPrimary();
            touchControls.Touch.SecondaryFingerContact.started += _ => StartZooming();
            touchControls.Touch.SecondaryFingerContact.canceled += _ => StopZooming();
        }

        private void StartZooming()
        {
            secondaryCanceled = false;

            Vector2 primaryPos = touchControls.Touch.PrimaryFingerPosition.ReadValue<Vector2>();
            Vector2 secondrayPos = touchControls.Touch.SecondaryFingerPosition.ReadValue<Vector2>();
            Vector2 pos = (secondrayPos - primaryPos) * .5f + primaryPos;

            OnZoomStarted?.Invoke(pos);
            zoomingRoutine = StartCoroutine(ZoomingRoutine(primaryPos, secondrayPos));
        }

        private void StopZooming()
        {
            if (zoomingRoutine != null)
            {
                secondaryCanceled = true;
                StopCoroutine(zoomingRoutine);
                zoomingRoutine = null;
            }
        }

        private void CancelPrimary()
        {
            if (zoomingRoutine != null)
            {
                StopZooming();
            }

            if (secondaryCanceled)
            {
                OnZoomStopped?.Invoke();
                secondaryCanceled = false;
            }
        }

        private IEnumerator ZoomingRoutine(Vector2 primaryPos, Vector2 secondrayPos)
        {
            float previousDistance = Vector2.Distance(primaryPos, secondrayPos);
            Vector2 previousCenter = (secondrayPos - primaryPos) * .5f + primaryPos;

            while (true)
            {
                yield return null;

                Vector2 newPrimary = touchControls.Touch.PrimaryFingerPosition.ReadValue<Vector2>();
                Vector2 newSecondary = touchControls.Touch.SecondaryFingerPosition.ReadValue<Vector2>();
                float currentDistance = Vector2.Distance(newPrimary, newSecondary);
                Vector2 center = (newSecondary - newPrimary) * .5f + newPrimary;

                OnZoom?.Invoke(currentDistance - previousDistance, center - previousCenter);
                previousDistance = currentDistance;
                previousCenter = center;
            }
        }
    }
}
