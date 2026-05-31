using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemPanelUI : MonoBehaviour
{
    public static ItemPanelUI Instance { get; private set; }

    [Header("????????UI???")]
    public GameObject itemPanel;
    public Button toggleButton;
    public Transform itemCardsContainer;
    public GameObject itemCardPrefab;

    [Header("????")]
    public bool startHidden = true;

    private bool isPanelVisible = false;
    private List<GameObject> currentCards = new List<GameObject>();

    private void Awake()
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

    private void Start()
    {
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(TogglePanel);
        }

        if (itemPanel != null)
        {
            itemPanel.SetActive(!startHidden);
            isPanelVisible = !startHidden;
        }

        UpdateItemDisplay();
    }

    public void TogglePanel()
    {
        isPanelVisible = !isPanelVisible;
        if (itemPanel != null)
        {
            itemPanel.SetActive(isPanelVisible);
        }

        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.PlaySFX(SFXClip.UIClick);
        }

        if (isPanelVisible)
        {
            UpdateItemDisplay();
        }
    }

    public void ShowPanel()
    {
        isPanelVisible = true;
        if (itemPanel != null)
        {
            itemPanel.SetActive(true);
        }
        UpdateItemDisplay();
    }

    public void HidePanel()
    {
        isPanelVisible = false;
        if (itemPanel != null)
        {
            itemPanel.SetActive(false);
        }
    }

    public void UpdateItemDisplay()
    {
        ClearCurrentCards();

        if (GameManager.Instance == null) return;

        Player currentPlayer = GameManager.Instance.currentPlayer;
        if (currentPlayer == null) return;

        if (ItemManager.Instance == null) return;

        List<ItemData> items = ItemManager.Instance.GetPlayerItems(currentPlayer);

        foreach (ItemData item in items)
        {
            CreateItemCard(item, currentPlayer);
        }
    }

    private void CreateItemCard(ItemData item, Player player)
    {
        GameObject cardObj;

        if (itemCardPrefab != null && itemCardsContainer != null)
        {
            cardObj = Instantiate(itemCardPrefab, itemCardsContainer);
        }
        else
        {
            cardObj = new GameObject("ItemCard");
            if (itemCardsContainer != null)
            {
                cardObj.transform.SetParent(itemCardsContainer, false);
            }

            Image bgImage = cardObj.AddComponent<Image>();
            bgImage.color = new Color(0.9f, 0.9f, 0.9f, 0.95f);

            RectTransform rect = cardObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(120, 160);
        }

        ItemCardUI cardUI = cardObj.GetComponent<ItemCardUI>();
        if (cardUI == null)
        {
            cardUI = cardObj.AddComponent<ItemCardUI>();
            SetupBasicCardUI(cardObj, cardUI);
        }

        cardUI.Setup(item, player);
        currentCards.Add(cardObj);
    }

    private void SetupBasicCardUI(GameObject cardObj, ItemCardUI cardUI)
    {
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(cardObj.transform, false);
        Image iconImage = iconObj.AddComponent<Image>();
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 1f);
        iconRect.anchorMax = new Vector2(0.5f, 1f);
        iconRect.pivot = new Vector2(0.5f, 1f);
        iconRect.anchoredPosition = new Vector2(0, -10);
        iconRect.sizeDelta = new Vector2(80, 80);
        cardUI.iconImage = iconImage;

        GameObject nameObj = new GameObject("Name");
        nameObj.transform.SetParent(cardObj.transform, false);
        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        RectTransform nameRect = nameObj.GetComponent<RectTransform>();
        nameRect.anchorMin = Vector2.zero;
        nameRect.anchorMax = Vector2.one;
        nameRect.offsetMin = new Vector2(10, 50);
        nameRect.offsetMax = new Vector2(-10, -60);
        nameText.alignment = TextAlignmentOptions.Top;
        nameText.fontSize = 14;
        nameText.color = Color.black;
        cardUI.nameText = nameText;

        GameObject descObj = new GameObject("Description");
        descObj.transform.SetParent(cardObj.transform, false);
        TextMeshProUGUI descText = descObj.AddComponent<TextMeshProUGUI>();
        RectTransform descRect = descObj.GetComponent<RectTransform>();
        descRect.anchorMin = Vector2.zero;
        descRect.anchorMax = Vector2.one;
        descRect.offsetMin = new Vector2(10, 10);
        descRect.offsetMax = new Vector2(-10, -90);
        descText.alignment = TextAlignmentOptions.Top;
        descText.fontSize = 10;
        descText.color = Color.gray;
        descText.enableWordWrapping = true;
        cardUI.descriptionText = descText;

        GameObject buttonObj = new GameObject("UseButton");
        buttonObj.transform.SetParent(cardObj.transform, false);
        Button useButton = buttonObj.AddComponent<Button>();
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.6f, 0.9f);
        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.anchoredPosition = new Vector2(0, 10);
        buttonRect.sizeDelta = new Vector2(100, 30);

        GameObject buttonTextObj = new GameObject("Text");
        buttonTextObj.transform.SetParent(buttonObj.transform, false);
        TextMeshProUGUI buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
        RectTransform buttonTextRect = buttonTextObj.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;
        buttonText.text = "???";
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.fontSize = 12;
        buttonText.color = Color.white;
        cardUI.useButton = useButton;
    }

    private void ClearCurrentCards()
    {
        foreach (GameObject card in currentCards)
        {
            if (card != null)
            {
                Destroy(card);
            }
        }
        currentCards.Clear();
    }
}
