using UnityEngine;
using UnityEngine.EventSystems;

public class UIHoverElevate : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("elevation settings")]
    [SerializeField] private float hoverDistanceY = 8.0f;  // how many pixels to float up
    [SerializeField] private float transitionSpeed = 14.0f; // smooth ease speed
    [SerializeField] private bool enableSubtleScale = true; // slight scale up for extra punch
    [SerializeField] private float hoverScale = 1.04f;

    [Header("optional target (leave empty to float this object)")]
    [SerializeField] private RectTransform targetRect;

    private bool isHovered = false;
    private float currentOffsetY = 0f;
    private float currentScale = 1.0f;
    private Vector3 basePosition;
    private bool hasCapturedBase = false;

    private void Awake()
    {
        if (targetRect == null)
            targetRect = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        // reset state on enable
        isHovered = false;
        currentOffsetY = 0f;
        currentScale = 1.0f;
        hasCapturedBase = false;

        if (targetRect != null)
        {
            targetRect.localScale = Vector3.one;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }

    private void LateUpdate()
    {
        if (targetRect == null) return;

        // capture layout position before applying offset
        if (!hasCapturedBase || !isHovered && Mathf.Abs(currentOffsetY) < 0.01f)
        {
            basePosition = targetRect.localPosition - new Vector3(0, currentOffsetY, 0);
            hasCapturedBase = true;
        }

        float targetOffsetY = isHovered ? hoverDistanceY : 0f;
        float targetScaleMultiplier = isHovered && enableSubtleScale ? hoverScale : 1.0f;

        // use unscaled delta time so it works even if game is paused
        currentOffsetY = Mathf.Lerp(currentOffsetY, targetOffsetY, Time.unscaledDeltaTime * transitionSpeed);
        currentScale = Mathf.Lerp(currentScale, targetScaleMultiplier, Time.unscaledDeltaTime * transitionSpeed);

        // apply offset in LateUpdate so layout groups do not override it
        Vector3 newPos = basePosition;
        newPos.y += currentOffsetY;
        targetRect.localPosition = newPos;

        targetRect.localScale = Vector3.one * currentScale;
    }
}