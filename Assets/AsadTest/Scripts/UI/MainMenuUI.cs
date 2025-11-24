using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MainMenuUI : MonoBehaviour
{
    [Header("Managers")]
    public CraftingFlowManager flowManager;

    [Header("UI Containers")]
    public Transform inventoryContainer;
    public Transform craftingPileContainer;
    public GameObject buttonPrefab;

    [Header("Inventory Controls (Crafting)")]
    public Button craftButton;
    private List<InventoryUIButton> selectedInventoryButtons = new List<InventoryUIButton>();

    [Header("Pile Controls (Claiming)")]
    public Button claimButton;
    private InventoryUIButton selectedPileButton;

    void Start()
    {
        // 1. Setup Craft Button
        craftButton.interactable = false;
        craftButton.onClick.RemoveAllListeners();
        craftButton.onClick.AddListener(OnCraftButtonClicked);

        // 2. Setup Claim Button
        claimButton.interactable = false;
        claimButton.onClick.RemoveAllListeners();
        claimButton.onClick.AddListener(OnClaimButtonClicked);

        RefreshInventory();
        RefreshPile();
    }

    // =========================================================
    // LOGIC
    // =========================================================

    public void RefreshInventory()
    {
        foreach (Transform child in inventoryContainer) Destroy(child.gameObject);

        if (InventoryManager.Instance != null)
        {
            foreach (ShapeBlueprint item in InventoryManager.Instance.mainInventory)
            {
                // Inventory Click -> Multi-Selection Logic
                SpawnButton(item, inventoryContainer, (btnScript) => {
                    OnInventoryItemClicked(btnScript);
                });
            }
        }
    }

    public void RefreshPile()
    {
        // Reset selection when refreshing
        selectedPileButton = null;
        claimButton.interactable = false;

        foreach (Transform child in craftingPileContainer) Destroy(child.gameObject);

        if (InventoryManager.Instance != null)
        {
            foreach (ShapeBlueprint item in InventoryManager.Instance.craftingPile)
            {
                // Pile Click -> Single-Selection Logic
                SpawnButton(item, craftingPileContainer, (btnScript) => {
                    OnPileItemClicked(btnScript);
                });
            }
        }
    }

    private void SpawnButton(ShapeBlueprint item, Transform container, System.Action<InventoryUIButton> onClickAction)
    {
        GameObject newBtn = Instantiate(buttonPrefab, container);
        InventoryUIButton script = newBtn.GetComponent<InventoryUIButton>();

        // Find Icon (Custom Snapshot or Database Default)
        Sprite icon = GetIconFromDatabase(item.itemID);
        script.GetComponent<Image>().sprite = icon;

        script.Setup(item, icon, () => onClickAction(script));
    }

    private Sprite GetIconFromDatabase(string id)
    {
        if (InventoryManager.Instance == null) return null;

        // Check for snapshot first
        string path = Application.persistentDataPath + "/" + id + ".png";
        if (System.IO.File.Exists(path))
        {
            byte[] fileData = System.IO.File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(fileData);
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }

        // Fallback to database
        foreach (var entry in InventoryManager.Instance.database.allItems)
        {
            if (entry.id == id) return entry.icon;
        }

        // Fallback for welded items without snapshots (check children)
        ShapeBlueprint bp = InventoryManager.Instance.craftingPile.Find(b => b.itemID == id);
        if (bp != null && bp.children.Count > 0) return GetIconFromDatabase(bp.children[0].partBlueprint.itemID);

        return null;
    }

    // =========================================================
    // INVENTORY SELECTION (Max 2)
    // =========================================================

    void OnInventoryItemClicked(InventoryUIButton btnScript)
    {
        if (selectedInventoryButtons.Contains(btnScript))
        {
            selectedInventoryButtons.Remove(btnScript);
            btnScript.SetSelected(false);
        }
        else
        {
            if (selectedInventoryButtons.Count < 2)
            {
                selectedInventoryButtons.Add(btnScript);
                btnScript.SetSelected(true);
            }
        }
        craftButton.interactable = (selectedInventoryButtons.Count == 2);
    }

    void OnCraftButtonClicked()
    {
        if (selectedInventoryButtons.Count == 2)
        {
            ShapeBlueprint itemA = selectedInventoryButtons[0].itemData;
            ShapeBlueprint itemB = selectedInventoryButtons[1].itemData;

            foreach (var btn in selectedInventoryButtons) btn.SetSelected(false);
            selectedInventoryButtons.Clear();
            craftButton.interactable = false;

            flowManager.StartCraftingSession(itemA, itemB);
        }
    }

    // =========================================================
    // PILE SELECTION (Max 1) & MOVING
    // =========================================================

    void OnPileItemClicked(InventoryUIButton btnScript)
    {
        // Case A: Clicking the ALREADY selected item -> Deselect it
        if (selectedPileButton == btnScript)
        {
            btnScript.SetSelected(false);
            selectedPileButton = null;
        }
        // Case B: Clicking a NEW item
        else
        {
            // 1. Deselect the old one (if any)
            if (selectedPileButton != null)
            {
                selectedPileButton.SetSelected(false);
            }

            // 2. Select the new one
            selectedPileButton = btnScript;
            btnScript.SetSelected(true);
        }

        // Enable button only if we have a selection
        claimButton.interactable = (selectedPileButton != null);
    }

    void OnClaimButtonClicked()
    {
        if (selectedPileButton != null)
        {
            // 1. Move Data
            InventoryManager.Instance.MoveFromPileToInventory(selectedPileButton.itemData);

            // 2. Clear Selection
            selectedPileButton = null;
            claimButton.interactable = false;

            // 3. Refresh UI
            RefreshPile();
            RefreshInventory(); 
        }
    }

    public void OnClearPileButtonClicked()
    {
        //Call the Manager to delete the actual data
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ClearCraftingPile();
        }

        //Refresh the UI
        RefreshPile();

        // Reset selection state
        selectedPileButton = null;
        claimButton.interactable = false;
    }
}