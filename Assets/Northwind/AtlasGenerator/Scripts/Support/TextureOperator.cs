using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Northwind.AtlasGen
{
    public static class TextureOperator
    {

        public enum InterpolatingMethods { Point, Bilinear, Trilinear, Unique };

        public static void UpdateTexture(List<Texture2D> textures, int resultSize, ref RenderTexture resultTexture, ref RenderTexture previewTexture, InterpolatingMethods interpolationMethod, List<IAtlasGenEffect> effects)
        {

            if (textures.Count <= 0)
            {
                return;
            }

            Dictionary<int, List<int>> lSizes = new Dictionary<int, List<int>>();

            int lMaxTexSize = 0, lMinTexSize = int.MaxValue;

            Texture2D[] lTextures = new Texture2D[textures.Count];

            for (int t = 0; t < textures.Count; t++)
            {
                int lSize = (int)RoundToBinary(textures[t].width);
                if (!lSizes.ContainsKey(lSize))
                {
                    lSizes.Add(lSize, new List<int>());
                }
                lSizes[lSize].Add(t);

                if (lMaxTexSize < textures[t].width)
                {
                    lMaxTexSize = textures[t].width;
                }
                if (lMinTexSize > textures[t].width)
                {
                    lMinTexSize = textures[t].width;
                }

                lTextures[t] = CopyTexture(textures[t]);
                switch (interpolationMethod)
                {
                    case InterpolatingMethods.Point: lTextures[t].filterMode = FilterMode.Point; break;
                    case InterpolatingMethods.Bilinear: lTextures[t].filterMode = FilterMode.Bilinear; break;
                    case InterpolatingMethods.Trilinear: lTextures[t].filterMode = FilterMode.Trilinear; break;
                    case InterpolatingMethods.Unique: lTextures[t].filterMode = textures[t].filterMode; break;
                }
            }

            List<KeyValuePair<int, List<int>>> list = new List<KeyValuePair<int, List<int>>>();

            foreach (KeyValuePair<int, List<int>> pair in lSizes)
            {
                list.Add(pair);
            }

            list.Sort((x, y) => y.Key.CompareTo(x.Key));

            lSizes.Clear();

            foreach (KeyValuePair<int, List<int>> pair in list)
            {
                lSizes.Add(pair.Key, pair.Value);
            }

            string lContents = "";

            foreach (KeyValuePair<int, List<int>> pair in lSizes)
            {
                lContents += pair.Value.Count + "x" + pair.Key + ", ";
            }

            List<Texture2D> subTextures = new List<Texture2D>();
            List<int> subTexIDs = lSizes[lMaxTexSize];
            for (int i = 0; i < subTexIDs.Count; i++)
            {
                subTextures.Add(lTextures[subTexIDs[i]]);
            }

            List<TextureSquare> subSquares = new List<TextureSquare>();

            foreach (KeyValuePair<int, List<int>> pair in lSizes)
            {
                if (pair.Key < lMaxTexSize)
                {
                    if (subSquares.Count == 0)
                    {
                        subSquares.Add(new TextureSquare(lMaxTexSize));
                    }

                    TextureSquare lSquare = subSquares[subSquares.Count - 1];

                    for (int t = 0; t < pair.Value.Count; t++)
                    {
                        bool lAdded = lSquare.AddTexture(lTextures[pair.Value[t]]);

                        if (!lAdded)
                        {
                            subSquares.Add(new TextureSquare(lMaxTexSize));
                            lSquare = subSquares[subSquares.Count - 1];

                            lSquare.AddTexture(lTextures[pair.Value[t]]);
                        }
                    }
                }
            }


            int squareAmount = CeilToBinary(subTextures.Count + subSquares.Count);
            squareAmount = CeilToSqrtable(subTextures.Count + subSquares.Count);
            int simpleAmount = subTextures.Count + subSquares.Count;
            /////////////////////////////Debug.Log(subTextures.Count + " Textures : Sqaures " + subSquares.Count);
            /////////////////////////////Debug.Log(squareAmount);

            if (simpleAmount == 1)
            {
                SetResultTexture(resultSize, resultSize, ref resultTexture);
                SetResultTexture(resultSize, resultSize, ref previewTexture);
                BlitOnResult(subTextures[0], new Vector4(0f, 0f, 1f, 1f), effects, ref resultTexture);
            }
            else
            if (simpleAmount == 2)
            {
                SetResultTexture(resultSize, resultSize, ref resultTexture);
                SetResultTexture(resultSize, resultSize, ref previewTexture);
                if (subTextures.Count == 2)
                {
                    BlitOnResult(subTextures[0], new Vector4(0f, 0f, 0.5f, 1f), effects, ref resultTexture);
                    BlitOnResult(subTextures[1], new Vector4(0.5f, 0f, 0.5f, 1f), effects, ref resultTexture);
                }
                else
                {
                    int t = 0;
                    for (t = 0; t < subTextures.Count; t++)
                    {
                        BlitOnResult(subTextures[t], new Vector4(t * 0.5f, 0f, 0.5f, 1f), effects, ref resultTexture);
                    }
                    for (int s = 0; s < subSquares.Count; s++)
                    {
                        Dictionary<Rect, Texture2D> lTexRects = subSquares[s].GetFittedTextures(new Rect(t * 0.5f, 0f, 0.5f, 1f));
                        foreach (KeyValuePair<Rect, Texture2D> pair in lTexRects)
                        {
                            BlitOnResult(pair.Value, new Vector4(pair.Key.x, pair.Key.y, pair.Key.width, pair.Key.height), effects, ref resultTexture);
                        }
                    }
                }
            }
            else
            {
                int tableSize = (int)Mathf.Sqrt(squareAmount);
                SetResultTexture(resultSize, resultSize, ref resultTexture);
                SetResultTexture(resultSize, resultSize, ref previewTexture);

                int lData = 0;

                for (int tx = 0; tx < tableSize; tx++)
                {
                    for (int ty = 0; ty < tableSize; ty++)
                    {

                        if (lData < subTextures.Count)
                        {
                            BlitOnResult(subTextures[lData], new Vector4((float)tx / (float)tableSize, (float)ty / (float)tableSize, 1f / tableSize, 1f / tableSize), effects, ref resultTexture);
                        }
                        else if ((lData - (subTextures.Count)) < subSquares.Count)
                        {
                            int lDataSquares = lData - (subTextures.Count);
                            Dictionary<Rect, Texture2D> lTexRects = subSquares[lDataSquares].GetFittedTextures(new Rect((float)tx / (float)tableSize, (float)ty / (float)tableSize, 1f / tableSize, 1f / tableSize));
                            foreach (KeyValuePair<Rect, Texture2D> pair in lTexRects)
                            {
                                BlitOnResult(pair.Value, new Vector4(pair.Key.x, pair.Key.y, pair.Key.width, pair.Key.height), effects, ref resultTexture);
                            }
                        }

                        lData++;
                    }
                }
            }

            resultTexture.filterMode = FilterMode.Point;

            for (int t = 0; t < lTextures.Length; t++)
            {
                UnityEditor.Editor.DestroyImmediate(lTextures[t]);
            }


            Material lAlphaGrid = new Material(Shader.Find("Hidden/AtlasGen/AlphaGridShader"));
            Graphics.Blit(resultTexture, previewTexture, lAlphaGrid);

            //////////////////////////////Mesh mesh = new Mesh();
            //////////////////////////////mesh.SetUVs()
            //TODO: MESH VISE
        }

        public static int RoundToBinary(int value)
        {
            int oldValue = 2;
            int newValue = 4;

            while (Mathf.Abs(value - oldValue) > Mathf.Abs(value - newValue))
            {
                oldValue = newValue;
                newValue = newValue * 2;
            }

            return oldValue;
        }

        public static int CeilToBinary(int value)
        {
            int oldValue = 1;
            int newValue = 2;

            while ((oldValue - value) < 0 || Mathf.Abs(value - oldValue) > Mathf.Abs(value - newValue))
            {
                oldValue = newValue;
                newValue = newValue * 2;
            }

            return oldValue;
        }

        public static int CeilToSqrtable(int value)
        {
            float lR = Mathf.Sqrt((float)value);
            return (int)Mathf.Pow(Mathf.Ceil(lR), 2f);//(int)Mathf.Ceil(lR);
        }

        public static Texture2D CopyTexture(Texture2D source)
        {
            Texture2D texCopy = new Texture2D(source.width, source.height, source.format, source.mipmapCount > 1, true);
            texCopy.LoadRawTextureData(source.GetRawTextureData());
            texCopy.Apply();
            return texCopy;
        }

        public static void BlitOnResult(Texture2D texture, Vector4 rect, List<IAtlasGenEffect> effects, ref RenderTexture target)
        {
            Material blitMat = new Material(Shader.Find("Hidden/AtlasGen/BlitToCoord"));
            blitMat.SetVector("_BlitSize", rect);

            RenderTexture lTempRenderTarget = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            RenderTexture lTempBackBuffer = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);

            Graphics.Blit(texture, lTempRenderTarget);

            for (int e = 0; e < effects.Count; e++)
            {
                Graphics.Blit(lTempRenderTarget, lTempBackBuffer);
                effects[e].BlitEffect(lTempBackBuffer, lTempRenderTarget);
            }

            Graphics.Blit(lTempRenderTarget, target, blitMat);

            lTempBackBuffer.Release();
            lTempRenderTarget.Release();

        }

        public static void SetResultTexture(int width, int height, ref RenderTexture texture)
        {
            RenderTexture.active = null;
            texture.DiscardContents();
            texture.Release();

            texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            texture.DiscardContents();

            Material lMat = new Material(Shader.Find("Hidden/AtlasGen/ClearShader"));
            RenderTexture lTempClear = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Graphics.Blit(lTempClear, texture, lMat);
            lTempClear.Release();
        }

        public static Dictionary<int, Rect> CalculateIDPos(List<IDSqaure> squares)
        {
            if (squares.Count <= 0)
            {
                return new Dictionary<int, Rect>();
            }

            Dictionary<int, List<int>> lSizes = new Dictionary<int, List<int>>();

            int lMaxTexSize = 0, lMinTexSize = int.MaxValue;

            for (int t = 0; t < squares.Count; t++)
            {
                int lSize = (int)RoundToBinary((int)squares[t].size);
                if (!lSizes.ContainsKey(lSize))
                {
                    lSizes.Add(lSize, new List<int>());
                }
                lSizes[lSize].Add(t);

                if (lMaxTexSize < squares[t].size)
                {
                    lMaxTexSize = (int)squares[t].size;
                }
                if (lMinTexSize > squares[t].size)
                {
                    lMinTexSize = (int)squares[t].size;
                }
            }

            List<KeyValuePair<int, List<int>>> list = new List<KeyValuePair<int, List<int>>>();

            foreach (KeyValuePair<int, List<int>> pair in lSizes)
            {
                list.Add(pair);
            }

            list.Sort((x, y) => y.Key.CompareTo(x.Key));

            lSizes.Clear();

            foreach (KeyValuePair<int, List<int>> pair in list)
            {
                lSizes.Add(pair.Key, pair.Value);
            }

            string lContents = "";

            foreach (KeyValuePair<int, List<int>> pair in lSizes)
            {
                lContents += pair.Value.Count + "x" + pair.Key + ", ";
            }

            List<IDSqaure> subIDSquares = new List<IDSqaure>();
            List<int> subTexIDs = lSizes[lMaxTexSize];
            for (int i = 0; i < subTexIDs.Count; i++)
            {
                subIDSquares.Add(squares[subTexIDs[i]]);
            }

            List<IDSquareLayouter> subSquares = new List<IDSquareLayouter>();

            foreach (KeyValuePair<int, List<int>> pair in lSizes)
            {
                if (pair.Key < lMaxTexSize)
                {
                    if (subSquares.Count == 0)
                    {
                        subSquares.Add(new IDSquareLayouter(lMaxTexSize));
                    }

                    IDSquareLayouter lSquare = subSquares[subSquares.Count - 1];

                    for (int t = 0; t < pair.Value.Count; t++)
                    {
                        bool lAdded = lSquare.AddSquare(squares[pair.Value[t]]);

                        if (!lAdded)
                        {
                            subSquares.Add(new IDSquareLayouter(lMaxTexSize));
                            lSquare = subSquares[subSquares.Count - 1];

                            lSquare.AddSquare(squares[pair.Value[t]]);
                        }
                    }
                }
            }

            int squareAmount = CeilToBinary(subIDSquares.Count + subSquares.Count);
            squareAmount = CeilToSqrtable(subIDSquares.Count + subSquares.Count);
            int simpleAmount = subIDSquares.Count + subSquares.Count;

            /////////////////////////////Debug.Log(subTextures.Count + " Textures : Sqaures " + subSquares.Count);
            /////////////////////////////Debug.Log(squareAmount);
            Dictionary<int, Rect> lResult = new Dictionary<int, Rect>();

            if (simpleAmount == 1)
            {
                lResult.Add(squares[0].id, new Rect(new Vector2(0f, 0f), new Vector2(1f, 1f)));
                return lResult;
            }
            else
            if (simpleAmount == 2)
            {
                if (subIDSquares.Count == 2)
                {
                    lResult.Add(subIDSquares[0].id, new Rect(new Vector2(0f, 0f), new Vector2(0.5f, 1f)));
                    lResult.Add(subIDSquares[1].id, new Rect(new Vector2(0.5f, 0f), new Vector2(0.5f, 1f)));
                    return lResult;
                }
                else
                {
                    int t = 0;
                    for (t = 0; t < subIDSquares.Count; t++)
                    {
                        lResult.Add(squares[t].id, new Rect(new Vector4(t * 0.5f, 0f), new Vector2(0.5f, 1f)));
                    }
                    for (int s = 0; s < subSquares.Count; s++)
                    {
                        Dictionary<int, Rect> lTexRects = subSquares[s].GetFittedIDSquares(new Rect(t * 0.5f, 0f, 0.5f, 1f));
                        foreach (KeyValuePair<int, Rect> cSquare in lTexRects)
                        {
                            lResult.Add(cSquare.Key, cSquare.Value);
                        }
                    }
                    return lResult;
                }
            }
            else
            {
                int tableSize = (int)Mathf.Sqrt(squareAmount);
                int lData = 0;

                for (int tx = 0; tx < tableSize; tx++)
                {
                    for (int ty = 0; ty < tableSize; ty++)
                    {
                        if (lData < subIDSquares.Count)
                        {
                            lResult.Add(subIDSquares[lData].id, new Rect((float)tx / (float)tableSize, (float)ty / (float)tableSize, 1f / tableSize, 1f / tableSize));
                        }
                        else if ((lData - (subIDSquares.Count)) < subSquares.Count)
                        {
                            int lDataSquares = lData - (subIDSquares.Count);
                            Dictionary<int, Rect> lTexRects = subSquares[lDataSquares].GetFittedIDSquares(new Rect((float)tx / (float)tableSize, (float)ty / (float)tableSize, 1f / tableSize, 1f / tableSize));
                            foreach (KeyValuePair<int, Rect> cSquare in lTexRects)
                            {
                                lResult.Add(cSquare.Key, cSquare.Value);
                            }
                        }

                        lData++;
                    }
                }
                return lResult;
            }
        }
    }
}