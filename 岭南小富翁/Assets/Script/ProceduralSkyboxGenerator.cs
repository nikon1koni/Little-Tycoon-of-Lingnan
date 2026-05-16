using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ProceduralSkyboxGenerator : MonoBehaviour
{
    [Header("Sky Colors")]
    public Color topColor = new Color(0.1f, 0.3f, 0.6f);
    public Color horizonColor = new Color(0.4f, 0.6f, 0.8f);
    public Color groundColor = new Color(0.2f, 0.3f, 0.4f);
    
    [Header("Sun Settings")]
    public Color sunColor = new Color(1.0f, 0.9f, 0.7f);
    public float sunIntensity = 2.0f;
    public float sunSize = 0.05f;
    
    [Header("Atmosphere")]
    public float atmosphereThickness = 1.0f;
    public Color atmosphereColor = new Color(0.8f, 0.6f, 0.4f);
    
    [Header("Clouds")]
    public int cloudLayers = 3;
    public float cloudSpeed = 0.1f;
    public float cloudScale = 5.0f;
    public Color cloudColor = new Color(0.9f, 0.9f, 0.9f);
    
    [Header("Stars")]
    public int starCount = 1000;
    public float starBrightness = 0.5f;
    
    private Texture2D skyTexture;
    private Material skyMaterial;
    private ComputeBuffer starBuffer;
    
    [System.Serializable]
    public struct StarData
    {
        public Vector3 position;
        public float brightness;
    }
    
    private StarData[] stars;
    
    void Awake()
    {
        GenerateSkybox();
        GenerateStars();
    }
    
    void GenerateSkybox()
    {
        int resolution = 512;
        skyTexture = new Texture2D(resolution, resolution, TextureFormat.RGB24, false);
        
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float uvY = (float)y / resolution;
                Color color = Color.Lerp(groundColor, horizonColor, Mathf.Clamp01(uvY * 2));
                color = Color.Lerp(color, topColor, Mathf.Clamp01((uvY - 0.5f) * 2));
                
                skyTexture.SetPixel(x, y, color);
            }
        }
        
        skyTexture.Apply();
        
        skyMaterial = new Material(Shader.Find("Skybox/Procedural"));
        skyMaterial.SetTexture("_SkyTex", skyTexture);
        RenderSettings.skybox = skyMaterial;
    }
    
    void GenerateStars()
    {
        stars = new StarData[starCount];
        for (int i = 0; i < starCount; i++)
        {
            stars[i].position = Random.onUnitSphere;
            stars[i].brightness = Random.Range(0.3f, 1.0f) * starBrightness;
        }
        
        starBuffer = new ComputeBuffer(starCount, System.Runtime.InteropServices.Marshal.SizeOf(typeof(StarData)));
        starBuffer.SetData(stars);
    }
    
    void Update()
    {
        if (starBuffer != null)
        {
            skyMaterial.SetBuffer("_StarBuffer", starBuffer);
            skyMaterial.SetFloat("_Time", Time.time * cloudSpeed);
        }
    }
    
    void OnDestroy()
    {
        if (starBuffer != null)
            starBuffer.Release();
        
        if (skyMaterial != null)
            Destroy(skyMaterial);
        
        if (skyTexture != null)
            Destroy(skyTexture);
    }
}