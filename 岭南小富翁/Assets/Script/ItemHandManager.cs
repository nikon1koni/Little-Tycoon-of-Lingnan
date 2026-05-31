using System.Collections.Generic;
using UnityEngine;

public class ItemHandManager : MonoBehaviour
{
    public static ItemHandManager Instance { get; private set; }

    [Header("????????")]
    public Transform handContainer;

    [Header("?????????")]
    public GameObject dragCardPrefab;

    [Header("???????????")]
    public float cardWidth = 120f;
    public float cardSpacing = 10f;
    public float centerOffsetY = -100f;
    public float fanAngle = 15f;

    private List<ItemDragCard> handCards = new List<ItemDragCard>();
    private Player currentPlayer;

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
        if (handContainer == null)
        {
            GameObject container = new GameObject("HandContainer");
            container.transform.SetParent(transform);
            container.AddComponent<RectTransform>();
            handContainer = container.transform;

            RectTransform rect = handContainer.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0, centerOffsetY);
            rect.sizeDelta = new Vector2(Screen.width, 300);
        }
    }

    public void SetupHand(Player player)
    {
        currentPlayer = player;
        ClearHand();

        if (ItemManager.Instance == null) return;

        List<ItemData> items = ItemManager.Instance.GetPlayerItems(player);
        foreach (ItemData item in items)
        {
            AddCardToHand(item, player);
        }

        LayoutHand();
    }

    public void AddCardToHand(ItemData item, Player player)
    {
        if (handContainer == null || dragCardPrefab == null) return;

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

            // ????¦Ë??
            float baseX = startX + i * (cardWidth + cardSpacing);
            float baseY = 0f;

            // ????§¹??
            float angle = 0f;
            float yOffset = 0f;

            if (cardCount > 1)
            {
                float normalizedPos = (float)i / (cardCount - 1) - 0.5f;
                angle = normalizedPos * fanAngle;
                yOffset = Mathf.Abs(normalizedPos) * 20f;
            }

            // ????¦Ë??
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(baseX, baseY + yOffset);
            rect.localRotation = Quaternion.Euler(0, 0, angle);
            rect.sizeDelta = new Vector2(cardWidth, cardWidth * 1.5f);
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
}
