
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Threading.Tasks;

public class Board : MonoBehaviour
{
    public Tilemap Tilemap { get; private set; }
    public Piece ActivePiece { get; private set; }
    public TetrominoData[] tetrominoes;
    public Vector2Int boardSize = new Vector2Int(10, 20);

    public TileBase destroyedTile;
    public float destroyTime;

    public RectInt Bounds
    {
        get
        {
            Vector2Int position = new Vector2Int(-boardSize.x / 2, -boardSize.y / 2);
            return new RectInt(position, boardSize);
        }
    }

    private void Awake()
    {
        Tilemap = GetComponentInChildren<Tilemap>();
        ActivePiece = GetComponent<Piece>();
        for (int i = 0; i < tetrominoes.Length; i++)
        {
            tetrominoes[i].Initialize();
        }
    }

    private void Start()
    {
        SpawnPiece();
    }

    public void SpawnPiece()
    {
        int random = Random.Range(0, tetrominoes.Length);
        TetrominoData data = tetrominoes[random];

        Vector3Int spawnPosition = new Vector3Int()
        {
            x = -1,
            y = 9 - data.cells.Max(x => x.y),
            z = 0
        };

        ActivePiece.Initialize(this, spawnPosition, data);
        if (IsValidPosition(ActivePiece, spawnPosition))
        {
            Set(ActivePiece);
        }
        else
        {
            GameOver();
        }
    }

    private void GameOver()
    {
        Tilemap.ClearAllTiles();

    }

    public void Set(Piece piece)
    {
        for (int i = 0; i < piece.Cells.Length; i++)
        {
            Vector3Int tilePosition = piece.Cells[i] + piece.Position;
            Tilemap.SetTile(tilePosition, piece.TetrominoData.tile);
        }
    }

    public void Clear(Piece piece)
    {
        for (int i = 0; i < piece.Cells.Length; i++)
        {
            Vector3Int tilePosition = piece.Cells[i] + piece.Position;
            Tilemap.SetTile(tilePosition, null);
        }
    }

    public bool IsValidPosition(Piece piece, Vector3Int position)
    {
        for (int i = 0; i < piece.Cells.Length; i++)
        {
            Vector3Int tilePosition = piece.Cells[i] + position;

            if (!Bounds.Contains((Vector2Int)tilePosition))
            {
                return false;
            }
            if (Tilemap.HasTile(tilePosition))
            {
                return false;
            }
        }
        return true;
    }

    public async Task TryClearLines()
    {
        RectInt bounds = Bounds;
        int row = bounds.yMin;
        List<int> rowsToClear = new List<int>();
        while (row < bounds.yMax)
        {
            bool isRowFull = IsLineFull(row);
            if (isRowFull)
            {
                rowsToClear.Add(row);
            }
            row++;
        }

        if (rowsToClear.Count > 0)
            await LineClear(rowsToClear);
        else
            await Task.Yield();
    }

    private async Task LineClear(List<int> rowsToClear)
    {
        RectInt bounds = Bounds;
        for (int i = 0; i < rowsToClear.Count; i++)
        {
            int row = rowsToClear[i];
            for (int col = bounds.xMin; col < bounds.xMax; col++)
            {
                Vector3Int position = new Vector3Int(col, row, 0);
                Tilemap.SetTile(position, destroyedTile);

            }
        }

        await FlashRows(rowsToClear);

        SetTileOnRows(rowsToClear, null);

        for (int j = 0; j < rowsToClear.Count; j++)
        {
            rowsToClear[j] -= j;
        }
        for (int i = 0; i < rowsToClear.Count; i++)
        {
            int row = rowsToClear[i];
            while (row < bounds.yMax)
            {
                for (int col = bounds.xMin; col < bounds.xMax; col++)
                {
                    Vector3Int position = new Vector3Int(col, row + 1, 0);
                    TileBase above = Tilemap.GetTile(position);

                    position = new Vector3Int(col, row, 0);
                    Tilemap.SetTile(position, above);
                }
                row++;
            }
        }

    }

    private async Task FlashRows(List<int> rowsToClear)
    {
        float t = 0;
        float n = 0;
        while (t < destroyTime)
        {
            if (Mathf.RoundToInt(n) % 2 == 0)
            {
                SetTileOnRows(rowsToClear, null);
            }
            else
            {
                SetTileOnRows(rowsToClear, destroyedTile);
            }
            n += 0.25f;
            t += Time.deltaTime;
            await Task.Yield();
        }
    }

    private void SetTileOnRows(List<int> rowsToClear, TileBase tile)
    {
        RectInt bounds = Bounds;
        for (int i = 0; i < rowsToClear.Count; i++)
        {
            int row = rowsToClear[i];
            for (int col = bounds.xMin; col < bounds.xMax; col++)
            {
                Vector3Int position = new Vector3Int(col, row, 0);
                Tilemap.SetTile(position, tile);
            }
        }
    }

    private bool IsLineFull(int row)
    {
        RectInt bounds = Bounds;
        for (int col = bounds.xMin; col < bounds.xMax; col++)
        {
            Vector3Int position = new Vector3Int(col, row, 0);
            if (!Tilemap.HasTile(position))
            {
                return false;
            }
        }
        return true;
    }
}
