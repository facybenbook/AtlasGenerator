using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlurGenEffect : IAtlasGenEffect
{
    public override void BlitEffect(RenderTexture src, RenderTexture dest)
    {
        Graphics.Blit(src, dest);
    }
}
