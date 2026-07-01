﻿using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemEffect
{
    [Tooltip("效果类型")]
    public ItemData.ItemEffectType effectType;

    [Tooltip("效果数值")]
    public int effectValue = 0;

    [Tooltip("效果百分比")]
    public float effectPercent = 0f;

    [Tooltip("持续回合数")]
    public int durationRounds = 0;
}

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

    [Header("Item Effects")]
    [Tooltip("物品的所有效果（支持复合效果）")]
    public List<ItemEffect> effects = new List<ItemEffect>();

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
        Custom,
        MoveForward
    }
}
