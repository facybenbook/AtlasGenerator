using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class TextureAtlasMeshEditor : EditorWindow
{

    MeshRenderer renderer;
    MeshFilter filter;
    Mesh mesh;

    Material[] materials;
    List<Shader> shaders;

    List<int[]> triangles;

    List<string> textureProperties;

    List<Texture> mainTextures;

    Dictionary<string, List<Texture>> textures;
    
    string status = "";

    [MenuItem("Window/Texture Atlas Mesh Creator")]
    static void Init()
    {
        TextureAtlasMeshEditor window = (TextureAtlasMeshEditor)EditorWindow.GetWindow(typeof(TextureAtlasMeshEditor));
        window.Show();
    }

    void OnEnable()
    {
        this.titleContent = new GUIContent("Atlas Mesh Creator", "Texture Atlas Mesh Generator");
        this.minSize = new Vector2(1024f, 512f);
    }


    void AddStatus(string state)
    {
        if (status != "")
        {
            status += " | ";
        }
        status += state;
    }

    void OnGUI()
    {
        renderer = (MeshRenderer)EditorGUILayout.ObjectField(new GUIContent("Mesh Renderer"), renderer, typeof(MeshRenderer), true);
        
        if (GUILayout.Button("UpdateMesh") && renderer)
        {

            status = "";
            filter = renderer.GetComponent<MeshFilter>();
            mesh = filter.sharedMesh;
            AddStatus("Submeshes: " + mesh.subMeshCount);

            materials = renderer.sharedMaterials;
            AddStatus("Materials: " + materials.Length);

            shaders = new List<Shader>();
            for (int m = 0; m < materials.Length; m++)
            {
                Shader lShader = materials[m].shader;
                if (!shaders.Contains(lShader))
                {
                    shaders.Add(lShader);
                }
            }
            AddStatus("Shaders: " + shaders.Count);

            int texturePropertiesCount = 0;
            int setTexturePropertiesCount = 0;
            int commonPropertiesCount = 0;
            int setCommonPropertiesCount = 0;

            textureProperties = new List<string>();
            textures = new Dictionary<string, List<Texture>>();
            for (int s = 0; s < shaders.Count; s++)
            {
                for (int p = 0; p < ShaderUtil.GetPropertyCount(shaders[s]); p++)
                {
                    ShaderUtil.ShaderPropertyType lType = ShaderUtil.GetPropertyType(shaders[s], p);
                    if (!ShaderUtil.IsShaderPropertyHidden(shaders[s], p) && lType == ShaderUtil.ShaderPropertyType.TexEnv)
                    {
                        string propName = ShaderUtil.GetPropertyName(shaders[s], p);

                        texturePropertiesCount++;
                        bool lGiven = true;
                        bool lSet = false;
                        for (int m = 0; m < materials.Length; m++)
                        {
                            if (materials[m].shader == shaders[s])
                            {
                                if (materials[m].GetTexture(ShaderUtil.GetPropertyName(shaders[s], p)) != null)
                                {
                                    setTexturePropertiesCount++;
                                    lSet = true;
                                    if (!textureProperties.Contains(propName))
                                    {
                                        if (!textures.ContainsKey(propName))
                                        {
                                            textures.Add(propName, new List<Texture>());
                                        }
                                        textures[propName].Add(materials[m].GetTexture(propName));
                                    }
                                }
                            } else
                            {
                                Shader lMatShader = materials[m].shader;
                                bool lFound = false;
                                for (int p1 = 0; p1 < ShaderUtil.GetPropertyCount(lMatShader); p1++)
                                {
                                    if (ShaderUtil.GetPropertyName(lMatShader, p1) == ShaderUtil.GetPropertyName(shaders[s], p))
                                    {
                                        ShaderUtil.ShaderPropertyType lType1 = ShaderUtil.GetPropertyType(lMatShader, p1);
                                        if (lType1 == ShaderUtil.ShaderPropertyType.TexEnv)
                                        {
                                            lFound = true;

                                            if (materials[m].GetTexture(ShaderUtil.GetPropertyName(lMatShader, p1)) != null)
                                            {
                                                lSet = true;
                                                if (!textureProperties.Contains(propName))
                                                {
                                                    if (!textures.ContainsKey(propName))
                                                    {
                                                        textures.Add(propName, new List<Texture>());
                                                    }
                                                    textures[propName].Add(materials[m].GetTexture(propName));
                                                }
                                            }
                                        }
                                    }
                                }

                                lGiven = !lFound ? false : lGiven;
                            }
                        }

                        if (lGiven)
                        {
                            if (!textureProperties.Contains(propName))
                            {
                                commonPropertiesCount++;
                                textureProperties.Add(propName);
                                if (lSet)
                                {
                                    setCommonPropertiesCount++;
                                }
                            }
                        }
                    }
                }
            }
            AddStatus("Textures Set: " + setTexturePropertiesCount + "/" + texturePropertiesCount + " :: useable: " + setCommonPropertiesCount + "/" + commonPropertiesCount);

            triangles = new List<int[]>();
            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                triangles.Add(mesh.GetTriangles(s));
            }
            AddStatus("Triangles: " + triangles.Count);
            
            /*
            mainTextures = new List<Texture>();
            for (int s = 0; s < materials.Length; s++)
            {
                mainTextures.Add(materials[s].mainTexture);
            }
            AddStatus("Textures: " + mainTextures.Count);*/
            

        }

        if (mainTextures != null)
        {
            for (int t = 0; t < mainTextures.Count; t++)
            {
                EditorGUI.DrawPreviewTexture(new Rect(8f + t * 72f, 48f, 64f, 64f), mainTextures[t]);
            }
        }

        if (textures != null)
        {
            int keyCount = 0;
            float yPos = 24f;
            int length = 0;
            foreach (KeyValuePair<string, List<Texture>> pair in textures)
            {
                List<Texture> lTextures = pair.Value;

                if (lTextures.Count > length)
                {
                    length = lTextures.Count;
                }
            }

            foreach (KeyValuePair<string, List<Texture>> pair in textures)
            {
                List<Texture> lTextures = pair.Value;

                EditorGUI.DrawRect(new Rect(4f, yPos + 48f + 96f * keyCount, 72f * (lTextures.Count), 84f), new Color(0f, 0f, 0f, 0.75f));
                EditorGUI.LabelField(new Rect(8f, yPos + 48f + 96f * keyCount, lTextures.Count * 72f - 8f, 16f), pair.Key);
                for (int t = 0; t < lTextures.Count; t++)
                {
                    EditorGUI.DrawRect(new Rect(7f + t * 72f, yPos + 63f + 96f * keyCount, 66f, 66f), new Color(1f, 1f, 1f, 0.75f));
                    EditorGUI.DrawPreviewTexture(new Rect(8f + t * 72f, yPos + 64f + 96f * keyCount, 64f, 64f), lTextures[t]);
                    EditorGUI.DrawRect(new Rect(8f + t * 72f, yPos + 112f + 96f * keyCount, 64f, 16f), new Color(0f, 0f, 0f, 0.75f));
                    EditorGUI.LabelField(new Rect(8f + t * 72f, yPos + 112f + 96f * keyCount, 64f, 16f), lTextures[t].width + "px");
                }
                keyCount++;
            }
            for (int h = 0; h < length; h++)
            {
                EditorGUI.LabelField(new Rect(8f + h * 72f, 48f, 64f, 16f), "Mesh " + Mathf.Min(h + 1, mesh.subMeshCount));
            }
        }

        EditorGUI.HelpBox(new Rect(0f, this.position.height - 16f, this.position.width, 16f), status, MessageType.None);
    }

    IEnumerator UpdateMesh()
    {
        bool ready = false;
        while (!ready)
        {
            status = "";
            filter = renderer.GetComponent<MeshFilter>();
            mesh = filter.sharedMesh;
            AddStatus("Submeshes: " + mesh.subMeshCount);

            materials = renderer.sharedMaterials;
            AddStatus("Materials: " + materials.Length);
            triangles = new List<int[]>();

            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                triangles.Add(mesh.GetTriangles(s));
            }
            AddStatus("Triangles: " + triangles.Count);

            ready = true;
        }
        yield return null;
    }

}
