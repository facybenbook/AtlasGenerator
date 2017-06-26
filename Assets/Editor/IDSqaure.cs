using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IDSqaure {

    private int mySize;
    public int size
    {
        get
        {
            return mySize;
        }
    }

    private Vector2 myPos;
    public Vector2 pos
    {
        get
        {
            return myPos;
        }
    }

    private int myID;
    public int id
    {
        get
        {
            return myID;
        }
    }

    public IDSqaure(Vector2 aPos, int aSize, int aID)
    {
        myPos = aPos;
        mySize = aSize;
        myID = aID;
    }
}
