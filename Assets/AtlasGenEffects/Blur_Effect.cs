using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Blur_Effect : IAtlasGenEffect
{
    private string blurShader = "Hidden/FastBlur";

    [Range(0f, 10f)]
    public float blurAmount;

    public override void BlitEffect(RenderTexture src, RenderTexture dest)
    {
        Material blurMat = new Material(Shader.Find(blurShader));
        blurMat.SetVector("_Parameter", new Vector4(blurAmount, blurAmount, 0f, 0f));

        RenderTexture lTempBuffer = RenderTexture.GetTemporary(src.width, src.height, src.depth);

        Graphics.Blit(src, lTempBuffer, blurMat, 0);
        Graphics.Blit(lTempBuffer, src);
        Graphics.Blit(src, lTempBuffer, blurMat, 1);
        Graphics.Blit(lTempBuffer, src);
        Graphics.Blit(src, dest, blurMat, 2);

        lTempBuffer.Release();
    }
}
