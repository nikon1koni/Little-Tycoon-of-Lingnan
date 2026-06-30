using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ItemDragCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Card Info")]
    public ItemData itemData;
    public Player ownerPlayer;

    [Header("UI Components")]
    [Tooltip("Drag Icon child object here")]
    public Image iconImage;
    [Tooltip("Drag Name child object here")]
    public TextMeshProUGUI nameText;
    [Tooltip("Drag Description child object here")]
    public TextMeshProUGUI descriptionText;

    [Header("Drag Settings")]
    public float dragScale = 1.2f;
    public float dragYOffset = 50f;
    public bool canDrag = true;

    

    [Header("Visual Feedback")]
    public Color normalColor = Color.white;
    public Color dragOutlineColor = Color.green;
    public Color invalidOutlineColor = Color.red;
    public float outlineWidth = 5f;

    [Header("Drop Zone")]
    public GameObject dropZone;
    public string validDropZoneTag = "ItemDropZone";

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Image cardImage;
    private Outline cardOutline;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 originalScale;
    private Vector2 originalAnchoredPosition;
    private bool isDragging = false;
    private bool isHovering = false;
    private GameObject draggedCardInstance;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        cardImage = GetComponent<Image>();
        cardOutline = GetComponent<Outline>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (cardOutline == null)
        {
            cardOutline = gameObject.AddComponent<Outline>();
        }
        
        cardOutline.enabled = false;
        cardOutline.effectColor = dragOutlineColor;
        cardOutline.effectDistance = new Vector2(outlineWidth, outlineWidth);

        originalScale = transform.localScale;
    }

    void Start()
    {
        UpdateOriginalPosition();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isDragging) return;
        isHovering = true;
        OnHoverStateChanged();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isDragging) return;
        isHovering = false;
        OnHoverStateChanged();
    }

    private void OnHoverStateChanged()
    {
        if (isHovering)
        {
            float scale = ItemHandManager.Instance != null ? ItemHandManager.Instance.hoverScale : 1.1f;
            float yOffset = ItemHandManager.Instance != null ? ItemHandManager.Instance.hoverYOffset : 50f;
            
            StartCoroutine(ScaleTo(transform, originalScale * scale, 0.15f));
            rectTransform.anchoredPosition = originalAnchoredPosition + Vector2.up * yOffset;
        }
        else
        {
            StartCoroutine(ScaleTo(transform, originalScale, 0.15f));
            rectTransform.anchoredPosition = originalAnchoredPosition;
        }
    }

    public void Setup(ItemData item, Player player)
    {
        itemData = item;
        ownerPlayer = player;

        UpdateCardDisplay();
    }

    public void UpdateOriginalPosition()
    {
        originalAnchoredPosition = rectTransform.anchoredPosition;
    }

    private void UpdateCardDisplay()
    {
        if (itemData == null) return;

        // ???????
        if (iconImage != null)
        {
            iconImage.sprite = itemData.itemIcon;
            iconImage.color = itemData.itemIcon != null ? Color.white : Color.gray;
        }

        // ????????
        if (nameText != null)
        {
            nameText.text = itemData.itemName;
        }

        // ????????
        if (descriptionText != null)
        {
            descriptionText.text = itemData.itemDescription;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!canDrag)
        {
            eventData.pointerDrag = null;
            return;
        }

        if (itemData == null || ownerPlayer == null) return;

        if (!CanUseCard())
        {
            eventData.pointerDrag = null;
            return;
        }

        isDragging = true;

        originalPosition = transform.position;
        originalRotation = transform.rotation;

        draggedCardInstance = Instantiate(gameObject, transform.parent);
        draggedCardInstance.name = "DraggingCard";

        ItemDragCard dragCard = draggedCardInstance.GetComponent<ItemDragCard>();
        dragCard.enabled = false;
        dragCard.canvasGroup.blocksRaycasts = false;

        RectTransform dragRect = draggedCardInstance.GetComponent<RectTransform>();
        dragRect.sizeDelta = rectTransform.sizeDelta;

        Outline outline = draggedCardInstance.GetComponent<Outline>();
        if (outline == null)
        {
            outline = draggedCardInstance.AddComponent<Outline>();
        }
        // 开始拖动时不显示描边，只有拖到有效投放区且可使用时才显示
        outline.effectColor = dragOutlineColor;
        outline.effectDistance = new Vector2(outlineWidth, outlineWidth);
        outline.enabled = false;

        canvasGroup.blocksRaycasts = false;

        StartCoroutine(ScaleTo(draggedCardInstance.transform, originalScale * dragScale, 0.1f));

        draggedCardInstance.transform.SetAsLastSibling();

        EnableAllDropZones();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || draggedCardInstance == null) return;

        Vector3 mousePos = Input.mousePosition;
        draggedCardInstance.transform.position = new Vector3(mousePos.x, mousePos.y + dragYOffset, mousePos.z);

        CheckDropZone();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        isDragging = false;

        bool droppedOnValidZone = IsOverValidDropZone();
        bool canUse = CanUseCard();

        if (droppedOnValidZone && itemData != null && canUse)
        {
            UseItem();
        }
        else
        {
            ReturnToOriginalPosition();

            if (draggedCardInstance != null)
            {
                Destroy(draggedCardInstance);
                draggedCardInstance = null;
            }

            canvasGroup.blocksRaycasts = true;
        }

        DisableAllDropZones();
    }

    private void EnableAllDropZones()
    {
        ItemDropZone[] dropZones = FindObjectsOfType<ItemDropZone>();
        foreach (ItemDropZone zone in dropZones)
        {
            zone.EnableDropDetection();
        }
    }

    private void DisableAllDropZones()
    {
        ItemDropZone[] dropZones = FindObjectsOfType<ItemDropZone>();
        foreach (ItemDropZone zone in dropZones)
        {
            zone.DisableDropDetection();
        }
    }

    private bool IsOverValidDropZone()
    {
        if (draggedCardInstance == null) 
        {
            Debug.Log("IsOverValidDropZone: draggedCardInstance 为空");
            return false;
        }

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        var raycastResults = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, raycastResults);

        if (raycastResults.Count == 0)
        {
            Debug.Log("IsOverValidDropZone: δ????κ?UI???");
        }
        else
        {
            Debug.Log($"IsOverValidDropZone: 检测到 {raycastResults.Count} 个UI元素");
            foreach (var result in raycastResults)
            {
                Debug.Log($"  - {result.gameObject.name} (Tag: {result.gameObject.tag})");
                if (result.gameObject.CompareTag(validDropZoneTag) ||
                    result.gameObject.GetComponent<ItemDropZone>() != null)
                {
                    Debug.Log("?????Ч????????: " + result.gameObject.name);
                    return true;
                }
            }
        }

        Debug.Log("IsOverValidDropZone: δ?????Ч????????");
        return false;
    }

    private bool CanUseCard()
    {
        if (GameManager.Instance == null)
        {
            Debug.Log("CanUseCard: GameManager.Instance is null");
            return true;
        }
        
        if (GameManager.Instance.isMoving)
        {
            Debug.Log("CanUseCard: Player is moving, cannot use card");
            return false;
        }
        
        if (ItemManager.Instance != null)
        {
            bool canUse = ItemManager.Instance.CanUseItem(ownerPlayer, itemData);
            Debug.Log($"CanUseCard: ItemManager.CanUseItem returned {canUse}");
            return canUse;
        }
        
        Debug.Log("CanUseCard: ItemManager.Instance is null, returning true");
        return true;
    }

    private void CheckDropZone()
    {
        if (draggedCardInstance == null) return;

        Outline outline = draggedCardInstance.GetComponent<Outline>();
        if (outline == null) return;

        // 只有当卡牌拖到有效投放区上方时才显示描边
        if (!IsPointerOverValidDropZone())
        {
            outline.enabled = false;
            return;
        }

        // 在投放区上方：可使用显示绿色，不可使用显示红色
        bool canUse = CanUseCard();
        outline.enabled = true;
        outline.effectColor = canUse ? dragOutlineColor : invalidOutlineColor;
        outline.effectDistance = new Vector2(outlineWidth, outlineWidth);
    }

    private bool IsPointerOverValidDropZone()
    {
        if (EventSystem.current == null) return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        var raycastResults = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, raycastResults);

        foreach (var result in raycastResults)
        {
            if (result.gameObject.CompareTag(validDropZoneTag) ||
                result.gameObject.GetComponent<ItemDropZone>() != null)
            {
                return true;
            }
        }
        return false;
    }

    private void UseItem()
    {
        if (itemData == null || ownerPlayer == null) return;

        // ??????Ч
        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.PlaySFX(SFXClip.UIClick);
        }

        // ??????
        bool success = ItemManager.Instance.UseItem(ownerPlayer, itemData);

        if (success)
        {
            Debug.Log($"{ownerPlayer.playerName} 成功使用物品: {itemData.itemName}");

            // ??????? - ????????????????
            StartCoroutine(FlyToTarget());
        }
        else
        {
            ReturnToOriginalPosition();
        }
    }

    private System.Collections.IEnumerator FlyToTarget()
    {
        if (draggedCardInstance == null) yield break;

        Vector3 startPos = draggedCardInstance.transform.position;
        Vector3 targetPos;
        
        if (Camera.main != null)
        {
            targetPos = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, 10));
        }
        else
        {
            targetPos = new Vector3(Screen.width / 2, Screen.height / 2, 10);
        }
        
        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (draggedCardInstance == null) yield break;
            
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            draggedCardInstance.transform.position = Vector3.Lerp(startPos, targetPos, t);
            draggedCardInstance.transform.localScale = Vector3.Lerp(originalScale * dragScale, Vector3.zero, t);
            yield return null;
        }

        Destroy(draggedCardInstance);
        draggedCardInstance = null;

        ItemHandManager.Instance.RemoveCard(this);
    }

    private void ReturnToOriginalPosition()
    {
        if (draggedCardInstance != null)
        {
            StartCoroutine(MoveToPosition(draggedCardInstance.transform, originalPosition, 0.2f));
        }
    }

    private System.Collections.IEnumerator MoveToPosition(Transform target, Vector3 destination, float duration)
    {
        if (target == null) yield break;
        
        Vector3 startPos = target.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (target == null) yield break;
            
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            target.position = Vector3.Lerp(startPos, destination, t);
            yield return null;
        }

        if (target != null)
        {
            target.position = destination;
        }
    }

    private System.Collections.IEnumerator ScaleTo(Transform target, Vector3 destinationScale, float duration)
    {
        if (target == null) yield break;
        
        Vector3 startScale = target.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (target == null) yield break;
            
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            target.localScale = Vector3.Lerp(startScale, destinationScale, t);
            yield return null;
        }

        if (target != null)
        {
            target.localScale = destinationScale;
        }
    }

    // ?????? - ??????????????
    public void DisableCard()
    {
        canDrag = false;
        canvasGroup.alpha = 0.5f;
    }

    // ??????
    public void EnableCard()
    {
        canDrag = true;
        canvasGroup.alpha = 1f;
    }

    void OnDestroy()
    {
        if (draggedCardInstance != null)
        {
            Destroy(draggedCardInstance);
        }
    }
}
