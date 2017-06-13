using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class IAtlasGenEffect : UnityEngine.Object {

    public abstract void BlitEffect(RenderTexture src, RenderTexture dest);

}
