using UnityEditor;
using UnityEngine;

public static class AutoAssignDiceSFX
{
    [MenuItem("Tools/Auto Assign Dice SFX")]
    public static void Assign()
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

        string[] diceGuids = AssetDatabase.FindAssets("骰子滚动音效", new[] { "Assets/Music" });
        AudioClip diceClip = diceGuids.Length > 0
            ? AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(diceGuids[0]))
            : null;

        if (diceClip == null)
        {
            Debug.LogError("在 Assets/Music 文件夹中未找到骰子滚动音效文件");
            return;
        }

        Undo.RecordObject(config, "Assign Dice SFX");
        config.diceRoll = diceClip;
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();

        Debug.Log($"已将「{diceClip.name}」分配到 SFXConfig.diceRoll");
    }
}
