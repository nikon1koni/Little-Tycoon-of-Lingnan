using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Game/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Item Info")]
    [InspectorName("名称")]
    public string itemName;
    
    [InspectorName("图标")]
    public Sprite itemIcon;
    
    [TextArea(3, 10)]
    [InspectorName("描述")]
    public string itemDescription;

    [Header("Item Effect")]
    [InspectorName("效果类型")]
    public ItemEffectType effectType;
    
    [Header("Effect Parameters")]
    [InspectorName("效果数值")]
    public int effectValue = 0;
    
    [InspectorName("效果百分比")]
    public float effectPercent = 0f;
    
    [InspectorName("持续回合数")]
    public int durationRounds = 0;
    
    [Header("Usage Limits")]
    [InspectorName("随时可用")]
    public bool canUseAnytime = true;
    
    [InspectorName("回合内可用")]
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
