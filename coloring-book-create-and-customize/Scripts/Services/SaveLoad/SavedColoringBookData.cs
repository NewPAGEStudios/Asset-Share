using HootyBird.ColoringBook.Gameplay;
using HootyBird.ColoringBook.Tools;
using System;
using System.Linq;
using UnityEngine;

namespace HootyBird.ColoringBook.Services.SaveLoad
{
    [Serializable]
    public class SavedColoringBookData
    {
        [SerializeField]
        private SavedRegionData[] regionsData;

        public SavedRegionData[] SavedRegionData => regionsData;

        public SavedColoringBookData(ColoringBookView view)
        {
            regionsData = view.Regions
                .Where(region => region.FilledPixelsCount > Settings.InternalAppSettings.MinFilledPixelsInRegionToSave)
                .Select(region => new SavedRegionData(region))
                .ToArray();
        }
    }
}
