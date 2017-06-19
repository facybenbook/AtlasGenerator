using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class TextureAtlasEditor : EditorWindow {

    private List<Texture2D> textures = new List<Texture2D>();

    private RenderTexture resultTexture, previewTexture;

    private Vector2 scrollView = Vector2.zero;
    private Vector2 scrollEffect = Vector2.zero;

    private Texture2D tempTexture;

    private enum TextureSizes { _16 = 16, _32 = 32, _64 = 64, _128 = 128, _256 = 256, _512 = 512, _1024 = 1024, _2048 = 2048, _4096 = 4096, _8192 = 8192};
    private int resultSize = 512;

    private enum InterpolatingMethods { Point, Bilinear, Trilinear, Unique};
    private InterpolatingMethods interpolationMethod = InterpolatingMethods.Unique;

    [System.Flags]
    private enum ExtraShaders { Blur = 1, Distort = 2};
    private ExtraShaders extraShader = 0;

    private IAtlasGenEffect selectedEffect;
    private List<IAtlasGenEffect> effects = new List<IAtlasGenEffect>();

    [Range(0f, 10f)]
    private float blurStrength = 2f;

    private string shaderName = "Hidden/AtlasGen/BlitToCoord";
    private Material blitMat;

    private string blurShader = "Hidden/FastBlur";
    private Material blurMat;

    // Add menu named "My Window" to the Window menu
    [MenuItem("Window/Texture Atlas Creator")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        TextureAtlasEditor window = (TextureAtlasEditor)EditorWindow.GetWindow(typeof(TextureAtlasEditor));
        window.Show();
    }

    void OnEnable()
    {
        this.titleContent = new GUIContent("Atlas Creator", "Texture Atlas Generator");
        this.minSize = new Vector2(1024f, 512f);
        //this.maxSize = new Vector2(768.1f, 512.1f);

        resultTexture = new RenderTexture(8, 8, 0);
        resultTexture.Create();

        previewTexture = new RenderTexture(8, 8, 0);
        previewTexture.Create();

        blitMat = new Material(Shader.Find(shaderName));
        blurMat = new Material(Shader.Find(blurShader));
    }

    private void Update()
    {
        if (addedTexture)
        {
            textures.Add(addedTexture);
            addedTexture = null;
            Repaint();
            UpdateTexture();
        }
        
        if (addedEffect)
        {
            effects.Add(addedEffect);
            addedEffect = null;
            Repaint();
            UpdateTexture();
        }

        if (clear)
        {
            textures.Clear();
            clear = false;
            Repaint();
            UpdateTexture();
        }

        if (delTexture != null)
        {
            textures.RemoveAt(delTexture.Value);
            delTexture = null;
            Repaint();
            UpdateTexture();
        }

        if (delEffect != null)
        {
            effects.RemoveAt(delEffect.Value);
            delEffect = null;
            Repaint();
            UpdateTexture();
        }
    }
    
    Texture2D addedTexture;
    IAtlasGenEffect addedEffect;
    bool clear = false;
    int? delTexture;
    int? delEffect;

    void DrawTextureBox(int textureID, float width, float height)
    {
        GUILayout.BeginHorizontal(EditorStyles.miniButtonRight, GUILayout.Width(width), GUILayout.Height(height));
        GUILayout.BeginVertical();
        GUILayout.Space(3f);
        textures[textureID] = (Texture2D)EditorGUILayout.ObjectField(textures[textureID], typeof(Texture2D), false, GUILayout.Width(height - 8f), GUILayout.Height(height - 8f));
        GUILayout.EndVertical();
        GUILayout.BeginVertical(GUILayout.Width(width - height - 16f));
        GUILayout.Space(3f);
        GUILayout.Label(!textures[textureID] ? "" : textures[textureID].name, GUILayout.Width(width - height - 16f));
        GUILayout.Label(!textures[textureID] ? "" : "From: " + textures[textureID].width + "x" + textures[textureID].height, GUILayout.Width(width - height - 16f));
        GUILayout.Label(!textures[textureID] ? "" : "To: " + RoundToBinary(textures[textureID].width) + "x" + RoundToBinary(textures[textureID].width), GUILayout.Width(width - height - 16f));
        GUILayout.Space(-1f);
        Color lOld = GUI.backgroundColor;
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("Remove", GUILayout.Width(width - height - 16f)))
        {
            delTexture = textureID;
        }
        GUI.backgroundColor = lOld;
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }

    void DrawTexturePanel(Rect rect)
    {
        GUILayout.BeginArea(new Rect(0f, 0f, rect.width, this.position.height - 24f));
        scrollView = GUILayout.BeginScrollView(scrollView, false, true);

        for (int t = 0; t < textures.Count; t++)
        {
            DrawTextureBox(t, rect.width - 20f, 80f);
        }

        GUILayout.EndScrollView();

        GUILayout.EndArea();
        GUILayout.BeginArea(new Rect(0f, this.position.height - 24f, rect.width + 4f, 24f));

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Add", EditorStyles.miniButtonMid, GUILayout.Width((rect.width / 2f) + 2f), GUILayout.Height(24f)))
        {
            EditorGUIUtility.ShowObjectPicker<Texture2D>(tempTexture, false, "", 10);
        }

        if (GUILayout.Button("Clear", EditorStyles.miniButtonRight, GUILayout.Width((rect.width / 2f) + 2f), GUILayout.Height(24f)))
        {
            clear = true;
        }

        GUILayout.EndHorizontal();

        GUILayout.EndArea();

        for (int t = textures.Count - 1; t >= 0; t--)
        {
            if (textures[t] == null)
            {
                textures.RemoveAt(t);
            }
        }
    }
    
    void AddEffect(object obj)
    {
        MonoScript script = (MonoScript)obj;

        addedEffect = (IAtlasGenEffect)IAtlasGenEffect.CreateInstance(script.GetClass());
    }

    void DrawSettingsPanel(Rect rect)
    {

        GUILayout.BeginArea(rect);

        resultSize = (int)(TextureSizes)EditorGUILayout.EnumPopup("Result Size", (TextureSizes)resultSize);
        interpolationMethod = (InterpolatingMethods)EditorGUILayout.EnumPopup("Interpolating Method", interpolationMethod);
        //extraShader = (ExtraShaders)EditorGUILayout.EnumMaskPopup("Extra Shaders", extraShader);

        EditorGUILayout.Space();

        if (GUILayout.Button("Add Effect"))
        {
            MonoScript[] scripts = (MonoScript[])Resources.FindObjectsOfTypeAll(typeof(MonoScript));

            List<MonoScript> result = new List<MonoScript>();

            foreach (MonoScript m in scripts)
            {
                if (m.GetType() != typeof(Shader) && m.GetClass() != null && m.GetClass().IsSubclassOf(typeof(IAtlasGenEffect)))
                {
                    result.Add(m);
                }
            }

            GenericMenu menu = new GenericMenu();
            
            for (int m = 0; m < result.Count; m++)
            {
                menu.AddItem(new GUIContent(result[m].name.Replace('_', ' ')), false, AddEffect, result[m]);
            }

            menu.ShowAsContext();
        }

        GUILayout.Space(-2f);
        GUILayout.BeginHorizontal();
        GUILayout.Space(8f);
        scrollEffect = GUILayout.BeginScrollView(scrollEffect, false, true, GUILayout.Width(rect.width - 16f), GUILayout.Height(rect.height - 128f));
        for (int e = 0; e < effects.Count; e++)
        {
            GUILayout.BeginVertical(EditorStyles.miniButton);
            Editor effectInspector = Editor.CreateEditor(effects[e]);
            effectInspector.OnInspectorGUI();

            Color lOld = GUI.backgroundColor;
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Remove"))
            {
                delEffect = e;
            }
            GUI.backgroundColor = lOld;

            GUILayout.EndVertical();
        }
        GUILayout.EndScrollView();
        GUILayout.EndHorizontal();
        /*
        if ((extraShader & ExtraShaders.Blur) == ExtraShaders.Blur)
        {
            blurStrength = EditorGUILayout.Slider("Blur Strength", blurStrength, 0f, 10f);
        }*/

        GUILayout.EndArea();
    }

    void DrawResultPanel(Rect rect)
    {
        GUILayout.BeginArea(rect);
        
        EditorGUI.DrawPreviewTexture(new Rect(16f, 0f, rect.width - 32f, rect.width - 32f), previewTexture);

        if (GUI.Button(new Rect(16f, rect.width - 24f, rect.width - 32f, 24f), "Update"))
        {
            UpdateTexture();
        }

        if (GUI.Button(new Rect(16f, rect.width + 4f, rect.width - 32f, 24f), "Save As"))
        {
            if (resultTexture != null)
            {
                RenderTexture.active = resultTexture;
                Texture2D lExport = new Texture2D(resultTexture.width, resultTexture.height);
                lExport.ReadPixels(new Rect(0f, 0f, resultTexture.width, resultTexture.height), 0, 0);
                lExport.Apply();
                RenderTexture.active = null;

                string path = EditorUtility.SaveFilePanelInProject("Save Texture Atlas", "AtlasTexture", "png", "", "Assets/");

                if (path != "")
                {
                    byte[] bytes;
                    bytes = lExport.EncodeToPNG();

                    System.IO.File.WriteAllBytes(
                        path, bytes);

                    AssetDatabase.Refresh();

                    TextureImporter textureImporter = (TextureImporter)TextureImporter.GetAtPath(path);
                    textureImporter.alphaIsTransparency = true;
                    textureImporter.mipmapEnabled = false;
                    textureImporter.wrapMode = TextureWrapMode.Clamp;
                    textureImporter.SaveAndReimport();
                }
            }
        }

        GUILayout.EndArea();
    }

    void OnGUI()
    {

        this.minSize = new Vector2(this.minSize.x, this.position.width * (5f/8f));

        EditorGUI.BeginChangeCheck();

        float height = this.position.height;
        float width = this.position.width - 264f;

        DrawTexturePanel(new Rect(0f, 0f, 256f, height));

        DrawSettingsPanel(new Rect(264f, 8f, width * 0.4f, height));

        if (EditorGUI.EndChangeCheck())
        {
            UpdateTexture();
        }

        DrawResultPanel(new Rect(264f + width * 0.4f, 8f, width * 0.6f, height));

        if (Event.current.commandName == "ObjectSelectorClosed")
        {
            if (EditorGUIUtility.GetObjectPickerControlID() == 10)
            {
                addedTexture = (Texture2D)EditorGUIUtility.GetObjectPickerObject();
            } else if (EditorGUIUtility.GetObjectPickerControlID() == 11)
            {
                if (EditorGUIUtility.GetObjectPickerObject() == null)
                {
                    return;
                }

                addedEffect = (IAtlasGenEffect)IAtlasGenEffect.CreateInstance(EditorGUIUtility.GetObjectPickerObject().GetType());
                if (effects.Contains(addedEffect))
                {
                    addedEffect = null;
                }
            }
        }
    }

    void UpdateTexture()
    {
        if (textures.Count <= 0)
        {
            return;
        }

        Dictionary<int, List<int>> lSizes = new Dictionary<int, List<int>>();

        int lMaxTexSize = 0, lMinTexSize = int.MaxValue;

        Texture2D[] lTextures = new Texture2D[textures.Count];

        for (int t = 0; t < textures.Count; t++)
        {
            int lSize = (int)RoundToBinary(textures[t].width);
            if (!lSizes.ContainsKey(lSize))
            {
                lSizes.Add(lSize, new List<int>());
            }
            lSizes[lSize].Add(t);

            if (lMaxTexSize < textures[t].width)
            {
                lMaxTexSize = textures[t].width;
            }
            if (lMinTexSize > textures[t].width)
            {
                lMinTexSize = textures[t].width;
            }

            lTextures[t] = CopyTexture(textures[t]);
            switch(interpolationMethod)
            {
                case InterpolatingMethods.Point: lTextures[t].filterMode = FilterMode.Point; break;
                case InterpolatingMethods.Bilinear: lTextures[t].filterMode = FilterMode.Bilinear; break;
                case InterpolatingMethods.Trilinear: lTextures[t].filterMode = FilterMode.Trilinear; break;
                case InterpolatingMethods.Unique: lTextures[t].filterMode = textures[t].filterMode; break;
            }
        }

        List<KeyValuePair<int, List<int>>> list = new List<KeyValuePair<int, List<int>>>();

        foreach(KeyValuePair<int, List<int>> pair in lSizes)
        {
            list.Add(pair);
        }

        list.Sort((x, y) => y.Key.CompareTo(x.Key));

        lSizes.Clear();

        foreach (KeyValuePair<int, List<int>> pair in list)
        {
            lSizes.Add(pair.Key, pair.Value);
        }

        //Fill with objects of CustomClass...

        //list.Sort((x, y) => x.CustomIntVariable.ComareTo(y.CustomIntVariable));

        //Debug.Log(lMinTexSize + " < " + lMaxTexSize);

        string lContents = "";

        foreach (KeyValuePair<int, List<int>> pair in lSizes)
        {
            lContents += pair.Value.Count + "x" + pair.Key + ", ";
        }

        List<Texture2D> subTextures = new List<Texture2D>();
        List<int> subTexIDs = lSizes[lMaxTexSize];
        for (int i = 0; i < subTexIDs.Count; i++)
        {
            subTextures.Add(lTextures[subTexIDs[i]]);
        }

        List<TextureSquare> subSquares = new List<TextureSquare>();

        foreach (KeyValuePair<int, List<int>> pair in lSizes)
        {
            if (pair.Key < lMaxTexSize)
            {
                if (subSquares.Count == 0)
                {
                    subSquares.Add(new TextureSquare(lMaxTexSize));
                }

                TextureSquare lSquare = subSquares[subSquares.Count - 1];

                for (int t = 0; t < pair.Value.Count; t++)
                {
                    bool lAdded = lSquare.AddTexture(lTextures[pair.Value[t]]);

                    if (!lAdded)
                    {
                        subSquares.Add(new TextureSquare(lMaxTexSize));
                        lSquare = subSquares[subSquares.Count - 1];

                        lSquare.AddTexture(lTextures[pair.Value[t]]);
                    }
                }
            }
        }


        int squareAmount = CeilToBinary(subTextures.Count + subSquares.Count);
        squareAmount = CeilToSqrtable(subTextures.Count + subSquares.Count);

        /////////////////////////////Debug.Log(subTextures.Count + " Textures : Sqaures " + subSquares.Count);
        /////////////////////////////Debug.Log(squareAmount);

        if (squareAmount == 1)
        {
            SetResultTexture(resultSize, resultSize);
            BlitOnResult(subTextures[0], new Vector4(0f, 0f, 1f, 1f));
        }
        else
        if (squareAmount == 2)
        {
            SetResultTexture(resultSize, resultSize / 2);
            blitMat.SetVector("_BlitSize", new Vector4(0f, 0f, 0.5f, 1f));
            if (subTextures.Count == 2)
            {
                BlitOnResult(subTextures[0], new Vector4(0f, 0f, 0.5f, 1f));
                BlitOnResult(subTextures[1], new Vector4(0.5f, 0f, 0.5f, 1f));
            } else
            {
                int t = 0;
                for (t = 0; t < subTextures.Count; t++)
                {
                    BlitOnResult(subTextures[t], new Vector4(t * 0.5f, 0f, 0.5f, 1f));
                }
                for (int s = 0; s < subSquares.Count; s++)
                {
                    Dictionary<Rect, Texture2D> lTexRects = subSquares[s].GetFittedTextures(new Rect(t * 0.5f, 0f, 0.5f, 1f));
                    Debug.Log("Textures: " + lTexRects.Count);
                    foreach(KeyValuePair<Rect, Texture2D> pair in lTexRects)
                    {
                        BlitOnResult(pair.Value, new Vector4(pair.Key.x, pair.Key.y, pair.Key.width, pair.Key.height));
                    }
                }
            }

        } else
        {
            int tableSize = (int)Mathf.Sqrt(squareAmount);
            SetResultTexture(resultSize, resultSize);

            int lData = 0;

            for (int tx = 0; tx < tableSize; tx++)
            {
                for (int ty = 0; ty < tableSize; ty++)
                {
                    
                    if (lData < subTextures.Count)
                    {
                        BlitOnResult(subTextures[lData], new Vector4((float)tx / (float)tableSize, (float)ty / (float)tableSize, 1f / tableSize, 1f / tableSize));
                    }
                    else if ((lData - (subTextures.Count)) < subSquares.Count)
                    {
                        int lDataSquares = lData - (subTextures.Count);
                        Dictionary<Rect, Texture2D> lTexRects = subSquares[lDataSquares].GetFittedTextures(new Rect((float)tx / (float)tableSize, (float)ty / (float)tableSize, 1f / tableSize, 1f / tableSize));
                        foreach (KeyValuePair<Rect, Texture2D> pair in lTexRects)
                        {
                            BlitOnResult(pair.Value, new Vector4(pair.Key.x, pair.Key.y, pair.Key.width, pair.Key.height));
                        }
                    }

                    lData++;
                }
            }
        }

        resultTexture.filterMode = FilterMode.Point;

        for (int t = 0; t < lTextures.Length; t++)
        {
            DestroyImmediate(lTextures[t]);
        }


        Material lAlphaGrid = new Material(Shader.Find("Hidden/AlphaGridShader"));
        Graphics.Blit(resultTexture, previewTexture, lAlphaGrid);

        //////////////////////////////Mesh mesh = new Mesh();
        //////////////////////////////mesh.SetUVs()
        //TODO: MESH VISE
    }

    int RoundToBinary(int value)
    {
        int oldValue = 2;
        int newValue = 4;

        while(Mathf.Abs(value - oldValue) > Mathf.Abs(value - newValue))
        {
            oldValue = newValue;
            newValue = newValue * 2;
        }

        return oldValue;
    }

    int CeilToBinary(int value)
    {
        int oldValue = 1;
        int newValue = 2;

        while ((oldValue - value) < 0 || Mathf.Abs(value - oldValue) > Mathf.Abs(value - newValue))
        {
            oldValue = newValue;
            newValue = newValue * 2;
        }

        return oldValue;
    }

    int CeilToSqrtable(int value)
    {
        float lR = Mathf.Sqrt((float)value);
        return (int)Mathf.Pow(Mathf.Ceil(lR), 2f);
    }

    Texture2D CopyTexture(Texture2D source)
    {
        Texture2D texCopy = new Texture2D(source.width, source.height, source.format, source.mipmapCount > 1);
        texCopy.LoadRawTextureData(source.GetRawTextureData());
        texCopy.Apply();
        return texCopy;
    }

    void BlitOnResult(Texture2D texture, Vector4 rect)
    {
        blitMat.SetVector("_BlitSize", rect);

        RenderTexture lTempRenderTarget = RenderTexture.GetTemporary(texture.width, texture.height, 0);
        RenderTexture lTempBackBuffer = RenderTexture.GetTemporary(texture.width, texture.height, 0);

        Graphics.Blit(texture, lTempRenderTarget);
        
        for(int e = 0; e < effects.Count; e++)
        {
            Graphics.Blit(lTempRenderTarget, lTempBackBuffer);
            effects[e].BlitEffect(lTempBackBuffer, lTempRenderTarget);
        }

        /*if ((extraShader & ExtraShaders.Blur) == ExtraShaders.Blur)
        {
            blurMat.SetVector("_Parameter", new Vector4(blurStrength, blurStrength, 0f, 0f));

            Graphics.Blit(lTempRenderTarget, lTempBackBuffer, blurMat, 0);
            Graphics.Blit(lTempBackBuffer, lTempRenderTarget);
            Graphics.Blit(lTempRenderTarget, lTempBackBuffer, blurMat, 1);
            Graphics.Blit(lTempBackBuffer, lTempRenderTarget);
            Graphics.Blit(lTempRenderTarget, lTempBackBuffer, blurMat, 2);
            Graphics.Blit(lTempBackBuffer, lTempRenderTarget);
        }*/

        Graphics.Blit(lTempRenderTarget, resultTexture, blitMat);

        lTempBackBuffer.Release();
        lTempRenderTarget.Release();

    }

    void SetResultTexture(int width, int height)
    {
        RenderTexture.active = null;
        resultTexture.DiscardContents();
        resultTexture.Release();
        previewTexture.Release();

        resultTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        resultTexture.DiscardContents();
        previewTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        previewTexture.DiscardContents();

        Material lMat = new Material(Shader.Find("Hidden/ClearShader"));
        RenderTexture lTempClear = RenderTexture.GetTemporary(width, height, 0);
        Graphics.Blit(lTempClear, resultTexture, lMat);
        lTempClear.Release();
        
        /*function Start ()
{
    mat = new Material(
        "Shader \"Hidden/Clear Alpha\" {" +
        "Properties { _Alpha(\"Alpha\", Float)=1.0 } " +
        "SubShader {" +
        "    Pass {" +
        "        ZTest Always Cull Off ZWrite Off" +
        "        ColorMask A" +
        "        SetTexture [_Dummy] {" +
        "            constantColor(0,0,0,[_Alpha]) combine constant }" +
        "    }" +
        "}" +
        "}"
    );
}
 
function OnPostRender()
{
    GL.PushMatrix();
    GL.LoadOrtho();
    mat.SetFloat( "_Alpha", alpha );
    mat.SetPass(0);
    GL.Begin( GL.QUADS );
    GL.Vertex3( 0, 0, 0.1 );
    GL.Vertex3( 1, 0, 0.1 );
    GL.Vertex3( 1, 1, 0.1 );
    GL.Vertex3( 0, 1, 0.1 );
    GL.End();
    GL.PopMatrix();
}*/

        /*
        Material lColorMat = new Material(Shader.Find("Unlit/Color"));

        RenderTexture lText = RenderTexture.GetTemporary(width, height, 0);

        lColorMat.SetColor("_Color", Color.clear);
        Graphics.Blit(lText, resultTexture, lColorMat);

        lText.Release();*/
    }
}
