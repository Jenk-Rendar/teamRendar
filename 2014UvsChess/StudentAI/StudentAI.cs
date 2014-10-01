using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UvsChess;

namespace StudentAI
{
    public class StudentAI : IChessAI
    {
        #region IChessAI Members that are implemented by the Student

        /// <summary>
        /// The name of your AI
        /// </summary>
        public string Name
        {
#if DEBUG
            get { return "Team RENDAR (Debug)"; }
#else
            get { return "Team RENDAR"; }
#endif
        }

        enum PieceValues : int
        {
            Pawn = 1000,
            Knight = 3000,
            Bishop = 3000,
            Rook = 5000,
            Queen = 9000,
            King = 100000
        }

        private class CalcMove
        {
            public ChessMove move;
            public int value;
        }

        int _turnsTaken;
        ChessLocation _ourKingLoc;
        ChessLocation _enemyKingLoc;
        ChessColor _ourColor, _enemyColor;
        // List<ChessMove> _possibleMoves;
        // List<FinalMove> _moveQueue;
        ChessMove _previousMove, _prePreviousMove, _previousCheck, _prePreviousCheck;

        HashSet<ChessLocation> _kingMoves = new HashSet<ChessLocation>()
        {
            {new ChessLocation(-1,-1)},
            {new ChessLocation(0,-1)},
            {new ChessLocation(1,-1)},
            {new ChessLocation(1,0)},
            {new ChessLocation(1,1)},
            {new ChessLocation(0,1)},
            {new ChessLocation(-1,1)},
            {new ChessLocation(-1,0)}
        };

        HashSet<ChessLocation> _pawnMoves = new HashSet<ChessLocation>()
        {
            {new ChessLocation(1,1)},
            {new ChessLocation(0,1)},
            {new ChessLocation(0,2)},
            {new ChessLocation(-1,1)}
        };

        HashSet<ChessLocation> _rookMoves = new HashSet<ChessLocation>()
        {
            {new ChessLocation(0,-1)},
            {new ChessLocation(1,0)},
            {new ChessLocation(0,1)},
            {new ChessLocation(-1,0)}
        };

        HashSet<ChessLocation> _bishopMoves = new HashSet<ChessLocation>()
        {
            {new ChessLocation(-1,-1)},
            {new ChessLocation(1,-1)},
            {new ChessLocation(1,1)},
            {new ChessLocation(-1,1)},
        };

        HashSet<ChessLocation> _knightMoves = new HashSet<ChessLocation>()
        {
            {new ChessLocation(-2,1)},
            {new ChessLocation(-1,2)},
            {new ChessLocation(1,2)},
            {new ChessLocation(2,1)},
            {new ChessLocation(2,-1)},
            {new ChessLocation(1,-2)},
            {new ChessLocation(-1,-2)},
            {new ChessLocation(-2,-1)}
        };

        HashSet<ChessPiece> _slowPieces = new HashSet<ChessPiece>()
        {
            ChessPiece.WhitePawn,
            ChessPiece.BlackPawn,
            ChessPiece.WhiteKnight,
            ChessPiece.BlackKnight,
            ChessPiece.WhiteKing,
            ChessPiece.BlackKing
        };

        /// <summary>
        /// Evaluates the chess board and decided which move to make. This is the main method of the AI.
        /// The framework will call this method when it's your turn.
        /// </summary>
        /// <param name="board">Current chess board</param>
        /// <param name="yourColor">Your color</param>
        /// <returns> Returns the best chess move the player has for the given chess board</returns>
        public ChessMove GetNextMove(ChessBoard board, ChessColor myColor)
        {
            if (_turnsTaken == 0)
            {
                if(myColor == ChessColor.White)
                {
                    _ourKingLoc = new ChessLocation(4, 7);
                    _enemyKingLoc = new ChessLocation(4, 0);
                }
                else
                {
                    _ourKingLoc = new ChessLocation(4, 0);
                    _enemyKingLoc = new ChessLocation(4, 7);
                }
            }

            _turnsTaken++;
            _ourColor = myColor;
            _enemyColor = _ourColor == ChessColor.White ? ChessColor.Black : ChessColor.White;

            List<CalcMove> moves = GenerateMoves(board, myColor, 3);
            List<CalcMove> sortedMoves = SortMoves(moves);

            while (sortedMoves[0].move == _prePreviousMove || sortedMoves[0].move == _prePreviousCheck)
            {
                sortedMoves.Remove(sortedMoves[0]);
            }

            ChessMove selectedMove = sortedMoves[0].move;
            _prePreviousMove = _previousMove;
            _previousMove = selectedMove;

            ChessBoard tempBoard = board.Clone();
            tempBoard[selectedMove.To] = tempBoard[selectedMove.From];
            tempBoard[selectedMove.From] = ChessPiece.Empty;

            if ((tempBoard[selectedMove.To] == ChessPiece.WhitePawn && selectedMove.To.Y == 0) || (tempBoard[selectedMove.To] == ChessPiece.BlackPawn && selectedMove.To.Y == 7))
            {
                tempBoard[selectedMove.To] = myColor == ChessColor.White ? ChessPiece.WhiteQueen : ChessPiece.BlackQueen;
            }

            // Checks if their king is threatened after our move
            if (PieceThreatened(tempBoard,_enemyKingLoc,_enemyColor) > 0)
            {
                if (!CanKingSurvive(tempBoard,_enemyKingLoc,_enemyColor))
                {
                    selectedMove.Flag = ChessFlag.Checkmate;
                }
                else
                {
                    selectedMove.Flag = ChessFlag.Check;
                    _prePreviousCheck = _previousCheck;
                    _previousCheck = selectedMove;
                }
            }

            return selectedMove;
        }

        //private int CalculateMaxValue(List<CalcMove> moves)
        //{
        //    int maxIndex = 0;
        //    int curMax = moves[maxIndex].value;

        //    for (int i = 1; i < moves.Count; i++)
        //    {
        //        if (moves[i].value > curMax)
        //        {
        //            maxIndex = i;
        //            curMax = moves[i].value;
        //        }
        //    }

        //    return maxIndex;
        //}

        private int CalculateBoardState(ChessBoard board, ChessColor color)
        {
            int total = 0;

            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    if (board[x,y] != ChessPiece.Empty)
                    {
                        int val = GetPieceValue(board[x,y]);

                        if (color == ChessColor.White)
                        {
                            if (board[x,y] < ChessPiece.Empty)
                            {
                                val *= -1;
                            }
                        }
                        else
                        {
                            if (board[x, y] > ChessPiece.Empty)
                            {
                                val *= -1;
                            }
                        }

                        total += val;
                    }
                }
            }

            return total;
        }

        private List<CalcMove> GenerateMoves(ChessBoard board, ChessColor color, int depth)
        {
            if(depth == 0)
            {
                return null;
            }

            List<CalcMove> moves = new List<CalcMove>();
            double[,] boardState = new double[8,8];

            for(int y = 0; y < 8; y++)
            {
                for(int x = 0; x < 8; x++)
                {
                    if (board[x,y] != ChessPiece.Empty)
                    {
                        ChessPiece piece = board[x, y];

                        // If the piece is our piece
                        if ((piece < ChessPiece.Empty && _ourColor == ChessColor.Black) || (piece > ChessPiece.Empty && _ourColor == ChessColor.White))
                        {
                            HashSet<ChessLocation> moveSet = GetMoveSet(piece);

                            if (piece == ChessPiece.WhiteKing || piece == ChessPiece.BlackKing)
                            {
                                _ourKingLoc = new ChessLocation(x, y);
                            }

                            ChessLocation currentLoc = new ChessLocation(x, y);

                            foreach (ChessLocation loc in moveSet)
                            {
                                if (_slowPieces.Contains(piece))
                                {
                                    CalcMove posMove = GetPieceMove(board, currentLoc, loc, color, 1);
                                    if(posMove != null)
                                    {
                                        moves.Add(posMove);
                                    }
                                }
                                else
                                {
                                    for (int i = 1; i < 8; i++)
                                    {
                                        CalcMove posMove = GetPieceMove(board, currentLoc, loc, color, i);
                                        if (posMove != null)
                                        {
                                            moves.Add(posMove);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (piece == ChessPiece.WhiteKing || piece == ChessPiece.BlackKing)
                            {
                                _enemyKingLoc = new ChessLocation(x, y);
                            }
                        }
                    }
                }
            }

            

            for (int i = 0; i < moves.Count; i++)
            {
                ChessBoard tempBoard = board.Clone();
                tempBoard[moves[i].move.To] = tempBoard[moves[i].move.From];
                tempBoard[moves[i].move.From] = ChessPiece.Empty;

                if (CheckThreatensEnemyKing(tempBoard, moves[i].move.To, _ourColor) && !(PieceThreatened(tempBoard, moves[i].move.To, _ourColor) > 0))
                {
                    moves[i].value += 20;
                }
            }

            this.Log("");
            return moves;
        }

        private CalcMove GetPieceMove(ChessBoard board, ChessLocation fromLoc, ChessLocation toLocDelta, ChessColor color, int deltaMod)
        {
            ChessPiece piece = board[fromLoc];
            int val = 0;
            int sum_x = fromLoc.X + toLocDelta.X * deltaMod;
            int sum_y = piece == ChessPiece.WhitePawn ? fromLoc.Y - toLocDelta.Y : fromLoc.Y + toLocDelta.Y * deltaMod;

            ChessLocation toLoc = new ChessLocation(sum_x, sum_y);

            if (toLoc.X < 0 || toLoc.X > 7 || toLoc.Y < 0 || toLoc.Y > 7)
                return null;

            ChessMove move = new ChessMove(fromLoc, toLoc);
            bool valid = IsValidMove(board, move, color);

            ChessBoard tempBoard = board.Clone();
            tempBoard[move.To] = tempBoard[move.From];
            tempBoard[move.From] = ChessPiece.Empty;

            if ((piece == ChessPiece.WhitePawn && move.To.Y == 0) || (piece == ChessPiece.BlackPawn && move.To.Y == 7))
            {
                tempBoard[move.To] = color == ChessColor.White ? ChessPiece.WhiteQueen : ChessPiece.BlackQueen;
            }

            int threatened = PieceThreatened(tempBoard, toLoc, color);

            if ((piece == ChessPiece.WhiteKing || piece == ChessPiece.BlackKing) && threatened > 0)
            {
                valid = false;
            }

            if (PieceThreatened(tempBoard, _ourKingLoc, color) > 0)
            {
                valid = false;
            }

            if (_ourColor == color && ((_previousCheck != null && _previousCheck.To == move.From) || (_prePreviousCheck != null && _prePreviousCheck.To == move.From)))
            {
                valid = false;
            }

            if (valid)
            {
                val += CalculateBoardState(tempBoard, color);

                if (PieceThreatened(board, _ourKingLoc, color) > 0)
                {
                    if (!(PieceThreatened(tempBoard, _ourKingLoc, color) > 0))
                    {
                        val += 1000000;
                        this.Log("Piece can get king out of check, val = " + val);
                    }
                }

                if (board[toLoc] != ChessPiece.Empty)
                {
                    val += GetPieceValue(board[toLoc]);
                }

                if (threatened > 0)
                {
                    val -= GetPieceValue(piece) * 2;
                }

                CalcMove moveToAdd = new CalcMove();
                moveToAdd.move = move;
                moveToAdd.value = val;

                this.Log(moveToAdd.move.ToString() + ", value = " + moveToAdd.value);

                return moveToAdd;
            }

            return null;
        }

        private HashSet<ChessLocation> GetMoveSet(ChessPiece piece)
        {
            HashSet<ChessLocation> moveSet = null;
            if (piece == ChessPiece.WhitePawn || piece == ChessPiece.BlackPawn)
            {
                moveSet = _pawnMoves;
            }
            else if (piece == ChessPiece.WhiteKnight || piece == ChessPiece.BlackKnight)
            {
                moveSet = _knightMoves;
            }
            else if (piece == ChessPiece.WhiteBishop || piece == ChessPiece.BlackBishop)
            {
                moveSet = _bishopMoves;
            }
            else if (piece == ChessPiece.WhiteRook || piece == ChessPiece.BlackRook)
            {
                moveSet = _rookMoves;
            }
            else if (piece == ChessPiece.WhiteQueen || piece == ChessPiece.BlackQueen)
            {
                moveSet = _kingMoves;
            }
            else if (piece == ChessPiece.WhiteKing || piece == ChessPiece.BlackKing)
            {
                moveSet = _kingMoves;
            }
            return moveSet;
        }

        private int GetPieceValue(ChessPiece piece)
        {
            int val = 0;

            if (piece == ChessPiece.WhitePawn || piece == ChessPiece.BlackPawn)
            {
                val += (int)PieceValues.Pawn;
            }
            else if (piece == ChessPiece.WhiteKnight || piece == ChessPiece.BlackKnight)
            {
                val += (int)PieceValues.Knight;
            }
            else if (piece == ChessPiece.WhiteBishop || piece == ChessPiece.BlackBishop)
            {
                val += (int)PieceValues.Bishop;
            }
            else if (piece == ChessPiece.WhiteRook || piece == ChessPiece.BlackRook)
            {
                val += (int)PieceValues.Rook;
            }
            else if (piece == ChessPiece.WhiteQueen || piece == ChessPiece.BlackQueen)
            {
                val += (int)PieceValues.Queen;
            }
            else if (piece == ChessPiece.WhiteKing || piece == ChessPiece.BlackKing)
            {
                val += (int)PieceValues.King;
            }
            return val;
        }

        // Check if piece in loc is in danger
        private int PieceThreatened(ChessBoard board, ChessLocation currentLoc, ChessColor color)
        {
            ChessColor enemyColor = color == ChessColor.White ? ChessColor.Black : ChessColor.White;
            List<ChessMove> moves = new List<ChessMove>();

            for (int y = 0; y < 8; y++ )
            {
                for (int x = 0; x < 8; x++)
                {
                    ChessPiece piece = board[x,y];

                    if (piece != ChessPiece.Empty)
                    {
                        ChessMove move = new ChessMove(new ChessLocation(x, y), currentLoc);
                        if (IsValidMove(board, move, enemyColor))
                        {
                            moves.Add(move);
                        }
                    }
                }
            }

            return moves.Count;
        }

        // See if the king is capable of moving
        private bool CanKingSurvive(ChessBoard board, ChessLocation currentLoc, ChessColor color)
        {
            int numPiecesThreateningKing = PieceThreatened(board, currentLoc, color);

            List<ChessMove> moves = new List<ChessMove>();

            foreach (ChessLocation loc in _kingMoves)
            {
                ChessLocation newLoc = new ChessLocation(currentLoc.X + loc.X, currentLoc.Y + loc.Y);

                if (newLoc.X < 0 || newLoc.X > 7 || newLoc.Y < 0 || newLoc.Y > 7)
                    continue;

                ChessMove move = new ChessMove(currentLoc, newLoc);
                bool valid = IsValidMove(board, move, color);

                if (valid)
                {
                    moves.Add(move);
                }
            }

            if (moves.Count == 0 && numPiecesThreateningKing > 1)
            {
                return false;
            }

            ChessColor enemyColor = color == ChessColor.White ? ChessColor.Black : ChessColor.White;

            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    ChessPiece piece = board[x, y];

                    if (piece != ChessPiece.Empty)
                    {
                        ChessLocation pieceLoc = new ChessLocation(x, y);
                        ChessMove move = new ChessMove(pieceLoc, currentLoc);
                        if (IsValidMove(board, move, enemyColor))
                        {
                            if (PieceThreatened(board, pieceLoc, enemyColor) > 0)
                            {
                                moves.Add(move);
                            }
                            else
                            {
                                if(board[pieceLoc] != ChessPiece.WhiteKnight && board[pieceLoc] != ChessPiece.BlackKnight)
                                {
                                    int d_x = currentLoc.X - pieceLoc.X;
                                    int d_y = currentLoc.Y - pieceLoc.Y;

                                    int xDir = 0;

                                    if (d_x != 0)
                                    {
                                        xDir = d_x < 0 ? -1 : 1;
                                    }

                                    int yDir = 0;

                                    if (d_y != 0)
                                    {
                                        yDir = d_y < 0 ? -1 : 1;
                                    }

                                    int dist = d_x > d_y ? Math.Abs(d_x) : Math.Abs(d_y);

                                    for(int i = 1; i <= dist; i++)
                                    {
                                        int xLoc = x + xDir * i;
                                        int yLoc = x + yDir * i;

                                        if (xLoc == 0 && yLoc == 0)
                                            continue;

                                        ChessLocation blockLoc = new ChessLocation(xLoc, yLoc);
                                        for (int y2 = 0; y2 < 8; y2++)
                                        {
                                            for (int x2 = 0; x2 < 8; x2++)
                                            {
                                                ChessLocation blockPieceLoc = new ChessLocation(x2, y2);
                                                ChessMove blockMove = new ChessMove(blockPieceLoc, blockLoc);
                                                if (IsValidMove(board, blockMove, color))
                                                {
                                                    ChessBoard tempBoard = board.Clone();
                                                    tempBoard[blockMove.To] = tempBoard[blockMove.From];
                                                    tempBoard[blockMove.From] = ChessPiece.Empty;

                                                    if (!(PieceThreatened(tempBoard, currentLoc, color) > 0))
                                                    {
                                                        moves.Add(move);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            this.Log("Number of moves enemy can make for king to survive = " + moves.Count);

            return moves.Count > 0;
        }

        private List<CalcMove> SortMoves(List<CalcMove> moves)
        {
            List<CalcMove> sortedMoves = new List<CalcMove>();

            foreach(CalcMove move in moves)
            {
                CalcMove tempMove = new CalcMove();
                tempMove.move = move.move;
                tempMove.value = move.value;

                if (sortedMoves.Count > 0)
                {
                    int i = 0;
                    if (_ourColor == ChessColor.White)
                    {
                        while (i < sortedMoves.Count && move.value <= sortedMoves[i].value)
                            i++;
                    }
                    else
                    {
                        while (i < sortedMoves.Count && move.value < sortedMoves[i].value)
                            i++;
                    }
                                        

                    sortedMoves.Insert(i, tempMove);
                }
                else
                {
                    sortedMoves.Add(tempMove);
                }
            }

            return sortedMoves;
        }

        private bool CheckThreatensEnemyKing(ChessBoard board, ChessLocation currentLoc, ChessColor color)
        {
            ChessMove move = new ChessMove(currentLoc, _enemyKingLoc);

            return IsValidMove(board, move, color);
        }

        /// <summary>
        /// Validates a move. The framework uses this to validate the opponents move.
        /// </summary>
        /// <param name="boardBeforeMove">The board as it currently is _before_ the move.</param>
        /// <param name="moveToCheck">This is the move that needs to be checked to see if it's valid.</param>
        /// <param name="colorOfPlayerMoving">This is the color of the player who's making the move.</param>
        /// <returns>Returns true if the move was valid</returns>
        public bool IsValidMove(ChessBoard boardBeforeMove, ChessMove moveToCheck, ChessColor colorOfPlayerMoving)  
        {
            bool validMove = false;
            
            ChessPiece pieceToMove = boardBeforeMove[moveToCheck.From];

            if (moveToCheck.To.X < 0 || moveToCheck.To.X > 7 || moveToCheck.To.Y < 0 || moveToCheck.To.Y > 7)
                return false;

            if (colorOfPlayerMoving == ChessColor.White)
            {
                if (pieceToMove == ChessPiece.WhitePawn)
                {
                    validMove = CheckPawn(boardBeforeMove, moveToCheck, colorOfPlayerMoving);
                }
                else if (pieceToMove == ChessPiece.WhiteKnight)
                {
                    validMove = CheckKnight(boardBeforeMove, moveToCheck, colorOfPlayerMoving);
                }
                else if (pieceToMove == ChessPiece.WhiteBishop)
                {
                    validMove = CheckBishop(boardBeforeMove, moveToCheck, colorOfPlayerMoving);
                }
                else if (pieceToMove == ChessPiece.WhiteRook)
                {
                    validMove = CheckRook(boardBeforeMove, moveToCheck, colorOfPlayerMoving);
                }
                else if (pieceToMove == ChessPiece.WhiteQueen)
                {
                    validMove = CheckQueen(boardBeforeMove, moveToCheck, colorOfPlayerMoving);
                }
                else if (pieceToMove == ChessPiece.WhiteKing)
                {
                    validMove = CheckKing(boardBeforeMove, moveToCheck, colorOfPlayerMoving);
                }
            }
            else
            {
                if (pieceToMove == ChessPiece.BlackPawn)
                {
                    validMove = CheckPawn(boardBeforeMove, moveToCheck, colorOfPlayerMoving);
                }
                else if (pieceToMove == ChessPiece.BlackKnight)
                {
                    validMove = CheckKnight(boardBeforeMove, moveToCheck, colorOfPlayerMoving);
                }
                else if (pieceToMove == ChessPiece.BlackBishop)
                {
                    validMove = CheckBishop(boardBeforeMove, moveToCheck, colorOfPlayerMoving);
                }
                else if (pieceToMove == ChessPiece.BlackRook)
                {
                    validMove = CheckRook(boardBeforeMove, moveToCheck, colorOfPlayerMoving);
                }
                else if (pieceToMove == ChessPiece.BlackQueen)
                {
                    validMove = CheckQueen(boardBeforeMove, moveToCheck, colorOfPlayerMoving);
                }
                else if (pieceToMove == ChessPiece.BlackKing)
                {
                    validMove = CheckKing(boardBeforeMove, moveToCheck, colorOfPlayerMoving);
                }
            }
            
            return validMove;
        }

        bool CheckPawn(ChessBoard board, ChessMove move, ChessColor color)
        {
            bool moveable = false;

            int x1 = move.From.X;
            int y1 = move.From.Y;
            int x2 = move.To.X;
            int y2 = move.To.Y;
            int d_x = x2 - x1;
            int d_y = y2 - y1;
            
            if(color == ChessColor.White)
            {
                if ((d_x == 0 && (d_y == -1 || (d_y == -2 && y1 == 6 && board[x2, y2 + 1] == ChessPiece.Empty)) && board[x2, y2] == ChessPiece.Empty) || ((d_x == 1 || d_x == -1) && d_y == -1 && board[x2, y2] != ChessPiece.Empty)) //y1 == 6 = starting pawn position for white. Probably could change to a global.
                {
                    moveable = true;
                }
            }
            else
            {
                if((d_x == 0 && (d_y == 1 || (d_y == 2 && y1 == 1 && board[x2, y2 - 1] == ChessPiece.Empty)) && board[x2,y2] == ChessPiece.Empty) || ((d_x == 1 || d_x == -1) && d_y == 1 && board[x2,y2] != ChessPiece.Empty)) //y1 == 1 = starting pawn position for black. Probably could change to global.
                {
                    moveable = true;
                }
            }

            if(!DiffColorAtDest(board,move,color))
            {
                moveable = false;
            }

            return moveable;
        }

        bool CheckKnight(ChessBoard board, ChessMove move, ChessColor color)
        {
            bool moveable = false;

            int x1 = move.From.X;
            int y1 = move.From.Y;
            int x2 = move.To.X;
            int y2 = move.To.Y;
            int d_x = x2 - x1;
            int d_y = y2 - y1;

            if ( ((Math.Abs(d_x) == 1 && Math.Abs(d_y) == 2) || (Math.Abs(d_x) == 2 && Math.Abs(d_y) == 1)))
                moveable = true;

            if (!DiffColorAtDest(board, move, color))
            {
                moveable = false;
            }

            return moveable;
        }

        bool CheckBishop(ChessBoard board, ChessMove move, ChessColor color)
        {
            int x1 = move.From.X;
            int y1 = move.From.Y;
            int x2 = move.To.X;
            int y2 = move.To.Y;
            int d_x = x2 - x1;
            int d_y = y2 - y1;

            bool moveable = true;
            if (Math.Abs(d_x) == Math.Abs(d_y))
            {
                if(d_y > 0)
                {
                    if(d_x > 0)
                    {
                        for(int i = 1; i < d_x; ++i)
                        {
                            if (board[x1 + i, y1 + i] != ChessPiece.Empty)
                                moveable = false;
                        }
                    }
                    else
                    {
                        for(int i = -1; i > d_x; --i)
                        {
                            if (board[x1 + i, y1 - i] != ChessPiece.Empty)
                                moveable = false;
                        }
                    }
                }
                else
                {
                    if(d_x > 0)
                    {
                        for(int i = 1; i < d_x; ++i)
                        {
                            if (board[x1 + i, y1 - i] != ChessPiece.Empty)
                                moveable = false;
                        }
                    }
                    else
                    {
                        for(int i = -1; i > d_x; --i)
                        {
                            if (board[x1 + i, y1 + i] != ChessPiece.Empty)
                                moveable = false;
                        }
                    }
                }
            }
            else moveable = false;

            if (!DiffColorAtDest(board, move, color))
            {
                moveable = false;
            }

            return moveable;
        }

        bool CheckRook(ChessBoard board, ChessMove move, ChessColor color)
        {
            int x1 = move.From.X;
            int y1 = move.From.Y;
            int x2 = move.To.X;
            int y2 = move.To.Y;
            int d_x = x2 - x1;
            int d_y = y2 - y1;

            bool moveable = true;
            if (d_x == 0)
            {
                if (d_y > 0)
                {
                    for (int i = 1; i < d_y; ++i)
                    {
                        if (board[x1, y1 + i] != ChessPiece.Empty)
                            moveable = false;
                    }
                }
                else
                {
                    for (int i = -1; i > d_y; --i)
                    {
                        if (board[x1, y1 + i] != ChessPiece.Empty)
                            moveable = false;
                    }
                }
            }
            else if (d_y == 0)
            {
                if (d_x > 0)
                {
                    for (int i = 1; i < d_x; ++i)
                    {
                        if (board[x1 + i, y1] != ChessPiece.Empty)
                        {
                            moveable = false;
                        }
                    }
                }
                else
                {
                    for (int i = -1; i > d_x; --i)
                    {
                        if (board[x1 + i, y1] != ChessPiece.Empty)
                            moveable = false;
                    }
                }
            }
            else moveable = false;

            if (!DiffColorAtDest(board, move, color))
            {
                moveable = false;
            }

            return moveable;
        }

        bool CheckQueen(ChessBoard board, ChessMove move, ChessColor color)
        {
            int x1 = move.From.X;
            int y1 = move.From.Y;
            int x2 = move.To.X;
            int y2 = move.To.Y;
            int d_x = x2 - x1;
            int d_y = y2 - y1;

            bool moveable = false;

            if (Math.Abs(d_x) == Math.Abs(d_y))
                moveable = CheckBishop(board, move, color);
            else if (d_x == 0 || d_y == 0)
                moveable = CheckRook(board, move, color);

            if (!DiffColorAtDest(board, move, color))
            {
                moveable = false;
            }

            return moveable;
        }

        bool CheckKing(ChessBoard board, ChessMove move, ChessColor color)
        {
            int x1 = move.From.X;
            int y1 = move.From.Y;
            int x2 = move.To.X;
            int y2 = move.To.Y;
            int d_x = x2 - x1;
            int d_y = y2 - y1;

            bool moveable = false;

            if (Math.Abs(d_x) <= 1 && Math.Abs(d_y) <= 1 && move.To != move.From)
            {
                ChessBoard tempBoard = board.Clone();
                tempBoard[move.To] = tempBoard[move.From];
                tempBoard[move.From] = ChessPiece.Empty;

                if(!(PieceThreatened(tempBoard, move.To, color) > 0))
                {
                    moveable = true;
                }
            }

            if (!DiffColorAtDest(board, move, color))
            {
                moveable = false;
            }
            
            return moveable;
        }

        //Checks to see if the color at the dest is the same color as the piece moving there.
        //MAKE CHANGES NECESSARY TO ALLOW DETECTING OF SAME COLOR FOR COVERAGE
        bool DiffColorAtDest(ChessBoard board, ChessMove move, ChessColor color)
        {
            if (color == ChessColor.White)
            {
                if (board[move.To] > ChessPiece.Empty)
                    return false;
            }
            else
            {
                if (board[move.To] < ChessPiece.Empty)
                    return false;
            }
            return true;
        }
        #endregion
        // Bryson Murray    Brad Hawkins    Adam Clayton

        #region IChessAI Members that should be implemented as automatic properties and should NEVER be touched by students.
        /// <summary>
        /// This will return false when the framework starts running your AI. When the AI's time has run out,
        /// then this method will return true. Once this method returns true, your AI should return a 
        /// move immediately.
        /// 
        /// You should NEVER EVER set this property!
        /// This property should be defined as an Automatic Property.
        /// This property SHOULD NOT CONTAIN ANY CODE!!!
        /// </summary>
        public AIIsMyTurnOverCallback IsMyTurnOver { get; set; }

        /// <summary>
        /// Call this method to print out debug information. The framework subscribes to this event
        /// and will provide a log window for your debug messages.
        /// 
        /// You should NEVER EVER set this property!
        /// This property should be defined as an Automatic Property.
        /// This property SHOULD NOT CONTAIN ANY CODE!!!
        /// </summary>
        /// <param name="message"></param>
        public AILoggerCallback Log { get; set; }

        /// <summary>
        /// Call this method to catch profiling information. The framework subscribes to this event
        /// and will print out the profiling stats in your log window.
        /// 
        /// You should NEVER EVER set this property!
        /// This property should be defined as an Automatic Property.
        /// This property SHOULD NOT CONTAIN ANY CODE!!!
        /// </summary>
        /// <param name="key"></param>
        public AIProfiler Profiler { get; set; }

        /// <summary>
        /// Call this method to tell the framework what decision print out debug information. The framework subscribes to this event
        /// and will provide a debug window for your decision tree.
        /// 
        /// You should NEVER EVER set this property!
        /// This property should be defined as an Automatic Property.
        /// This property SHOULD NOT CONTAIN ANY CODE!!!
        /// </summary>
        /// <param name="message"></param>
        public AISetDecisionTreeCallback SetDecisionTree { get; set; }
        #endregion
    }
}
