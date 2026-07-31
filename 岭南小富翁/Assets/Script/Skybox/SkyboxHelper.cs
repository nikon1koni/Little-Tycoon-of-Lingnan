using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SkyboxHelper : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Tools/Create Procedural SkyDome")]
    public static void CreateSkyDome()
    {
        // ??????SkyDome????
        GameObject skyDome = GameObject.Find("SkyDome");
        if (skyDome == null)
        {
            skyDome = new GameObject("SkyDome");
            Undo.RegisterCreatedObjectUndo(skyDome, "Create SkyDome");
        }
        
        // ????SkyDome????????????MeshFilter??MeshRenderer??
        SkyDome dome = skyDome.GetComponent<SkyDome>();
        if (dome == null)
        {
            dome = skyDome.AddComponent<SkyDome>();
        }
        
        // ????????????
        Material skyMat = FindSkyboxMaterial();
        if (skyMat != null)
        {
            dome.skyboxMaterial = skyMat;
        }
        
        // ??????????
        dome.domeRadius = 500;
        dome.subdivisions = 64;
        
        // ???????
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
        
        // ?????????????"Procedural"?????
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
#endif
}