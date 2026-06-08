using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SFXConfig", menuName = "Game/SFX Config")]
public class SFXConfig : ScriptableObject
{
    [Header("UI音效")]
    public AudioClip uiClick;
    public AudioClip uiHover;
    public AudioClip uiOpen;
    public AudioClip uiClose;

    [Header("角色音效")]
    public AudioClip playerJump;
    public AudioClip playerLand;
    public AudioClip playerMove;

    [Header("事件音效")]
    public AudioClip eventGainMoney;
    public AudioClip eventLoseMoney;
    public AudioClip eventPropertyBought;
    public AudioClip eventBuildingPlaced;
    public AudioClip eventBuildingUpgraded;
    public AudioClip eventGoToJail;
    public AudioClip eventTaxPaid;
    public AudioClip eventBuffActivated;

    [Header("骰子音效")]
    public AudioClip diceRoll;
    public AudioClip diceStop;

    [Header("交互音效")]
    public AudioClip diceClick;        // 骰子点击音效
    public AudioClip tileSelect;       // 地块选择音效
    public AudioClip buildingSold;      // 出售建筑音效
    public AudioClip eventSelect;      // 事件选择音效

    public List<SFXEntry> GetAllEntries()
    {
        List<SFXEntry> entries = new List<SFXEntry>();

        entries.Add(new SFXEntry(SFXClip.UIClick, uiClick));
        entries.Add(new SFXEntry(SFXClip.UIHover, uiHover));
        entries.Add(new SFXEntry(SFXClip.UIOpen, uiOpen));
        entries.Add(new SFXEntry(SFXClip.UIClose, uiClose));

        entries.Add(new SFXEntry(SFXClip.PlayerJump, playerJump));
        entries.Add(new SFXEntry(SFXClip.PlayerLand, playerLand));
        entries.Add(new SFXEntry(SFXClip.PlayerMove, playerMove));

        entries.Add(new SFXEntry(SFXClip.EventGainMoney, eventGainMoney));
        entries.Add(new SFXEntry(SFXClip.EventLoseMoney, eventLoseMoney));
        entries.Add(new SFXEntry(SFXClip.EventPropertyBought, eventPropertyBought));
        entries.Add(new SFXEntry(SFXClip.EventBuildingPlaced, eventBuildingPlaced));
        entries.Add(new SFXEntry(SFXClip.EventBuildingUpgraded, eventBuildingUpgraded));
        entries.Add(new SFXEntry(SFXClip.EventGoToJail, eventGoToJail));
        entries.Add(new SFXEntry(SFXClip.EventTaxPaid, eventTaxPaid));
        entries.Add(new SFXEntry(SFXClip.EventBuffActivated, eventBuffActivated));

        entries.Add(new SFXEntry(SFXClip.DiceRoll, diceRoll));
        entries.Add(new SFXEntry(SFXClip.DiceStop, diceStop));

        entries.Add(new SFXEntry(SFXClip.DiceClick, diceClick));
        entries.Add(new SFXEntry(SFXClip.TileSelect, tileSelect));
        entries.Add(new SFXEntry(SFXClip.BuildingSold, buildingSold));
        entries.Add(new SFXEntry(SFXClip.EventSelect, eventSelect));

        return entries;
    }
}

[System.Serializable]
public class SFXEntry
{
    public SFXClip clipType;
    public AudioClip clip;

    public SFXEntry(SFXClip type, AudioClip audioClip)
    {
        clipType = type;
        clip = audioClip;
    }
}
