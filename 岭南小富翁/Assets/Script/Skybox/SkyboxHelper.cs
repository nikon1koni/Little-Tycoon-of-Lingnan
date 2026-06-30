using UnityEngine;
using UnityEditor;

public class SkyboxHelper : MonoBehaviour
{
    [MenuItem("Tools/Create Procedural SkyDome")]
    public static void CreateSkyDome()
    {
        // 查找或创建SkyDome对象
        GameObject skyDome = GameObject.Find("SkyDome");
        if (skyDome == null)
        {
            skyDome = new GameObject("SkyDome");
            Undo.RegisterCreatedObjectUndo(skyDome, "Create SkyDome");
        }
        
        // 添加SkyDome脚本（自动添加MeshFilter和MeshRenderer）
        SkyDome dome = skyDome.GetComponent<SkyDome>();
        if (dome == null)
        {
            dome = skyDome.AddComponent<SkyDome>();
        }
        
        // 查找天空盒材质
        Material skyMat = FindSkyboxMaterial();
        if (skyMat != null)
        {
            dome.skyboxMaterial = skyMat;
        }
        
        // 设置默认参数
        dome.domeRadius = 500;
        dome.subdivisions = 64;
        
        // 选中对象
        Selection.activeGameObject = skyDome;
        
        Debug.Log("SkyDome created! Don't forget to:");
        Debug.Log("1. Assign your Skybox Material");
        Debug.Log("2. Remove Skybox from Render Settings");
    }
    
    private static Material FindSkyboxMaterial()
    {
        string[] guids = AssetDatabase.FindAssets("t:Material l:ProceduralSkyboxMat");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }
        
        // 尝试找名字包含"Procedural"的材质
        guids = AssetDatabase.FindAssets("Procedural t:Material");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null && mat.shader.name == "Skybox/Procedural")
            {
                return mat;
            }
        }
        
        return null;
    }
    
    [MenuItem("Tools/Remove Skybox from Render Settings")]
    public static void RemoveSkyboxFromSettings()
    {
        RenderSettings.skybox = null;
        Debug.Log("Skybox removed from Render Settings!");
    }
}