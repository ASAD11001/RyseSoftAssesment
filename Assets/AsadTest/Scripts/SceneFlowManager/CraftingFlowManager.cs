using UnityEngine;
using System.Collections;
using UnityEditor;
using System.IO;

public class CraftingFlowManager : MonoBehaviour
{
    public enum CraftingState { Idle, MovingCamera, PlacingObjectA, PlacingObjectB, ReadyToWeld }

    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject gameplayPanel;
    public GameObject ObjectPlacementPanel;
    public GameObject ObjectSavingPanel;

    [Header("Scene References")]
    public MainMenuUI mainMenuUI;
    public BlueprintSpawner spawner;
    public Transform spawnPoint;
    public Camera mainCamera;
    public Transform craftingCameraView;
    public Transform weldingCameraView;
    public GameObject weldingWedge;

    [Header("Snapshot Settings")]
    public Camera snapshotCamera;
    public int iconResolution = 256;

    [Header("Settings")]
    public float moveSpeed = 5f;
    public float rotateSpeed = 100f;
    public float cameraTransitionDuration = 1.5f;

    // Internal State
    private CraftingState currentState = CraftingState.Idle;
    private ShapeBlueprint bpA;
    private ShapeBlueprint bpB;
    private GameObject objA;
    private GameObject objB;

    
    private Vector3 originalCamPos;
    private Quaternion originalCamRot;

    // =========================================================
    // 1. INITIALIZATION & CAMERA MOVE
    // =========================================================

    public void StartCraftingSession(ShapeBlueprint itemA, ShapeBlueprint itemB)
    {
        bpA = itemA;
        bpB = itemB;

        // Save original camera spot
        originalCamPos = mainCamera.transform.position;
        originalCamRot = mainCamera.transform.rotation;

        
        mainMenuPanel.SetActive(false);
        gameplayPanel.SetActive(true);

        
        StartCoroutine(MoveCameraRoutine());
    }

    private IEnumerator MoveCameraRoutine()
    {
        currentState = CraftingState.MovingCamera;
        float elapsed = 0f;
        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;

        while (elapsed < cameraTransitionDuration)
        {
            float t = elapsed / cameraTransitionDuration;
            // Smooth "Ease In Out" curve
            t = t * t * (3f - 2f * t);

            mainCamera.transform.position = Vector3.Lerp(startPos, craftingCameraView.position, t);
            mainCamera.transform.rotation = Quaternion.Lerp(startRot, craftingCameraView.rotation, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Snap to final position
        mainCamera.transform.position = craftingCameraView.position;
        mainCamera.transform.rotation = craftingCameraView.rotation;

        ObjectPlacementPanel.SetActive(true);

        
        SpawnObjectA();
    }

    // =========================================================
    // 2. SPAWNING LOGIC
    // =========================================================

    private void SpawnObjectA()
    {
        currentState = CraftingState.PlacingObjectA;
        // Spawn A at spawnPoint
        objA = spawner.Spawn(bpA, spawnPoint.position, Quaternion.identity, null);
    }

    private void SpawnObjectB()
    {
        currentState = CraftingState.PlacingObjectB;
        // Spawn B at spawnPoint
        objB = spawner.Spawn(bpB, spawnPoint.position, Quaternion.identity, null);
    }

    // =========================================================
    // 3. INPUT LOGIC (Update Loop)
    // =========================================================

    void Update()
    {
        
        if (currentState == CraftingState.PlacingObjectA || currentState == CraftingState.PlacingObjectB)
        {
            HandleMovementInput();
        }
    }

    void HandleMovementInput()
    {
        // Determine which object we are currently controlling
        GameObject activeObj = (currentState == CraftingState.PlacingObjectA) ? objA : objB;

        if (activeObj == null) return;

        float dt = Time.deltaTime;

        // --- TRANSLATION (Movement) ---
        Vector3 moveDir = Vector3.zero;

        if (Input.GetKey(KeyCode.D)) moveDir += Vector3.forward;
        if (Input.GetKey(KeyCode.A)) moveDir += Vector3.back;
        if (Input.GetKey(KeyCode.S)) moveDir += Vector3.right;
        if (Input.GetKey(KeyCode.W)) moveDir += Vector3.left;
        if (Input.GetKey(KeyCode.Q)) moveDir += Vector3.up;
        if (Input.GetKey(KeyCode.E)) moveDir += Vector3.down;

        // Apply Movement (World Space)
        activeObj.transform.Translate(moveDir * moveSpeed * dt, Space.World);

        // --- ROTATION ---
        if (Input.GetKeyDown(KeyCode.Z))
        {
            activeObj.transform.Rotate(Vector3.forward, 90f);
        }
        if (Input.GetKeyDown(KeyCode.X))
        {
            activeObj.transform.Rotate(Vector3.right, 90f);
        }
        if (Input.GetKeyDown(KeyCode.Y))
        {
            activeObj.transform.Rotate(Vector3.up, 90f);
        }
    }

    // =========================================================
    // 4. UI BUTTON LISTENERS
    // =========================================================

    public void OnDoneButtonClicked()
    {
        switch (currentState)
        {
            case CraftingState.PlacingObjectA:
                SpawnObjectB();
                break;

            case CraftingState.PlacingObjectB:
                //Start the transition to Welding.
                StartCoroutine(MoveToWeldingState());
                break;
        }
    }

    private IEnumerator MoveToWeldingState()
    {
        
        currentState = CraftingState.ReadyToWeld;

        float elapsed = 0f;
        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;

        // 3. Move Camera Closer
        while (elapsed < cameraTransitionDuration)
        {
            float t = elapsed / cameraTransitionDuration;
            t = t * t * (3f - 2f * t);

            mainCamera.transform.position = Vector3.Lerp(startPos, weldingCameraView.position, t);
            mainCamera.transform.rotation = Quaternion.Lerp(startRot, weldingCameraView.rotation, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Snap to final close-up position
        mainCamera.transform.position = weldingCameraView.position;
        mainCamera.transform.rotation = weldingCameraView.rotation;

        ObjectPlacementPanel.SetActive(false);
        ObjectSavingPanel.SetActive(true);

        // 4. Activate the Wedge
        if (weldingWedge != null)
        {
            weldingWedge.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Wedge object is not assigned in Inspector!");
        }

        
        Debug.Log("Camera moved. Wedge Active. Ready for final Weld.");
    }

    private void CaptureAndSaveIcon(string itemID)
    {
        // 1. Setup RenderTexture
        RenderTexture rt = new RenderTexture(iconResolution, iconResolution, 24);
        snapshotCamera.targetTexture = rt;
        Texture2D screenShot = new Texture2D(iconResolution, iconResolution, TextureFormat.RGBA32, false);

        Vector3 centerPoint = (objA.transform.position + objB.transform.position) / 2f;

        // 3. Render
        snapshotCamera.gameObject.SetActive(true);
        snapshotCamera.Render();

        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, iconResolution, iconResolution), 0, 0);
        screenShot.Apply();

        // 4. Cleanup
        snapshotCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);
        snapshotCamera.gameObject.SetActive(false);

        // 5. Save to Disk
        byte[] bytes = screenShot.EncodeToPNG();
        string path = Application.persistentDataPath + "/" + itemID + ".png";
        File.WriteAllBytes(path, bytes);

        Debug.Log($"Saved icon snapshot to: {path}");
    }

    public void FinishAndSave()
    {
        // Safety Check
        if (objA == null || objB == null)
        {
            Debug.LogError("Error: Objects missing. Cannot Save.");
            return;
        }

        // 1. Generate ID and Name
        string newID = System.Guid.NewGuid().ToString(); // Random Unique ID
        string newName = $"Welded {bpA.displayName}-{bpB.displayName}";

        // Create the Container Blueprint
        ShapeBlueprint newWeld = new ShapeBlueprint(newID, newName);

        // -------------------------------------------------------
        // PART A: The Anchor
        // We treat Object A as the "Center" (0,0,0) of this new creation.
        // -------------------------------------------------------
        WeldPartData partA = new WeldPartData();
        partA.partBlueprint = bpA; // Keep the original data
        partA.localPos = Vector3.zero;
        partA.localRot = Quaternion.identity;
        partA.localScale = Vector3.one;

        newWeld.children.Add(partA);

        // -------------------------------------------------------
        // PART B: The Attachment
        // We calculate where B is RELATIVE to A.
        // -------------------------------------------------------
        WeldPartData partB = new WeldPartData();
        partB.partBlueprint = bpB; // Keep the original data

        // MATH: "If A is the center of the world, where is B?"
        partB.localPos = objA.transform.InverseTransformPoint(objB.transform.position);

        // MATH: "What is the rotation difference?"
        partB.localRot = Quaternion.Inverse(objA.transform.rotation) * objB.transform.rotation;

        // MATH: Keep B's scale
        partB.localScale = objB.transform.localScale;

        newWeld.children.Add(partB);

        CaptureAndSaveIcon(newID);

        // -------------------------------------------------------
        // SAVE TO INVENTORY
        // -------------------------------------------------------
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddToCraftingPile(newWeld);
            Debug.Log($"<color=green>SUCCESS:</color> Saved {newName} to Crafting Pile!");
        }
        else
        {
            Debug.LogError("InventoryManager Instance not found!");
        }

        // 3. Reset the Game Loop
        CleanupSession();
    }

    private void CleanupSession()
    {
        // 1. Destroy Scene Objects
        if (objA != null) Destroy(objA);
        if (objB != null) Destroy(objB);

        // 2. Hide Wedge
        if (weldingWedge != null) weldingWedge.SetActive(false);

        // 3. Reset Camera
        mainCamera.transform.position = originalCamPos;
        mainCamera.transform.rotation = originalCamRot;

        // 4. Reset UI
        gameplayPanel.SetActive(false);
        ObjectSavingPanel.SetActive(false);
        mainMenuPanel.SetActive(true);

        mainMenuUI.RefreshPile();
        mainMenuUI.RefreshInventory();

        // 5. Reset State
        currentState = CraftingState.Idle;
    }
}