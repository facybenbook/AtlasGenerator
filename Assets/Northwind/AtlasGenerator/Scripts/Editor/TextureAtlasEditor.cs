using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Northwind.AtlasGen
{
    public class TextureAtlasEditor : EditorWindow
    {

        private List<Texture2D> textures = new List<Texture2D>();

        private RenderTexture resultTexture, previewTexture;

        private Vector2 scrollView = Vector2.zero;
        private Vector2 scrollEffect = Vector2.zero;

        private enum TextureSizes { _16 = 16, _32 = 32, _64 = 64, _128 = 128, _256 = 256, _512 = 512, _1024 = 1024, _2048 = 2048, _4096 = 4096, _8192 = 8192 };
        private int resultSize = 512;

        private TextureOperator.InterpolatingMethods interpolationMethod = TextureOperator.InterpolatingMethods.Unique;

        private IAtlasGenEffect selectedEffect;
        private List<IAtlasGenEffect> effects = new List<IAtlasGenEffect>();

        [MenuItem("Northwind/Atlas Generator/Texture Atlas")]
        static void Init()
        {
            TextureAtlasEditor window = (TextureAtlasEditor)EditorWindow.GetWindow(typeof(TextureAtlasEditor));
            window.Show();
        }

        void OnEnable()
        {
            this.titleContent = new GUIContent("NAG: Texture", "Northwind Atlas Generator: Texture");
            this.minSize = new Vector2(1024f, 512f);

            resultTexture = new RenderTexture(8, 8, 0);
            resultTexture.Create();

            previewTexture = new RenderTexture(8, 8, 0);
            previewTexture.Create();
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
            GUILayout.Label(!textures[textureID] ? "" : "To: " + TextureOperator.RoundToBinary(textures[textureID].width) + "x" + TextureOperator.RoundToBinary(textures[textureID].width), GUILayout.Width(width - height - 16f));
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
                EditorGUIUtility.ShowObjectPicker<Texture2D>(null, false, "", 10);
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
            interpolationMethod = (TextureOperator.InterpolatingMethods)EditorGUILayout.EnumPopup("Interpolating Method", interpolationMethod);

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

            GUILayout.EndArea();
        }

        void DrawResultPanel(Rect rect)
        {
            GUILayout.BeginArea(rect);

            previewTexture.filterMode = FilterMode.Point;
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

            this.minSize = new Vector2(this.minSize.x, this.position.width * (5f / 8f));

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
                }
                else if (EditorGUIUtility.GetObjectPickerControlID() == 11)
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
            TextureOperator.UpdateTexture(textures, resultSize, ref resultTexture, ref previewTexture, interpolationMethod, effects);
        }
    }
}