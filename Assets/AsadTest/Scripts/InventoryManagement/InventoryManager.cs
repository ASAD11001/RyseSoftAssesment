using UnityEngine;
using System.Collections.Generic;
using System.IO; 

public class InventoryManager : MonoBehaviour
{
    
    public static InventoryManager Instance;

    [Header("Configuration")]
    public ItemDatabase database;

    [Header("Live Data")]
    public List<ShapeBlueprint> mainInventory = new List<ShapeBlueprint>();
    public List<ShapeBlueprint> craftingPile = new List<ShapeBlueprint>();

    // We store the file path here so we don't calculate it every frame
    private string savePath;

    void Awake()
    {
        
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            database.Initialize();

            savePath = Application.persistentDataPath + "/inventory_data.json";

            LoadData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    
    public void AddToCraftingPile(ShapeBlueprint newItem)
    {
        craftingPile.Add(newItem);
        SaveData();
    }

    public void MoveFromPileToInventory(ShapeBlueprint item)
    {
        // 1. Check if the item is actually in the pile
        if (craftingPile.Contains(item))
        {
            // 2. Remove from Pile
            craftingPile.Remove(item);

            // 3. Add to Main Inventory
            mainInventory.Add(item);

            // 4. Save changes to Disk immediately
            SaveData();

            Debug.Log($"Moved {item.displayName} to Inventory.");
        }
    }

    // --- Save System (JSON Logic) ---

    [ContextMenu("Save")]
    public void SaveData()
    {
        SaveContainer container = new SaveContainer();
        container.inventory = mainInventory;
        container.pile = craftingPile;

        string json = JsonUtility.ToJson(container, true);

        File.WriteAllText(savePath, json);
        Debug.Log("Saved data to: " + savePath);
    }

    [ContextMenu("Load")]
    public void LoadData()
    {
        if (File.Exists(savePath))
        {
            // Existing Save Found: Load it as normal
            string json = File.ReadAllText(savePath);
            SaveContainer container = JsonUtility.FromJson<SaveContainer>(json);

            mainInventory = container.inventory ?? new List<ShapeBlueprint>();
            craftingPile = container.pile ?? new List<ShapeBlueprint>();
        }
        else
        {
            // === NEW PLAYER LOGIC ===

            Debug.Log("New Game detected. Populating inventory from Database...");

            foreach (var dbItem in database.allItems)
            {
                
                ShapeBlueprint newItem = new ShapeBlueprint(dbItem.id, dbItem.prefab.name);

                mainInventory.Add(newItem);
            }
            SaveData();
        }
    }

    public void ClearCraftingPile()
    {
        
        craftingPile.Clear();
        SaveData();

        Debug.Log("Crafting Pile has been emptied and saved.");
    }


}

// Simple wrapper class because JsonUtility needs a class to hold the lists
[System.Serializable]
public class SaveContainer
{
    public List<ShapeBlueprint> inventory;
    public List<ShapeBlueprint> pile;
}