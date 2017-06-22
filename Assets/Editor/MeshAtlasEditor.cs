using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MeshAtlasEditor : EditorWindow
{
    MeshRenderer renderer;

    enum PreferredSizes { Smallest, Biggest };
    PreferredSizes preferredSize;

    private enum TextureSizes { _16 = 16, _32 = 32, _64 = 64, _128 = 128, _256 = 256, _512 = 512, _1024 = 1024, _2048 = 2048, _4096 = 4096, _8192 = 8192 };
    private int resultSize = 512;

    string saveName;

    bool debugDetails;

    MeshAtlasData meshdata;

    Color positiveColor = new Color(0f, 1f, 0f);
    Color negativeColor = new Color(1f, 0f, 0f);
    Color highlightColor = new Color(0.75f, 0.75f, 0f);

    string status = "";
    Vector2 statusScroll = Vector2.zero;

    Vector2 debugScroll = Vector2.zero;
    float debugHeight = 200f;


    RenderTexture resultTexture, previewTexture;

    float process = 0f;

    IEnumerator routine;

    [MenuItem("Window/Texture Mesh Atlas Creator")]
    static void Init()
    {
        MeshAtlasEditor window = (MeshAtlasEditor)EditorWindow.GetWindow(typeof(MeshAtlasEditor));
        window.Show();
    }

    void OnEnable()
    {
        this.titleContent = new GUIContent("Atlas Mesh Creator", "Texture Atlas Mesh Generator");
        this.minSize = new Vector2(1024f, 512f);
    }

    void OnGUI()
    {
        SettingsArea(new Rect(0f, 0f, this.position.width, 64f));
        EditorGUI.ProgressBar(new Rect(0f, 64f, this.position.width, 16f), process / 100f, "Process " + (int)Mathf.Round(process) + "%");
        DebugArea(new Rect(0f, 80f, this.position.width, this.position.height - 80f));
    }

    void SettingsArea(Rect pos)
    {
        Vector2 lCenter = new Vector2(pos.width * 0.75f, pos.height * 0.5f);
        float lLastY = 4f;
        GUILayout.BeginArea(pos, EditorStyles.helpBox);

        renderer = (MeshRenderer)EditorGUI.ObjectField(new Rect(4f, lLastY, lCenter.x - 8f, 16f), new GUIContent("Mesh Renderer"), renderer, typeof(MeshRenderer), true);
        lLastY += 20f;

        preferredSize = (PreferredSizes)EditorGUI.EnumPopup(new Rect(4f, lLastY, (lCenter.x / 2f) - 4f, 16f), new GUIContent("Preferred Size"), preferredSize);
        resultSize = (int)(TextureSizes)EditorGUI.EnumPopup(new Rect((lCenter.x / 2f) + 4f, lLastY, (lCenter.x / 2f) - 8f, 16f), new GUIContent("Result Size"), (TextureSizes)resultSize);
        lLastY += 20f;

        saveName = EditorGUI.TextField(new Rect(4f, lLastY, lCenter.x - 8f, 16f), new GUIContent("Save Name"), saveName);
        lLastY += 20f;

        lLastY = 4f;
        Color lOldColor = GUI.backgroundColor;
        GUI.backgroundColor = debugDetails ? positiveColor : negativeColor;
        if (GUI.Button(new Rect(lCenter.x + 4f, lLastY, (pos.width - lCenter.x) - 8f, 16f), new GUIContent("Show Details")))
        {
            debugDetails = !debugDetails;
        }
        lLastY += 20f;
        
        GUI.backgroundColor = highlightColor;
        if (GUI.Button(new Rect(lCenter.x + 4f, lLastY, (pos.width - lCenter.x) - 8f, 36f), new GUIContent("Update"))) {
            UpdateMesh();
        }
        GUI.backgroundColor = lOldColor;

        GUILayout.EndArea();
    }

    void DebugArea(Rect pos)
    {
        Vector2 lCenter = new Vector2(pos.width * 0.5f, pos.height * 0.5f);
        GUILayout.BeginArea(pos, EditorStyles.helpBox);

        debugScroll = GUI.BeginScrollView(new Rect(0f, 0f, lCenter.x - 4f, pos.height), debugScroll, new Rect(0f, 0f, lCenter.x - 20f, debugHeight), false, true);
        debugHeight = 4f;
        if (meshdata.textures != null)
        {
            GUI.Label(new Rect(4f, debugHeight, lCenter.x - 24f, 16f), new GUIContent("Textures"), EditorStyles.boldLabel);
            debugHeight += 20f;
            for (int m = 0; m < meshdata.materials.Length; m++)
            {
                EditorGUI.DrawRect(new Rect(6f + 72f * m, debugHeight, 70f, 16f), new Color(0f, 0f, 0f, 0.75f));
                GUI.Label(new Rect(4f + 72f * m, debugHeight, 72f, 16f), new GUIContent("Mat " + m), EditorStyles.centeredGreyMiniLabel);
            }
            debugHeight += 24f;
            for (int p = 0; p < meshdata.setProperties.Count; p++)
            {
                EditorGUI.DrawRect(new Rect(4f, debugHeight, meshdata.materials.Length * 72f, 84f), new Color(0f, 0f, 0f, 0.75f));
                GUI.Label(new Rect(8f, debugHeight, meshdata.materials.Length * 72f, 16f), new GUIContent(meshdata.setProperties[p]));
                debugHeight += 16f;
                for (int m = 0; m < meshdata.materials.Length; m++)
                {
                    Texture lTexture = meshdata.textures[m][meshdata.setProperties[p]];
                    if (lTexture != null)
                    {
                        EditorGUI.DrawPreviewTexture(new Rect(8f + m * 72f, debugHeight, 64f, 64f), lTexture);
                    } else
                    {
                        EditorGUI.DrawRect(new Rect(8f + m * 72f, debugHeight, 64f, 64f), new Color(0f, 0f, 0f, 0.75f));
                    }
                }
                debugHeight += 72f;
            }
            debugHeight += 8f;
        }
        if (meshdata.observeTextures != null)
        {
            GUI.Label(new Rect(4f, debugHeight, lCenter.x - 24f, 16f), new GUIContent("Observed Textures"), EditorStyles.boldLabel);
            debugHeight += 20f;
            for (int m = 0; m < meshdata.materials.Length; m++)
            {
                EditorGUI.DrawRect(new Rect(6f + 72f * m, debugHeight, 70f, 16f), new Color(0f, 0f, 0f, 0.75f));
                GUI.Label(new Rect(4f + 72f * m, debugHeight, 72f, 16f), new GUIContent("Mat " + m), EditorStyles.centeredGreyMiniLabel);
            }
            debugHeight += 24f;
            for (int m = 0; m < meshdata.materials.Length; m++)
            {
                for (int p = 0; p < meshdata.nicFilledProperties[m].Count; p++)
                {
                    EditorGUI.DrawRect(new Rect(4f, debugHeight, meshdata.materials.Length * 72f, 84f), new Color(0f, 0f, 0f, 0.75f));
                    GUI.Label(new Rect(8f, debugHeight, meshdata.materials.Length * 72f, 16f), new GUIContent(meshdata.nicFilledProperties[m][p]));
                    debugHeight += 16f;
                    {
                        Texture lTexture = meshdata.observeTextures[m][meshdata.nicFilledProperties[m][p]];
                        if (lTexture != null)
                        {
                            EditorGUI.DrawPreviewTexture(new Rect(8f + m * 72f, debugHeight, 64f, 64f), lTexture);
                        }
                        else
                        {
                            EditorGUI.DrawRect(new Rect(8f + m * 72f, debugHeight, 64f, 64f), new Color(0f, 0f, 0f, 0.75f));
                        }
                    }
                    debugHeight += 72f;
                }
            }
            debugHeight += 8f;
        }
        if (meshdata.texSize != null)
        {
            GUI.Label(new Rect(4f, debugHeight, lCenter.x - 24f, 16f), new GUIContent("Processed Sizes"), EditorStyles.boldLabel);
            debugHeight += 20f;
            for (int m = 0; m < meshdata.materials.Length; m++)
            {
                EditorGUI.DrawRect(new Rect(6f + 72f * m, debugHeight, 70f, 16f), new Color(0f, 0f, 0f, 0.75f));
                GUI.Label(new Rect(4f + 72f * m, debugHeight, 72f, 16f), new GUIContent(meshdata.texSize[m] + "x" + meshdata.texSize[m]), EditorStyles.centeredGreyMiniLabel);
            }
            debugHeight += 32f;
        }
        if (meshdata.textureAtlas != null)
        {
            GUI.Label(new Rect(4f, debugHeight, lCenter.x - 24f, 16f), new GUIContent("Atlas Textures"), EditorStyles.boldLabel);
            debugHeight += 20f;
            int left = 0;
            foreach (KeyValuePair<string, Texture> pair in meshdata.textureAtlas)
            {
                EditorGUI.DrawRect(new Rect(6f + 80f * left, debugHeight, 70f, 16f), new Color(0f, 0f, 0f, 0.75f));
                GUI.Label(new Rect(4f + 80f * left, debugHeight, 72f, 16f), new GUIContent(pair.Key), EditorStyles.centeredGreyMiniLabel);

                EditorGUI.DrawRect(new Rect(4f + left * 80f, debugHeight + 24f, 72f, 72f), new Color(0f, 0f, 0f, 0.75f));
                EditorGUI.DrawPreviewTexture(new Rect(8f + left * 80f, debugHeight + 28f, 64f, 64f), pair.Value);
                left++;
            }
            debugHeight += 104f;
        }
        if (meshdata.observedAtlas != null)
        {
            GUI.Label(new Rect(4f, debugHeight, lCenter.x - 24f, 16f), new GUIContent("Observed Atlas Textures"), EditorStyles.boldLabel);
            debugHeight += 20f;
            for (int m = 0; m < meshdata.materials.Length; m++)
            {
                if (meshdata.observedAtlas[m].Count < 1)
                {
                    continue;
                }
                GUI.Label(new Rect(4f, debugHeight, lCenter.x - 24f, 16f), new GUIContent("Material " + m));
                debugHeight += 20f;
                int left = 0;
                foreach (KeyValuePair<string, Texture> pair in meshdata.observedAtlas[m])
                {
                    EditorGUI.DrawRect(new Rect(6f + 80f * left, debugHeight, 70f, 16f), new Color(0f, 0f, 0f, 0.75f));
                    GUI.Label(new Rect(4f + 80f * left, debugHeight, 72f, 16f), new GUIContent(pair.Key), EditorStyles.centeredGreyMiniLabel);

                    EditorGUI.DrawRect(new Rect(4f + left * 80f, debugHeight + 24f, 72f, 72f), new Color(0f, 0f, 0f, 0.75f));
                    EditorGUI.DrawPreviewTexture(new Rect(8f + left * 80f, debugHeight + 28f, 64f, 64f), pair.Value);
                    left++;
                }
                debugHeight += 104f;
            }
            debugHeight += 8f;//104f;
        }

        GUI.EndScrollView();
        debugHeight = Mathf.Max(debugHeight, pos.height);

        statusScroll = GUI.BeginScrollView(new Rect(lCenter.x + 4f, 0f, (pos.width - lCenter.x) - 4f, pos.height), statusScroll, new Rect(0f, 0f, (pos.width - lCenter.x) - 20f, EditorStyles.label.CalcHeight(new GUIContent(status), pos.width - lCenter.x)), false, true);
        GUI.Label(new Rect(0f, 0f, (pos.width - lCenter.x) - 16f, EditorStyles.label.CalcHeight(new GUIContent(status), pos.x - lCenter.x)), status, EditorStyles.label);
        GUI.EndScrollView();

        GUILayout.EndArea();
    }

    private void Update()
    {
        if (routine != null)
        {
            if (!routine.MoveNext())
            {
                routine = null;
            }
            debugScroll = statusScroll = new Vector2(0f, float.MaxValue);
            Repaint();
        }
    }

    void AddStatus(string state)
    {
        status += state + "\n";
    }

    void Abandon(string information = "")
    {
        status += "\n\n" + "Error: " + information + "\n";
        status += "Abandon Generation!";
        routine = null;
    }

    void UpdateMesh()
    {
        //TODO: Update the meshdata
        routine = UpdateStepManager();
    }

    IEnumerator UpdateStepManager()
    {
        status = "";
        process = 0f;
        float oldProcess = 0f;

        AddStatus("");
        AddStatus("------------------------");
        AddStatus("Start Generating Process");
        AddStatus("------------------------");

        meshdata = new MeshAtlasData();

        AddStatus("");
        AddStatus("Collect Mesh-Data");
        AddStatus("------------------------");
        float collectedMeshData = 0f;
        while(collectedMeshData < 1f)
        {
            collectedMeshData = CollectMeshInformation();
            process = collectedMeshData * 5f;
            yield return this;
        }
        oldProcess = process;

        AddStatus("");
        AddStatus("Collect Material-Data");
        AddStatus("------------------------");
        float collectedMaterialData = 0f;
        while (collectedMaterialData < 1f)
        {
            collectedMaterialData = CollectMaterialInformation();
            process = oldProcess + collectedMaterialData * 10f;
            yield return this;
        }
        oldProcess = process;


        AddStatus("");
        AddStatus("Process Texture-Data");
        AddStatus("------------------------");
        float processedTextureData = 0f;
        while (processedTextureData < 1f)
        {
            processedTextureData = ProcessTextureData();
            process = oldProcess + processedTextureData * 35f;
            yield return this;
        }
        oldProcess = process;

        //process = 100f;

        yield return null;
    }

    float CollectMeshInformation()
    {
        float lSteps = 5f;
        if (!renderer)
        {
            Abandon("No Mesh Renderer set");
            return 0f;
        }
        if (!meshdata.filter)
        {
            meshdata.filter = renderer.GetComponent<MeshFilter>();
            AddStatus("Collecting Mesh Filter: " + meshdata.filter.name);
            return 1f / lSteps;
        }
        if (!meshdata.mesh)
        {
            meshdata.mesh = meshdata.filter.sharedMesh;
            AddStatus("Collecting Mesh: " + meshdata.mesh.name);
            return 2f / lSteps;
        }
        if (meshdata.subMeshCount == 0)
        {
            meshdata.subMeshCount = meshdata.mesh.subMeshCount;
            AddStatus("Collected Submeshes: " + meshdata.subMeshCount);
            return 3f / lSteps;
        }
        if (meshdata.subMeshCount < 2)
        {
            Abandon("Not enough Submeshes");
            return 4f / lSteps;
        }
        if (meshdata.triangles == null)
        {
            meshdata.triangles = new List<int>[meshdata.subMeshCount];
            for (int t = 0; t < meshdata.subMeshCount; t++)
            {
                meshdata.triangles[t] = new List<int>(meshdata.mesh.GetTriangles(t));
            }
            AddStatus("Collected UVs: " + meshdata.triangles.Length);
            return 5f / lSteps;
        }
        return 1f;
    }

    float CollectMaterialInformation()
    {
        float lSteps = 9f;

        if (meshdata.materials == null)
        {
            meshdata.materials = renderer.sharedMaterials;
            AddStatus("Collected Materials: " + meshdata.materials.Length);
            return 1f / lSteps;
        }
        if (meshdata.shaders == null)
        {
            meshdata.shaders = new List<Shader>();
            for (int m = 0; m < meshdata.materials.Length; m++)
            {
                if (!meshdata.shaders.Contains(meshdata.materials[m].shader))
                {
                    meshdata.shaders.Add(meshdata.materials[m].shader);
                }
            }
            AddStatus("Collected Shaders: " + meshdata.shaders.Count);
            return 2f / lSteps;
        }
        if (meshdata.allProperties == null)
        {
            int propertiesFound = 0;
            meshdata.allProperties = new List<string>[meshdata.shaders.Count];
            for (int s = 0; s < meshdata.shaders.Count; s++)
            {
                meshdata.allProperties[s] = new List<string>();
                Shader lShader = meshdata.shaders[s];
                for (int p = 0; p < ShaderUtil.GetPropertyCount(lShader); p++)
                {
                    if (ShaderUtil.GetPropertyType(lShader, p) == ShaderUtil.ShaderPropertyType.TexEnv)
                    {
                        meshdata.allProperties[s].Add(ShaderUtil.GetPropertyName(lShader, p));
                        propertiesFound++;
                    }
                }
            }
            AddStatus("Collected Texture Properties: " + propertiesFound);
            for (int s = 0; s < meshdata.shaders.Count; s++)
            {
                AddStatus("  ->   Shader " + (s + 1) + " Texture Properties: " + meshdata.allProperties[s].Count);
            }
            return 3f / lSteps;
        }
        if (!meshdata.checkedRenderTexture)
        {
            for (int m = 0; m < meshdata.materials.Length; m++)
            {
                Shader lShader = meshdata.materials[m].shader;
                for (int s = 0; s < meshdata.shaders.Count; s++)
                {
                    if (lShader == meshdata.shaders[s])
                    {
                        for (int p = 0; p < meshdata.allProperties[s].Count; p++)
                        {
                            Texture lTexture = meshdata.materials[m].GetTexture(meshdata.allProperties[s][p]);
                            if (lTexture != null && lTexture.GetType() == typeof(RenderTexture))
                            {
                                Abandon("RenderTexture found\nRenderTextures are not supported in a Texture Atlas\n  ->   " + meshdata.materials[m].name + "/" + meshdata.allProperties[s][p]);
                                return 3f / lSteps;
                            }
                        }
                    }
                }
            }
            meshdata.checkedRenderTexture = true;
            return 4f / lSteps;
        }
        if (meshdata.properties == null)
        {
            if (meshdata.allProperties.Length == 1)
            {
                meshdata.properties = meshdata.allProperties[0];
                AddStatus("Only one Shader -> Skip Compare Step");
                return 5f / lSteps;
            }

            meshdata.properties = new List<string>();
            for (int p = 0; p < meshdata.allProperties[0].Count; p++)
            {
                bool lFoundInAll = true;
                for (int s = 1; s < meshdata.allProperties.Length; s++)
                {
                    if (!meshdata.allProperties[s].Contains(meshdata.allProperties[0][p]))
                    {
                        lFoundInAll = false;
                    }
                }
                if (lFoundInAll)
                {
                    meshdata.properties.Add(meshdata.allProperties[0][p]);
                }
            }
            AddStatus("Texture Properties in common: " + meshdata.properties.Count);
            return 5f / lSteps;
        }
        if (meshdata.setProperties == null)
        {
            meshdata.setProperties = new List<string>();
            for (int p = 0; p < meshdata.properties.Count; p++)
            {
                bool lIsSet = false;
                for (int m = 0; m < meshdata.materials.Length; m++)
                {
                    if (meshdata.materials[m].GetTexture(meshdata.properties[p]) != null)
                    {
                        lIsSet = true;
                    }
                }
                if (lIsSet)
                {
                    meshdata.setProperties.Add(meshdata.properties[p]);
                }
            }
            AddStatus("Texture Properties filled: " + meshdata.setProperties.Count);
            return 6f / lSteps;
        }
        if (meshdata.nicFilledProperties == null)
        {
            meshdata.nicFilledProperties = new List<string>[meshdata.materials.Length];

            for (int m = 0; m < meshdata.materials.Length; m++)
            {
                meshdata.nicFilledProperties[m] = new List<string>();
            }

            if (meshdata.allProperties.Length == 1)
            {
                AddStatus("Only one Shader -> Skip observe lost Textures Step");
                return 7f / lSteps;
            }

            int notInCommonButFilled = 0;
            for (int s = 0; s < meshdata.shaders.Count; s++)
            {
                for (int p = 0; p < meshdata.allProperties[s].Count; p++)
                {
                    if (meshdata.setProperties.Contains(meshdata.allProperties[s][p]))
                    {
                        continue;
                    }
                    for (int m = 0; m < meshdata.materials.Length; m++)
                    {
                        Shader lShader = meshdata.materials[m].shader;
                        for (int sp = 0; sp < ShaderUtil.GetPropertyCount(lShader); sp++)
                        {
                            if (ShaderUtil.GetPropertyName(lShader, sp) == meshdata.allProperties[s][p])
                            {
                                if (ShaderUtil.GetPropertyType(lShader, sp) == ShaderUtil.ShaderPropertyType.TexEnv)
                                {
                                    if (meshdata.materials[m].GetTexture(meshdata.allProperties[s][p]) != null)
                                    {
                                        if (!meshdata.nicFilledProperties[m].Contains(meshdata.allProperties[s][p]))
                                        {
                                            meshdata.nicFilledProperties[m].Add(meshdata.allProperties[s][p]);
                                            notInCommonButFilled++;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            AddStatus("Texture Properties which are not in common but filled: " + notInCommonButFilled);
            for (int m = 0; m < meshdata.materials.Length; m++)
            {
                AddStatus("  ->   Material " + (m + 1) + " Texture Properties are filled: " + meshdata.nicFilledProperties[m].Count);
            }
            return 7f / lSteps;
        }
        if (meshdata.textures == null)
        {
            meshdata.textures = new Dictionary<string, Texture>[meshdata.materials.Length];
            int collectedTextures = 0;
            for (int m = 0; m < meshdata.materials.Length; m++)
            {
                meshdata.textures[m] = new Dictionary<string, Texture>();
                for (int p = 0; p < meshdata.setProperties.Count; p++)
                {
                    Texture lTexture = meshdata.materials[m].GetTexture(meshdata.setProperties[p]);
                    meshdata.textures[m].Add(meshdata.setProperties[p], lTexture);
                    if (lTexture)
                    {
                        collectedTextures++;
                    }
                }
            }
            AddStatus("Collected Texures: " + collectedTextures);
            return 8f / lSteps;
        }
        if (meshdata.observeTextures == null)
        {
            meshdata.observeTextures = new Dictionary<string, Texture>[meshdata.materials.Length];
            int collectedObserveTextures = 0;
            for (int m = 0; m < meshdata.materials.Length; m++)
            {
                meshdata.observeTextures[m] = new Dictionary<string, Texture>();
                for (int p = 0; p < meshdata.nicFilledProperties[m].Count; p++)
                {
                    meshdata.observeTextures[m].Add(meshdata.nicFilledProperties[m][p], meshdata.materials[m].GetTexture(meshdata.nicFilledProperties[m][p]));
                    collectedObserveTextures++;
                }
            }
            AddStatus("Collected Observe Texures: " + collectedObserveTextures);
            return 9f / lSteps;
        }

        return 1f;
    }

    float ProcessTextureData()
    {
        float lSteps = 4f;
        if (meshdata.texSize == null)
        {
            AddStatus("Chosen Size Mode: " + preferredSize.ToString());
            meshdata.texSize = new List<int>();
            for (int m = 0; m < meshdata.materials.Length; m++)
            {
                int lMinSize = int.MaxValue;
                int lMaxSize = 0;
                for (int p = 0; p < meshdata.setProperties.Count; p++)
                {
                    Texture lTexture = meshdata.textures[m][meshdata.setProperties[p]];
                    if (lTexture != null)
                    {
                        if (lMinSize > lTexture.width)
                        {
                            lMinSize = lTexture.width;
                        }
                        if (lMaxSize < lTexture.width)
                        {
                            lMaxSize = lTexture.width;
                        }
                    }
                }
                for (int p = 0; p < meshdata.observeTextures[m].Count; p++)
                {
                    Texture lTexture = meshdata.observeTextures[m][meshdata.nicFilledProperties[m][p]];
                    if (lTexture != null)
                    {
                        if (lMinSize > lTexture.width)
                        {
                            lMinSize = lTexture.width;
                        }
                        if (lMaxSize < lTexture.width)
                        {
                            lMaxSize = lTexture.width;
                        }
                    }
                }
                switch (preferredSize)
                {
                    case PreferredSizes.Biggest:
                        meshdata.texSize.Add(lMaxSize);
                        break;
                    case PreferredSizes.Smallest:
                        meshdata.texSize.Add(lMinSize);
                        break;
                }
            }
            AddStatus("Calculated Sizes!");
            return 1f / lSteps;
        }
        if (!meshdata.resized)
        {
            for (int m = 0; m < meshdata.materials.Length; m++)
            {
                int lSize = meshdata.texSize[m];
                RenderTexture lRenderTexture = RenderTexture.GetTemporary(lSize, lSize);
                for (int p = 0; p < meshdata.setProperties.Count; p++)
                {
                    Texture lTexture = meshdata.textures[m][meshdata.setProperties[p]];
                    if (lTexture != null)
                    {
                        Texture2D lNewTexture = new Texture2D(lSize, lSize);
                        Graphics.Blit(lTexture, lRenderTexture);
                        RenderTexture.active = lRenderTexture;
                        lNewTexture.ReadPixels(new Rect(0, 0, lRenderTexture.width, lRenderTexture.height), 0, 0);
                        lNewTexture.Apply();
                        meshdata.textures[m][meshdata.setProperties[p]] = lNewTexture;
                    } else
                    {
                        meshdata.textures[m][meshdata.setProperties[p]] = new Texture2D(lSize, lSize);
                    }
                }
                for (int p = 0; p < meshdata.observeTextures[m].Count; p++)
                {
                    Texture lTexture = meshdata.observeTextures[m][meshdata.nicFilledProperties[m][p]];
                    if (lTexture != null)
                    {
                        Texture2D lNewTexture = new Texture2D(lSize, lSize);
                        Graphics.Blit(lTexture, lRenderTexture);
                        RenderTexture.active = lRenderTexture;
                        lNewTexture.ReadPixels(new Rect(0, 0, lRenderTexture.width, lRenderTexture.height), 0, 0);
                        lNewTexture.Apply();
                        meshdata.observeTextures[m][meshdata.nicFilledProperties[m][p]] = lNewTexture;
                    }
                }
                RenderTexture.ReleaseTemporary(lRenderTexture);
            }
            meshdata.resized = true;
            AddStatus("Processed Sizes!");
            return 2f / lSteps;
        }
        if (meshdata.textureAtlas == null)
        {
            AddStatus("Generate Atlas Textures:");
            meshdata.textureAtlas = new Dictionary<string, Texture>();
            for (int p = 0; p < meshdata.setProperties.Count; p++)
            {
                List<Texture2D> lTextures = new List<Texture2D>();
                for (int m = 0; m < meshdata.materials.Length; m++)
                {
                    lTextures.Add((Texture2D)meshdata.textures[m][meshdata.setProperties[p]]);
                }
                resultTexture = new RenderTexture(resultSize, resultSize, 0);
                previewTexture = new RenderTexture(resultSize, resultSize, 0);
                TextureOperator.UpdateTexture(lTextures, resultSize, ref resultTexture, ref previewTexture, TextureOperator.InterpolatingMethods.Unique, new List<IAtlasGenEffect>());

                Texture2D lNewTexture = new Texture2D(resultSize, resultSize);
                RenderTexture.active = resultTexture;
                lNewTexture.ReadPixels(new Rect(0, 0, resultTexture.width, resultTexture.height), 0, 0);
                lNewTexture.Apply();
                meshdata.textureAtlas.Add(meshdata.setProperties[p], lNewTexture);
                AddStatus("  ->   " + (meshdata.setProperties[p]));
            }
            AddStatus("Generated Atlas Textures");
            return 3f / lSteps;
        }
        if (meshdata.observedAtlas == null)
        {
            AddStatus("Generate Observed Atlas Textures:");
            meshdata.observedAtlas = new Dictionary<string, Texture>[meshdata.materials.Length];
            for (int m = 0; m < meshdata.materials.Length; m++) {
                meshdata.observedAtlas[m] = new Dictionary<string, Texture>();
                for (int p = 0; p < meshdata.observeTextures[m].Count; p++)
                {
                    List<Texture2D> lTextures = new List<Texture2D>();
                    for (int mt = 0; mt < meshdata.materials.Length; mt++)
                    {
                        if (mt == m)
                        {
                            lTextures.Add((Texture2D)meshdata.observeTextures[m][meshdata.nicFilledProperties[m][p]]);
                        } else
                        {
                            lTextures.Add(new Texture2D(meshdata.texSize[mt], meshdata.texSize[mt]));
                        }
                    }
                    resultTexture = new RenderTexture(resultSize, resultSize, 0);
                    previewTexture = new RenderTexture(resultSize, resultSize, 0);
                    TextureOperator.UpdateTexture(lTextures, resultSize, ref resultTexture, ref previewTexture, TextureOperator.InterpolatingMethods.Unique, new List<IAtlasGenEffect>());

                    Texture2D lNewTexture = new Texture2D(resultSize, resultSize);
                    RenderTexture.active = resultTexture;
                    lNewTexture.ReadPixels(new Rect(0, 0, resultTexture.width, resultTexture.height), 0, 0);
                    lNewTexture.Apply();
                    meshdata.observedAtlas[m].Add(meshdata.nicFilledProperties[m][p], lNewTexture);
                    AddStatus("  ->   " + (meshdata.nicFilledProperties[m][p]));
                }
            }
            AddStatus("Generated Observed Atlas Textures");
            return 4f / lSteps;
        }
        return 1f;
    }
}
