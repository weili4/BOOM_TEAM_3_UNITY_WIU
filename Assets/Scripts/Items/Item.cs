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
        initialPosition = transform.position; // save spawn position

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && itemData != null)
        {
            spriteRenderer.sprite = itemData.itemImage;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
    }

    private void Update()
    {
        if (playerTransform == null || itemData == null || itemEffect == null) return;

        float dist = Vector2.Distance(transform.position, playerTransform.position);

        // check magnet range
        if (dist <= magnetRadius)
        {
            isMagnetized = true;
        }
        else if (dist > loseMagnetDistance)
        {
            isMagnetized = false; // lose magnet if player gets too far
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
            // return smoothly to initial spawn position
            if (Vector3.Distance(transform.position, initialPosition) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(transform.position, initialPosition, magnetSpeed * 0.5f * Time.deltaTime);
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
            inventory.AddItemStack(itemData, itemEffect);

            if (pickupSound != null)
            {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFX(pickupSound, transform.position);
                else
                    AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            }

            Destroy(gameObject);
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