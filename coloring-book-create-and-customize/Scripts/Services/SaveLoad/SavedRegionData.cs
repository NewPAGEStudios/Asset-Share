using HootyBird.ColoringBook.Gameplay;
using System;
using UnityEngine;

namespace HootyBird.ColoringBook.Services.SaveLoad
{
    [Serializable]
    public class SavedRegionData
    {
        [SerializeField]
        private string textureName;
        [SerializeField]
        private int filledPixelCount;

        public string TextureName => textureName;
        public int FilledPixelCount => filledPixelCount;

        public SavedRegionData(RegionDataView view) 
        {
            textureName = view.RegionData.Texture.name;
            filledPixelCount = view.FilledPixelsCount;
        }
    }
}
