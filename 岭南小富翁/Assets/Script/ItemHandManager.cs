using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemHandManager : MonoBehaviour
{
    public static ItemHandManager Instance { get; private set; }

    [Header("??????UI")]
    public Button toggleButton;
    public Transform handContainer;

    [Header("?????????")]
    public GameObject defaultCardPrefab;
    
    [Header("??§Ø??????")]
    public RarityCardPrefab[] rarityPrefabs;
    
    [System.Serializable]
    public class RarityCardPrefab
    {
        public ItemData.ItemRarity rarity;
        public GameObject cardPrefab;
    }

    [Header("????????")]
    public float cardWidth = 120f;
    public float cardSpacing = 10f;
    public float centerOffsetY = 100f;
    public float fanAngle = 15f;

    [Header("???????????")]
    public float hoverScale = 1.1f;
    public float hoverYOffset = 50f;

    [Header("?????")]
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
                Debug.Log("ItemHandManager: ??????? Canvas ??");
            }
            else
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                transform.SetParent(canvas.transform);
                Debug.Log("ItemHandManager: ????????? Canvas");
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
        rect.anchoredPosition = new Vector2(0, centerOffsetY);
        rect.sizeDelta = new Vector2(Screen.width, 300);
    }

    private void AutoInitialize()
    {
        if (GameManager.Instance != null && GameManager.Instance.currentPlayer != null)
        {
            Debug.Log("ItemHandManager: ????????????");
            SetupHand(GameManager.Instance.currentPlayer);
        }
        else
        {
            Debug.Log("ItemHandManager: ??????????...");
        }
    }

    public void SetupHand(Player player)
    {
        currentPlayer = player;
        ClearHand();

        if (ItemManager.Instance == null)
        {
            Debug.LogWarning("ItemHandManager: ItemManager ??????");
            return;
        }

        List<ItemData> items = ItemManager.Instance.GetPlayerItems(player);
        
        if (items.Count == 0)
        {
            Debug.Log("ItemHandManager: ?????????????");
            return;
        }

        foreach (ItemData item in items)
        {
            AddCardToHand(item, player);
        }

        LayoutHand();
        Debug.Log($"ItemHandManager: ????? {items.Count} ?????");
    }

    public void AddCardToHand(ItemData item, Player player)
    {
        if (handContainer == null)
        {
            Debug.LogWarning("ItemHandManager: ??????????????");
            return;
        }

        GameObject prefabToUse = GetCardPrefabByRarity(item.rarity);
        if (prefabToUse == null)
        {
            Debug.LogWarning($"ItemHandManager: ??????? {item.rarity} ????");
            return;
        }

        GameObject cardObj = Instantiate(prefabToUse, handContainer);
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

    private GameObject GetCardPrefabByRarity(ItemData.ItemRarity rarity)
    {
        foreach (RarityCardPrefab pair in rarityPrefabs)
        {
            if (pair.rarity == rarity && pair.cardPrefab != null)
            {
                return pair.cardPrefab;
            }
        }
        
        return defaultCardPrefab;
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
                buttonText.text = isVisible ? "????????" : "???????";
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

        for (int i = 0; i < cardCount; i++)
        {
            ItemDragCard card = handCards[i];
            RectTransform rect = card.GetComponent<RectTransform>();

            float baseX = startX + i * (cardWidth + cardSpacing);
            float baseY = 0f;

            float angle = 0f;
            float yOffset = 0f;

            if (cardCount > 1)
            {
                float normalizedPos = (float)i / (cardCount - 1) - 0.5f;
                angle = normalizedPos * fanAngle;
                yOffset = Mathf.Abs(normalizedPos) * 20f;
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(baseX, baseY + yOffset);
            rect.localRotation = Quaternion.Euler(0, 0, angle);
            rect.sizeDelta = new Vector2(cardWidth, cardWidth * 1.5f);

            card.UpdateOriginalPosition();
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
}
