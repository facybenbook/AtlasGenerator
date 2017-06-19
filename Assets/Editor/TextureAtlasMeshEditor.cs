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
    List<int[]> triangles;

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


    void OnGUI()
    {
        renderer = (MeshRenderer)EditorGUILayout.ObjectField(new GUIContent("Mesh Renderer"), renderer, typeof(MeshRenderer), true);

        if (GUILayout.Button("UpdateMesh") && renderer)
        {
            filter = renderer.GetComponent<MeshFilter>();
            mesh = filter.sharedMesh;

            materials = renderer.sharedMaterials;
            triangles = new List<int[]>();

            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                triangles.Add(mesh.GetTriangles(s));
            }
        }
    }

}
