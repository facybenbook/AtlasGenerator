using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class IAtlasGenEffect : ScriptableObject {

    public abstract void BlitEffect(RenderTexture src, RenderTexture dest);

}
