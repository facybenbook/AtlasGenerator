using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Northwind.AtlasGen
{
    public class IDSquareLayouter
    {

        private List<IDSqaure> squares = new List<IDSqaure>();

        private int size;
        private int subSize
        {
            get
            {
                return size / 2;
            }
        }

        private IDSqaure[,] subIDSquares = new IDSqaure[2, 2];
        private IDSquareLayouter[,] subSquares = new IDSquareLayouter[2, 2];

        public IDSquareLayouter(int squareSize)
        {
            size = RoundToBinary(squareSize);
        }

        public bool AddSquare(IDSqaure square)
        {
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    if (subIDSquares[x, y] == null)
                    {
                        if (subSize > RoundToBinary(square.size))
                        {
                            if (subSquares[x, y] == null)
                            {
                                subSquares[x, y] = new IDSquareLayouter(subSize);
                            }
                            bool lAdded = subSquares[x, y].AddSquare(square);
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
                            squares.Add(square);
                            subIDSquares[x, y] = square;
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

        public Dictionary<int, Rect> GetFittedIDSquares(Rect pos)
        {
            Dictionary<int, Rect> lSquares = new Dictionary<int, Rect>();
            int subIDs = 0;
            int subS = 0;
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    Rect lSubRect = new Rect(pos.x + (x * (pos.width / 2f)), pos.y + (y * (pos.height / 2f)), pos.width / 2f, pos.height / 2f);
                    if (subIDSquares[x, y] != null)
                    {
                        lSquares.Add(subIDSquares[x, y].id, lSubRect);
                        subIDs++;
                    }
                    else if (subSquares[x, y] != null)
                    {
                        subS++;
                        Dictionary<int, Rect> lSubSquares = new Dictionary<int, Rect>();
                        lSubSquares = subSquares[x, y].GetFittedIDSquares(lSubRect);

                        foreach (KeyValuePair<int, Rect> subSquare in lSubSquares)
                        {
                            lSquares.Add(subSquare.Key, subSquare.Value);
                        }
                    }
                }
            }
            return lSquares;
        }
    }
}