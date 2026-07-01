﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemHandManager : MonoBehaviour
{
    public static ItemHandManager Instance { get; private set; }

    [Header("手牌UI")]
    public Button toggleButton;
    public Transform handContainer;

    [Header("默认卡牌预制体")]
    public GameObject defaultCardPrefab;
    
    [Header("稀有度卡牌预制体")]
    public RarityCardPrefab[] rarityPrefabs;
    
    [System.Serializable]
    public class RarityCardPrefab
    {
        public ItemData.ItemRarity rarity;
        public GameObject cardPrefab;
    }

    [Header("布局设置")]
    public float cardWidth = 120f;
    public float cardSpacing = 10f;
    public float centerOffsetY = 100f;
    public float fanAngle = 15f;

    [Header("悬停效果")]
    public float hoverScale = 1.1f;
    public float hoverYOffset = 50f;

    [Header("初始可见性")]
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
                    Debug.Log("ItemHandManager: 已找到并设置为 Canvas 子对象");
                }
                else
                {
                    GameObject canvasObj = new GameObject("Canvas");
                    canvas = canvasObj.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvasObj.AddComponent<CanvasScaler>();
                    canvasObj.AddComponent<GraphicRaycaster>();
                    transform.SetParent(canvas.transform);
                    Debug.Log("ItemHandManager: 创建并设置为新 Canvas 子对象");
                }
        }
    }

    private void CreateHandContainer()
    {
        // 手牌容器必须挂在满屏的根 Canvas 下，底部中心锚点才会以整屏为基准；
        // 否则父物体不是满屏 RectTransform 时，改变分辨率会导致手牌不在底部正中间
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        Transform containerParent = canvas != null ? canvas.rootCanvas.transform : transform;

        if (handContainer == null)
        {
            GameObject container = new GameObject("HandContainer");
            container.AddComponent<RectTransform>();
            handContainer = container.transform;
        }

        handContainer.SetParent(containerParent, false);

        RectTransform rect = handContainer.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, centerOffsetY);
        rect.sizeDelta = new Vector2(0, 300);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private void AutoInitialize()
    {
        if (GameManager.Instance != null && GameManager.Instance.currentPlayer != null)
        {
            Debug.Log("ItemHandManager: 自动初始化手牌");
            SetupHand(GameManager.Instance.currentPlayer);
        }
        else
        {
            Debug.Log("ItemHandManager: GameManager 未就绪...");
        }
    }

    public void SetupHand(Player player)
    {
        currentPlayer = player;
        ClearHand();

        if (ItemManager.Instance == null)
        {
            Debug.LogWarning("ItemHandManager: ItemManager 未找到");
            return;
        }

        List<ItemData> items = ItemManager.Instance.GetPlayerItems(player);
        
        if (items.Count == 0)
        {
            Debug.Log("ItemHandManager: 该玩家没有物品");
            return;
        }

        foreach (ItemData item in items)
        {
            AddCardToHand(item, player);
        }

        LayoutHand();
        Debug.Log($"ItemHandManager: 已加载 {items.Count} 张卡牌");
    }

    public void AddCardToHand(ItemData item, Player player)
    {
        if (handContainer == null)
        {
            Debug.LogWarning("ItemHandManager: 手牌容器未设置");
            return;
        }

        GameObject prefabToUse = GetCardPrefabByRarity(item.rarity);
        if (prefabToUse == null)
        {
            Debug.LogWarning($"ItemHandManager: 未找到 {item.rarity} 稀有度的预制体");
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

    // 按具体卡牌实例移除（打出卡时使用，避免同名卡误删错误的那张）
    public void RemoveCard(ItemDragCard card)
    {
        if (card == null) return;

        handCards.Remove(card);
        Destroy(card.gameObject);
        LayoutHand();
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
                buttonText.text = isVisible ? "隐藏手牌" : "显示手牌";
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

            // 1~2 张保持竖直平铺；3 张及以上才展开扇形（中间最高、向两侧外倾）
            if (cardCount >= 3)
            {
                float normalizedPos = (float)i / (cardCount - 1) - 0.5f;
                angle = -normalizedPos * fanAngle;
                yOffset = -Mathf.Abs(normalizedPos) * 20f;
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
