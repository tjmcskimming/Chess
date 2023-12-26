using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;

public class Game : MonoBehaviour
{
    [SerializeField] private GameObject pieceObject; 
    [SerializeField] private AudioSource pieceMoveSound;
    private List<Piece[,]> _boardHistory = new();
    private Piece[,] _currentBoard = new Piece[8, 8];

    private void Start()
    {
        pieceMoveSound.Play();
        CreateAndSetupPiece(Piece.Type.King, Piece.Color.White, 1, 5);
        CreateAndSetupPiece(Piece.Type.Queen, Piece.Color.White, 1, 4);
        CreateAndSetupPiece(Piece.Type.Bishop, Piece.Color.White, 1, 6);
        CreateAndSetupPiece(Piece.Type.Bishop, Piece.Color.White, 1, 3);
        CreateAndSetupPiece(Piece.Type.Knight, Piece.Color.White, 1, 7);
        CreateAndSetupPiece(Piece.Type.Knight, Piece.Color.White, 1, 2);
        CreateAndSetupPiece(Piece.Type.Rook, Piece.Color.White, 1, 8);
        CreateAndSetupPiece(Piece.Type.Rook, Piece.Color.White, 1, 1);
        for (int i = 1; i <= 8; i++)
        {
            CreateAndSetupPiece(Piece.Type.Pawn, Piece.Color.White, 2, i);
        }

        CreateAndSetupPiece(Piece.Type.King, Piece.Color.Black, 8, 5);
        CreateAndSetupPiece(Piece.Type.Queen, Piece.Color.Black, 8, 4);
        CreateAndSetupPiece(Piece.Type.Bishop, Piece.Color.Black, 8, 6);
        CreateAndSetupPiece(Piece.Type.Bishop, Piece.Color.Black, 8, 3);
        CreateAndSetupPiece(Piece.Type.Knight, Piece.Color.Black, 8, 7);
        CreateAndSetupPiece(Piece.Type.Knight, Piece.Color.Black, 8, 2);
        CreateAndSetupPiece(Piece.Type.Rook, Piece.Color.Black, 8, 8);
        CreateAndSetupPiece(Piece.Type.Rook, Piece.Color.Black, 8, 1);
        for (int i = 1; i <= 8; i++)
        {
            CreateAndSetupPiece(Piece.Type.Pawn, Piece.Color.Black, 7, i);
        }
        // add the current board state to the history
        // Create a deep copy of the current board
        Piece[,] boardCopy = new Piece[8, 8];
        Array.Copy(_currentBoard, boardCopy, _currentBoard.Length);
        _boardHistory.Add(boardCopy);
    }
    
    [CanBeNull]
    private Piece GetPieceAt(int file, int rank, Piece[,] board = null)
    {
        return board == null ? _currentBoard[file - 1, rank - 1] : board[file - 1, rank - 1];
    }

    private void SetPieceAt(int file, int rank, [CanBeNull] Piece piece)
    {
        _currentBoard[file - 1, rank - 1] = piece;
        if (piece != null)
        {
            piece.SetRankAndFile(rank, file);
        }
    }

    private void CreateAndSetupPiece(Piece.Type type, Piece.Color color, int rank, int file)
    {
        GameObject obj = Instantiate(pieceObject, new Vector3(0, 0, -1), Quaternion.identity);
        Piece piece = obj.GetComponent<Piece>();
        piece.name = $"{color}_{type}".ToLower();
        piece.Activate(type, color, rank, file);
        piece.TryMove += OnPieceTriedMove;
        SetPieceAt(file, rank, piece);
    }

    // Triggered when a Piece tries to move.
    // checks legality, then handles special moves
    // then performs move and updates board
    private void OnPieceTriedMove(Piece piece, int toRank, int toFile)
    {
        Debug.Log("Piece tried move");
        Debug.Log("Check legality");
        bool legal = IsLegalMove(piece, toRank, toFile); 
        
        if (!legal)
        {
            Debug.Log("Not a legal move");
            piece.SetRankAndFile(piece.Rank, piece.File);
            return;
        }

        // handle castling
        if (piece.PieceType == Piece.Type.King)
        {
            KingMoveType kingMoveType = IsLegalKingMove(piece, toRank, toFile);
            if (kingMoveType == KingMoveType.Castle)
            {
                int rookFile = toFile - piece.File < 0 ? 1 : 8;
                Piece castlingRook = GetPieceAt(rookFile, piece.Rank);
                Debug.Assert(castlingRook != null, "Piece in rook position was null");
                int newRookFile = piece.File + Math.Sign(toFile - piece.File);
                SetPieceAt(newRookFile, piece.Rank, castlingRook);
                SetPieceAt(castlingRook.File, castlingRook.Rank, null);
            }
        }
        // handle en pessant and promotion
        else if (piece.PieceType == Piece.Type.Pawn)
        {
            // Pawn is a special case due to pesky en Pessant move, and promotion
            PawnMoveType pawnMoveType = IsLegalPawnMove(piece, toRank, toFile);
            if (pawnMoveType == PawnMoveType.EnPessant)
            {
                Destroy(GetPieceAt(toFile, piece.Rank).GameObject());
            }
            if (pawnMoveType == PawnMoveType.Promotion)
            {
                // TODO: Piece Type Selection
                Piece.Type promotionPieceType = Piece.Type.Queen;
                piece.Activate(promotionPieceType, piece.PieceColor, piece.Rank, piece.File);
            } 
        }

        Piece squareOccupant = GetPieceAt(toFile, toRank);
        if (squareOccupant != null)
        {
            Destroy(squareOccupant.gameObject);
        }

        SetPieceAt(piece.File, piece.Rank, null);
        SetPieceAt(toFile, toRank, piece);
        
        pieceMoveSound.Play();
        
        // Create a deep copy of the current board
        Piece[,] boardCopy = new Piece[8, 8];
        Array.Copy(_currentBoard, boardCopy, _currentBoard.Length);
        _boardHistory.Add(boardCopy);
        
    }

    private bool IsLegalMove(Piece piece, int toRank, int toFile)
    {
        // check position is on board
        if (!IsOnBoard(toRank, toFile))
        {
            Debug.Log("New position not on board");
            piece.SetRankAndFile(piece.Rank, piece.File);
            return false;
        }

        // check piece has moved
        if (!PieceHasMoved(piece, toRank, toFile))
        {
            Debug.Log("Piece did not move");
            piece.SetRankAndFile(piece.Rank, piece.File);
            return false;
        }
        
        // check there is no ally on the target square
        if (GetPieceAt(toFile, toRank)?.PieceColor == piece.PieceColor)
        {
            Debug.Log("Square occupied by Ally Piece");
            piece.SetRankAndFile(piece.Rank, piece.File);
            return false;
        }
        
        // piece specific rules
        bool legal = true;
        switch (piece.PieceType)
        {
            case Piece.Type.King:
                KingMoveType kingMoveType = IsLegalKingMove(piece, toRank, toFile);
                legal &= (kingMoveType != KingMoveType.Illegal);
                break;
            case Piece.Type.Queen:
                legal &= IsLegalQueenMove(piece, toRank, toFile);
                break;
            case Piece.Type.Bishop:
                legal &= IsLegalBishopMove(piece, toRank, toFile);
                break;
            case Piece.Type.Knight:
                legal &= IsLegalKnightMove(piece, toRank, toFile);
                break;
            case Piece.Type.Rook:
                legal &= IsLegalRookMove(piece, toRank, toFile);
                break;
            case Piece.Type.Pawn:
                PawnMoveType pawnMoveType =  IsLegalPawnMove(piece, toRank, toFile);
                legal &= (pawnMoveType != PawnMoveType.Illegal);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return legal;
    }

    private static bool IsOnBoard(int toRank, int toFile)
    {
        return toRank is >= 1 and <= 8 && toFile is >= 1 and <= 8;
    }

    private static bool PieceHasMoved(Piece piece, int toRank, int toFile)
    {
        return piece.File != toFile || piece.Rank != toRank;
    }

    private enum KingMoveType
    {
        Normal,
        Castle,
        Illegal
    }

    private KingMoveType IsLegalKingMove(Piece king, int toRank, int toFile)
    {
        int deltaRank = toRank - king.Rank;
        int deltaFile = toFile - king.File;
        if (Math.Abs(deltaRank) <= 1 && Math.Abs(deltaFile) <= 1)
        {
            return KingMoveType.Normal;
        }

        // check for castling
        bool isTwoFileStepDown = deltaFile == -2 && deltaRank == 0;
        bool isTwoFileStepUp = deltaFile == 2 && deltaRank == 0;

        int rookFile;
        if (isTwoFileStepDown)
        {
            rookFile = 1;
        }
        else if (isTwoFileStepUp)
        {
            rookFile = 8;
        }
        else
        {
            return KingMoveType.Illegal;
        }

        Piece rook = GetPieceAt(rookFile, king.Rank);
        if (rook == null || rook.PieceType != Piece.Type.Rook || rook.PieceColor != king.PieceColor) 
        {
            return KingMoveType.Illegal;
        }
        
        // traverse history to check that king and rook have not moved
        bool kingHasMoved = false;
        bool rookHasMoved = false;
        for (int i = _boardHistory.Count - 1; i >= 0; i--)
        {
            Piece rookSpotPiece = GetPieceAt(rookFile, king.Rank, _boardHistory[i]);
            Piece kingSpotPiece = GetPieceAt( king.File, king.Rank, _boardHistory[i]);
            if (rookSpotPiece == null || rookSpotPiece != rook)
            {
                rookHasMoved = true;
                break;
            }
            if (kingSpotPiece == null || kingSpotPiece != king)
            {
                kingHasMoved = true;
                break;
            }
        }

        bool kingInCheck = IsSpaceThreatened(king.File, king.Rank, king.PieceColor);
        bool pathHasCheck = IsSpaceThreatened(king.File + Math.Sign(deltaFile), king.Rank, king.PieceColor);
        bool pathIsClear = PathIsClear(king.Rank, king.File, toRank, toFile);

        if (!rookHasMoved
            && !kingHasMoved
            && !kingInCheck
            && !pathHasCheck
            && pathIsClear)
        {
            return KingMoveType.Castle;
        }

        return KingMoveType.Illegal;
    }
    
    // determines whether a space can be attacked
    private bool IsSpaceThreatened(int file, int rank, Piece.Color color, Piece[,] board = null)
    {
        board ??= _currentBoard;
        foreach (Piece piece in board)
        {
            if (piece == null || piece.PieceColor == color)
            {
                continue;
            }
            if (IsLegalMove(piece, rank, file))
            {
                return true;
            }
        }
        return false;
    }

    // determines whether any pieces are on the squares between [file, rank] and [toFile, toRank]
    // works for horizontal, vertical and diagonal moves
    private bool PathIsClear(int rank, int file, int toRank, int toFile)
    {
        int deltaRank = toRank - rank;
        int deltaFile = toFile - file;
        bool clear = true;
        for (int i = 1; i < Math.Max(Math.Abs(deltaRank), Math.Abs(deltaFile)); i++)
        {
            clear &= (GetPieceAt(file +(i*Math.Sign(deltaFile)), rank+(i*Math.Sign(deltaRank))) == null);
        }

        return clear;
    }

    private bool IsLegalQueenMove(Piece piece, int toRank, int toFile)
    {
        int deltaRank = toRank - piece.Rank;
        int deltaFile = toFile - piece.File;
        bool legal = true;
        // check is horizontal or vertical
        if ((deltaRank != 0 && deltaFile != 0)
            && Math.Abs(deltaRank) != Math.Abs(deltaFile))
        {
            return false;
        }
        // check intermediate squares are empty
        legal &= PathIsClear(piece.Rank, piece.File, toRank, toFile);
        return legal;
    }

    private bool IsLegalBishopMove(Piece piece, int toRank, int toFile)
    {
        int deltaRank = toRank - piece.Rank;
        int deltaFile = toFile - piece.File;
        bool legal = true;
        // check is diagonal
        if (Math.Abs(deltaRank) != Math.Abs(deltaFile))
        {
            return false;
        }
        // check squares along diagonal are empty
        legal &= PathIsClear(piece.Rank, piece.File, toRank, toFile);
        return legal;
    }

    private bool IsLegalKnightMove(Piece piece, int toRank, int toFile)
    {
        int deltaRank = toRank - piece.Rank;
        int deltaFile = toFile - piece.File;
        // simply check for L shape
        return (Math.Abs(deltaRank) == 1 && Math.Abs(deltaFile) == 2)
            || (Math.Abs(deltaFile) == 1 && Math.Abs(deltaRank) == 2);
    }

    private bool IsLegalRookMove(Piece piece, int toRank, int toFile)
    {
        int deltaRank = toRank - piece.Rank;
        int deltaFile = toFile - piece.File;
        bool legal = true;
        
        // check is horizontal or vertical
        if (deltaRank != 0 && deltaFile != 0)
        {
            return false;
        }
        // check intermediate squares are empty
        legal &= PathIsClear(piece.Rank, piece.File, toRank, toFile);
        return legal;
    }
    
    // Pawns behave differently to all other pieces,
    // so we need some extra information beyond legal/illegal
    private enum PawnMoveType
    {
        Illegal,
        Normal,
        EnPessant,
        Promotion
    }

    private PawnMoveType IsLegalPawnMove(Piece piece, int toRank, int toFile)
    {
        int deltaRank = toRank - piece.Rank;
        int deltaFile = toFile - piece.File;

        if (IsNormalPawnMove(piece, toRank, toFile, deltaRank, deltaFile)
        || IsPawnDoubleMove(piece, toRank, toFile, deltaRank, deltaFile)
        || IsPawnAttackMove(piece, toRank, toFile, deltaRank, deltaFile))
        {
            int promotionRank = (piece.PieceColor == Piece.Color.White) ? 8 : 1;
            return toRank == promotionRank ? PawnMoveType.Promotion : PawnMoveType.Normal;
        }
        if (IsEnPassantMove(piece, toRank, toFile, deltaRank, deltaFile))
        {
            return PawnMoveType.EnPessant;
        }

        return PawnMoveType.Illegal;
    }
    
    private bool IsNormalPawnMove(Piece piece, int toRank, int toFile, int deltaRank, int deltaFile)
    {
        bool isForwardMove = deltaRank == (piece.PieceColor == Piece.Color.Black ? -1 : 1);
        bool isStraightMove = deltaFile == 0;
        bool isPathClear = GetPieceAt(toFile, toRank) == null;

        return isForwardMove && isStraightMove && isPathClear;
    }
    
    private bool IsPawnDoubleMove(Piece piece, int toRank, int toFile, int deltaRank, int deltaFile)
    {
        bool isStartingPosition = piece.Rank == (piece.PieceColor == Piece.Color.Black ? 7 : 2);
        bool isDoubleForwardMove = deltaRank == (piece.PieceColor == Piece.Color.Black ? -2 : 2);
        bool isStraightMove = deltaFile == 0;
        bool isPathClear = GetPieceAt(toFile, toRank) == null;
        bool isIntermediateSquareClear = GetPieceAt(piece.File, piece.Rank + (piece.PieceColor == Piece.Color.Black ? -1 : 1)) == null;

        return isStartingPosition && isDoubleForwardMove && isStraightMove && isPathClear && isIntermediateSquareClear;
    }

    private bool IsPawnAttackMove(Piece piece, int toRank, int toFile, int deltaRank, int deltaFile)
    {
        bool isForwardMove = deltaRank == (piece.PieceColor == Piece.Color.Black ? -1 : 1);
        bool isDiagonalMove = Math.Abs(deltaFile) == 1;
        Piece targetPiece = GetPieceAt(toFile, toRank);
        bool isCapturing = targetPiece != null && targetPiece.PieceColor != piece.PieceColor;

        return isForwardMove && isDiagonalMove && isCapturing;
    }
    
    private bool IsEnPassantMove(Piece piece, int toRank, int toFile, int deltaRank, int deltaFile)
    {
        // enPessant only possible at specific rank
        if (piece.Rank != (piece.PieceColor == Piece.Color.Black ? 4 : 5))
        {
            return false;
        }

        int stepDirection = (piece.PieceColor == Piece.Color.Black) ? -1 : 1;
        bool isForwardMove = deltaRank == stepDirection;
        bool isSidewaysMove = Math.Abs(deltaFile) == 1;

        Piece adjacentPiece = GetPieceAt(toFile, piece.Rank);
        bool isEnemyPawnAdjacent = adjacentPiece != null 
                                   && adjacentPiece.PieceType == Piece.Type.Pawn 
                                   && adjacentPiece.PieceColor != piece.PieceColor;
        
        int enemyPawnStartRank = piece.Rank + 2 * stepDirection;
        bool didEnemyPawnJustDoubleMove = GetPieceAt(toFile, enemyPawnStartRank, _boardHistory[^2]) == adjacentPiece;
        
        return isForwardMove && isSidewaysMove && isEnemyPawnAdjacent && didEnemyPawnJustDoubleMove;
    }
}
