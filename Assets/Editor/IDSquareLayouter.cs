using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IDSquareLayouter : MonoBehaviour {

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

    public List<IDSqaure> GetFittedIDSquares(Rect pos)
    {
        List<IDSqaure> lSquares = new List<IDSqaure>();
        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                Rect lSubRect = new Rect(pos.x + (x * (pos.width / 2f)), pos.y + (y * (pos.height / 2f)), pos.width / 2f, pos.height / 2f);
                if (subIDSquares[x, y] != null)
                {
                    IDSqaure lIDSquare = new IDSqaure(lSubRect.position, (int)lSubRect.width, subIDSquares[x, y].id);
                    lSquares.Add(subIDSquares[x, y]);
                }
                else if (subSquares[x, y] != null)
                {
                    List<IDSqaure> lSubSquares = new List<IDSqaure>();
                    lSubSquares = subSquares[x, y].GetFittedIDSquares(lSubRect);

                    foreach (IDSqaure subSquare in lSubSquares)
                    {
                        lSquares.Add(subSquare);
                    }
                }
            }
        }
        return lSquares;
    }
}
