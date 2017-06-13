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
    private int textureMaxSize;

    private enum InterpolatingMethods { Point, Bilinear, Trilinear, Unique};
    private InterpolatingMethods interpolationMethod = 0;

    [System.Flags]
    private enum ExtraShaders { Blur = 2 };
    private ExtraShaders extraShader = 0;

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
        this.minSize = new Vector2(768f, 512f);
        this.maxSize = new Vector2(768.1f, 512.1f);

        resultTexture = new RenderTexture(8, 8, 0);
        resultTexture.Create();
    }

    void OnGUI()
    {
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

        GUILayout.BeginArea(new Rect(96f, 0f, (this.position.width - 96f) * 0.4f, this.position.height));

        textureMaxSize = (int)(TextureSizes)EditorGUILayout.EnumPopup("Texture max Size", (TextureSizes)textureMaxSize);
        interpolationMethod = (InterpolatingMethods)EditorGUILayout.EnumPopup("Interpolating Method", interpolationMethod);
        extraShader = (ExtraShaders)EditorGUILayout.EnumMaskPopup("Extra Shaders", extraShader);

        GUILayout.EndArea();

        if (EditorGUI.EndChangeCheck())
        {
            UpdateTexture();
        }

        GUILayout.BeginArea(new Rect(96f + 256f, 0f, (this.position.width - 96f) * 0.6f, this.position.height));

        EditorGUI.DrawPreviewTexture(new Rect(32f, 0f, (this.position.width - 96f) * 0.6f - 32f, (this.position.width - 96f) * 0.6f - 32f), resultTexture);

        if (GUI.Button(new Rect(32f, (this.position.width - 96f) * 0.6f - 24f, (this.position.width - 96f) * 0.6f - 32f, 32f), "Update"))
        {
            UpdateTexture();
        }

        GUILayout.EndArea();
    }

    void UpdateTexture()
    {
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

        Debug.Log(lMinTexSize + " < " + lMaxTexSize);

        resultTexture = new RenderTexture(lMaxTexSize, lMaxTexSize, 0);
        //resultTexture.filterMode = FilterMode.Point;

        Graphics.Blit(lTextures[0], resultTexture);

        for (int t = 0; t < lTextures.Length; t++)
        {
            DestroyImmediate(lTextures[t]);
        }
    }

    float RoundToBinary(float value)
    {
        float oldValue = 2f;
        float newValue = 4f;

        while(Mathf.Abs(value - oldValue) < Mathf.Abs(value - newValue))
        {
            oldValue = newValue;
            newValue = newValue * newValue;
        }

        return oldValue;
    }

    Texture2D CopyTexture(Texture2D source)
    {
        Texture2D texCopy = new Texture2D(source.width, source.height, source.format, source.mipmapCount > 1);
        texCopy.LoadRawTextureData(source.GetRawTextureData());
        texCopy.Apply();
        return texCopy;
    }

}
