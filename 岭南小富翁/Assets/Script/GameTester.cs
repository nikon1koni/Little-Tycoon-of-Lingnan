// GameTester.cs - ??????????????????????
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameTester : MonoBehaviour
{
    void Update()
    {
        // ??????????
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            GoToTile(0);
        }
    }

    void RestartGame()
    {
        // ???????
        SceneManager.LoadScene("New");
        Debug.Log("?????????");
    }

    void GoToTile(int tileIndex)
    {
        // ???????????1
        var tiles = FindObjectsOfType<BoardTile>();
        if (tiles.Length > tileIndex)
        {
            var tile = tiles[tileIndex];
            Debug.Log($"?????: {tile.tileName}");
        }
        else
        {
            Debug.LogWarning($"¦Ä????????{tileIndex}????");
        }
    }
}
