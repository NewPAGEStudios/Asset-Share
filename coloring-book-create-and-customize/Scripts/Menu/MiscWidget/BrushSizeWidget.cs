using System;
using UnityEngine;
using UnityEngine.UI;

namespace HootyBird.ColoringBook.Menu.Widgets
{
    public class BrushSizeWidget : MonoBehaviour
    {
        [SerializeField]
        private Slider slider;

        public Action<float> OnValueChanged { get; set; }

        private void Awake()
        {
            slider.onValueChanged.AddListener((value) => OnValueChanged?.Invoke(value));
        }

        public void SetSliderMinMax(float min, float max)
        {
            slider.minValue = min;
            slider.maxValue = max;
        }

        public void SetSliderValue(float value)
        {
            slider.value = value;
        }
    }
}
