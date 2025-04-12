using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HootyBird.ColoringBook.Menu.Widgets
{
    [RequireComponent(typeof(Slider))]
    public class ProgressBarWidget : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text label;

        private Slider slider;

        private void Awake()
        {
            slider = GetComponent<Slider>();
        }

        public void SetValue(float value)
        {
            slider.value = Mathf.FloorToInt(value * slider.maxValue);
            label.text = $"{Mathf.FloorToInt(value * 100)}%";
        }
    }
}