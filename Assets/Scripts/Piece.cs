using System;
using UnityEngine;

public class Piece : MonoBehaviour
{
    public Sprite 
        white_king,
        white_queen,
        white_bishop,
        white_knight,
        white_rook,
        white_pawn,
        black_king,
        black_queen,
        black_bishop,
        black_knight,
        black_rook,
        black_pawn;
    
    
    public enum PieceType
    {
        King,
        Queen,
        Bishop,
        Knight,
        Rook,
        Pawn
    }

    public enum Color
    {
        Black,
        White
    }
    
    public PieceType pieceType;
    public Color color;
    public int rank;
    public int file;
    public Action<Piece, int, int> TryMove;
    public AudioSource piece_move_sound;
    
    public void Activate(PieceType pieceType, Color color, int rank, int file)
    {
        this.pieceType = pieceType;
        this.color = color;
        GetComponent<SpriteRenderer>().sprite = InitSprite();
        SetRankAndFile(rank, file);
    }

    public void SetRankAndFile(int rank, int file)
    {
        this.rank = rank;
        this.file = file;
        transform.position = new Vector3(file, rank, -1);
    }

    Sprite InitSprite()
    {
        Sprite sprite = black_king;
        switch (color)
        {
            case Color.White:
                switch (this.pieceType)
                {
                    case PieceType.King:
                        sprite = white_king;
                        break;
                    case PieceType.Queen:
                        sprite = white_queen;
                        break;
                    case PieceType.Bishop:
                        sprite = white_bishop;
                        break;
                    case PieceType.Knight:
                        sprite = white_knight;
                        break;
                    case PieceType.Rook:
                        sprite = white_rook;
                        break;
                    case PieceType.Pawn:
                        sprite = white_pawn;
                        break;
                }
                break;
            case Color.Black:
                switch (this.pieceType)
                {
                    case PieceType.King:
                        sprite = black_king;
                        break;
                    case PieceType.Queen:
                        sprite = black_queen;
                        break;
                    case PieceType.Bishop:
                        sprite = black_bishop;
                        break;
                    case PieceType.Knight:
                        sprite = black_knight;
                        break;
                    case PieceType.Rook:
                        sprite = black_rook;
                        break;
                    case PieceType.Pawn:
                        sprite = black_pawn;
                        break;
                }
                break;
        }
        return sprite;
    }


    void OnMouseDown()
    {
        var pos = transform.position;
        Vector3 clicked_screen_coord = Camera.main.WorldToScreenPoint(pos);
        screen_zCoordinate = clicked_screen_coord.z;
    }

    float screen_zCoordinate;
    Vector3 GetMouseWorldPos()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = screen_zCoordinate - 1;
        return Camera.main.ScreenToWorldPoint(mousePoint);
    }

    void OnMouseDrag()
    {
        transform.position = GetMouseWorldPos();
    }

    void OnMouseUp()
    {
        var mouse_pos_w = GetMouseWorldPos();
        int new_file = (int)Math.Round(mouse_pos_w.x);
        int new_rank = (int)Math.Round(mouse_pos_w.y);
        TryMove.Invoke(this, new_rank, new_file);
    }
    
}