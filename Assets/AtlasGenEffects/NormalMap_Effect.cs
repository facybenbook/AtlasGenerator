using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NormalMap_Effect : IAtlasGenEffect
{
    
    private string normalMapShader = "Hidden/NormalMapShader";

    [Range(0f, 1f)]
    public float strength = 0.5f;

    public override void BlitEffect(RenderTexture src, RenderTexture dest)
    {
        Material nmMat = new Material(Shader.Find(normalMapShader));
        nmMat.SetFloat("_Strength", strength * 2f);

        Graphics.Blit(src, dest, nmMat);
    }
}
