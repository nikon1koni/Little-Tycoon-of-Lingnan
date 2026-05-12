using UnityEngine;

public class BoardGenerator : MonoBehaviour
{
    public GameObject gridTilePrefab; // ���� GridTile Ԥ����
    public int rows = 3; // ����������������ĵ�ͼ������
    public float tileSize = 1f; // ���Ӵ�С����ƽ�����Ŷ�Ӧ��
    public Vector2 offset = new Vector2(0, 0); // ���Ӽ��ƫ��

    void Start()
    {
        GenerateBoard();
    }

    void GenerateBoard()
    {
        for (int row = 0; row < rows; row++)
        {
            // ���㵱ǰ�еĸ������������Σ���n���� 2n+1 �����������ĵ�ͼ������
            int tileCount = row + 1; // ʾ������һ��1�����ڶ���2������ƥ����ĵ�ͼ��
            for (int col = 0; col < tileCount; col++)
            {
                // ʵ��������
                GameObject tile = Instantiate(gridTilePrefab, transform);
                // ����λ�ã����ε������߼���x��zƫ�ƣ��ø��ӳ��������У�
                float x = (col - tileCount / 2f) * tileSize + offset.x;
                float z = (row - rows / 2f) * tileSize + offset.y;
                tile.transform.position = new Vector3(x, 0, z);
                // ����ѡ�����ø��ӵ���ת�������θ���Ȼ���� 45�ȣ�����ԣ�
                tile.transform.rotation = Quaternion.Euler(0, 45, 0);
            }
        }
    }
}