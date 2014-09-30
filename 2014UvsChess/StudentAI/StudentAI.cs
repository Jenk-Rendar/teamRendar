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
            Pawn = 1,
            Knight = 3,
            Bishop = 3,
            Rook = 5,
            Queen = 9,
            King = 10
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
        // ChessMove _previousMove, _prePreviousMove, _previousCheck, _prePreviousCheck;

        Dictionary<string, double> _pieceValues = new Dictionary<string, double>()
        {
            { "King", 10.0 },
            { "Queen", 9.0 },
            { "Rook", 5.0 },
            { "Bishop", 3.0 },
            { "Knight", 3.0 },
            { "Pawn", 1.0 }
        };

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
            List<CalcMove> maxMoves = CalculateMaxMoves(moves);

            Random rnd = new Random();

            int selectedIndex = 0;
            if (maxMoves.Count > 0)
            {
                selectedIndex = rnd.Next() % maxMoves.Count;
            }
            else
            {
                selectedIndex = CalculateMaxValue(moves);
            }

            ChessMove selectedMove = moves[selectedIndex].move;

            ChessBoard tempBoard = board.Clone();
            tempBoard[selectedMove.To] = tempBoard[selectedMove.From];
            tempBoard[selectedMove.From] = ChessPiece.Empty;

            if ((tempBoard[selectedMove.To] == ChessPiece.WhitePawn && selectedMove.To.Y == 0) || (tempBoard[selectedMove.To] == ChessPiece.BlackPawn && selectedMove.To.Y == 7))
            {
                tempBoard[selectedMove.To] = myColor == ChessColor.White ? ChessPiece.WhiteQueen : ChessPiece.BlackQueen;
            }

            // Checks if their king is threatened after our move
            if (PieceThreatened(tempBoard,_enemyKingLoc,_enemyColor))
            {
                if (!CanKingMove(tempBoard,_enemyKingLoc,_enemyColor))
                {
                    selectedMove.Flag = ChessFlag.Checkmate;
                }
                else
                {
                    selectedMove.Flag = ChessFlag.Check;
                }
            }

            this.Log("Board Value = " + moves[selectedIndex].value);

            return selectedMove;
        }

        private List<CalcMove> CalculateMaxMoves(List<CalcMove> moves)
        {
            if (!(moves.Count > 0))
            {
                return null;
            }

            List<CalcMove> maxMoves = new List<CalcMove>();

            int curMax = moves[0].value;

            for (int i = 1; i < moves.Count; i++)
            {
                if (moves[i].value > curMax)
                {
                    maxMoves.Clear();
                    maxMoves.Add(moves[i]);
                    curMax = moves[i].value;
                }
                else if (moves[i].value == curMax)
                {
                    maxMoves.Add(moves[i]);
                }
            }

            return maxMoves;
        }

        private int CalculateMaxValue(List<CalcMove> moves)
        {
            int maxIndex = 0;
            int curMax = moves[maxIndex].value;

            for (int i = 1; i < moves.Count; i++)
            {
                if (moves[i].value >= curMax)
                {
                    maxIndex = i;
                    curMax = moves[i].value;
                }
            }

            return maxIndex;
        }

        private int CalculateBoardState(ChessBoard board, ChessColor ourColor)
        {
            int total = 0;
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    if (board[x,y] != ChessPiece.Empty)
                    {
                        int val = 0;

                        if (board[x,y] == ChessPiece.WhitePawn || board[x,y] == ChessPiece.BlackPawn)
                        {
                            val = (int)PieceValues.Pawn;
                        }
                        else if (board[x,y] == ChessPiece.WhiteKnight || board[x,y] == ChessPiece.BlackKnight)
                        {
                            val = (int)PieceValues.Knight;
                        }
                        else if (board[x,y] == ChessPiece.WhiteBishop || board[x,y] == ChessPiece.BlackBishop)
                        {
                            val = (int)PieceValues.Bishop;
                        }
                        else if (board[x,y] == ChessPiece.WhiteRook || board[x,y] == ChessPiece.BlackRook)
                        {
                            val = (int)PieceValues.Rook;
                        }
                        else if (board[x,y] == ChessPiece.WhiteQueen || board[x,y] == ChessPiece.BlackQueen)
                        {
                            val = (int)PieceValues.Queen;
                        }
                        else if (board[x,y] == ChessPiece.WhiteKing || board[x,y] == ChessPiece.BlackKing)
                        {
                            val = (int)PieceValues.King;
                        }

                        if (ourColor == ChessColor.White)
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

            bool kingThreatened = false;

            if (PieceThreatened(board,_ourKingLoc,color))
            {
                kingThreatened = true;
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
                                _ourKingLoc = new ChessLocation(x,y);
                                moveSet = _kingMoves;
                            }

                            ChessLocation currentLoc = new ChessLocation(x, y);

                            foreach (ChessLocation loc in moveSet)
                            {
                                ChessLocation newLoc = null;

                                if (_slowPieces.Contains(piece))
                                {
                                    int sum_x = currentLoc.X + loc.X;
                                    int sum_y = piece == ChessPiece.WhitePawn ? currentLoc.Y - loc.Y : currentLoc.Y + loc.Y;

                                    newLoc = new ChessLocation(sum_x, sum_y);
                                        
                                    if (newLoc.X < 0 || newLoc.X > 7 || newLoc.Y < 0 || newLoc.Y > 7)
                                        continue;

                                    ChessMove move = new ChessMove(currentLoc, newLoc);
                                    bool valid = IsValidMove(board, move, color);

                                    ChessBoard tempBoard = board.Clone();
                                    tempBoard[move.To] = tempBoard[move.From];
                                    tempBoard[move.From] = ChessPiece.Empty;

                                    if ((piece == ChessPiece.WhitePawn && move.To.Y == 0) || (piece == ChessPiece.BlackPawn && move.To.Y == 7))
                                    {
                                        tempBoard[move.To] = color == ChessColor.White ? ChessPiece.WhiteQueen : ChessPiece.BlackQueen;
                                    }

                                    bool threatened = PieceThreatened(tempBoard, newLoc, color);

                                    if ((piece == ChessPiece.WhiteKing || piece == ChessPiece.BlackKing) && threatened)
                                    {
                                        valid = false;
                                    }

                                    if (PieceThreatened(tempBoard, _ourKingLoc, color))
                                    {
                                        valid = false;
                                    }

                                    if (valid)
                                    {
                                        int val = CalculateBoardState(tempBoard, color);
                                        //if (depth > 0)
                                        //{
                                        //    List<CalcMove> nextMoves = GenerateMoves(tempBoard, color, --depth);
                                        //    if (nextMoves != null)
                                        //    {
                                        //        int maxIndex = CalculateMaxValue(nextMoves);
                                        //        val = nextMoves[maxIndex].value;
                                        //    }
                                        //}

                                        if (kingThreatened)
                                        {
                                            if (!PieceThreatened(tempBoard,_ourKingLoc,color))
                                            {
                                                val += 1000000;
                                                this.Log("Piece can get king out of check, val = " + val);
                                            }
                                        }

                                        CalcMove moveToAdd = new CalcMove();
                                        moveToAdd.move = move;
                                        moveToAdd.value = val;

                                        moves.Add(moveToAdd);
                                        this.Log(moveToAdd.move.ToString() + ", value = " + moveToAdd.value);
                                    }
                                }
                                else
                                {
                                    for (int i = 1; i < 8; i++)
                                    {
                                        newLoc = new ChessLocation(currentLoc.X + loc.X * i, currentLoc.Y + loc.Y * i);

                                        if (newLoc.X < 0 || newLoc.X > 7 || newLoc.Y < 0 || newLoc.Y > 7)
                                            continue;

                                        ChessMove move = new ChessMove(currentLoc, newLoc);
                                        bool valid = IsValidMove(board, move, color);

                                        ChessBoard tempBoard = board.Clone();
                                        tempBoard[move.To] = tempBoard[move.From];
                                        tempBoard[move.From] = ChessPiece.Empty;

                                        bool threatened = PieceThreatened(tempBoard, newLoc, color);

                                        if (PieceThreatened(tempBoard, _ourKingLoc, color))
                                        {
                                            valid = false;
                                        }

                                        if (valid)
                                        {
                                            int val = CalculateBoardState(tempBoard, color);
                                            //if (depth > 0)
                                            //{
                                            //    List<CalcMove> nextMoves = GenerateMoves(tempBoard, color, --depth);
                                            //    if (nextMoves != null)
                                            //    {
                                            //        int maxIndex = CalculateMaxValue(nextMoves);
                                            //        val = nextMoves[maxIndex].value;
                                            //    }
                                            //}

                                            if (kingThreatened)
                                            {
                                                if (!PieceThreatened(tempBoard, _ourKingLoc, color))
                                                {
                                                    val += 1000000;
                                                    this.Log("Piece can get king out of check, val = " + val);
                                                }
                                            }

                                            CalcMove moveToAdd = new CalcMove();
                                            moveToAdd.move = move;
                                            moveToAdd.value = val;

                                            moves.Add(moveToAdd);
                                            this.Log(moveToAdd.move.ToString() + ", value = " + moveToAdd.value);
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

            this.Log("");
            return moves;
        }

        // Check if piece in loc is in danger
        private bool PieceThreatened(ChessBoard board, ChessLocation currentLoc, ChessColor color)
        {
            ChessColor enemyColor = color == ChessColor.White ? ChessColor.Black : ChessColor.White;
            List<ChessMove> moves = new List<ChessMove>();

            for (int y = 0; y < 8; y++ )
            {
                for (int x = 0; x < 8; x++)
                {
                    ChessPiece piece = board[x,y];

                    bool isKing = false;
                    if (piece == ChessPiece.WhiteKing || piece == ChessPiece.BlackKing)
                    {
                        isKing = true;
                    }

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

            return moves.Count > 0;
        }

        // See if the king is capable of moving
        private bool CanKingMove(ChessBoard board, ChessLocation currentLoc, ChessColor color)
        {
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
                            if (!PieceThreatened(board, pieceLoc, color))
                            {
                                moves.Add(move);
                            }
                        }
                    }
                }
            }

            this.Log("Number of moves king can make = " + moves.Count);

            return moves.Count > 0;
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

                if(!PieceThreatened(tempBoard, move.To, color))
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
