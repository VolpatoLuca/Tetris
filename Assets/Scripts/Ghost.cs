using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Ghost : MonoBehaviour
{
    public Tile tile;
    public Board board;
    public Piece trackingPiece;

    public Tilemap tilemap { get; private set; }
    public Vector3Int[] Cells { get; private set; }
    public Vector3Int Position { get; private set; }

    private void Awake()
    {
        tilemap = GetComponentInChildren<Tilemap>();
        Cells = new Vector3Int[4];
    }

    private void LateUpdate()
    {
        Clear();
        if (trackingPiece.isLocked) return;
        Copy();
        Drop();
        Set();
    }

    private void Clear()
    {
        for (int i = 0; i < Cells.Length; i++)
        {
            Vector3Int tilePosition = Cells[i] + Position;
            tilemap.SetTile(tilePosition, null);
        }
    }

    private void Copy()
    {
        for (int i = 0; i < Cells.Length; i++)
        {
            Cells[i] = trackingPiece.Cells[i];
        }
    }

    private void Drop()
    {
        Vector3Int position = trackingPiece.Position;

        int currentRow = position.y;
        int bottom = -board.boardSize.y / 2 - 1;

        board.Clear(trackingPiece);

        for (int row = currentRow; row >= bottom; row--)
        {
            position.y = row;

            if (board.IsValidPosition(trackingPiece, position))
            {
                Position = position;
            }
            else
            {
                break;
            }
        }

        print("ghost set");
        board.Set(trackingPiece);
    }

    private void Set()
    {
        for (int i = 0; i < Cells.Length; i++)
        {
            Vector3Int tilePosition = Cells[i] + Position;
            tilemap.SetTile(tilePosition, tile);
        }
    }
}
