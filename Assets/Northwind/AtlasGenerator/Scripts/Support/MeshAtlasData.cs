using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Northwind.AtlasGen
{
    public struct MeshAtlasData
    {

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
        public Dictionary<string, string>[] oldTexturePaths;

        //Process Textures
        public List<int> texSize;
        public bool resized;
        public Dictionary<string, Texture> textureAtlas;
        public Dictionary<string, Texture>[] observedAtlas;

        public Dictionary<string, bool> textureIsNormalMap;
        public Dictionary<string, float> normalMapStrength;

        //Process Mesh
        public Mesh resultMesh;
        public Material[] resultMaterials;

        //Finalize
        public string folderRoot;
        public string fileEnding;
        public bool savedMesh;
        public string meshPath;
        public bool savedTextures;
        public Dictionary<string, string> texturePaths;
        public bool postdefinedTexSettings;
        public bool savedMaterials;
        public Dictionary<int, string> materialPaths;
        public bool rebindTextures;
        public bool foundSaveMode;
        public bool updateOriginal;
        public bool finished;

    }
}