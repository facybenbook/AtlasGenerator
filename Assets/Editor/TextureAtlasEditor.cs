using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class TextureAtlasEditor : EditorWindow {

    private List<Texture2D> textures = new List<Texture2D>();

    private RenderTexture resultTexture;

    private Vector2 scrollView = Vector2.zero;

    private Texture2D tempTexture;

    private enum TextureSizes { _16 = 16, _32 = 32, _64 = 64, _128 = 128, _256 = 256, _512 = 512, _1024 = 1024, _2048 = 2048, _4096 = 4096, _8192 = 8192};
    private int resultSize = 512;

    private enum InterpolatingMethods { Point, Bilinear, Trilinear, Unique};
    private InterpolatingMethods interpolationMethod = InterpolatingMethods.Unique;

    [System.Flags]
    private enum ExtraShaders { Blur = 1, Distort = 2};
    private ExtraShaders extraShader = 0;

    private IAtlasGenEffect selectedEffect;

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
        this.minSize = new Vector2(800f, 500f);
        //this.maxSize = new Vector2(768.1f, 512.1f);

        resultTexture = new RenderTexture(8, 8, 0);
        resultTexture.Create();

        blitMat = new Material(Shader.Find(shaderName));
        blurMat = new Material(Shader.Find(blurShader));
    }

    void OnGUI()
    {
        this.minSize = new Vector2(this.minSize.x, this.position.width * (5f/8f));

        EditorGUI.BeginChangeCheck();

        GUILayout.BeginArea(new Rect(0f, 0f, 96f, this.position.height));
        scrollView = GUILayout.BeginScrollView(scrollView);

        for (int t = 0; t < textures.Count; t++)
        {
            textures[t] = (Texture2D)EditorGUILayout.ObjectField(textures[t], typeof(Texture2D), false, GUILayout.Width(72f), GUILayout.Height(72f));
        }

        if (GUILayout.Button("+", GUILayout.Width(72f), GUILayout.Height(72f)))
        {
            EditorGUIUtility.ShowObjectPicker<Texture2D>(tempTexture, false, "", 10);
            return;
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();

        if (Event.current.commandName == "ObjectSelectorClosed")
        {
            if (EditorGUIUtility.GetObjectPickerControlID() == 10)
            {
                textures.Add((Texture2D)EditorGUIUtility.GetObjectPickerObject());
                Repaint();
                return;
            }
        }

        for (int t = textures.Count - 1; t >= 0 ; t--)
        {
            if (textures[t] == null)
            {
                textures.RemoveAt(t);
            }
        }

        GUILayout.BeginArea(new Rect(104f, 8f, (this.position.width - 96f) * 0.4f, this.position.height));

        resultSize = (int)(TextureSizes)EditorGUILayout.EnumPopup("Texture max Size", (TextureSizes)resultSize);
        interpolationMethod = (InterpolatingMethods)EditorGUILayout.EnumPopup("Interpolating Method", interpolationMethod);
        extraShader = (ExtraShaders)EditorGUILayout.EnumMaskPopup("Extra Shaders", extraShader);

        EditorGUILayout.Space();

        if (GUILayout.Button("Add Effect"))
        {
            EditorGUIUtility.ShowObjectPicker<IAtlasGenEffect>(selectedEffect, false, "", 11);
            return;
            //selectedEffect
        }

        if ((extraShader & ExtraShaders.Blur) == ExtraShaders.Blur)
        {
            blurStrength = EditorGUILayout.Slider("Blur Strength", blurStrength, 0f, 10f);
        }

        GUILayout.EndArea();

        if (EditorGUI.EndChangeCheck())
        {
            UpdateTexture();
        }

        GUILayout.BeginArea(new Rect(96f + (this.position.width - 96f) * 0.4f, 8f, (this.position.width - 96f) * 0.6f, this.position.height));

        EditorGUI.DrawPreviewTexture(new Rect(16f, 0f, (this.position.width - 96f) * 0.6f - 32f, (this.position.width - 96f) * 0.6f - 32f), resultTexture);

        if (GUI.Button(new Rect(16f, (this.position.width - 96f) * 0.6f - 24f, (this.position.width - 96f) * 0.6f - 32f, 32f), "Update"))
        {
            UpdateTexture();
        }

        if (GUI.Button(new Rect(16f, (this.position.width - 96f) * 0.6f - 24f + 40f, (this.position.width - 96f) * 0.6f - 32f, 32f), "Save As"))
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
                }
            }
        }

        GUILayout.EndArea();
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
        
        if ((extraShader & ExtraShaders.Blur) == ExtraShaders.Blur)
        {
            blurMat.SetVector("_Parameter", new Vector4(blurStrength, blurStrength, 0f, 0f));

            Graphics.Blit(lTempRenderTarget, lTempBackBuffer, blurMat, 0);
            Graphics.Blit(lTempBackBuffer, lTempRenderTarget);
            Graphics.Blit(lTempRenderTarget, lTempBackBuffer, blurMat, 1);
            Graphics.Blit(lTempBackBuffer, lTempRenderTarget);
            Graphics.Blit(lTempRenderTarget, lTempBackBuffer, blurMat, 2);
            Graphics.Blit(lTempBackBuffer, lTempRenderTarget);
        }

        Graphics.Blit(lTempRenderTarget, resultTexture, blitMat);

        lTempBackBuffer.Release();
        lTempRenderTarget.Release();

    }

    void SetResultTexture(int width, int height)
    {
        resultTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);

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
