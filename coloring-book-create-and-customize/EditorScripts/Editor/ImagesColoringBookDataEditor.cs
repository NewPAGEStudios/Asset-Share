using HootyBird.ColoringBook.Data;
using HootyBird.ColoringBook.Serialized;
using HootyBird.ColoringBook.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace HootyBird.ColoringBook.Editor
{
    [CustomEditor(typeof(ImagesColoringBookData))]
    public class ImagesColoringBookDataEditor : UnityEditor.Editor
    {
        private static JobHandle jobHandle;

        private const float ZoomStep = .2f;
        private float MaxZoomMltp = 20f;

        public VisualTreeAsset editorAsset;
        public VisualTreeAsset regionDataEntryAsset;
        public VisualTreeAsset regionIndexLocationAsset;

        private ImagesColoringBookData coloringBook;
        private VisualElement textureContainer;
        private VisualElement singleRegionContainer;
        private VisualElement anyRegionContainer;
        private VisualElement multiRegionsContainer;
        private SliderInt errorSlider;
        private ListView regionsList;
        private VisualElement scrollView;
        private Button addRegionButton;
        private Toggle removeTextureToggle;
        private Toggle removeMergeTargets;
        private ListView colorsList;
        private ColorField customColorField;
        private ColorField regionsOfColor;

        private float maxZoom;
        private float minZoom;
        private States state;
        private List<Color> colors;

        // Serialized properties.
        private SerializedProperty regionsProperty;
        private SerializedProperty zoomProperty;

        public override VisualElement CreateInspectorGUI()
        {
            if (Application.isPlaying)
            {
                return new Label("Coloring Book editor not available while editor is playing.");
            }

            VisualElement root = editorAsset.CloneTree();

            ObjectField textureField = root.Q<ObjectField>("texture");
            textureField.RegisterValueChangedCallback(OnTextureValueChanged);

            regionsList = root.Q<ListView>("regions");
            regionsList.selectionChanged += RegionSelectionChanged;
            regionsList.makeItem = () =>
            {
                TemplateContainer templateContainer = regionDataEntryAsset.CloneTree();

                ColorField colorField = templateContainer.Q<ColorField>();
                colorField.SetEnabled(false);

                Toggle activeToggle = templateContainer.Q<Toggle>("active-toggle");
                activeToggle.RegisterValueChangedCallback((toggle) => UpdateRegionsView());

                return templateContainer;
            };

            // Add region panel.
            errorSlider = root.Q<SliderInt>("error-size");
            addRegionButton = root.Q<Button>("add-region");
            addRegionButton.clicked += AddRegionButtonClicked;
            Button addRegionsOfColorButton = root.Q<Button>("add-regions-from-color");
            addRegionsOfColorButton.clicked += AddRegionsOfColorButtonClicked;
            regionsOfColor = root.Q<ColorField>("regions-of-color");

            // Control panel.
            singleRegionContainer = root.Q<VisualElement>("single-panel");
            singleRegionContainer.SetEnabled(false);
            Button growButton = root.Q<Button>("grow-button");
            growButton.clicked += GrowButtonClicked;

            multiRegionsContainer = root.Q<VisualElement>("multi-panel");
            multiRegionsContainer.SetEnabled(false);
            anyRegionContainer = root.Q<VisualElement>("any-panel");
            anyRegionContainer.SetEnabled(false);
            removeTextureToggle = root.Q<Toggle>("remove-texture-toggle");
            Button deleteButton = root.Q<Button>("delete-button");
            deleteButton.clicked += DeleteButtonClicked;
            Button showButton = root.Q<Button>("show-button");
            showButton.clicked += ShowButtonClicked;
            Button hideButton = root.Q<Button>("hide-button");
            hideButton.clicked += HideButtonClicked;
            removeMergeTargets = root.Q<Toggle>("remove-merged");
            Button mergeButton = root.Q<Button>("merge-button");
            mergeButton.clicked += MergeButtonClicked;
            Button updateColorsButton = root.Q<Button>("update-colors");
            updateColorsButton.clicked += UpdateColorsButtonClicked;
            customColorField = root.Q<ColorField>("custom-color");

            // Colors panel.
            colorsList = root.Q<ListView>("colors-list");
            colorsList.selectionChanged += ColorsSelectionChanged;
            colorsList.makeItem = () =>
            {
                ColorField colorField = new ColorField();
                colorField.label = string.Empty;
                colorField.SetEnabled(false);

                return colorField;
            };

            // Texture view panel.
            scrollView = root.Q<VisualElement>("scroll-view");
            scrollView.RegisterCallback<WheelEvent>(OnWheelEvent);
            scrollView.RegisterCallback<MouseMoveEvent>(OnScrollViewMouseMoved);
            scrollView.RegisterCallback<GeometryChangedEvent>(OnSizeChanged);

            textureContainer = root.Q<VisualElement>("texture-container");
            textureContainer.RegisterCallback<MouseUpEvent>(OnMouseUp);

            UpdateColors();
            UpdateRegionsView();

            return root;
        }

        private void OnEnable()
        {
            coloringBook = target as ImagesColoringBookData;
            state = States.None;
            colors = new List<Color>();

            regionsProperty = serializedObject.FindProperty("regions");
            zoomProperty = serializedObject.FindProperty("panelZoomValue");
        }

        private void OnScrollViewMouseMoved(MouseMoveEvent evt)
        {
            switch (evt.pressedButtons)
            {
                // Middle button.
                case 4:
                    Vector2 offset = new Vector2(
                        textureContainer.style.translate.value.x.value + evt.mouseDelta.x,
                        textureContainer.style.translate.value.y.value + evt.mouseDelta.y);

                    Vector2 halfTexturePanelSize = new Vector2(
                        textureContainer.resolvedStyle.width,
                        textureContainer.resolvedStyle.height) * .5f;

                    Vector2 position = new Vector2(
                        Mathf.Clamp(offset.x, -halfTexturePanelSize.x, halfTexturePanelSize.x),
                        Mathf.Clamp(offset.y, -halfTexturePanelSize.y, halfTexturePanelSize.y));
                    MoveTetxureContainer(position);

                    break;
            }
        }

        private void OnWheelEvent(WheelEvent evt)
        {
            if (evt.delta.y != 0f)
            {
                evt.StopImmediatePropagation();

                if (evt.delta.y > 0)
                {
                    zoomProperty.floatValue = Mathf.Max(zoomProperty.floatValue - ZoomStep, minZoom);
                }
                else if (evt.delta.y < 0)
                {
                    zoomProperty.floatValue = Mathf.Min(zoomProperty.floatValue + ZoomStep, maxZoom);
                }

                Vector2 prevPanelSize = new Vector2(
                    textureContainer.style.width.value.value,
                    textureContainer.style.height.value.value);

                // Update panel size.
                UpdateTexturePanelSize();
                Vector2 newTexturePanelSize = new Vector2(
                    coloringBook.Texture.width * zoomProperty.floatValue,
                    coloringBook.Texture.height * zoomProperty.floatValue);

                Vector2 localMPoint =
                    (scrollView.ChangeCoordinatesTo(textureContainer, evt.localMousePosition) - (prevPanelSize * .5f)) / (prevPanelSize * .5f);

                Vector2 sizeDiff = newTexturePanelSize - prevPanelSize;
                if (sizeDiff != Vector2.zero)
                {
                    Vector2 change = -localMPoint * sizeDiff * .5f;

                    Vector2 currentOffset = new Vector2(
                        textureContainer.style.translate.value.x.value,
                        textureContainer.style.translate.value.y.value);

                    Vector2 position = new Vector2(
                        Mathf.Clamp(currentOffset.x + change.x, -newTexturePanelSize.x * .5f, newTexturePanelSize.x * .5f),
                        Mathf.Clamp(currentOffset.y + change.y, -newTexturePanelSize.y * .5f, newTexturePanelSize.y * .5f));
                    MoveTetxureContainer(position);
                }
            }
        }

        private void MoveTetxureContainer(Vector2 position)
        {
            serializedObject.FindProperty("panelPosition").vector2Value = position;
            textureContainer.style.translate = new StyleTranslate(new Translate(position.x, position.y));

            serializedObject.ApplyModifiedProperties();
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            switch (evt.button)
            {
                case 0:
                    // Pick location.
                    Vector2 location = new Vector2(
                        evt.localMousePosition.x / textureContainer.resolvedStyle.width,
                        (textureContainer.resolvedStyle.height - evt.localMousePosition.y) /
                            textureContainer.resolvedStyle.height);

                    int2 pixelLocation = new int2(
                        Mathf.FloorToInt(location.x * coloringBook.Texture.width),
                        Mathf.FloorToInt(location.y * coloringBook.Texture.height)
                    );

                    bool addRegion = state == States.WaitingForLocationPick || evt.shiftKey;
                    if (addRegion)
                    {
                        Debug.Log($"Getting region at pixel location {pixelLocation}.");
                        RegionData newRegion = RegionFromPixelLocation(pixelLocation);

                        // Add region to serialized object.
                        regionsProperty.InsertArrayElementAtIndex(regionsProperty.arraySize);
                        SerializedProperty emptyRegionProperty = regionsProperty.GetArrayElementAtIndex(regionsProperty.arraySize - 1);
                        emptyRegionProperty.boxedValue = newRegion;
                        serializedObject.ApplyModifiedProperties();

                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();

                        UpdateColors();
                        UpdateRegionsView();
                    }
                    else
                    {
                        // Find region at pixel location.
                        List<IRegionData> regions = coloringBook.Regions.ToList();
                        foreach (RegionData regionData in regions)
                        {
                            // Skip all that are outside of region rect.
                            if (pixelLocation.x < regionData.Location.x ||
                                pixelLocation.x > regionData.Location.x + regionData.TextureSize.x ||
                                pixelLocation.y < regionData.Location.y ||
                                pixelLocation.y > regionData.Location.y + regionData.TextureSize.y)
                            {
                                continue;
                            }

                            // Check region pixel.
                            int2 regionPixelLocation = pixelLocation - regionData.Location;
                            if (!ColoringBookTools.TextureHavePixelAt(regionData.Texture, regionPixelLocation))
                            {
                                continue;
                            }

                            int regionIndex = regions.IndexOf(regionData);
                            bool selected = regionsList.selectedIndices.Contains(regionIndex);

                            if (selected)
                            {
                                regionsProperty
                                    .GetArrayElementAtIndex(regionIndex)
                                    .FindPropertyRelative("colorIndexLocation")
                                    .boxedValue = pixelLocation;
                                serializedObject.ApplyModifiedProperties();
                            }
                            else
                            {
                                // Check control button.
                                if (evt.ctrlKey)
                                {
                                    regionsList.AddToSelection(regionIndex);
                                }
                                else
                                {
                                    regionsList.ClearSelection();
                                    regionsList.AddToSelection(regionIndex);
                                }
                            }

                            UpdateRegionsView();
                            break;
                        }
                    }

                    break;
            }
        }

        private RegionData RegionFromPixelLocation(int2 pixelLocation)
        {
            ExtractRegionJob.Result extractRegionJobResult =
                ExtractRegionFromTexture(coloringBook.Texture, errorSlider.value, pixelLocation);

            // Get current path.
            string path = AssetDatabase.GetAssetPath(coloringBook);
            string filename = Guid.NewGuid().ToString();
            string pathFixed = Path.GetDirectoryName(path);
            // Save texture.
            File.WriteAllBytes(Path.Combine(pathFixed, $"{filename}-texture.png"), extractRegionJobResult.texture.EncodeToPNG());
            // Refresh assets database.
            AssetDatabase.Refresh();

            // Update new region references.
            RegionData newRegion = new RegionData()
            {
                active = true,
            };
            newRegion.SetTexture(AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(pathFixed, $"{filename}-texture.png")));
            newRegion.SetMainColor(ColoringBookTools.FindSimilar(extractRegionJobResult.mainColor, colors));
            newRegion.SetLocation(new int2(
                extractRegionJobResult.textureLocation.x,
                extractRegionJobResult.textureLocation.y));
            newRegion.SetTextureSize(new int2(newRegion.Texture.width, newRegion.Texture.height));
            newRegion.SetColorIndexLocation(pixelLocation);
            newRegion.SetPixelCount(extractRegionJobResult.pixelCount);

            return newRegion;
        }

        private void RegionSelectionChanged(IEnumerable<object> obj)
        {
            singleRegionContainer.SetEnabled(obj.Count() == 1);
            anyRegionContainer.SetEnabled(obj.Count() > 0);
            multiRegionsContainer.SetEnabled(obj.Count() > 1);

            UpdateRegionsView();
        }

        private void ColorsSelectionChanged(IEnumerable<object> obj)
        {
            regionsList.ClearSelection();

            foreach (int index in colorsList.selectedIndices)
            {
                List<IRegionData> regions = coloringBook.Regions.ToList();
                for (int regionIndex = 0; regionIndex < regions.Count; regionIndex++)
                {
                    if (regions[regionIndex].MainColor.Compare(colors[index]))
                    {
                        regionsList.AddToSelection(regionIndex);
                    }
                }
            }
        }

        private void OnSizeChanged(GeometryChangedEvent evt)
        {
            UpdateZoomValues();
            UpdateTexturePanelSize();
        }

        private void OnTextureValueChanged(ChangeEvent<UnityEngine.Object> evt)
        {
            Texture2D newTexture = evt.newValue as Texture2D;
            textureContainer.style.backgroundImage = newTexture;

            if (newTexture)
            {
                UpdateZoomValues();
                UpdateTexturePanelSize();
            }
        }

        private void UpdateZoomValues()
        {
            if (coloringBook.Texture)
            {
                minZoom = scrollView.resolvedStyle.width / coloringBook.Texture.width;
            }
            else
            {
                minZoom = 1f;
            }

            float prevMaxZoom = maxZoom;
            maxZoom = minZoom * MaxZoomMltp;

            if (prevMaxZoom != maxZoom)
            {
                Vector2 panelPosition = serializedObject.FindProperty("panelPosition").vector2Value;
                textureContainer.style.translate = new StyleTranslate(new Translate(panelPosition.x, panelPosition.y));
            }
        }

        private void UpdateRegionsView()
        {
            // Remove previous.
            textureContainer.Clear();

            List<IRegionData> regions = coloringBook.Regions.ToList();
            IEnumerable<IRegionData> selected = regionsList.selectedIndices.Select(index => regions[index]);
            // Add new ones.
            foreach (RegionData regionData in coloringBook.Regions)
            {
                VisualElement visualElement = new VisualElement();
                bool regionSelected = selected.Contains(regionData);

                Color regionColor = regionSelected ? Color.green : Color.white;
                Color tintColor = regionData.active ? regionColor : new Color(0f, 0f, 0f, .6f);

                visualElement.style.backgroundImage = regionData.Texture;
                visualElement.style.unityBackgroundImageTintColor = tintColor;
                visualElement.style.width = new StyleLength(
                    new Length((float)regionData.TextureSize.x / coloringBook.Texture.width * 100f, LengthUnit.Percent));
                visualElement.style.height = new StyleLength(
                    new Length((float)regionData.TextureSize.y / coloringBook.Texture.height * 100f, LengthUnit.Percent));
                visualElement.style.position = new StyleEnum<Position>(Position.Absolute);
                visualElement.style.left = new StyleLength(
                    new Length((float)regionData.Location.x / coloringBook.Texture.width * 100f, LengthUnit.Percent));
                visualElement.style.bottom = new StyleLength(
                    new Length((float)regionData.Location.y / coloringBook.Texture.height * 100f, LengthUnit.Percent));
                visualElement.userData = regionData;

                textureContainer.Add(visualElement);
                
                if (!regionSelected)
                {
                    visualElement.SendToBack();
                }
            }

            // Index locations added separately.
            foreach (RegionData regionData in coloringBook.Regions)
            {
                VisualElement regionIndexUGUI = regionIndexLocationAsset.CloneTree();
                VisualElement container = regionIndexUGUI.Q<VisualElement>("container");
                container.style.backgroundColor = regionData.MainColor;
                Label label = regionIndexUGUI.Q<Label>();
                label.text = $"{GetRegionColorIndex(regionData)}";
                regionIndexUGUI.style.position = new StyleEnum<Position>(Position.Absolute);
                regionIndexUGUI.style.left = new StyleLength(new Length(
                    (float)(regionData.ColorIndexLocation.x) / coloringBook.Texture.width * 100f, 
                    LengthUnit.Percent));
                regionIndexUGUI.style.bottom = new StyleLength(new Length(
                    (float)(regionData.ColorIndexLocation.y) / coloringBook.Texture.height * 100f, 
                    LengthUnit.Percent));

                textureContainer.Add(regionIndexUGUI);
            }

            int GetRegionColorIndex(RegionData regionData)
            {
                int index = -1;
                // Find index.
                for (int colorIndex = 0; colorIndex < colors.Count; colorIndex++)
                {
                    if (colors[colorIndex].Compare(regionData.MainColor))
                    {
                        index = colorIndex;
                        break;
                    }
                }

                return index;
            }
        }

        private void UpdateColors()
        {
            Debug.Log("ColorUpdating");

            colors = new List<Color>(coloringBook.Colors);
            colorsList.ClearSelection();
            colorsList.itemsSource = colors;
            colorsList.bindItem += (item, index) => 
            { 
                (item as ColorField).value = colors[index];
            };
        }

        private void UpdateTexturePanelSize()
        {
            if (!coloringBook.Texture)
            {
                return;
            }

            textureContainer.style.width = new StyleLength(coloringBook.Texture.width * zoomProperty.floatValue);
            textureContainer.style.height = new StyleLength(coloringBook.Texture.height * zoomProperty.floatValue);
        }

        private void RemoveRegionAtIndex(int index)
        {
            if (removeTextureToggle.value)
            {
                // Delete attached texture.
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(((RegionData)coloringBook.Regions.ElementAt(index)).Texture));
                AssetDatabase.Refresh();
            }

            regionsProperty.DeleteArrayElementAtIndex(index);
        }

#region Region Operations

        private ExtractRegionJob.Result ExtractRegionFromTexture(
            Texture2D inputTexture,
            int errorSize,
            int2 targetPixelLocation)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            // Create texture copy.
            Texture2D copy = ColoringBookTools.GetTextureCopy(inputTexture);
            ExtractRegionJob extractRegionJob = new ExtractRegionJob()
            {
                input = copy.GetPixelData<Color>(0),
                targetLocation = new NativeReference<int2>(targetPixelLocation, Allocator.TempJob),
                inputTextureSize = new NativeReference<int2>(new int2(inputTexture.width, inputTexture.height), Allocator.TempJob),
                errorSize = new NativeReference<int>(errorSize, Allocator.TempJob),
            };
            extractRegionJob.Init();
            jobHandle = extractRegionJob.Schedule();
            jobHandle.Complete();

            ExtractRegionJob.Result extractResults = extractRegionJob.GetJobResult();
            extractRegionJob.Dispose();

            // Clear texture copy.
            DestroyImmediate(copy);

            Debug.Log($"Region extract time: {sw.ElapsedMilliseconds}ms");
            sw.Stop();

            return extractResults;
        }

        private GrowRegionJob.Result GrowTexture(
            Texture2D inputTexture,
            int top = 1,
            int right = 1,
            int bottom = 1,
            int left = 1)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            // Create texture copy.
            Texture2D copy = ColoringBookTools.GetTextureCopy(inputTexture);

            GrowRegionJob growRegionJob = new GrowRegionJob()
            {
                input = copy.GetPixelData<Color>(0),
                textureSize = new NativeReference<int2>(new int2(copy.width, copy.height), Allocator.TempJob),
                extentMin = new NativeReference<int2>(new int2(left, bottom), Allocator.TempJob),
                extentMax = new NativeReference<int2>(new int2(right, top), Allocator.TempJob),
            };
            growRegionJob.Init();
            jobHandle = growRegionJob.Schedule();
            jobHandle.Complete();

            GrowRegionJob.Result growRegionJobResult = growRegionJob.GetResults();
            growRegionJob.Dispose();

            // Clear texture copy.
            DestroyImmediate(copy);

            Debug.Log($"Texture grow time: {sw.ElapsedMilliseconds}ms");
            sw.Stop();

            return growRegionJobResult;
        }

        private RegionData MergeRegions(params RegionData[] regions)
        {
            if (regions.Length == 0)
            {
                return null;
            }

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            RegionData result = new RegionData();
            int2 min = new int2(int.MaxValue, int.MaxValue);
            int2 max = new int2(int.MinValue, int.MinValue);

            // Find min/max.
            foreach (RegionData r in regions)
            {
                min = new int2(Mathf.Min(r.Location.x, min.x), Mathf.Min(r.Location.y, min.y));
                max = new int2(Mathf.Max(r.Location.x + r.TextureSize.x, max.x), Mathf.Max(r.Location.y + r.TextureSize.y, max.y));
            }
            RenderTexture prev = RenderTexture.active;

            Texture2D mergedTexture = new Texture2D(
                max.x - min.x,
                max.y - min.y,
                TextureFormat.RGBA32,
                false);
            RenderTexture renderTex = RenderTexture.GetTemporary(
                mergedTexture.width,
                mergedTexture.height,
                0,
                RenderTextureFormat.ARGB32);

            // Render each region onto render texture.
            CommandBuffer commandBuffer = new CommandBuffer();
            commandBuffer.SetViewMatrix(Matrix4x4.TRS(new Vector3(0f, 0f, -1f), Quaternion.identity, Vector3.one));
            commandBuffer.SetProjectionMatrix(Matrix4x4.Ortho(
                min.x,
                max.x,
                min.y,
                max.y,
                .1f,
                2f));
            commandBuffer.SetRenderTarget(renderTex);
            Matrix4x4 meshMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);

            Mesh mesh = new Mesh();
            Material material = new Material(Shader.Find("UI/Unlit/Transparent"));
            foreach (RegionData r in regions)
            {
                mesh.vertices = new Vector3[]
                {
                    new Vector3(r.Location.x, r.Location.y),
                    new Vector3(r.Location.x, r.Location.y + r.TextureSize.y),
                    new Vector3(r.Location.x + r.TextureSize.x, r.Location.y+ r.TextureSize.y),
                    new Vector3(r.Location.x + r.TextureSize.x, r.Location.y),
                };
                mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
                mesh.uv = new Vector2[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(0, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(1, 0f),
                };
                material.mainTexture = r.Texture;

                commandBuffer.DrawMesh(mesh, meshMatrix, material, 0);
                Graphics.ExecuteCommandBuffer(commandBuffer);
            }
            DestroyImmediate(mesh);
            DestroyImmediate(material);

            // Read from render texture into texture2D.
            RenderTexture.active = renderTex;
            mergedTexture.ReadPixels(new Rect(0, 0, mergedTexture.width, mergedTexture.height), 0, 0);
            RenderTexture.active = prev;
            mergedTexture.Apply();
            RenderTexture.ReleaseTemporary(renderTex);

            // Save texture.
            string originalTexturePath = AssetDatabase.GetAssetPath(regions[0].Texture);
            string fullTexturePath = Path.Combine(
                Path.GetDirectoryName(originalTexturePath),
                $"{System.Guid.NewGuid()}-texture.png");
            File.WriteAllBytes(fullTexturePath, mergedTexture.EncodeToPNG());
            AssetDatabase.Refresh();

            // Update result region data.
            result.SetTexture(AssetDatabase.LoadAssetAtPath<Texture2D>(fullTexturePath));
            result.SetTextureSize(new int2(mergedTexture.width, mergedTexture.height));
            result.SetLocation(min);
            result.SetMainColor(regions[0].MainColor);
            result.SetPixelCount(regions.Sum(data => data.PixelCount));

            DestroyImmediate(mergedTexture);

            Debug.Log($"Regions merge time: {sw.ElapsedMilliseconds}ms");
            sw.Stop();

            return result;
        }

        private Texture2D AddRegionToTexture(Texture2D input, RegionData region)
        {
            Texture2D mergedTexture = new Texture2D(
                input.width,
                input.height,
                TextureFormat.RGBAFloat,
                false);
            RenderTexture renderTex = RenderTexture.GetTemporary(
                mergedTexture.width,
                mergedTexture.height,
                0,
                RenderTextureFormat.ARGBFloat);
            Material material = new Material(Shader.Find("UI/Unlit/Transparent"));
            Graphics.Blit(input, renderTex, material);

            // Render each region onto render texture.
            CommandBuffer commandBuffer = new CommandBuffer();
            commandBuffer.SetViewMatrix(Matrix4x4.TRS(new Vector3(0f, 0f, -1f), Quaternion.identity, Vector3.one));
            commandBuffer.SetProjectionMatrix(Matrix4x4.Ortho(0f, input.width, 0f, input.height, .1f, 2f));
            commandBuffer.SetRenderTarget(renderTex);
            Matrix4x4 meshMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);

            Mesh mesh = new Mesh();
            mesh.vertices = new Vector3[]
            {
                new Vector3(region.Location.x, region.Location.y),
                new Vector3(region.Location.x, region.Location.y + region.TextureSize.y),
                new Vector3(region.Location.x + region.TextureSize.x, region.Location.y+ region.TextureSize.y),
                new Vector3(region.Location.x + region.TextureSize.x, region.Location.y),
            };
            mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
            mesh.uv = new Vector2[]
            {
                new Vector2(0f, 0f),
                new Vector2(0, 1f),
                new Vector2(1f, 1f),
                new Vector2(1, 0f),
            };
            material.mainTexture = region.Texture;

            commandBuffer.DrawMesh(mesh, meshMatrix, material, 0);
            Graphics.ExecuteCommandBuffer(commandBuffer);

            DestroyImmediate(mesh);
            DestroyImmediate(material);

            // Read from render texture into texture2D.
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = renderTex;
            mergedTexture.ReadPixels(new Rect(0, 0, mergedTexture.width, mergedTexture.height), 0, 0);
            RenderTexture.active = prev;
            mergedTexture.Apply();
            RenderTexture.ReleaseTemporary(renderTex);

            return mergedTexture;
        }

#endregion

#region Buttons callbacks.

        private void AddRegionButtonClicked()
        {
            switch (state)
            {
                case States.WaitingForLocationPick:
                    state = States.None;
                    addRegionButton.style.backgroundColor = Color.clear;

                    break;

                case States.None:
                    state = States.WaitingForLocationPick;
                    addRegionButton.style.backgroundColor = Color.green;

                    break;
            }
        }

        private void AddRegionsOfColorButtonClicked()
        {
            Color selectedColor = regionsOfColor.value;

            if (selectedColor == Color.white || selectedColor == Color.black)
            {
                Debug.LogWarning("Selected color cannot be white or black.");

                return;
            }

            Texture2D current = ColoringBookTools.GetTextureCopy(coloringBook.Texture);

            for (int x = 0; x < current.width; x++)
            {
                for (int y = 0; y < current.height; y++)
                {
                    if (current.GetPixel(x, y).Compare(selectedColor))
                    {
                        RegionData newRegion = RegionFromPixelLocation(new int2(x, y));

                        regionsProperty.InsertArrayElementAtIndex(regionsProperty.arraySize);
                        SerializedProperty emptyRegionProperty = regionsProperty.GetArrayElementAtIndex(regionsProperty.arraySize - 1);
                        emptyRegionProperty.boxedValue = newRegion;

                        serializedObject.ApplyModifiedPropertiesWithoutUndo();
                        UpdateColors();

                        // Add region texture to current texture so it will be skipped later on.
                        Texture2D currentUpdated = AddRegionToTexture(current, newRegion);
                        DestroyImmediate(current);
                        current = currentUpdated;
                    }
                }
            }

            DestroyImmediate(current);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            UpdateRegionsView();
        }

        private void GrowButtonClicked()
        {
            foreach (int selected in regionsList.selectedIndices)
            {
                RegionData region = coloringBook.Regions.ElementAt(selected) as RegionData;

                // Find to what values texture can be extended.
                int top = region.Location.y + region.TextureSize.y < coloringBook.Texture.height ? 1 : 0;
                int bottom = region.Location.y > 0 ? 1 : 0;
                int right = region.Location.x + region.TextureSize.x < coloringBook.Texture.width ? 1 : 0;
                int left = region.Location.x > 0 ? 1 : 0;
                GrowRegionJob.Result growRegionJob = GrowTexture(region.Texture, top, right, bottom, left);

                // Delete previous texture.
                string path = AssetDatabase.GetAssetPath(region.Texture);
                AssetDatabase.DeleteAsset(path);

                // Save new texture.
                File.WriteAllBytes(path, growRegionJob.texture.EncodeToPNG());
                // Refresh assets database.
                AssetDatabase.Refresh();

                region.SetTexture(AssetDatabase.LoadAssetAtPath<Texture2D>(path));
                // Update location and texture soze
                region.SetLocation(region.Location - growRegionJob.extentMin);
                region.SetTextureSize(
                    new int2(growRegionJob.texture.width, growRegionJob.texture.height));
            }

            serializedObject.ApplyModifiedProperties();

            UpdateRegionsView();
        }

        private void DeleteButtonClicked()
        {
            if (regionsList.selectedIndices.Count() > 0)
            {
                bool clearSelected = false;
                foreach (int selected in regionsList.selectedIndices.OrderByDescending(value => value))
                {
                    // Disable control panel if last element was removed.
                    if (selected == coloringBook.Regions.Count() - 1)
                    {
                        singleRegionContainer.SetEnabled(false);
                        anyRegionContainer.SetEnabled(false);
                        multiRegionsContainer.SetEnabled(false);

                        clearSelected = true;
                    }

                    RemoveRegionAtIndex(selected);
                }
                serializedObject.ApplyModifiedPropertiesWithoutUndo();

                if (clearSelected)
                {
                    regionsList.ClearSelection();
                }

                UpdateColors();
                UpdateRegionsView();
            }
        }

        private void ShowButtonClicked()
        {
            List<IRegionData> regions = coloringBook.Regions.ToList();
            foreach (RegionData region in regionsList.selectedIndices.Select(index => regions[index]))
            {
                region.active = true;
            }

            UpdateRegionsView();
        }

        private void HideButtonClicked()
        {
            List<IRegionData> regions = coloringBook.Regions.ToList();
            foreach (RegionData region in regionsList.selectedIndices.Select(index => regions[index]))
            {
                region.active = false;
            }

            UpdateRegionsView();
        }

        private void MergeButtonClicked()
        {
            List<IRegionData> regions = coloringBook.Regions.ToList();
            IEnumerable<RegionData> selected = regionsList.selectedIndices.Select(index => regions[index] as RegionData);

            RegionData merged = MergeRegions(selected.ToArray());
            merged.active = true;

            // Add merged to serialized object.
            regionsProperty.InsertArrayElementAtIndex(regionsProperty.arraySize);
            SerializedProperty emptyRegionProperty = regionsProperty.GetArrayElementAtIndex(regionsProperty.arraySize - 1);
            emptyRegionProperty.boxedValue = merged;

            // Remove merged if checked.
            if (removeMergeTargets.value)
            {
                foreach (int index in regionsList.selectedIndices.OrderByDescending(value => value))
                {
                    RemoveRegionAtIndex(index);
                }
            }
            serializedObject.ApplyModifiedProperties();

            regionsList.ClearSelection();
            colorsList.ClearSelection();

            UpdateColors();
            UpdateRegionsView();
        }

        private void UpdateColorsButtonClicked()
        {
            foreach (int selectedIndex in regionsList.selectedIndices)
            {
                SerializedProperty regionProperty = regionsProperty.GetArrayElementAtIndex(selectedIndex);
                regionProperty.FindPropertyRelative("mainColor").colorValue = customColorField.value;
            }
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();

            UpdateColors();
            UpdateRegionsView();
        }

#endregion

        private enum States
        {
            None = 0,
            WaitingForLocationPick = 1,
        }
    }
}