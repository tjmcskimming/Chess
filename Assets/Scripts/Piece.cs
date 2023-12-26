using System;
using UnityEngine;
using Debug = System.Diagnostics.Debug;

public class Piece : MonoBehaviour
{
    // Used to set the sprite within Unity
    public Sprite 
        whiteKing,
        whiteQueen,
        whiteBishop,
        whiteKnight,
        whiteRook,
        whitePawn,
        blackKing,
        blackQueen,
        blackBishop,
        blackKnight,
        blackRook,
        blackPawn;
    
    public enum Type
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
    
    public Type PieceType { get; private set; }
    public Color PieceColor { get; private set; }
    public int Rank { get; private set; }
    public int File { get; private set; }
    public Action<Piece, int, int> TryMove; // Triggered when a player tries a move
   
    // Initialise a new piece, called at beginning and on Pawn promotion
    public void Activate(Type pieceType, Color pieceColor, int startRank, int startFile)
    {
        PieceType = pieceType;
        PieceColor = pieceColor;
        GetComponent<SpriteRenderer>().sprite = SelectSprite();
        SetRankAndFile(startRank, startFile);
    }
    
    public void SetRankAndFile(int newRank, int newFile)
    {
        Rank = newRank;
        File = newFile;
        transform.position = new Vector3(newFile, newRank, -1);
    }

    // Chooses a sprite from Sprites based on Type and Color
    private Sprite SelectSprite()
    {
        Sprite sprite = blackKing;
        switch (PieceColor)
        {
            case Color.White:
                sprite = PieceType switch
                {
                    Type.King => whiteKing,
                    Type.Queen => whiteQueen,
                    Type.Bishop => whiteBishop,
                    Type.Knight => whiteKnight,
                    Type.Rook => whiteRook,
                    Type.Pawn => whitePawn,
                    _ => sprite
                };
                break;
            case Color.Black:
                sprite = PieceType switch
                {
                    Type.King => blackKing,
                    Type.Queen => blackQueen,
                    Type.Bishop => blackBishop,
                    Type.Knight => blackKnight,
                    Type.Rook => blackRook,
                    Type.Pawn => blackPawn,
                    _ => sprite
                };
                break;
        }
        return sprite;
    }
    
    private void OnMouseDown()
    {
        Vector3 pos = transform.position;
        Debug.Assert(Camera.main != null, "Camera.main != null");
        Vector3 clickedScreenCoordinates = Camera.main.WorldToScreenPoint(pos);
        _screenZCoordinate = clickedScreenCoordinates.z;
    }

    private float _screenZCoordinate;

    private Vector3 GetMouseWorldPos()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = _screenZCoordinate - 1;
        Debug.Assert(Camera.main != null, "Camera.main != null");
        return Camera.main.ScreenToWorldPoint(mousePoint);
    }

    private void OnMouseDrag()
    {
        transform.position = GetMouseWorldPos();
    }

    private void OnMouseUp()
    {
        Vector3 mousePosW = GetMouseWorldPos();
        int newFile = (int)Math.Round(mousePosW.x);
        int newRank = (int)Math.Round(mousePosW.y);
        TryMove?.Invoke(this, newRank, newFile);
    }
    
}