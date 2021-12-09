
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Threading.Tasks;

public class Board : MonoBehaviour
{
    public Tilemap Tilemap { get; private set; }
    public Piece ActivePiece { get; private set; }
    public Task ClearingLines { get; private set; }
    public int Score { get; private set; }
    public int CurrentLevel { get; private set; }
    public TetrominoData[] tetrominoes;
    public Vector2Int boardSize = new Vector2Int(10, 20);
    public Vector3Int nextPiecePosition = new Vector3Int(10, 10, 0);
    public TileBase destroyedTile;
    public float destroyTime;
    public TileBase incrementTile;
    public Board otherBoard;
    public TMP_Text levelText;
    public TMP_Text scoreText;
    private List<int> tetrominoesID = new List<int>();
    private TetrominoData nextPiece;
    private int currentLinesCompleted = 0;

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
        Score = 0;
        currentLinesCompleted = 0;
        CurrentLevel = 1;
        nextPiece = GetRandomPiece();
        SpawnPiece();
        scoreText.text = $"Score: {Score}";
        levelText.text = $"Level: {CurrentLevel}";
    }

    public void SpawnPiece()
    {

        TetrominoData data = nextPiece;

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

        Clear(nextPiece, nextPiecePosition);
        nextPiece = GetRandomPiece();
    }

    private TetrominoData GetRandomPiece()
    {
        if (tetrominoesID.Count == 0)
        {
            for (int i = 0; i < tetrominoes.Length; i++)
            {
                tetrominoesID.Add(i);
            }
            for (int i = tetrominoes.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (tetrominoesID[i], tetrominoesID[j]) = (tetrominoesID[j], tetrominoesID[i]);
            }
        }
        int random = Random.Range(0, tetrominoesID.Count);

        TetrominoData data = tetrominoes[tetrominoesID[random]];
        tetrominoesID.RemoveAt(random);
        Set(data, nextPiecePosition);
        return data;
    }

    public async void PieceLocked()
    {
        ClearingLines = TryClearLines();
        await ClearingLines;
        SpawnPiece();
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
    public void Set(TetrominoData piece, Vector3Int position)
    {
        for (int i = 0; i < piece.cells.Length; i++)
        {
            Vector3Int tilePosition = (Vector3Int)piece.cells[i] + position;
            Tilemap.SetTile(tilePosition, piece.tile);
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
    public void Clear(TetrominoData piece, Vector3Int position)
    {
        for (int i = 0; i < piece.cells.Length; i++)
        {
            Vector3Int tilePosition = (Vector3Int)piece.cells[i] + position;
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
            await CearLines(rowsToClear);
        else
            await Task.Yield();
    }

    private async Task CearLines(List<int> rowsToClear)
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

        currentLinesCompleted += rowsToClear.Count;
        if(currentLinesCompleted >= 10)
        {
            currentLinesCompleted -= 10;
            CurrentLevel++;
        }
        Score += 50 * Factorial(rowsToClear.Count) * (CurrentLevel + 1);
        otherBoard.Increment(rowsToClear.Count, Random.Range(bounds.xMin, bounds.xMax));

        scoreText.text = $"Score: {Score}";
        levelText.text = $"Level: {CurrentLevel}";
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
            n += 0.15f;
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


    public async void Increment(int rowsAmount, int column)
    {
        if (ClearingLines != null)
            await ClearingLines;
        //MOVE EVERYTHING UP
        RectInt bounds = Bounds;
        int row = bounds.yMax + rowsAmount;
        while (row >= bounds.yMin + rowsAmount)
        {
            for (int col = bounds.xMin; col < bounds.xMax; col++)
            {
                Vector3Int position = new Vector3Int(col, row - rowsAmount, 0);
                TileBase below = FilterTile(position, ActivePiece);

                position = new Vector3Int(col, row, 0);
                Tilemap.SetTile(position, below);
            }
            row--;
        }

        //ADD TILES UNDER
        List<int> rowsToFill = new List<int>();
        for (int i = 0; i < rowsAmount; i++)
        {
            rowsToFill.Add(i - boardSize.y / 2);
        }
        SetTileOnRows(rowsToFill, incrementTile);
        for (int i = 0; i < rowsAmount; i++)
        {
            Vector3Int position = new Vector3Int(column, i - boardSize.y / 2, 0);
            Tilemap.SetTile(position, null);
        }

        //CHECK IF PLAYER IS STUCK
        int n = 1;
        while (!IsValidPosition(ActivePiece, ActivePiece.Position))
        {
            ActivePiece.Move(Vector2Int.up * n);
            n++;
            if (n > boardSize.y)
            {
                GameOver();
            }
        }
    }

    private TileBase FilterTile(Vector3Int position, Piece piece)
    {
        for (int i = 0; i < piece.Cells.Length; i++)
        {
            if (Equals(position, (piece.Cells[i]) + piece.Position))
            {
                return null;
            }
        }

        return Tilemap.GetTile(position);

    }

    private int Factorial(int n)
    {
        int factorial = 1;
        for (int i = 1; i <= n; i++)
        {
            factorial *= i;
        }
        return factorial;
    }
}
