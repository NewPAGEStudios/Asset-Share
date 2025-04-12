using HootyBird.ColoringBook.Data;
using HootyBird.ColoringBook.Services;
using HootyBird.ColoringBook.Tools;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace HootyBird.ColoringBook.Gameplay
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(RawImage))]
    public class RegionDataView : MonoBehaviour
    {
        [SerializeField]
        private RawImage image;
        [SerializeField]
        private RenderTexture maskTexture;
        [SerializeField]
        private RenderTexture regionTexture;
        [SerializeField]
        private RenderTexture downscaleRegionTexture;

        private Coroutine activeFillCoroutine;
        private int downscale;
        private Color maskBGColor;
        private ColoringBookView coloringBookView;

        public RectTransform RectTransform { get; private set; }
        public Rect WorldRect { get; private set; }
        public Color FillColor { get; set; }
        public int MaxPixelCount { get; private set; }
        public int FilledPixelsCount { get; private set; }
        public IRegionData RegionData { get; private set; }
        public float FillPercent => Mathf.Clamp01((float)FilledPixelsCount / MaxPixelCount);
        public bool Colored => FillPercent == 1f;
        public RenderTexture MaskTexture => maskTexture;
        public Action<RegionDataView> OnFilledPixelCountUpdated { get; set; }
        public Action<RegionDataView> OnColored { get; set; }

        private void Awake()
        {
            RectTransform = GetComponent<RectTransform>();
            coloringBookView = GetComponentInParent<ColoringBookView>();

            // Adjust rect transform.
            RectTransform.anchorMin = Vector3.zero;
            RectTransform.anchorMax = Vector3.zero;
            RectTransform.pivot = Vector3.zero;
        }

        private void Start()
        {
            WorldRect = GetWorldRect();
        }

        public void UpdateWorldRect()
        {
            WorldRect = GetWorldRect();
        }

        private void OnDestroy()
        {
            ReleaseRenderTextures();
        }

        public void SetRegionData(IRegionData regionData)
        {
            RegionData = regionData;
            FilledPixelsCount = 0;
            maskBGColor = Color.clear;

            StopAllCoroutines();
            activeFillCoroutine = null;

            // Set downscale value.
            // If downscale value wasn't precalculated, resolve it now (region initialization takes longer).
            bool recalculateDownscalePixelsCount = regionData.Downscale == 0;
            if (recalculateDownscalePixelsCount)
            {
                downscale = ColoringBookTools.GetDownscaleValue(RegionData.TextureSize.x * RegionData.TextureSize.y);
            }
            else
            {
                downscale = regionData.Downscale;
            }

            #region Init textures.

            ReleaseRenderTextures();

            // Init mask texture.
            maskTexture = new RenderTexture(
                RegionData.TextureSize.x,
                RegionData.TextureSize.y,
                0,
                RenderTextureFormat.R8);
            maskTexture.name = $"{regionData.Texture.name}_MaskTexture";
            maskTexture.Create();

            // Init region render texture.
            regionTexture = new RenderTexture(
                RegionData.TextureSize.x,
                RegionData.TextureSize.y,
                0,
                RenderTextureFormat.ARGB4444);
            regionTexture.name = $"{regionData.Texture.name}_RegionTexture";
            regionTexture.Create();

            downscaleRegionTexture = new RenderTexture(
                regionTexture.width / downscale,
                regionTexture.height / downscale,
                0,
                RenderTextureFormat.R8);
            downscaleRegionTexture.name = $"{regionData.Texture.name}_DownscaleTexture";
            downscaleRegionTexture.Create();

            UpdateRegionTexture();

            // Assign MaxPixelCount.
            if (recalculateDownscalePixelsCount)
            {
                RenderTexture downscaleTexture = ColoringBookTools.GetDownscaleTexture(regionTexture, Color.white, downscale);
                MaxPixelCount =
                    ColoringBookTools.GetPixelErrorSize((int)TextureComputeService.CountPixels(downscaleTexture, Color.red)) *
                    downscale *
                    downscale;
                RenderTexture.ReleaseTemporary(downscaleTexture);
            }
            else
            {
                MaxPixelCount = ColoringBookTools.GetPixelErrorSize(regionData.DownscalePixelCount) * downscale * downscale;
            }

            #endregion

            // Update components.
            image.texture = regionTexture;
            RectTransform.anchoredPosition = new Vector2(regionData.Location.x, regionData.Location.y);
            RectTransform.sizeDelta = new Vector2(regionData.TextureSize.x, regionData.TextureSize.y);
        }

        /// <summary>
        /// Try to fill region with given color.
        /// Only invoked when coloring books' regionColoringStyle is not set to freeDrawing.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public bool TryColor(Color color, ClearMaskAnimation maskAnimation, Vector2 atScreenPosition)
        {
            if (!Colored && RegionData.MainColor.Compare(color))
            {
                FilledPixelsCount = MaxPixelCount;
                FillRegion(maskAnimation, atScreenPosition);

                OnFilledPixelCountUpdated?.Invoke(this);
                OnColored?.Invoke(this);

                return true;
            }

            return false;
        }

        public void SetFillColor(Color fillColor)
        {
            FillColor = fillColor;
        }

        public Rect GetWorldRect()
        {
            Vector3[] corners = new Vector3[4];
            RectTransform.GetWorldCorners(corners);

            return new Rect(corners[0].x, corners[0].y, corners[2].x - corners[0].x, corners[2].y - corners[0].y);
        }

        public void SetMaskColor(Color color)
        {
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = maskTexture;
            GL.Clear(true, true, color);
            RenderTexture.active = prev;
        }

        public void UpdateRegionTexture()
        {
            coloringBookView.RegionTextureUpdateMaterial.SetTexture("_MaskTex", maskTexture);
            coloringBookView.RegionTextureUpdateMaterial.SetColor("_Color", FillColor);

            // Clear region texture first.
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = regionTexture;
            GL.Clear(true, true, Color.clear);
            // Render graphics into region texture using mask.
            Graphics.Blit(RegionData.Texture, regionTexture, coloringBookView.RegionTextureUpdateMaterial);
            RenderTexture.active = prev;
        }

        /// <summary>
        /// Use <see cref="TextureComputeService"/> to count all pixels filled with <see cref="FillColor"/>
        /// </summary>
        public void UpdateFilledPixelsFromRegionTexture()
        {
            ColoringBookTools.BlitR8ToRenderTexture(regionTexture, downscaleRegionTexture, FillColor);
            uint count = TextureComputeService.CountPixels(downscaleRegionTexture);

            switch (coloringBookView.ColoringBookData.ColoringStyle)
            {
                case ColoringStyle.FillRegion:
                    UpdateFilledPixels((int)count * downscale * downscale);

                    break;

                case ColoringStyle.RemoveRegion:
                    UpdateFilledPixels(Mathf.Max(0, MaxPixelCount - ((int)count * downscale * downscale)));

                    break;
            }
        }

        /// <summary>
        /// Set the amount of filled pixels.
        /// </summary>
        /// <param name="count">Filled pixels count.</param>
        public void UpdateFilledPixels(int count)
        {
            bool colored = Colored;

            FilledPixelsCount = Mathf.Min(count, MaxPixelCount);
            OnFilledPixelCountUpdated?.Invoke(this);

            // Invoke colored event if state changed.
            if (!colored && Colored)
            {
                OnColored?.Invoke(this);
            }
        }

        /// <summary>
        /// Stops any ongoing fill animations.
        /// </summary>
        public void StopFillRegionAnimation(bool completeFillRoutine = true)
        {
            StopAllCoroutines();

            // If region is Colored, but have an ongoing fill coroutine, manuall update mask.
            if (activeFillCoroutine != null)
            {
                activeFillCoroutine = null;

                if (completeFillRoutine && Colored)
                {
                    SetMaskColor(Color.white);
                }
            }
        }

        private void FillRegion(ClearMaskAnimation style, Vector2 atScreenPosition)
        {
            switch (style)
            {
                case ClearMaskAnimation.None:
                    SetMaskColor(Color.white);
                    UpdateRegionTexture();

                    break;

                case ClearMaskAnimation.CircleClear:
                    if (activeFillCoroutine == null)
                    {
                        activeFillCoroutine = StartCoroutine(CircleClearMaskRoutine(atScreenPosition));
                    }

                    break;
            }
        }

        private void ReleaseRenderTextures()
        {
            if (maskTexture)
            {
                maskTexture.Release();
                Destroy(maskTexture);
            }

            if (regionTexture)
            {
                regionTexture.Release();
                Destroy(regionTexture);
            }

            if (downscaleRegionTexture)
            {
                downscaleRegionTexture.Release();
                Destroy(downscaleRegionTexture);
            }
        }

        private IEnumerator CircleClearMaskRoutine(Vector2 atScreenPosition)
        {
            Rect rect = new Rect(WorldRect);

            // Update mask render color.
            Color maskRenderColor = Color.white;

            // Find max radius.
            Vector3 atWorldPos = Camera.main.ScreenToWorldPoint(atScreenPosition);
            atWorldPos.z = 0f;
            float radius = Mathf.Max(
                Vector3.Distance(atWorldPos, rect.min),
                Vector3.Distance(atWorldPos, new Vector3(rect.min.x, rect.max.y)),
                Vector3.Distance(atWorldPos, rect.max),
                Vector3.Distance(atWorldPos, new Vector3(rect.max.x, rect.min.y)));
            // Add extra 10%.
            radius *= 1.1f;

            float duration = TextureFillService.Instance.FillAnimationDuration;
            float timer = 0f;
            float progress;
            do
            {
                timer += Time.deltaTime;
                progress = Mathf.Clamp01(timer / duration);

                TextureFillService.Instance.DrawCircle(
                    maskTexture,
                    maskBGColor,
                    maskRenderColor,
                    rect,
                    atWorldPos,
                    Mathf.Lerp(0f, radius, progress)
                );

                UpdateRegionTexture();

                yield return null;
            } while (progress < 1f);

            activeFillCoroutine = null;
        }

        public enum ClearMaskAnimation
        { 
            None,
            CircleClear,
        }
    }
}
