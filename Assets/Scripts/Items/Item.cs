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
    //private Vector3 initialPosition; //remove the initialposition so it wont bounce up down with rigidbody
    private SpriteRenderer spriteRenderer;

    /*===================================================================================================================*/
    [Header("Pick Up Delay Settings")]
    [SerializeField] private float PickUpDelayDuration = 2.0f;
    private float PickUpDelayTimer = 0f;
    private bool AllowToPickUp = false;
    /*===================================================================================================================*/

    /*===================================================================================================================*/
    [Header("Spawn Launch Effect")]
    [SerializeField] private float UpForce = 5f;
    [SerializeField] private float SideMinForce = 1.0f;
    [SerializeField] private float SideMaxForce = 1.0f;
    /*===================================================================================================================*/

    /*===================================================================================================================*/
    // Hover Setting to make it bob up and down
    [Header("Hover Settings")]
    [SerializeField] private float HoveringHeight = 0.15f;
    [SerializeField] private float HoveringSpeed = 2f;
    private float CurrentYPosition;
    private float HoveringOffsetTiming;
    /*===================================================================================================================*/

    /*===================================================================================================================*/
    // References to Rigidbody and GroundCheck
    private bool IsGrounded = false;
    private Rigidbody2D ItemRigidBody;
    /*===================================================================================================================*/


    private void Awake()
    {
        //initialPosition = transform.position;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && itemData != null)
        {
            spriteRenderer.sprite = itemData.itemImage;
        }

        /*===================================================================================================================*/
        // Set the hover starting values
        CurrentYPosition = transform.position.y;
        HoveringOffsetTiming = Random.Range(0f, 10f);
        ItemRigidBody = GetComponent<Rigidbody2D>();
        /*===================================================================================================================*/

        // find active leader on spawn
        UpdatePlayerTarget();
    }

    private void Update()
    {
        if(PickUpDelayTimer < PickUpDelayDuration && !AllowToPickUp)
        {
            PickUpDelayTimer += Time.deltaTime; 
        }
        else
        {
            PickUpDelayTimer = 0;
            AllowToPickUp = true;
        }

        // always track only the current active leader and ignore followers
        UpdatePlayerTarget();

        if (playerTransform == null || itemData == null || itemEffect == null) return;

        float dist = Vector2.Distance(transform.position, playerTransform.position);

        // check magnet range
        if (AllowToPickUp && (dist <= magnetRadius))
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

            /*===========================================================================================================*/
            // Set is not on ground when is following the player
            IsGrounded = false;
            if (ItemRigidBody != null)
            {
                ItemRigidBody.linearVelocity = Vector2.zero;
            }
            /*===========================================================================================================*/

            if (dist < 0.5f)
            {
                CollectItem();
            }
        }

        else if(IsGrounded)
        {
            // return to spawn position if player moves away
            //if (Vector3.Distance(transform.position, initialPosition) > 0.05f)
            //{
            //    transform.position = Vector3.MoveTowards(transform.position, initialPosition, magnetSpeed * 0.5f * Time.deltaTime);
            //}

            /*===========================================================================================================*/
            // Hovering Effect when is stationary
            float hoverY = CurrentYPosition + Mathf.Sin((Time.time + HoveringOffsetTiming) * HoveringSpeed) * HoveringHeight;
            transform.position = new Vector2(transform.position.x, hoverY);
            /*===========================================================================================================*/
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

    public void StartLaunchEffect(bool IsLaunchEffect)
    {
        Debug.Log("lAUNCHING THE FUCKING ITEM");

        /*===================================================================================================================*/
        // Launch effect
        if (IsLaunchEffect && ItemRigidBody != null)
        {
            float SidewayForce = Random.Range(SideMinForce, SideMaxForce);
            Vector2 LaunchForce = new Vector2(SidewayForce, UpForce);
            ItemRigidBody.AddForce(LaunchForce, ForceMode2D.Impulse);
        }
        /*===================================================================================================================*/
    }

    /*===================================================================================================================*/
    // Right now is just check collision on anything and asy is grounded
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsGrounded)
        {
            IsGrounded = true;
            if (ItemRigidBody != null)
            {
                ItemRigidBody.linearVelocity = Vector2.zero;
            }
            CurrentYPosition = transform.position.y;
        }
    }
    /*===================================================================================================================*/

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, magnetRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, loseMagnetDistance);
    }
}