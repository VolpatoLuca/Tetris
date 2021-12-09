using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class Piece : MonoBehaviour
{
    public Board Board { get; private set; }
    public TetrominoData TetrominoData { get; private set; }
    public Vector3Int[] Cells { get; private set; }
    public Vector3Int Position { get; private set; }
    public int RotationIndex { get; private set; }
    public bool IsLocked { get; private set; }

    public float stepDelay = 1;
    public float lockDelay = 0.5f;

    private float stepTime;
    private float lockTime;
    private Vector2Int bufferedMovement;
    private bool hardDrop = false;
    private bool down = false;
    private int bufferedRotation = 0;


    public void Initialize(Board board, Vector3Int position, TetrominoData data)
    {
        Board = board;
        Position = position;
        TetrominoData = data;
        RotationIndex = 0;
        stepTime = Time.time + stepDelay;
        lockTime = 0;
        IsLocked = false;

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
        if (IsLocked) return;
        Board.Clear(this);

        lockTime += Time.deltaTime;

        GetInput();

        if (Time.time >= stepTime)
        {
            Step();
        }

        if (IsLocked) return;
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

    private void Lock()
    {
        IsLocked = true;
        Board.Set(this);
        Board.PieceLocked();
        Board.Set(this);

    }
    public void SetMovement(InputAction.CallbackContext ctx)
    {
        if (ctx.phase != InputActionPhase.Performed)
            return;
        bufferedMovement = Vector2Int.right * (int)ctx.ReadValue<float>();
    }

    public void SetHardDrop(InputAction.CallbackContext ctx)
    {
        if (ctx.phase != InputActionPhase.Performed)
            return;
        hardDrop = true;
    }
    public void SetRotation(InputAction.CallbackContext ctx)
    {
        if (ctx.phase != InputActionPhase.Performed)
            return;
        bufferedRotation = (int)ctx.ReadValue<float>();
    }
    public void SetDown(InputAction.CallbackContext ctx)
    {
        if (ctx.phase != InputActionPhase.Performed)
            return;
        down = true;
    }


    private void GetInput()
    {
        //MOVEMENT
        if (bufferedMovement != Vector2Int.zero)
        {
            Move(bufferedMovement);
            bufferedMovement = Vector2Int.zero;
        }
        //DOWN
        if (down)
        {
            Move(Vector2Int.down);
            down = false;
        }
        //HARDDROP
        if (hardDrop)
        {
            HardDrop();
            hardDrop = false;
        }
        //ROTATE
        if (bufferedRotation != 0)
        {
            Rotate(bufferedRotation);
            bufferedRotation = 0;
        }
    }

    public void Rotate(int direction)
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

    public void HardDrop()
    {
        while (Move(Vector2Int.down))
        {
            continue;
        }
        lockTime = lockDelay * 0.7f;
    }

    public bool Move(Vector2Int translation)
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
