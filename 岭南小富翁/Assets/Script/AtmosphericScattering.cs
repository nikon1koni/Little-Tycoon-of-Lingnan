using UnityEngine;

[ExecuteInEditMode]
public class AtmosphericScattering : MonoBehaviour
{
    [Header("Atmosphere Settings")]
    public float planetRadius = 6371000f;
    public float atmosphereRadius = 6471000f;
    public Vector3 sunDirection = new Vector3(0.5f, 0.5f, -0.5f);
    
    [Header("Scattering Parameters")]
    public float rayleighScaleHeight = 8000f;
    public float mieScaleHeight = 1200f;
    public float mieScattering = 0.05f;
    
    [Header("Color Settings")]
    public Color rayleighColor = new Color(0.8f, 0.85f, 1.0f);
    public Color mieColor = new Color(1.0f, 0.9f, 0.7f);
    
    private Material atmosphereMaterial;
    private Mesh atmosphereMesh;
    
    void Awake()
    {
        CreateAtmosphereMesh();
        CreateAtmosphereMaterial();
    }
    
    void CreateAtmosphereMesh()
    {
        atmosphereMesh = new Mesh();
        int latCount = 32;
        int lonCount = 64;
        
        Vector3[] vertices = new Vector3[(latCount + 1) * lonCount];
        int[] triangles = new int[latCount * lonCount * 6];
        
        for (int lat = 0; lat <= latCount; lat++)
        {
            float theta = Mathf.PI * (float)lat / latCount;
            float sinTheta = Mathf.Sin(theta);
            float cosTheta = Mathf.Cos(theta);
            
            for (int lon = 0; lon < lonCount; lon++)
            {
                float phi = 2 * Mathf.PI * (float)lon / lonCount;
                float sinPhi = Mathf.Sin(phi);
                float cosPhi = Mathf.Cos(phi);
                
                int i = lat * lonCount + lon;
                vertices[i] = new Vector3(
                    atmosphereRadius * sinTheta * cosPhi,
                    atmosphereRadius * cosTheta,
                    atmosphereRadius * sinTheta * sinPhi
                );
            }
        }
        
        int triIdx = 0;
        for (int lat = 0; lat < latCount; lat++)
        {
            for (int lon = 0; lon < lonCount; lon++)
            {
                int v0 = lat * lonCount + lon;
                int v1 = (lat + 1) * lonCount + lon;
                int v2 = (lat + 1) * lonCount + ((lon + 1) % lonCount);
                int v3 = lat * lonCount + ((lon + 1) % lonCount);
                
                triangles[triIdx++] = v0;
                triangles[triIdx++] = v1;
                triangles[triIdx++] = v2;
                
                triangles[triIdx++] = v0;
                triangles[triIdx++] = v2;
                triangles[triIdx++] = v3;
            }
        }
        
        atmosphereMesh.vertices = vertices;
        atmosphereMesh.triangles = triangles;
        atmosphereMesh.RecalculateNormals();
        
        MeshFilter mf = gameObject.AddComponent<MeshFilter>();
        mf.sharedMesh = atmosphereMesh;
        
        MeshRenderer mr = gameObject.AddComponent<MeshRenderer>();
        mr.material = atmosphereMaterial;
        mr.renderingLayerMask = 1;
    }
    
    void CreateAtmosphereMaterial()
    {
        atmosphereMaterial = new Material(Shader.Find("Custom/Atmosphere"));
        UpdateMaterialProperties();
    }
    
    void UpdateMaterialProperties()
    {
        if (atmosphereMaterial == null) return;
        
        atmosphereMaterial.SetFloat("_PlanetRadius", planetRadius);
        atmosphereMaterial.SetFloat("_AtmosphereRadius", atmosphereRadius);
        atmosphereMaterial.SetVector("_SunDirection", sunDirection.normalized);
        atmosphereMaterial.SetFloat("_RayleighScaleHeight", rayleighScaleHeight);
        atmosphereMaterial.SetFloat("_MieScaleHeight", mieScaleHeight);
        atmosphereMaterial.SetFloat("_MieScattering", mieScattering);
        atmosphereMaterial.SetColor("_RayleighColor", rayleighColor);
        atmosphereMaterial.SetColor("_MieColor", mieColor);
    }
    
    void Update()
    {
        UpdateMaterialProperties();
        
        if (Camera.main != null)
        {
            transform.position = Camera.main.transform.position;
        }
    }
    
    void OnDestroy()
    {
        if (atmosphereMesh != null)
            DestroyImmediate(atmosphereMesh);
        
        if (atmosphereMaterial != null)
            DestroyImmediate(atmosphereMaterial);
    }
}