using UnityEngine;
using UnityEngine.UI;

namespace HootyBird.ColoringBook.Menu.Overlays
{
    public class MainMenuOverlay : MenuOverlay
    {
        [SerializeField]
        private Button playButton;

        protected override void Awake()
        {
            base.Awake();

            playButton.onClick.AddListener(PlayButtonOnClick);
        }

        public override void OnBack() { }

        public void PlayButtonOnClick()
        {
            MenuController.OpenOverlay(MenuController.GetOverlay<SelectColoringBookOverlay>());
        }
    }
}
