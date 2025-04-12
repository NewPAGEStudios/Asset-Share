using HootyBird.ColoringBook.Data;
using HootyBird.ColoringBook.Gameplay;
using HootyBird.ColoringBook.Services.SaveLoad;
using HootyBird.ColoringBook.Tools;
using UnityEngine;
using static HootyBird.ColoringBook.Gameplay.ColoringBookView;

namespace HootyBird.ColoringBook.Menu
{
    public class GameplayMenuController : MenuController
    {
        [SerializeField]
        private ColoringBookView coloringBookView;

        public IColoringBookData ColoringBookData { get; private set; }

        protected override void Awake()
        {
            base.Awake();

            Settings.InternalAppSettings.GameplayMenuControllerName = name;
        }

        public void LoadColoringBook(
            IColoringBookData coloringBookData,
            RegionColoringStyle coloringStyle,
            bool tryLoadSavedData = true)
        {
            ColoringBookData = coloringBookData;
            coloringBookData.InitializeAssets();

            coloringBookView.ColoringStyle = coloringStyle;
            coloringBookView.LoadColoringBook(coloringBookData);

            if (tryLoadSavedData)
            {
                SaveLoadService.LoadColoringBookFromSavedData(coloringBookView);
            }
        }

        public override void SetActive(bool state)
        {
            base.SetActive(state);

            if (!state)
            {
                // Release previous coloringBookData assets (if any).
                coloringBookView.ReleaseCurrentColoringBookAssets();
            }
        }
    }
}
