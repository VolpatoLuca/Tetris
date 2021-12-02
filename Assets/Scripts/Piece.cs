using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Piece : MonoBehaviour
{
    public Board Board { get; private set; }
    public TetrominoData TetrominoData { get; private set; }
    public Vector3Int[] Cells { get; private set; }
    public Vector3Int Position { get; private set; }
    public int RotationIndex { get; private set; }

    public float stepDelay = 1;
    public float lockDelay = 0.5f;

    private float stepTime;
    private float lockTime;

    public bool isLocked;

    public void Initialize(Board board, Vector3Int position, TetrominoData data)
    {
        Board = board;
        Position = position;
        TetrominoData = data;
        RotationIndex = 0;
        stepTime = Time.time + stepDelay;
        lockTime = 0;
        isLocked = false;

        if (Cells == null)
        {
            Cells = new Vector3Int[data.cells.Length];
        }

        for (int i = 0; i < Cells.Length; i++)
        {
            Cells[i] = (Vector3Int)data.cells[i];
        }
    }

    private void Update()
    {
        if(isLocked) return;
        Board.Clear(this);

        lockTime += Time.deltaTime;

        GetInput();

        if (Time.time >= stepTime)
        {
            Step();
        }

        if(isLocked) return;
        Board.Set(this);
    }

    private void Step()
    {
        stepTime = Time.time + stepDelay;

        Move(Vector2Int.down);

        if (lockTime > lockDelay)
        {
            Lock();
        }
    }

    private async void Lock()
    {
        isLocked = true;
        Board.Set(this);
        await Board.TryClearLines();
        Board.SpawnPiece();
    }

    private void GetInput()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            Move(Vector2Int.left);
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            Move(Vector2Int.right);
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            Move(Vector2Int.down);
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            HardDrop();
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Rotate(-1);
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            Rotate(1);
        }
    }

    private void Rotate(int direction)
    {
        int originalRotation = RotationIndex;
        RotationIndex = Wrap(RotationIndex + direction, 0, 4);
        ApplyRotationMatrix(direction);
        if (!TestWallKicks(RotationIndex, direction))
        {
            RotationIndex = originalRotation;
            ApplyRotationMatrix(-direction);
        }
    }

    private void ApplyRotationMatrix(int direction)
    {
        float[] matrix = Data.RotationMatrix;
        for (int i = 0; i < Cells.Length; i++)
        {
            Vector3 cell = Cells[i];
            int x, y;
            switch (TetrominoData.tetromino)
            {
                case Tetromino.I:
                case Tetromino.O:
                    cell.x -= 0.5f;
                    cell.y -= 0.5f;
                    x = Mathf.CeilToInt((cell.x * matrix[0] * direction) + (cell.y * matrix[1] * direction));
                    y = Mathf.CeilToInt((cell.x * matrix[2] * direction) + (cell.y * matrix[3] * direction));
                    break;
                default:
                    x = Mathf.RoundToInt((cell.x * matrix[0] * direction) + (cell.y * matrix[1] * direction));
                    y = Mathf.RoundToInt((cell.x * matrix[2] * direction) + (cell.y * matrix[3] * direction));
                    break;
            }
            Cells[i] = new Vector3Int(x, y, 0);
        }
    }
    private bool TestWallKicks(int rotationIndex, int rotationDirection)
    {
        int wallKickIndex = GetWallKickIndex(rotationIndex, rotationDirection);
        for (int i = 0; i < TetrominoData.wallKicks.GetLength(1); i++)
        {
            Vector2Int translation = TetrominoData.wallKicks[wallKickIndex, i];
            if (Move(translation))
            {
                return true;
            }
        }
        return false;
    }

    private int GetWallKickIndex(int rotationIndex, int rotationDirection)
    {
        int wallKickIndex = rotationIndex * 2;

        if (rotationDirection < 0)
        {
            wallKickIndex--;
        }
        return Wrap(wallKickIndex, 0, TetrominoData.wallKicks.GetLength(0));
    }

    private void HardDrop()
    {
        while (Move(Vector2Int.down))
        {
            continue;
        }

        Lock();
    }

    private bool Move(Vector2Int translation)
    {
        Vector3Int newPosition = Position;
        newPosition.x += translation.x;
        newPosition.y += translation.y;
        bool valid = Board.IsValidPosition(this, newPosition);
        if (valid)
        {
            Position = newPosition;
            lockTime = 0f;
        }

        return valid;
    }

    private int Wrap(int input, int min, int max)
    {
        if (input < min)
        {
            return max - (min - input) % (max - min);
        }
        else
        {
            return min + (input - min) % (max - min);
        }
    }
}
