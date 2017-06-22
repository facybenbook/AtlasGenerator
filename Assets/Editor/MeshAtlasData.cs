using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct MeshAtlasData {

    //Mesh Data
    public MeshFilter filter;
    public Mesh mesh;
    public int subMeshCount;
    public List<int>[] triangles;

    //Material Data
    public Material[] materials;
    public List<Shader> shaders;

    public List<string>[] allProperties;
    public List<string> properties;
    public bool checkedRenderTexture;
    public List<string> setProperties;
    public List<string>[] nicFilledProperties; //nic = Not in Common

    public Dictionary<string, Texture>[] textures;
    public Dictionary<string, Texture>[] observeTextures;

    //Process Textures
    public List<int> texSize;
    public bool resized;
    public Dictionary<string, Texture> textureAtlas;
    public Dictionary<string, Texture>[] observedAtlas;
}