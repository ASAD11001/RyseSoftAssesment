using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ShapeBlueprint
{
    // Unique ID for the item
    public string itemID;

    // Name displayed in UI
    public string displayName;

    // If this list is EMPTY, it's a Simple Shape.
    // If this list has content, it's a Welded Shape.
    public List<WeldPartData> children = new List<WeldPartData>();

    public ShapeBlueprint(string id, string name)
    {
        this.itemID = id;
        this.displayName = name;
    }
}

[System.Serializable]
public struct WeldPartData
{
    public ShapeBlueprint partBlueprint;
    public Vector3 localPos;
    public Quaternion localRot;
    public Vector3 localScale;
}
