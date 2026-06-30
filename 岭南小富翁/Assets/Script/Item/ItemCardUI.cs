using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemCardUI : MonoBehaviour
{
    [Header("UI组件")]
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public Button useButton;

    private ItemData currentItem;
    private Player currentPlayer;

    public void Setup(ItemData item, Player player)
    {
        currentItem = item;
        currentPlayer = player;

        if (iconImage != null)
        {
            iconImage.sprite = item.itemIcon;
            if (item.itemIcon == null)
            {
                iconImage.color = Color.gray;
            }
            else
            {
                iconImage.color = Color.white;
            }
        }

        if (nameText != null)
        {
            nameText.text = item.itemName;
        }

        if (descriptionText != null)
        {
            descriptionText.text = item.itemDescription;
        }

        if (useButton != null)
        {
            useButton.onClick.RemoveAllListeners();
            useButton.onClick.AddListener(OnUseButtonClicked);
            UpdateButtonState();
        }
    }

    private void UpdateButtonState()
    {
        if (useButton != null && ItemManager.Instance != null)
        {
            bool canUse = ItemManager.Instance.CanUseItem(currentPlayer, currentItem);
            useButton.interactable = canUse;
        }
    }

    private void OnUseButtonClicked()
    {
        if (ItemManager.Instance != null && currentItem != null && currentPlayer != null)
        {
            if (ItemManager.Instance.UseItem(currentPlayer, currentItem))
            {
                if (SFXManager.Instance != null)
                {
                    SFXManager.Instance.PlaySFX(SFXClip.UIClick);
                }
            }
        }
    }
}
