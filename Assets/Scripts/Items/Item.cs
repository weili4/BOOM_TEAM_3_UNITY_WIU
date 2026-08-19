using UnityEngine;

public class Item : MonoBehaviour
{
    public ItemData itemData;
    public ItemEffect itemEffect;

    public float magnetRadius = 4.0f;
    public float loseMagnetDistance = 6.5f; // lose magnet if player runs too far
    public float magnetSpeed = 12.0f;
    public AudioClip pickupSound;

    private Transform playerTransform;
    private bool isMagnetized = false;
    private Vector3 initialPosition;
    private SpriteRenderer spriteRenderer;

    private void Start()
    {
        initialPosition = transform.position;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && itemData != null)
        {
            spriteRenderer.sprite = itemData.itemImage;
        }

        // find active leader on spawn
        UpdatePlayerTarget();
    }

    private void Update()
    {
        // always track only the current active leader and ignore followers
        UpdatePlayerTarget();

        if (playerTransform == null || itemData == null || itemEffect == null) return;

        float dist = Vector2.Distance(transform.position, playerTransform.position);

        // check magnet range
        if (dist <= magnetRadius)
        {
            isMagnetized = true;
        }
        else if (dist > loseMagnetDistance)
        {
            isMagnetized = false;
        }

        if (isMagnetized)
        {
            transform.position = Vector3.MoveTowards(transform.position, playerTransform.position, magnetSpeed * Time.deltaTime);

            if (dist < 0.5f)
            {
                CollectItem();
            }
        }
        else
        {
            // return to spawn position if player moves away
            if (Vector3.Distance(transform.position, initialPosition) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(transform.position, initialPosition, magnetSpeed * 0.5f * Time.deltaTime);
            }
        }
    }

    // dynamically gets the active party leader from partymanager
    private void UpdatePlayerTarget()
    {
        if (PartyManager.Instance != null && PartyManager.Instance.ActivePlayerObj != null)
        {
            playerTransform = PartyManager.Instance.ActivePlayerObj.transform;
        }
        else if (playerTransform == null || !playerTransform.CompareTag("Player"))
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }
    }

    private void CollectItem()
    {
        Inventory inventory = null;
        if (playerTransform != null)
        {
            inventory = playerTransform.GetComponent<Inventory>();
        }

        if (inventory == null)
        {
            inventory = FindFirstObjectByType<Inventory>();
        }

        if (inventory != null)
        {
            /*===========================================================================================================*/
            // Check if the is max amount dont pick up and destroy
            if (inventory.AddItemStack(itemData, itemEffect))
            {
                if (pickupSound != null)
                {
                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlaySFX(pickupSound, transform.position);
                    else
                        AudioSource.PlayClipAtPoint(pickupSound, transform.position);
                }

                Destroy(gameObject);
            }
            /*===========================================================================================================*/
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, magnetRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, loseMagnetDistance);
    }
}