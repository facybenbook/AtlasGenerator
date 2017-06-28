using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Northwind.AtlasGen
{
    public class Saturation_Effect : IAtlasGenEffect
    {

        private string saturateShader = "Hidden/AtlasGen/SaturateShader";

        [Range(0f, 10f)]
        public float saturateRed = 0f;
        [Range(0f, 10f)]
        public float saturateGreen = 0f;
        [Range(0f, 10f)]
        public float saturateBlue = 0f;

        public override void BlitEffect(RenderTexture src, RenderTexture dest)
        {
            Material satMat = new Material(Shader.Find(saturateShader));
            satMat.SetVector("_Saturate", new Vector4(saturateRed, saturateGreen, saturateBlue, 0f));

            Graphics.Blit(src, dest, satMat);
        }
    }
}