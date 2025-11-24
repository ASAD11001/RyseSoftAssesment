using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "System/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [System.Serializable]
    public struct ItemEntry
    {
        public string id;
        public GameObject prefab;
        public Sprite icon;
    }

    public List<ItemEntry> allItems;

    // Helper Dictionary for fast lookup
    private Dictionary<string, GameObject> _lookup;

    public void Initialize()
    {
        _lookup = new Dictionary<string, GameObject>();
        foreach (var item in allItems)
        {
            if (!_lookup.ContainsKey(item.id))
                _lookup.Add(item.id, item.prefab);
        }
    }

    public GameObject GetPrefab(string id)
    {
        if (_lookup == null) Initialize();

        if (_lookup.TryGetValue(id, out GameObject prefab))
            return prefab;

        Debug.LogError($"Item ID not found: {id}");
        return null;
    }
}
