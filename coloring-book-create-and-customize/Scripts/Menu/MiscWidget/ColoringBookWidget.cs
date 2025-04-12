using HootyBird.ColoringBook.Data;
using HootyBird.ColoringBook.Menu.Overlays;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HootyBird.ColoringBook.Menu.Widgets
{
    [RequireComponent(typeof(Button))]
    public class ColoringBookWidget : MonoBehaviour
    {
        [SerializeField]
        private RawImage graphics;
        [SerializeField]
        private TMP_Text nameLabel;

        private AspectRatioFitter aspectRatioFitter;
        private Button button;
        private MenuOverlay menuOverlay;
        private IColoringBookData coloringBookData;

        private void Awake()
        {
            menuOverlay = GetComponentInParent<MenuOverlay>();
            aspectRatioFitter = graphics.GetComponent<AspectRatioFitter>();

            button = GetComponent<Button>();
            button.onClick.AddListener(OpenBookButtonClick);
        }

        public void SetData(IColoringBookData coloringBookData)
        {
            this.coloringBookData = coloringBookData;

            nameLabel.text = coloringBookData.Name;
            graphics.texture = coloringBookData.Texture;
            aspectRatioFitter.aspectRatio = (float)coloringBookData.Texture.width / coloringBookData.Texture.height;
        }

        private void OpenBookButtonClick()
        {
            ColoringStyleSelectPrompt styleSelectPrompt = menuOverlay.MenuController.GetOverlay<ColoringStyleSelectPrompt>();
            styleSelectPrompt.SetColoringBookToLoad(coloringBookData);

            menuOverlay.MenuController.OpenOverlay(styleSelectPrompt);
        }
    }
}
