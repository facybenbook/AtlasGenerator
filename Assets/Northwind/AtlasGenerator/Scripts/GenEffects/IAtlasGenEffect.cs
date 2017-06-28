using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Northwind.AtlasGen
{
    public abstract class IAtlasGenEffect : ScriptableObject
    {

        public abstract void BlitEffect(RenderTexture src, RenderTexture dest);

    }
}