using HootyBird.ColoringBook.Tween;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace HootyBird.ColoringBook.Menu.Widgets
{
    [RequireComponent(typeof(TweenBase))]
    public class RegionColorWidget : MonoBehaviour
    {
        [SerializeField]
        private Image colorTarget;
        [SerializeField]
        private TMP_Text label;

        private TweenBase tween;
        private RectTransform rectTransform;

        public Color Color
        {
            get => colorTarget.color;
            set => colorTarget.color = value;
        }

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            tween = GetComponent<TweenBase>();
        }

        public void SetPosition(int2 localPosition)
        {
            rectTransform.anchoredPosition = new Vector2(localPosition.x, localPosition.y);
        }

        public void SetText(string text)
        {
            label.text = text;
        }

        public void Hide(float time = 0f)
        {
            tween.playbackTime = time;
            tween.PlayBackward(false);
        }

        public void Show(float time = 0f)
        {
            tween.playbackTime = time;
            tween.PlayForward(false);
        }
    }
}
