using UnityEngine;

[RequireComponent(typeof(AudioSource))] 
public class WeldingWedge : MonoBehaviour
{
    [Header("Visual Effects")]
    public ParticleSystem weldingSparks;
    public string targetTag = "WeldZone";

    [Header("Movement Settings")]
    public float movementSmoothness = 20f;
    public float defaultDepth = 2.0f;

    [Header("Snapping Precision")]
    public Transform wedgeTipReference;

    [Header("Layer Config")]
    public LayerMask weldableLayers;

    private Camera cam;
    private AudioSource weldAudio;
    private bool isWelding = false;

    void OnEnable()
    {
        cam = Camera.main;

        // Get the Audio Source attached to this object
        weldAudio = GetComponent<AudioSource>();

        // Reset Effects
        if (weldingSparks != null) weldingSparks.Stop();
        if (weldAudio != null) weldAudio.Stop();

        if (wedgeTipReference == null)
        {
            Debug.LogError("CRITICAL: Assign 'Wedge Tip Reference' in Inspector!");
            enabled = false;
        }
    }

    void OnDisable()
    {
        // Safety: Stop sound if the object gets turned off
        if (weldAudio != null) weldAudio.Stop();
    }

    void Update()
    {
        HandleSnappingMovement();
    }

    void HandleSnappingMovement()
    {
        if (cam == null || wedgeTipReference == null) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        Vector3 targetPos;

        if (Physics.Raycast(ray, out hit, 100f, weldableLayers, QueryTriggerInteraction.Collide))
        {
            // Calculate Pivot Offset based on Tip location
            Vector3 pivotToTipVector = wedgeTipReference.position - transform.position;
            targetPos = hit.point - pivotToTipVector;
        }
        else
        {
            // Air Float Logic
            Vector3 pivotToTipVector = wedgeTipReference.position - transform.position;
            targetPos = ray.GetPoint(defaultDepth) - pivotToTipVector;
        }

        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * movementSmoothness);
    }

    // =========================================================
    // COLLISION & AUDIO LOGIC
    // =========================================================

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            isWelding = true;

            // 1. Play Particles
            if (weldingSparks != null && !weldingSparks.isPlaying)
            {
                weldingSparks.Play();
            }

            // 2. Play Sound
            if (weldAudio != null && !weldAudio.isPlaying)
            {
                weldAudio.Play();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            isWelding = false;

            // 1. Stop Particles
            if (weldingSparks != null)
            {
                weldingSparks.Stop();
            }

            // 2. Stop Sound
            if (weldAudio != null)
            {
                weldAudio.Stop();
            }
        }
    }
}