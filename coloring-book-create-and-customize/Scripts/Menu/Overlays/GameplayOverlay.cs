using HootyBird.ColoringBook.Gameplay;
using HootyBird.ColoringBook.Menu.Widgets;
using HootyBird.ColoringBook.Services;
using HootyBird.ColoringBook.Services.SaveLoad;
using HootyBird.ColoringBook.Tools;
using UnityEngine;

namespace HootyBird.ColoringBook.Menu.Overlays
{
    public class GameplayOverlay : MenuOverlay
    {
        [SerializeField]
        private ColoringBookView coloringBookView;
        [SerializeField]
        private ProgressBarWidget progressBar;
        [SerializeField]
        private BrushSizeWidget brushSizeWidget;

        private bool complete;

        protected override void Awake()
        {
            base.Awake();

            coloringBookView.OnCompletePercentUpdated += OnColoringBookCompletePercentUpdate;
            coloringBookView.OnDataSet += OnColoringBookDataSet;

            brushSizeWidget.OnValueChanged += OnBrushSizeChanged;
            brushSizeWidget.SetSliderMinMax(
                Settings.InternalAppSettings.MinBrushSize,
                Settings.InternalAppSettings.MaxBrushSize);
        }

        public void OnApplicationQuit()
        {
            if (coloringBookView.ColoringBookData != null)
            {
                SaveGame();
            }
        }

        public void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                if (coloringBookView.ColoringBookData != null)
                {
                    SaveGame();
                }
            }
        }

        private void OnBrushSizeChanged(float size)
        {
            TextureDrawService.Instance.BrushSize = size;
        }

        private void OnColoringBookDataSet()
        {
            complete = false;
            progressBar.SetValue(0f);

            switch (coloringBookView.ColoringStyle) 
            {
                case ColoringBookView.RegionColoringStyle.FreeDrawing:
                    brushSizeWidget.gameObject.SetActive(true);
                    brushSizeWidget.SetSliderValue(Settings.InternalAppSettings.DefaultBrushSize);

                    break;

                default:
                    brushSizeWidget.gameObject.SetActive(false);

                    break;
            }
        }

        public override void OnBack() 
        {
            // Try to save coloring book.
            SaveGame();

            MainMenuController mainMenuContoller =
                MenuController.GetMenuController<MainMenuController>(Settings.InternalAppSettings.MainMenuControllerName);

            mainMenuContoller.SetActive(true);
        }

        private void SaveGame()
        {
            if (Settings.InternalAppSettings.SaveProgress)
            {
                if (coloringBookView.CompletePercent < 1f)
                {
                    coloringBookView.StopColoringBookUpdates();
                    SaveLoadService.SaveColoringBook(coloringBookView);
                }
            }
        }

        private void OnColoringBookCompletePercentUpdate(float percentValue)
        {
            if (complete)
            {
                return;
            }

            progressBar.SetValue(percentValue);

            if (percentValue == 1f)
            {
                complete = true;

                ColoringBookCompletePrompt coloringBookCompletePrompt = MenuController.GetOverlay<ColoringBookCompletePrompt>();
                coloringBookCompletePrompt.CloseOnReject();
                coloringBookCompletePrompt.SetButtonsEvents(null, () => {
                    MainMenuController mainMenuContoller =
                        MenuController.GetMenuController<MainMenuController>(Settings.InternalAppSettings.MainMenuControllerName);

                    mainMenuContoller.SetActive(true);
                });

                MenuController.OpenOverlay(coloringBookCompletePrompt);

                // Clear saved data.
                SaveLoadService.ClearSavedColoringBook(coloringBookView);
            }
        }
    }
}
