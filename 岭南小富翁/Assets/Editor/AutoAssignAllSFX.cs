using UnityEditor;
using UnityEngine;
using System.IO;

public static class AutoAssignAllSFX
{
    [MenuItem("Tools/Auto Assign All SFX to Config")]
    public static void AssignAll()
    {
        SFXConfig config = AssetDatabase.FindAssets("t:SFXConfig").Length > 0
            ? AssetDatabase.LoadAssetAtPath<SFXConfig>(
                AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:SFXConfig")[0]))
            : null;

        if (config == null)
        {
            Debug.LogError("未找到 SFXConfig 资产，请先创建");
            return;
        }

        Undo.RecordObject(config, "Assign All SFX");

        // UI 音效
        Assign(config, ref config.uiClick, "Assets/Music/SFX/UI/click.wav");
        Assign(config, ref config.uiHover, "Assets/Music/SFX/UI/hover.wav");
        Assign(config, ref config.uiOpen, "Assets/Music/SFX/UI/open.wav");
        Assign(config, ref config.uiClose, "Assets/Music/SFX/UI/close.wav");

        // 角色音效
        Assign(config, ref config.playerJump, "Assets/Music/SFX/Character/jump.wav");
        Assign(config, ref config.playerLand, "Assets/Music/SFX/Character/land.wav");
        Assign(config, ref config.playerMove, "Assets/Music/SFX/Character/move.wav");

        // 事件音效
        Assign(config, ref config.eventGainMoney, "Assets/Music/SFX/Event/gain_money.wav");
        Assign(config, ref config.eventLoseMoney, "Assets/Music/SFX/Event/lose_money.wav");
        Assign(config, ref config.eventPropertyBought, "Assets/Music/SFX/Event/property_bought.wav");
        Assign(config, ref config.eventBuildingPlaced, "Assets/Music/SFX/Event/building_placed.wav");
        Assign(config, ref config.eventBuildingUpgraded, "Assets/Music/SFX/Event/building_upgraded.wav");
        Assign(config, ref config.eventGoToJail, "Assets/Music/SFX/Event/go_to_jail.wav");
        Assign(config, ref config.eventTaxPaid, "Assets/Music/SFX/Event/tax_paid.wav");
        Assign(config, ref config.eventBuffActivated, "Assets/Music/SFX/Event/buff_activated.wav");

        // 骰子音效 - 使用已有的骰子滚动音效.mp4
        string[] diceGuids = AssetDatabase.FindAssets("骰子滚动音效", new[] { "Assets/Music" });
        if (diceGuids.Length > 0)
        {
            AudioClip diceClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(diceGuids[0]));
            if (diceClip != null)
            {
                config.diceRoll = diceClip;
            }
        }
        else
        {
            Assign(config, ref config.diceRoll, "Assets/Music/SFX/Dice/dice_roll.wav");
        }
        Assign(config, ref config.diceStop, "Assets/Music/SFX/Dice/dice_stop.wav");

        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();

        Debug.Log("? 所有音效已自动分配到 SFXConfig！");
    }

    static void Assign(SFXConfig config, ref AudioClip field, string path)
    {
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (clip != null)
            field = clip;
    }
}
