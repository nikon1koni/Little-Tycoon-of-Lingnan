using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Game/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Item Info")]
    [InspectorName("Name")]
    public string itemName;
    
    [InspectorName("Icon")]
    public Sprite itemIcon;
    
    [TextArea(3, 10)]
    [InspectorName("Description")]
    public string itemDescription;
    
    [InspectorName("Rarity")]
    public ItemRarity rarity;
    
    public enum ItemRarity
    {
        Common,
        Rare,
        Epic,
        Legendary
    }

    [Header("Item Effect")]
    [InspectorName("Effect Type")]
    public ItemEffectType effectType;
    
    [Header("Effect Parameters")]
    [InspectorName("Effect Value")]
    public int effectValue = 0;
    
    [InspectorName("Effect Percent")]
    public float effectPercent = 0f;
    
    [InspectorName("Duration Rounds")]
    public int durationRounds = 0;
    
    [Header("Usage Limits")]
    [InspectorName("Can Use Anytime")]
    public bool canUseAnytime = true;
    
    [InspectorName("Can Use On Turn")]
    public bool canUseOnTurn = true;

    public enum ItemEffectType
    {
        GainMoney,
        LoseMoney,
        AddDice,
        SkipTurn,
        MoveToStart,
        MoveToRandom,
        IncomeBoost,
        StealMoney,
        DestroyBuilding,
        GiveBuff,
        ImmuneToNegative,
        TeleportToTile,
        Custom
    }
}
