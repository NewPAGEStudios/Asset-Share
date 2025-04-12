using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HootyBird.ColoringBook.Menu.Widgets
{
    public class ColorWidget : MonoBehaviour
    {
        [SerializeField]
        private Image graphics;
        [SerializeField]
        private Image border;
        [SerializeField]
        private Color selectedBorderColor;
        [SerializeField]
        private TMP_Text label;

        [SerializeField]
        private GameObject completeIcon;

        private Button button;
        private Color defaultBorderColor;

        public Color Color { get; private set; }
        public Action<ColorWidget> OnClicked { get; set; }

        private void Awake()
        {
            button = GetComponent<Button>();

            if (button) 
            {
                button.onClick.AddListener(SelectColorButtonClicked);
            }
            defaultBorderColor = border.color;
        }

        public void SetColor(Color color)
        {
            Color = color;
            graphics.color = color;
        }

        public void SetText(string text)
        {
            label.text = text;
        }

        public void SetSelectedState(bool value)
        {
            border.color = value ? selectedBorderColor : defaultBorderColor;
        }

        public void ShowCompleteIcon()
        {
            completeIcon.SetActive(true);
        }

        public void OnReset()
        {
            completeIcon.SetActive(false);
        }

        private void SelectColorButtonClicked()
        {
            OnClicked?.Invoke(this);
        }
    }
}
