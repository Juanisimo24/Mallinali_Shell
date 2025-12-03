using TMPro;
using Unity.Cinemachine;
using UnityEngine;

public class TransitionPoint : MonoBehaviour, IInteract
{
    [Header("Configuration")]
    [SerializeField] private bool isLocked = true; // Starts locked
    [SerializeField] private Transform destinationPoint;
    
    [Header("Camera & Bounds")]
    [SerializeField] private PolygonCollider2D targetMapBoundry;
    private CinemachineConfiner2D confiner;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI interactText;
    [SerializeField] private string lockedMessage = "Sealed by Guardian";
    [SerializeField] private string openMessage = "Press E to Enter";

    // Internal State
    private bool playerInRange;
    private bool haveText;

    private void Awake()
    {
        confiner = FindFirstObjectByType<CinemachineConfiner2D>();
        haveText = interactText != null;
        if (haveText) interactText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (canInteract())
        {
            beInteracted();
        }
    }

    // --- INTERACTION LOGIC ---
    public bool canInteract()
    {
        // Only allow interaction if In Range, Button Pressed, AND NOT LOCKED
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            if (isLocked)
            {
                // Optional: Play a "Locked" sound or shake effect
                Debug.Log("The portal is sealed.");
                return false;
            }
            return true;
        }
        return false;
    }

    public void beInteracted()
    {
        Debug.Log("Teleporting to Zone 3...");
        
        // 1. Update Camera Bounds
        if (confiner != null && targetMapBoundry != null)
        {
            confiner.BoundingShape2D = targetMapBoundry;
        }

        // 2. Teleport BOTH Characters (Crucial for your dual system)
        if (PlayerManagerDual.Instance != null)
        {
            GameObject active = PlayerManagerDual.Instance.GetActive();
            GameObject follower = PlayerManagerDual.Instance.GetFollower();

            if (active) 
            {
                active.transform.position = destinationPoint.position;
                // Reset physics to stop sliding
                if(active.GetComponent<Rigidbody2D>()) active.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            }
            
            if (follower) 
            {
                // Teleport follower slightly behind active so they don't stack
                follower.transform.position = destinationPoint.position; 
                if(follower.GetComponent<Rigidbody2D>()) follower.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            }
        }

        // 3. UI Cleanup
        playerInRange = false;
        if (haveText) interactText.gameObject.SetActive(false);
    }

    // --- EXTERNAL ACCESS ---
    public void UnlockPortal()
    {
        isLocked = false;
        Debug.Log("Portal Unlocked!");
        
        // Optional: Change sprite color to show it's active
        GetComponent<SpriteRenderer>().color = Color.cyan; 

        // Update text if player is currently standing there
        if (playerInRange && haveText) interactText.text = openMessage;
    }

    // --- TRIGGER EVENTS ---
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            if (haveText)
            {
                interactText.text = isLocked ? lockedMessage : openMessage;
                interactText.gameObject.SetActive(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (haveText) interactText.gameObject.SetActive(false);
        }
    }
}