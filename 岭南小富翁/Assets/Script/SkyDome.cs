using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SkyDome : MonoBehaviour
{
    [Header("Dome Settings")]
    [Tooltip("天空穹顶的半径，建议设置很大的值")]
    public float domeRadius = 2000f;
    
    [Tooltip("天空穹顶的细分程度")]
    public int subdivisions = 32;
    
    [Header("Material Settings")]
    [Tooltip("天空盒材质")]
    public Material skyboxMaterial;
    
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    
    void Awake()
    {
        SetupDome();
    }
    
    void SetupDome()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        
        if (meshFilter.sharedMesh == null)
        {
            CreateDomeMesh();
        }
        
        if (skyboxMaterial != null)
        {
            meshRenderer.material = skyboxMaterial;
        }
        
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.sortingOrder = -1000;  // 确保先渲染
        
        // 设置渲染顺序为最背景
        meshRenderer.material.renderQueue = 1000;
        
        transform.localScale = Vector3.one;  // 缩放将在网格中处理
    }
    
    void CreateDomeMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "ProceduralSkyDome";
        
        int segments = subdivisions;
        int vertexCount = (segments + 1) * (segments / 2 + 1);
        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        int[] indices = new int[(segments) * (segments / 2) * 6];
        
        int vertexIndex = 0;
        for (int lat = 0; lat <= segments / 2; lat++)
        {
            float theta = lat * Mathf.PI / segments;
            float sinTheta = Mathf.Sin(theta);
            float cosTheta = Mathf.Cos(theta);
            
            for (int lon = 0; lon <= segments; lon++)
            {
                float phi = lon * 2 * Mathf.PI / segments;
                float sinPhi = Mathf.Sin(phi);
                float cosPhi = Mathf.Cos(phi);
                
                Vector3 vertex = new Vector3(
                    cosPhi * sinTheta * domeRadius,
                    cosTheta * domeRadius,
                    sinPhi * sinTheta * domeRadius
                );
                
                vertices[vertexIndex] = vertex;
                uvs[vertexIndex] = new Vector2(
                    (float)lon / segments,
                    (float)lat / (segments / 2)
                );
                
                vertexIndex++;
            }
        }
        
        int indexIndex = 0;
        for (int lat = 0; lat < segments / 2; lat++)
        {
            for (int lon = 0; lon < segments; lon++)
            {
                int current = lat * (segments + 1) + lon;
                int next = current + segments + 1;
                
                indices[indexIndex++] = current;
                indices[indexIndex++] = next;
                indices[indexIndex++] = current + 1;
                
                indices[indexIndex++] = current + 1;
                indices[indexIndex++] = next;
                indices[indexIndex++] = next + 1;
            }
        }
        
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = indices;
        
        // 计算正确的法线
        Vector3[] normals = new Vector3[vertices.Length];
        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = vertices[i].normalized;
        }
        mesh.normals = normals;
        
        // 优化
        mesh.Optimize();
        mesh.RecalculateBounds();
        
        meshFilter.mesh = mesh;
    }
    
    void LateUpdate()
    {
        if (Camera.main != null)
        {
            // 严格跟随相机
            transform.position = Camera.main.transform.position;
        }
    }
    
    public void SetSkyboxMaterial(Material material)
    {
        skyboxMaterial = material;
        if (meshRenderer != null)
        {
            meshRenderer.material = material;
            meshRenderer.material.renderQueue = 1000;
        }
    }
    
    void OnValidate()
    {
        domeRadius = Mathf.Max(500f, domeRadius);
        subdivisions = Mathf.Max(16, subdivisions);
        
        // 如果在编辑器中，重新创建网格
        if (Application.isEditor && !Application.isPlaying)
        {
            if (meshFilter != null)
            {
                CreateDomeMesh();
            }
        }
    }
}