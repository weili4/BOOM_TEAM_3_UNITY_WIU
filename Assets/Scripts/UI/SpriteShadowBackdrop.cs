using UnityEngine;
using UnityEngine.UI;

public class SpriteShadowBackdrop : MonoBehaviour
{
    [Header("shadow offset (units for world sprites, pixels for ui images)")]
    [SerializeField] private Vector2 shadowOffset = new Vector2(4f, -4f);

    [Header("shadow styling")]
    [SerializeField] private Color shadowColor = new Color(0.04f, 0.06f, 0.09f, 0.6f); // dark navy shadow
    [SerializeField] private int spriteSortingOrderOffset = -1; // for world sprites only

    private SpriteRenderer parentSprite;
    private SpriteRenderer shadowSprite;

    private Image parentImage;
    private Image shadowImage;
    private RectTransform parentRect;
    private RectTransform shadowRect;

    private GameObject shadowObj;
    private bool isUI = false;

    private void Awake()
    {
        parentSprite = GetComponent<SpriteRenderer>();
        parentImage = GetComponent<Image>();
        parentRect = GetComponent<RectTransform>();

        // auto-detect if this is a canvas ui image or a world sprite
        if (parentImage != null && parentRect != null)
        {
            isUI = true;
            SetupUIShadow();
        }
        else if (parentSprite != null)
        {
            isUI = false;
            SetupWorldSpriteShadow();
        }
    }

    private void SetupUIShadow()
    {
        shadowObj = new GameObject("UI_Shadow_Backdrop");

        // place shadow under same parent and render it right behind the main image
        shadowObj.transform.SetParent(transform.parent, false);
        shadowObj.transform.SetSiblingIndex(transform.GetSiblingIndex());

        shadowRect = shadowObj.AddComponent<RectTransform>();
        shadowImage = shadowObj.AddComponent<Image>();

        shadowImage.color = shadowColor;
        shadowImage.raycastTarget = false; // so it does not block mouse clicks
        shadowImage.preserveAspect = parentImage.preserveAspect;

        SyncUIRect();
    }

    private void SetupWorldSpriteShadow()
    {
        shadowObj = new GameObject("World_Shadow_Backdrop");
        shadowObj.transform.SetParent(transform, false);
        shadowObj.transform.localPosition = (Vector3)shadowOffset;

        shadowSprite = shadowObj.AddComponent<SpriteRenderer>();
        shadowSprite.color = shadowColor;
        shadowSprite.sortingLayerID = parentSprite.sortingLayerID;
        shadowSprite.sortingOrder = parentSprite.sortingOrder + spriteSortingOrderOffset;
    }

    private void LateUpdate()
    {
        if (isUI)
        {
            if (parentImage == null || shadowImage == null || shadowRect == null) return;

            // sync sprite, visibility, and alpha with parent ui image
            shadowImage.sprite = parentImage.sprite;
            shadowImage.enabled = parentImage.enabled && parentImage.gameObject.activeInHierarchy;
            shadowImage.preserveAspect = parentImage.preserveAspect;

            // maintain custom shadow color while multiplying parent alpha for smooth fading
            Color finalColor = shadowColor;
            finalColor.a = shadowColor.a * parentImage.color.a;
            shadowImage.color = finalColor;

            SyncUIRect();
        }
        else
        {
            if (parentSprite == null || shadowSprite == null) return;

            // sync 2d world sprite properties
            shadowSprite.sprite = parentSprite.sprite;
            shadowSprite.flipX = parentSprite.flipX;
            shadowSprite.flipY = parentSprite.flipY;
            shadowSprite.enabled = parentSprite.enabled;
            shadowSprite.sortingLayerID = parentSprite.sortingLayerID;
            shadowSprite.sortingOrder = parentSprite.sortingOrder + spriteSortingOrderOffset;

            shadowObj.transform.localPosition = (Vector3)shadowOffset;
        }
    }

    private void SyncUIRect()
    {
        if (parentRect == null || shadowRect == null) return;

        shadowRect.anchorMin = parentRect.anchorMin;
        shadowRect.anchorMax = parentRect.anchorMax;
        shadowRect.pivot = parentRect.pivot;
        shadowRect.sizeDelta = parentRect.sizeDelta;
        shadowRect.localRotation = parentRect.localRotation;
        shadowRect.localScale = parentRect.localScale;

        // position shadow offset in pixel space
        shadowRect.anchoredPosition = parentRect.anchoredPosition + shadowOffset;
    }

    private void OnDestroy()
    {
        if (shadowObj != null)
        {
            Destroy(shadowObj);
        }
    }
}