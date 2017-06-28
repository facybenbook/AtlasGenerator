using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Northwind.AtlasGen
{
    public class TextureSquare
    {

        private List<Texture2D> textures = new List<Texture2D>();

        private int size;
        private int subSize
        {
            get
            {
                return size / 2;
            }
        }

        private Texture2D[,] subTextures = new Texture2D[2, 2];
        private TextureSquare[,] subSquares = new TextureSquare[2, 2];

        public TextureSquare(int squareSize)
        {
            size = RoundToBinary(squareSize);
        }

        public bool AddTexture(Texture2D texture)
        {
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    if (subTextures[x, y] == null)
                    {
                        if (subSize > RoundToBinary(texture.width))
                        {
                            if (subSquares[x, y] == null)
                            {
                                subSquares[x, y] = new TextureSquare(subSize);
                            }
                            bool lAdded = subSquares[x, y].AddTexture(texture);
                            if (lAdded)
                            {
                                return true;
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else if (subSquares[x, y] == null)
                        {
                            textures.Add(texture);
                            subTextures[x, y] = texture;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        int RoundToBinary(float value)
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

        public Dictionary<Rect, Texture2D> GetFittedTextures(Rect pos)
        {
            Dictionary<Rect, Texture2D> lTextures = new Dictionary<Rect, Texture2D>();
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    Rect lSubRect = new Rect(pos.x + (x * (pos.width / 2f)), pos.y + (y * (pos.height / 2f)), pos.width / 2f, pos.height / 2f);
                    if (subTextures[x, y] != null)
                    {
                        lTextures.Add(lSubRect, subTextures[x, y]);
                    }
                    else if (subSquares[x, y] != null)
                    {
                        Dictionary<Rect, Texture2D> lSubTextures = new Dictionary<Rect, Texture2D>();
                        lSubTextures = subSquares[x, y].GetFittedTextures(lSubRect);

                        foreach (KeyValuePair<Rect, Texture2D> subTexture in lSubTextures)
                        {
                            lTextures.Add(subTexture.Key, subTexture.Value);
                        }
                    }
                }
            }
            return lTextures;
        }

    }
}