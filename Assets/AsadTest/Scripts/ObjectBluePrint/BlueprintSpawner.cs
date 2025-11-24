using UnityEngine;

public class BlueprintSpawner : MonoBehaviour
{
    [Header("Dependencies")]
    public ItemDatabase database;

    // This is the function CraftingFlowManager calls
    public GameObject Spawn(ShapeBlueprint blueprint, Vector3 position, Quaternion rotation, Transform parent)
    {
        // 1. Create a root container (Empty GameObject)
        // This holds the entire object (whether it's 1 cube or 10 welded parts)
        GameObject rootObj = new GameObject(blueprint.displayName);
        rootObj.transform.position = position;
        rootObj.transform.rotation = rotation;
        rootObj.transform.SetParent(parent);

        
        BuildRecursive(rootObj.transform, blueprint);

        return rootObj;
    }

    // This function calls itself to handle nested/welded parts
    private void BuildRecursive(Transform parent, ShapeBlueprint bp)
    {
        // STEP A: Is this a basic item found in the database?
        GameObject prefab = database.GetPrefab(bp.itemID);

        if (prefab != null)
        {
            // Instantiate the visual model
            GameObject visual = Instantiate(prefab, parent);
            SetLayerRecursively(visual, LayerMask.NameToLayer("CraftingObjects"));

            // Reset transforms so it sits perfectly inside the parent
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

        }

        // STEP B: Does this item have children? (Is it a Welded Shape?)
        foreach (var part in bp.children)
        {
            // Create a holder for this specific part
            GameObject partObj = new GameObject(part.partBlueprint.displayName);
            partObj.transform.SetParent(parent);

            // Apply the saved offsets (Position/Rotation relative to parent)
            partObj.transform.localPosition = part.localPos;
            partObj.transform.localRotation = part.localRot;
            partObj.transform.localScale = part.localScale;

            // Recursively build this part
            BuildRecursive(partObj.transform, part.partBlueprint);
        }
    }

    void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (newLayer < 0) return;

        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
}