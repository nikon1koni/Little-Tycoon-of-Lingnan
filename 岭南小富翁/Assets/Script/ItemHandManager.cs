using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemHandManager : MonoBehaviour
{
    public static ItemHandManager Instance { get; private set; }

    [Header("Hand UI")]
    public Button toggleButton;
    public Transform handContainer;

    [Header("Card Prefab")]
    public GameObject dragCardPrefab;

    [Header("Card Settings")]
    public float cardWidth = 120f;
    public float cardSpacing = 10f;
    public float bottomPadding = 50f;
    public float fanAngle = 15f;

    [Header("Visibility")]
    public bool startVisible = true;

    private List<ItemDragCard> handCards = new List<ItemDragCard>();
    private Player currentPlayer;
    private bool isVisible = true;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        EnsureCanvasParent();
        CreateHandContainer();

        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(ToggleHand);
        }

        isVisible = startVisible;
        UpdateHandVisibility();

        AutoInitialize();
    }

    private void EnsureCanvasParent()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                transform.SetParent(canvas.transform);
                Debug.Log("ItemHandManager: Moved to existing Canvas");
            }
            else
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                transform.SetParent(canvas.transform);
                Debug.Log("ItemHandManager: Created new Canvas");
            }
        }
    }

    private void CreateHandContainer()
    {
        if (handContainer == null)
        {
            GameObject container = new GameObject("HandContainer");
            container.transform.SetParent(transform);
            container.AddComponent<RectTransform>();
            handContainer = container.transform;
        }

        RectTransform rect = handContainer.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(Screen.width, 400);
    }

    private void AutoInitialize()
    {
        if (GameManager.Instance != null && GameManager.Instance.currentPlayer != null)
        {
            Debug.Log("ItemHandManager: Auto initializing...");
            SetupHand(GameManager.Instance.currentPlayer);
        }
        else
        {
            Debug.Log("ItemHandManager: Waiting for player...");
        }
    }

    public void SetupHand(Player player)
    {
        currentPlayer = player;
        ClearHand();

        if (ItemManager.Instance == null)
        {
            Debug.LogWarning("ItemHandManager: ItemManager not found");
            return;
        }

        List<ItemData> items = ItemManager.Instance.GetPlayerItems(player);
        
        if (items.Count == 0)
        {
            Debug.Log("ItemHandManager: No items to display");
            return;
        }

        foreach (ItemData item in items)
        {
            AddCardToHand(item, player);
        }

        LayoutHand();
        Debug.Log($"ItemHandManager: Displayed {items.Count} items");
    }

    public void AddCardToHand(ItemData item, Player player)
    {
        if (handContainer == null || dragCardPrefab == null)
        {
            Debug.LogWarning("ItemHandManager: Missing references");
            return;
        }

        GameObject cardObj = Instantiate(dragCardPrefab, handContainer);
        ItemDragCard dragCard = cardObj.GetComponent<ItemDragCard>();

        if (dragCard != null)
        {
            dragCard.Setup(item, player);
            handCards.Add(dragCard);
        }

        LayoutHand();
    }

    public void RemoveCardFromHand(ItemData item)
    {
        ItemDragCard cardToRemove = handCards.Find(card => card.itemData == item);

        if (cardToRemove != null)
        {
            handCards.Remove(cardToRemove);
            Destroy(cardToRemove.gameObject);
            LayoutHand();
        }
    }

    public void ClearHand()
    {
        foreach (ItemDragCard card in handCards)
        {
            if (card != null)
            {
                Destroy(card.gameObject);
            }
        }
        handCards.Clear();
    }

    public void RefreshHand()
    {
        if (currentPlayer != null)
        {
            SetupHand(currentPlayer);
        }
    }

    public void ToggleHand()
    {
        isVisible = !isVisible;
        UpdateHandVisibility();

        if (toggleButton != null)
        {
            Text buttonText = toggleButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = isVisible ? "Hide Items" : "Show Items";
            }
        }
    }

    private void UpdateHandVisibility()
    {
        if (handContainer != null)
        {
            handContainer.gameObject.SetActive(isVisible);
        }
    }

    public void ShowHand()
    {
        isVisible = true;
        UpdateHandVisibility();
    }

    public void HideHand()
    {
        isVisible = false;
        UpdateHandVisibility();
    }

    private void LayoutHand()
    {
        int cardCount = handCards.Count;
        if (cardCount == 0) return;

        float totalWidth = (cardCount - 1) * (cardWidth + cardSpacing);
        float startX = -totalWidth / 2f;

        float cardHeight = cardWidth * 1.5f;

        Debug.Log($"LayoutHand: cardCount={cardCount}, totalWidth={totalWidth}, bottomPadding={bottomPadding}, Screen.height={Screen.height}");

        for (int i = 0; i < cardCount; i++)
        {
            ItemDragCard card = handCards[i];
            RectTransform rect = card.GetComponent<RectTransform>();

            float x = startX + i * (cardWidth + cardSpacing);

            float angle = 0f;
            float fanY = 0f;

            if (cardCount > 1)
            {
                float normalizedPos = (float)i / (cardCount - 1) - 0.5f;
                angle = normalizedPos * fanAngle;
                fanY = Mathf.Abs(normalizedPos) * 20f;
            }

            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(x, bottomPadding + fanY);
            rect.localRotation = Quaternion.Euler(0, 0, angle);
            rect.sizeDelta = new Vector2(cardWidth, cardHeight);

            Debug.Log($"Card {i}: x={x}, y={bottomPadding + fanY}, anchoredPosition={rect.anchoredPosition}, anchorMin={rect.anchorMin}");
        }
    }

    public void EnableAllCards()
    {
        foreach (ItemDragCard card in handCards)
        {
            card.EnableCard();
        }
    }

    public void DisableAllCards()
    {
        foreach (ItemDragCard card in handCards)
        {
            card.DisableCard();
        }
    }

    public int GetCardCount()
    {
        return handCards.Count;
    }

    public void OnPlayerChanged(Player newPlayer)
    {
        currentPlayer = newPlayer;
        SetupHand(newPlayer);
    }

    void OnRectTransformDimensionsChange()
    {
        if (handContainer != null)
        {
            RectTransform rect = handContainer.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(0, bottomPadding + 100f);
            rect.sizeDelta = new Vector2(Screen.width, 300);
        }
    }
}
