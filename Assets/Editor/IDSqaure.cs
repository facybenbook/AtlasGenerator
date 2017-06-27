using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IDSqaure {

    private float mySize;
    public float size
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

    public IDSqaure(Vector2 aPos, float aSize, int aID)
    {
        myPos = aPos;
        mySize = aSize;
        myID = aID;
    }
}
