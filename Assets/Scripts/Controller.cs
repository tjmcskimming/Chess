using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;

public class Game : MonoBehaviour
{
    public GameObject piece_o; // TODO, this is wierd I don't know why I have to do this
    private Piece.Color currentPlayer;
    [SerializeField] private AudioSource piece_move_sound;
    private List<Piece[,]> board_history = new();
    private Piece[,] current_board = new Piece[8, 8];   

    void Start()
    {
        currentPlayer = Piece.Color.White;
        piece_move_sound.Play();
        CreateAndSetupPiece(Piece.PieceType.King, Piece.Color.White, 1, 5);
        CreateAndSetupPiece(Piece.PieceType.Queen, Piece.Color.White, 1, 4);
        CreateAndSetupPiece(Piece.PieceType.Bishop, Piece.Color.White, 1, 6);
        CreateAndSetupPiece(Piece.PieceType.Bishop, Piece.Color.White, 1, 3);
        CreateAndSetupPiece(Piece.PieceType.Knight, Piece.Color.White, 1, 7);
        CreateAndSetupPiece(Piece.PieceType.Knight, Piece.Color.White, 1, 2);
        CreateAndSetupPiece(Piece.PieceType.Rook, Piece.Color.White, 1, 8);
        CreateAndSetupPiece(Piece.PieceType.Rook, Piece.Color.White, 1, 1);
        for (int i = 1; i <= 8; i++)
        {
            CreateAndSetupPiece(Piece.PieceType.Pawn, Piece.Color.White, 2, i);
        }

        CreateAndSetupPiece(Piece.PieceType.King, Piece.Color.Black, 8, 5);
        CreateAndSetupPiece(Piece.PieceType.Queen, Piece.Color.Black, 8, 4);
        CreateAndSetupPiece(Piece.PieceType.Bishop, Piece.Color.Black, 8, 6);
        CreateAndSetupPiece(Piece.PieceType.Bishop, Piece.Color.Black, 8, 3);
        CreateAndSetupPiece(Piece.PieceType.Knight, Piece.Color.Black, 8, 7);
        CreateAndSetupPiece(Piece.PieceType.Knight, Piece.Color.Black, 8, 2);
        CreateAndSetupPiece(Piece.PieceType.Rook, Piece.Color.Black, 8, 8);
        CreateAndSetupPiece(Piece.PieceType.Rook, Piece.Color.Black, 8, 1);
        for (int i = 1; i <= 8; i++)
        {
            CreateAndSetupPiece(Piece.PieceType.Pawn, Piece.Color.Black, 7, i);
        }
        // add the current board state to the history
        // Create a deep copy of the current board
        Piece[,] boardCopy = new Piece[8, 8];
        Array.Copy(current_board, boardCopy, current_board.Length);
        board_history.Add(boardCopy);
    }
    
    [CanBeNull]
    private Piece GetPieceAt(int file, int rank, Piece[,] board = null)
    {
        if (board == null)
        {
            return current_board[file - 1, rank - 1];
        }
        else
        {
            return board[file - 1, rank - 1];
        }
    }

    private void SetPieceAt(int file, int rank, [CanBeNull] Piece piece)
    {
        current_board[file - 1, rank - 1] = piece;
        piece?.SetRankAndFile(rank, file);
    }

    private void CreateAndSetupPiece(Piece.PieceType type, Piece.Color color, int rank, int file)
    {
        GameObject obj = Instantiate(piece_o, new Vector3(0, 0, -1), Quaternion.identity);
        Piece piece = obj.GetComponent<Piece>();
        piece.name = $"{color}_{type}".ToLower();
        piece.Activate(type, color, rank, file);
        piece.TryMove += OnPieceTriedMove;
        SetPieceAt(file, rank, piece);
    }

    private void OnPieceTriedMove(Piece piece, int to_rank, int to_file)
    {
        Debug.Log("Piece tried move");
        Debug.Log("Check legality");
        
        // check position is on board
        if (!IsOnBoard(to_rank, to_file))
        {
            Debug.Log("New position not on board");
            piece.SetRankAndFile(piece.rank, piece.file);
            return;
        }

        // check piece has moved
        if (!PieceHasMoved(piece, to_rank, to_file))
        {
            Debug.Log("Piece did not move");
            piece.SetRankAndFile(piece.rank, piece.file);
            return;
        }
        
        // check there is no ally on the target square
        if (GetPieceAt(to_file, to_rank)?.color == piece.color)
        {
            Debug.Log("Square occupied by Ally Piece");
            piece.SetRankAndFile(piece.rank, piece.file);
            return;
        }
        
        // piece specific rules
        bool legal = true;
        switch (piece.pieceType)
        {
            case Piece.PieceType.King:
                var king_move_type = IsLegalKingMove(piece, to_rank, to_file);
                legal &= (king_move_type != KingMoveType.Illegal);
                if (king_move_type == KingMoveType.CastleFile1)
                {
                    Piece castling_rook = GetPieceAt(1, piece.rank);
                    var new_rook_file = piece.file + Math.Sign(to_file - piece.file);
                    SetPieceAt(new_rook_file, piece.rank, castling_rook);
                    SetPieceAt(castling_rook.file, castling_rook.rank, null);
                }
                break;
            case Piece.PieceType.Queen:
                legal &= IsLegalQueenMove(piece, to_rank, to_file);
                break;
            case Piece.PieceType.Bishop:
                legal &= IsLegalBishopMove(piece, to_rank, to_file);
                break;
            case Piece.PieceType.Knight:
                legal &= IsLegalKnightMove(piece, to_rank, to_file);
                break;
            case Piece.PieceType.Rook:
                legal &= IsLegalRookMove(piece, to_rank, to_file);
                break;
            case Piece.PieceType.Pawn:
                // Pawn is a special case due to pesky en Pessant move, and promotion
                var pawn_move_type =  IsLegalPawnMove(piece, to_rank, to_file);
                legal &= (pawn_move_type != PawnMoveType.Illegal);
                if (pawn_move_type == PawnMoveType.EnPessant)
                {
                    Destroy(GetPieceAt(to_file, piece.rank).GameObject());
                }
                if (pawn_move_type == PawnMoveType.Promotion)
                {
                    Piece.PieceType promotion_piece_type = Piece.PieceType.Queen;
                    piece.Activate(promotion_piece_type, piece.color, piece.rank, piece.file);
                }
                break;
        }

        if (!legal)
        {
            Debug.Log("Not a legal move");
            piece.SetRankAndFile(piece.rank, piece.file);
            return;
        }

        Piece square_occupant = GetPieceAt(to_file, to_rank);
        if (square_occupant != null)
        {
            Destroy(square_occupant.gameObject);
        }

        SetPieceAt(piece.file, piece.rank, null);
        SetPieceAt(to_file, to_rank, piece);
        
        piece_move_sound.Play();
        
        // Create a deep copy of the current board
        Piece[,] boardCopy = new Piece[8, 8];
        Array.Copy(current_board, boardCopy, current_board.Length);
        board_history.Add(boardCopy);
        
    }

    private bool IsLegalMove(Piece piece, int to_rank, int to_file)
    {
        // check position is on board
        if (!IsOnBoard(to_rank, to_file))
        {
            Debug.Log("New position not on board");
            piece.SetRankAndFile(piece.rank, piece.file);
            return false;
        }

        // check piece has moved
        if (!PieceHasMoved(piece, to_rank, to_file))
        {
            Debug.Log("Piece did not move");
            piece.SetRankAndFile(piece.rank, piece.file);
            return false;
        }
        
        // check there is no ally on the target square
        if (GetPieceAt(to_file, to_rank)?.color == piece.color)
        {
            Debug.Log("Square occupied by Ally Piece");
            piece.SetRankAndFile(piece.rank, piece.file);
            return false;
        }
        
        // piece specific rules
        bool legal = true;
        switch (piece.pieceType)
        {
            case Piece.PieceType.King:
                var king_move_type = IsLegalKingMove(piece, to_rank, to_file);
                legal &= (king_move_type != KingMoveType.Illegal);
                if (king_move_type == KingMoveType.CastleFile1)
                {
                    Piece castling_rook = GetPieceAt(1, piece.rank);
                    var new_rook_file = piece.file + Math.Sign(to_file - piece.file);
                    SetPieceAt(new_rook_file, piece.rank, castling_rook);
                    SetPieceAt(castling_rook.file, castling_rook.rank, null);
                }
                break;
            case Piece.PieceType.Queen:
                legal &= IsLegalQueenMove(piece, to_rank, to_file);
                break;
            case Piece.PieceType.Bishop:
                legal &= IsLegalBishopMove(piece, to_rank, to_file);
                break;
            case Piece.PieceType.Knight:
                legal &= IsLegalKnightMove(piece, to_rank, to_file);
                break;
            case Piece.PieceType.Rook:
                legal &= IsLegalRookMove(piece, to_rank, to_file);
                break;
            case Piece.PieceType.Pawn:
                // Pawn is a special case due to pesky en Pessant move, and promotion
                var pawn_move_type =  IsLegalPawnMove(piece, to_rank, to_file);
                legal &= (pawn_move_type != PawnMoveType.Illegal);
                if (pawn_move_type == PawnMoveType.EnPessant)
                {
                    Destroy(GetPieceAt(to_file, piece.rank).GameObject());
                }
                if (pawn_move_type == PawnMoveType.Promotion)
                {
                    Piece.PieceType promotion_piece_type = Piece.PieceType.Queen;
                    piece.Activate(promotion_piece_type, piece.color, piece.rank, piece.file);
                }
                break;
        }
    }

    private bool IsOnBoard(int to_rank, int to_file)
    {
        return to_rank >= 1 && to_rank <= 8 && to_file >= 1 && to_file <= 8;
    }

    private bool PieceHasMoved(Piece piece, int to_rank, int to_file)
    {
        return piece.file != to_file || piece.rank != to_rank;
    }

    private enum KingMoveType
    {
        Normal,
        CastleFile1,
        CastleFile8,
        Illegal
    }

    private KingMoveType IsLegalKingMove(Piece piece, int to_rank, int to_file)
    {
        var delta_rank = to_rank - piece.rank;
        var delta_file = to_file - piece.file;
        var king_move_type = KingMoveType.Illegal;
        if (Math.Abs(delta_rank) <= 1 && Math.Abs(delta_file) <= 1)
        {
            king_move_type = KingMoveType.Normal;
        }

        bool isTwoFileStepDown = delta_file == 2 && delta_rank == 0;
        bool king_or_rook_has_moved = false;
        for (int i = board_history.Count - 1; i >= 0; i--)
        {
            var rook_spot_piece = GetPieceAt(1, piece.rank, board_history[i]);
            var king_spot_piece = GetPieceAt(piece.rank, piece.file, board_history[i]);
            if (rook_spot_piece == null
                || !(rook_spot_piece.pieceType == Piece.PieceType.Rook 
                     && rook_spot_piece.color == piece.color)
                || king_spot_piece != piece)
            {
                king_or_rook_has_moved = true;
                break;
            }
        }

        bool king_in_check = false;
        bool path_has_check = false;
        bool path_is_clear = true;

        return king_move_type;
    }

    private bool IsKingInCheck(Piece king, Piece[,] board = null)
    {
        if (board == null) board = current_board;
        foreach (Piece piece in board)
        {
            if (IsLegalMove(piece, king.rank, king.file))
            {
                return true;
            }
        }
        return false;
    }

    private bool IsLegalQueenMove(Piece piece, int to_rank, int to_file)
    {
        var delta_rank = to_rank - piece.rank;
        var delta_file = to_file - piece.file;
        bool legal = true;
        // check is horizontal or vertical
        if ((delta_rank != 0 && delta_file != 0)
            && Math.Abs(delta_rank) != Math.Abs(delta_file))
        {
            return false;
        }
        // check intermediate squares are empty
        for (int i = 1; i < Math.Max(Math.Abs(delta_rank), Math.Abs(delta_file)); i++)
        {
            legal &= (GetPieceAt(piece.file +(i*Math.Sign(delta_file)), piece.rank+(i*Math.Sign(delta_rank))) == null);
        }
        return legal;
    }

    private bool IsLegalBishopMove(Piece piece, int to_rank, int to_file)
    {
        var delta_rank = to_rank - piece.rank;
        var delta_file = to_file - piece.file;
        bool legal = true;
        // check is diagonal
        if (Math.Abs(delta_rank) != Math.Abs(delta_file))
        {
            return false;
        }
        // check squares along diagonal are empty
        for (int i = 1; i < Math.Abs(delta_rank); i++)
        {
            legal &= (GetPieceAt(piece.file +(i*Math.Sign(delta_file)), piece.rank+(i*Math.Sign(delta_rank))) == null);
        }
        return legal;
    }

    private bool IsLegalKnightMove(Piece piece, int to_rank, int to_file)
    {
        var delta_rank = to_rank - piece.rank;
        var delta_file = to_file - piece.file;
        // simply check for L shape
        return (Math.Abs(delta_rank) == 1 && Math.Abs(delta_file) == 2)
            || (Math.Abs(delta_file) == 1 && Math.Abs(delta_rank) == 2);
    }

    private bool IsLegalRookMove(Piece piece, int to_rank, int to_file)
    {
        var delta_rank = to_rank - piece.rank;
        var delta_file = to_file - piece.file;
        bool legal = true;
        
        // check is horizontal or vertical
        if (delta_rank != 0 && delta_file != 0)
        {
            return false;
        }
        // check intermediate squares are empty
        for (int i = 1; i < Math.Max(Math.Abs(delta_rank), Math.Abs(delta_file)); i++)
        {
            legal &= (GetPieceAt(piece.file +(i*Math.Sign(delta_file)), piece.rank+(i*Math.Sign(delta_rank))) == null);
        }
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

    private PawnMoveType IsLegalPawnMove(Piece piece, int to_rank, int to_file)
    {
        var delta_rank = to_rank - piece.rank;
        var delta_file = to_file - piece.file;

        if (IsNormalPawnMove(piece, to_rank, to_file, delta_rank, delta_file)
        || IsPawnDoubleMove(piece, to_rank, to_file, delta_rank, delta_file)
        || IsPawnAttackMove(piece, to_rank, to_file, delta_rank, delta_file))
        {
            int promotionRank = (piece.color == Piece.Color.White) ? 8 : 1;
            return to_rank == promotionRank ? PawnMoveType.Promotion : PawnMoveType.Normal;
        }
        if (IsEnPassantMove(piece, to_rank, to_file, delta_rank, delta_file))
        {
            return PawnMoveType.EnPessant;
        }

        return PawnMoveType.Illegal;
    }
    
    private bool IsNormalPawnMove(Piece piece, int to_rank, int to_file, int delta_rank, int delta_file)
    {
        bool isForwardMove = delta_rank == (piece.color == Piece.Color.Black ? -1 : 1);
        bool isStraightMove = delta_file == 0;
        bool isPathClear = GetPieceAt(to_file, to_rank) == null;

        return isForwardMove && isStraightMove && isPathClear;
    }
    
    private bool IsPawnDoubleMove(Piece piece, int to_rank, int to_file, int delta_rank, int delta_file)
    {
        bool isStartingPosition = piece.rank == (piece.color == Piece.Color.Black ? 7 : 2);
        bool isDoubleForwardMove = delta_rank == (piece.color == Piece.Color.Black ? -2 : 2);
        bool isStraightMove = delta_file == 0;
        bool isPathClear = GetPieceAt(to_file, to_rank) == null;
        bool isIntermediateSquareClear = GetPieceAt(piece.file, piece.rank + (piece.color == Piece.Color.Black ? -1 : 1)) == null;

        return isStartingPosition && isDoubleForwardMove && isStraightMove && isPathClear && isIntermediateSquareClear;
    }

    private bool IsPawnAttackMove(Piece piece, int to_rank, int to_file, int delta_rank, int delta_file)
    {
        bool isForwardMove = delta_rank == (piece.color == Piece.Color.Black ? -1 : 1);
        bool isDiagonalMove = Math.Abs(delta_file) == 1;
        Piece targetPiece = GetPieceAt(to_file, to_rank);
        bool isCapturing = targetPiece != null && targetPiece.color != piece.color;

        return isForwardMove && isDiagonalMove && isCapturing;
    }
    
    private bool IsEnPassantMove(Piece piece, int to_rank, int to_file, int delta_rank, int delta_file)
    {
        // enPessant only possible at specific rank
        if (piece.rank != (piece.color == Piece.Color.Black ? 4 : 5))
        {
            return false;
        }

        int stepDirection = (piece.color == Piece.Color.Black) ? -1 : 1;
        bool isForwardMove = delta_rank == stepDirection;
        bool isSidewaysMove = Math.Abs(delta_file) == 1;

        Piece adjacentPiece = GetPieceAt(to_file, piece.rank);
        bool isEnemyPawnAdjacent = adjacentPiece != null 
                                   && adjacentPiece.pieceType == Piece.PieceType.Pawn 
                                   && adjacentPiece.color != piece.color;
        
        int enemyPawnStartRank = piece.rank + 2 * stepDirection;
        bool didEnemyPawnJustDoubleMove = GetPieceAt(to_file, enemyPawnStartRank, board_history[^2]) == adjacentPiece;
        
        return isForwardMove && isSidewaysMove && isEnemyPawnAdjacent && didEnemyPawnJustDoubleMove;
    }
}
