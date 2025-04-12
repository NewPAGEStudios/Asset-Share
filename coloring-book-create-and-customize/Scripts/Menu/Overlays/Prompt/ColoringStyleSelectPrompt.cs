using HootyBird.ColoringBook.Data;
using HootyBird.ColoringBook.Tools;
using UnityEngine;
using UnityEngine.UI;
using static HootyBird.ColoringBook.Gameplay.ColoringBookView;

namespace HootyBird.ColoringBook.Menu.Overlays
{
    public class ColoringStyleSelectPrompt : PromptOverlay
    {
        [SerializeField]
        private Button dropColorButton;
        [SerializeField]
        private Button freeDrawingButton;

        private IColoringBookData bookToLoad;

        protected override void Awake()
        {
            base.Awake();

            dropColorButton.onClick.AddListener(OnDropColorSelected);
            freeDrawingButton.onClick.AddListener(OnFreeDrawingSelected);
        }

        public void SetColoringBookToLoad(IColoringBookData coloringBookData)
        {
            bookToLoad = coloringBookData;
        }

        public override void Open()
        {
            base.Open();

            CloseOnReject();
        }

        private void OnDropColorSelected()
        {
            Reject();

            LoadColoringBook(RegionColoringStyle.CircleFillAnimation);
        }

        private void OnFreeDrawingSelected()
        {
            Reject();

            LoadColoringBook(RegionColoringStyle.FreeDrawing);
        }

        private void LoadColoringBook(RegionColoringStyle coloringStyle)
        {
            if (bookToLoad == null)
            {
                return;
            }

            GameplayMenuController gameplayMenuController =
                MenuController.GetMenuController<GameplayMenuController>(Settings.InternalAppSettings.GameplayMenuControllerName);

            gameplayMenuController.SetActive(true);
            gameplayMenuController.LoadColoringBook(bookToLoad, coloringStyle, true);
        }
    }
}
