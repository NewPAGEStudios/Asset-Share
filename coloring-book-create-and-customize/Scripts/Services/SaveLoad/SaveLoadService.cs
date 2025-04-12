using HootyBird.ColoringBook.Gameplay;
using HootyBird.ColoringBook.Tools;
using System.IO;
using UnityEngine;

namespace HootyBird.ColoringBook.Services.SaveLoad
{
    public class SaveLoadService
    {
        public static void SaveColoringBook(ColoringBookView view, bool clearBeforeSave = true)
        {
            if (view == null)
            {
                return;
            }
            
            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

            SavedColoringBookData dataToSave = new SavedColoringBookData(view);
            // Nothing to save.
            if (dataToSave.SavedRegionData.Length == 0)
            {
                Debug.Log($"No regions data to save on given ColoringBookView");
                return;
            }

            // Check saved games folder path.
            string path = Path.Combine(Application.persistentDataPath, Settings.InternalAppSettings.SaveFolderPath);
            CheckFolder(path);

            // If set, clear existing saved data for this coloring book.
            if (clearBeforeSave)
            {
                ClearSavedColoringBook(view);
            }

            // Check folder path for coloring book.
            path = GetColoringBookToPath(path, view);
            CheckFolder(path);

            // Save masks for all regions to disk.
            foreach (SavedRegionData regionData in dataToSave.SavedRegionData)
            {
                RegionDataView regionView = view.Regions.Find(region => region.RegionData.Texture.name == regionData.TextureName);
                // There should not be a case where saved data that was just generated from coloring book contains region data 
                // about region that is NOT a part of this coloring book.
                if (regionView == null)
                {
                    continue;
                }

                // No need to save a texture for already colored region view.
                if (regionView.Colored)
                {
                    continue;
                }

                // Save it's mask to disk.
                Texture2D maskTexture = GetTextureFromMask(regionView.MaskTexture);
                File.WriteAllBytes(
                    Path.Combine(path, $"{regionView.RegionData.Texture.name}.png"),
                    maskTexture.EncodeToPNG());

                //Release texture.
                Object.Destroy(maskTexture);
            }

            // Save SavedColoringBookData to disk.
            File.WriteAllText(Path.Combine(path, "data.json"), JsonUtility.ToJson(dataToSave));

            Debug.Log($"Saved coloring book progress {view.ColoringBookData.Name} to {path} in {sw.ElapsedMilliseconds}ms");

            Texture2D GetTextureFromMask(RenderTexture renderTexture)
            {
                RenderTexture prev = RenderTexture.active;
                RenderTexture.active = renderTexture;
                Texture2D texture = new Texture2D(
                    renderTexture.width,
                    renderTexture.height,
                    TextureFormat.R8,
                    false);
                texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                texture.Apply();
            RenderTexture.active = prev;

                return texture;
            }
        }

        public static void LoadColoringBookFromSavedData(ColoringBookView view)
        {
            if (view == null)
            {
                return;
            }

            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

            // Check saved games folder path, return if none.
            string path = Path.Combine(Application.persistentDataPath, Settings.InternalAppSettings.SaveFolderPath);
            if (!Directory.Exists(path))
            {
                return;
            }

            // Check folder path for coloring book.
            path = GetColoringBookToPath(path, view);
            // There is no save folder for this coloring book, return.
            if (!Directory.Exists(path))
            {
                return;
            }

            // Load saved game data.
            SavedColoringBookData savedData = JsonUtility.FromJson<SavedColoringBookData>(
                File.ReadAllText(Path.Combine(path, "data.json")));
            foreach (SavedRegionData regionData in savedData.SavedRegionData)
            {
                RegionDataView regionView = view.Regions.Find(region => region.RegionData.Texture.name == regionData.TextureName);

                if (regionView == null)
                {
                    Debug.Log($"Issue with save file, failed to find region {regionData.TextureName}, is this region was removed after save file was created?");
                    continue;
                }

                regionView.UpdateFilledPixels(regionData.FilledPixelCount);
                // If already colored, no need to load mask texture.
                if (regionView.Colored)
                {
                    // Manually fill the mask.
                    regionView.SetMaskColor(Color.white);
                }
                else
                {
                    // Load mask from the disk.
                    Texture2D maskTexture = new Texture2D(
                        regionView.MaskTexture.width,
                        regionView.MaskTexture.height,
                        TextureFormat.R8,
                        false);

                    string maskTexturePath = Path.Combine(path, $"{regionView.RegionData.Texture.name}.png");
                    if (!File.Exists(maskTexturePath))
                    {
                        continue;
                    }

                    maskTexture.LoadImage(File.ReadAllBytes(maskTexturePath));
                    maskTexture.Apply();

                    Graphics.Blit(maskTexture, regionView.MaskTexture);

                    //Release texture.
                    Object.Destroy(maskTexture);
                }

                // Refresh region texture.
                regionView.UpdateRegionTexture();
            }

            Debug.Log($"Loaded coloring book progress {view.ColoringBookData.Name} to {path} in {sw.ElapsedMilliseconds}ms");
        }

        public static void ClearSavedColoringBook(ColoringBookView view)
        {
            // Check saved games folder path.
            string path = Path.Combine(Application.persistentDataPath, Settings.InternalAppSettings.SaveFolderPath);
            CheckFolder(path);

            // Check folder path for coloring book.
            path = GetColoringBookToPath(path, view);

            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }

        private static void CheckFolder(string path)
        {
            // Create folder if needed.
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private static string GetColoringBookToPath(string path, ColoringBookView view)
        {
            return Path.Combine(
                path,
                $"{view.ColoringBookData.Name}-for-style-{view.ColoringStyle}");
        }
    }
}
