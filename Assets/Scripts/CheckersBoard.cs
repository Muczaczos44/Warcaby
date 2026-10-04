using UnityEngine;
using Unity.Netcode; 

public class CheckersBoard : NetworkBehaviour
{
    [Header("Wygląd (Prefaby)")]
    public GameObject tilePrefab;
    public GameObject whitePiecePrefab;
    public GameObject blackPiecePrefab;

    [Header("Ustawienia Chwytania (Drag & Drop)")]
    public float dragHeight = 0.8f;
    public float wiggleSpeed = 14f;
    public float wiggleAmount = 10f;

    private GameObject[,] board = new GameObject[8, 8];
    private GameObject[,] pieces = new GameObject[8, 8];
    private bool[,] isKing = new bool[8, 8];

    public bool isWhiteTurn = true;

    private GameObject draggedPiece = null;
    private Vector2Int dragStartCoord;
    private Vector3 dragStartPos;
    private Camera mainCamera;
    
    private Plane boardPlane = new Plane(Vector3.up, Vector3.zero);

    private void Start()
    {
        mainCamera = Camera.main;
        GenerateBoard();
        SpawnPieces();
    }

    private void Update()
    {
        if (!NetworkManager.Singleton.IsListening) return;

        HandleDragAndDrop();
    }

    private void HandleDragAndDrop()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2Int coord = GetMouseBoardCoordinates();
            
            if (IsValidCoord(coord.x, coord.y) && pieces[coord.x, coord.y] != null)
            {
                bool isPieceWhite = pieces[coord.x, coord.y].name.StartsWith("Bialy_");

                if (isPieceWhite == isWhiteTurn)
                {
                    draggedPiece = pieces[coord.x, coord.y];
                    dragStartCoord = coord;
                    dragStartPos = draggedPiece.transform.position;
                }
            }
        }

        if (draggedPiece != null && Input.GetMouseButton(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (boardPlane.Raycast(ray, out float distance))
            {
                Vector3 hitPoint = ray.GetPoint(distance);
                float wiggleX = Mathf.Sin(Time.time * wiggleSpeed) * wiggleAmount;
                float wiggleZ = Mathf.Cos(Time.time * wiggleSpeed * 0.8f) * wiggleAmount;

                draggedPiece.transform.position = new Vector3(hitPoint.x, dragHeight, hitPoint.z);
                draggedPiece.transform.rotation = Quaternion.Euler(wiggleX, 0, wiggleZ);
            }
        }

        if (draggedPiece != null && Input.GetMouseButtonUp(0))
        {
            Vector2Int targetCoord = GetMouseBoardCoordinates();
            bool moveSuccessful = false;

            if (IsValidCoord(targetCoord.x, targetCoord.y))
            {
                if (ValidMove(dragStartCoord.x, dragStartCoord.y, targetCoord.x, targetCoord.y))
                {
                    SubmitMoveServerRpc(dragStartCoord.x, dragStartCoord.y, targetCoord.x, targetCoord.y);
                    moveSuccessful = true;
                }
            }

            if (!moveSuccessful)
            {
                draggedPiece.transform.position = dragStartPos;
            }

            draggedPiece.transform.rotation = Quaternion.identity;
            draggedPiece = null;
        }
    }
    
    
    [ServerRpc(RequireOwnership = false)]
    private void SubmitMoveServerRpc(int startX, int startZ, int endX, int endZ)
    {
        ExecuteMoveClientRpc(startX, startZ, endX, endZ);
    }
    
    [ClientRpc]
    private void ExecuteMoveClientRpc(int startX, int startZ, int endX, int endZ)
    {
        ExecuteMove(startX, startZ, endX, endZ);
        isWhiteTurn = !isWhiteTurn; 
        Debug.Log(isWhiteTurn ? "Tura BIAŁYCH" : "Tura CZARNYCH");
    }

    // -----------------------------

    private Vector2Int GetMouseBoardCoordinates()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (boardPlane.Raycast(ray, out float distance))
        {
            Vector3 hitPoint = ray.GetPoint(distance);
            int x = Mathf.RoundToInt(hitPoint.x);
            int z = Mathf.RoundToInt(hitPoint.z);
            return new Vector2Int(x, z);
        }
        return new Vector2Int(-1, -1);
    }

    private bool IsValidCoord(int x, int z)
    {
        return x >= 0 && x < 8 && z >= 0 && z < 8;
    }

    private void GenerateBoard()
    {
        for (int x = 0; x < 8; x++)
        {
            for (int z = 0; z < 8; z++)
            {
                GameObject tile = Instantiate(tilePrefab, new Vector3(x, 0, z), Quaternion.identity, transform);
                tile.name = $"Pole_{x}_{z}";
                tile.transform.localScale = new Vector3(1f, 0.1f, 1f);
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
                        SpawnSinglePiece(blackPiecePrefab, x, z, false);
                    }
                    else if (z > 4)
                    {
                        SpawnSinglePiece(whitePiecePrefab, x, z, true);
                    }
                }
            }
        }
    }

    private void SpawnSinglePiece(GameObject prefab, int x, int z, bool isWhite)
    {
        if (prefab == null) return;

        GameObject piece = Instantiate(prefab, new Vector3(x, 0.15f, z), Quaternion.identity, transform);
        piece.name = (isWhite ? "Bialy_" : "Czarny_") + $"{x}_{z}";

        Renderer rend = piece.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = isWhite ? Color.white : Color.gray;
        }

        pieces[x, z] = piece;
        isKing[x, z] = false;
    }

    private bool ValidMove(int startX, int startZ, int endX, int endZ)
    {
        if (pieces[endX, endZ] != null) return false;

        int deltaX = Mathf.Abs(endX - startX);
        int deltaZ = endZ - startZ;
        bool pieceIsKing = isKing[startX, startZ];

        if (deltaX == 1)
        {
            if (pieceIsKing && Mathf.Abs(deltaZ) == 1) return true;

            int allowedDirection = isWhiteTurn ? -1 : 1;
            if (deltaZ == allowedDirection) return true;
        }

        if (deltaX == 2)
        {
            if (!pieceIsKing)
            {
                int allowedDirection = isWhiteTurn ? -2 : 2;
                if (deltaZ != allowedDirection) return false;
            }
            else
            {
                if (Mathf.Abs(deltaZ) != 2) return false;
            }

            int midX = (startX + endX) / 2;
            int midZ = (startZ + endZ) / 2;

            if (pieces[midX, midZ] != null)
            {
                bool midIsWhite = pieces[midX, midZ].name.StartsWith("Bialy_");
                if (midIsWhite != isWhiteTurn)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void ExecuteMove(int startX, int startZ, int endX, int endZ)
    {
        GameObject piece = pieces[startX, startZ];
        bool pieceIsKing = isKing[startX, startZ];

        if (Mathf.Abs(endX - startX) == 2)
        {
            int midX = (startX + endX) / 2;
            int midZ = (startZ + endZ) / 2;

            if (pieces[midX, midZ] != null)
            {
                Destroy(pieces[midX, midZ]);
                pieces[midX, midZ] = null;
                isKing[midX, midZ] = false;
            }
        }

        piece.transform.position = new Vector3(endX, 0.15f, endZ);
        pieces[endX, endZ] = piece;
        pieces[startX, startZ] = null;

        isKing[endX, endZ] = pieceIsKing;
        isKing[startX, startZ] = false;

        CheckPromotion(endX, endZ);
    }

    private void CheckPromotion(int x, int z)
    {
        if ((isWhiteTurn && z == 0) || (!isWhiteTurn && z == 7))
        {
            if (!isKing[x, z])
            {
                isKing[x, z] = true;
                GameObject piece = pieces[x, z];
                piece.transform.localScale = new Vector3(0.8f, 0.5f, 0.8f);

                Renderer rend = piece.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.material.color = Color.yellow;
                }

                Debug.Log($"Pionek na [{x}, {z}] awansował na DAMKĘ!");
            }
        }
    }
}