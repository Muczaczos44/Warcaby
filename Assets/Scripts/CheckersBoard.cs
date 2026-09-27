using UnityEngine;

public class CheckersBoard : MonoBehaviour
{
    [Header("Wygląd gry (podepnij tutaj obiekty)")]
    public GameObject tilePrefab;
    public GameObject whitePiecePrefab;
    public GameObject blackPiecePrefab;
    
    private GameObject[,] board = new GameObject[8, 8];
    private GameObject[,] pieces = new GameObject[8, 8];

    private void Start()
    {
        GenerateBoard();
        SpawnPieces();
    }

    private void GenerateBoard()
    {
        for (int x = 0; x < 8; x++)
        {
            for (int z = 0; z < 8; z++)
            {
                GameObject tile = Instantiate(tilePrefab, new Vector3(x, 0, z), Quaternion.identity, transform);
                tile.name = $"Pole_{x}_{z}";
                board[x, z] = tile;
                
                Renderer rend = tile.GetComponent<Renderer>();
                if (rend != null)
                {
                    bool isBlack = (x + z) % 2 != 0;
                    rend.material.color = isBlack ? Color.black : Color.white;
                }
            }
        }
    }

    private void SpawnPieces()
    {
        for (int x = 0; x < 8; x++)
        {
            for (int z = 0; z < 8; z++)
            {
                if ((x + z) % 2 != 0)
                {
                    if (z < 3)
                    {
                        SpawnSinglePiece(blackPiecePrefab, x, z);
                    }
                    else if (z > 4)
                    {
                        SpawnSinglePiece(whitePiecePrefab, x, z);
                    }
                }
            }
        }
    }

    private void SpawnSinglePiece(GameObject prefab, int x, int z)
    {
        if (prefab == null) return;
        
        GameObject piece = Instantiate(prefab, new Vector3(x, 0.2f, z), Quaternion.identity, transform);
        piece.name = $"Pionek_{x}_{z}";
        pieces[x, z] = piece;
    }
}