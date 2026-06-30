using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPanelMenu : EditorWindow
{
    [MenuItem("Tools/岭南小富翁/配置设置面板")]
    public static void ConfigureSettingsPanel()
    {
        GameObject settingsPanel = GameObject.Find("SettingsPanel");
        if (settingsPanel == null) settingsPanel = GameObject.Find("Image");
        
        if (settingsPanel == null)
        {
            EditorUtility.DisplayDialog("错误", "未找到设置面板对象", "确定");
            return;
        }
        
        SettingsPanelController controller = settingsPanel.GetComponent<SettingsPanelController>();
        if (controller == null)
        {
            controller = settingsPanel.AddComponent<SettingsPanelController>();
        }
        
        controller.settingsPanel = settingsPanel;
        
        controller.brightnessSlider = FindSliderInChildren(settingsPanel.transform, "亮度");
        controller.musicSlider = FindSliderInChildren(settingsPanel.transform, "音乐");
        controller.sfxSlider = FindSliderInChildren(settingsPanel.transform, "音效");
        controller.closeButton = FindButtonInChildren(settingsPanel.transform, "关闭");
        
        EditorUtility.SetDirty(controller);
        EditorUtility.DisplayDialog("完成", "设置面板配置完成！\n\n亮度滑动条: " + (controller.brightnessSlider != null ? "已绑定" : "未找到") + "\n音乐滑动条: " + (controller.musicSlider != null ? "已绑定" : "未找到") + "\n音效滑动条: " + (controller.sfxSlider != null ? "已绑定" : "未找到") + "\n关闭按钮: " + (controller.closeButton != null ? "已绑定" : "未找到"), "确定");
    }
    
    static Slider FindSliderInChildren(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            Slider slider = child.GetComponent<Slider>();
            if (slider != null && child.name.Contains(name)) return slider;
            Slider found = FindSliderInChildren(child, name);
            if (found != null) return found;
        }
        return null;
    }
    
    static Button FindButtonInChildren(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            Button btn = child.GetComponent<Button>();
            if (btn != null && child.name.Contains(name)) return btn;
            Button found = FindButtonInChildren(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
