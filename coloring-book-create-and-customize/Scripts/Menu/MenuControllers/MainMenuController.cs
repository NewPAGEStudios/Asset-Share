using HootyBird.ColoringBook.Tools;
using UnityEngine;

namespace HootyBird.ColoringBook.Menu
{
    public class MainMenuController : MenuController
    {
        protected override void Awake()
        {
            base.Awake();

            Settings.InternalAppSettings.MainMenuControllerName = name;

            // Adjust framerate.
            Application.targetFrameRate = Settings.InternalAppSettings.TargetFramerate;
        }
    }
}
