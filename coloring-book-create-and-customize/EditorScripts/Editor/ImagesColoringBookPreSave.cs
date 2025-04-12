using System.Linq;
using HootyBird.ColoringBook.Serialized;
using HootyBird.ColoringBook.Services;
using HootyBird.ColoringBook.Tools;
using UnityEditor;
using UnityEngine;

namespace HootyBird.ColoringBook.Editor
{
    public class ImagesColoringBookPreSave : AssetModificationProcessor
    {
        private static string[] OnWillSaveAssets(string[] paths)
        {
            foreach (string assetPath in paths)
            {
                System.Type assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath.Replace(Application.dataPath, "Assets/"));
                if (assetType == null)
                {
                    continue;
                }

                if (assetType.GetType().GetMethod("AsType").Invoke(assetType, null).ToString() == typeof(ImagesColoringBookData).ToString())
                {
                    UpdateImagesColoringBookData(assetPath);
                }
            }

            return paths;
        }

        private static void UpdateImagesColoringBookData(string assetPath)
        {
            ImagesColoringBookData bookData = AssetDatabase.LoadAssetAtPath<ImagesColoringBookData>(assetPath);

            // Update downscale values for all regions.
            RenderTexture downscaleTexture = null;
            foreach (RegionData regionData in bookData.Regions.Cast<RegionData>())
            {
                int downscale = ColoringBookTools.GetDownscaleValue(regionData.TextureSize.x * regionData.TextureSize.y);
                downscaleTexture = ColoringBookTools.GetDownscaleTexture(regionData.Texture, Color.white, downscale);
                int downscalePixes = (int)TextureComputeService.CountPixels(downscaleTexture, Color.red);

                regionData.SetDownscaleValue(downscale);
                regionData.SetDownscalePixelCount(downscalePixes);
            }

            if (downscaleTexture != null)
            {
                RenderTexture.ReleaseTemporary(downscaleTexture);
            }

            SerializedObject serializedObject = new SerializedObject(bookData);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
