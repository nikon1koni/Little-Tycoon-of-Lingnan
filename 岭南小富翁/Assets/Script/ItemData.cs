using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Game/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Item Info")]
    [InspectorName("????")]
    public string itemName;
    
    [InspectorName("???")]
    public Sprite itemIcon;
    
    [TextArea(3, 10)]
    [InspectorName("????")]
    public string itemDescription;

    [Header("Item Effect")]
    [InspectorName("งน??????")]
    public ItemEffectType effectType;
    
    [Header("Effect Parameters")]
    [InspectorName("งน?????")]
    public int effectValue = 0;
    
    [InspectorName("งน??????")]
    public float effectPercent = 0f;
    
    [InspectorName("?????????")]
    public int durationRounds = 0;
    
    [Header("Usage Limits")]
    [InspectorName("???????")]
    public bool canUseAnytime = true;
    
    [InspectorName("????????")]
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
