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
            public double value;
        }

        int _turnsTaken, _numEnemyPieces;
        ChessLocation _ourKingLoc;
        ChessLocation _enemyKingLoc;
        double _coverageValue = 0.0;
        double _threatValue = 0.0;
        double _checkValue = 0.0;
        ChessColor _ourColor, _enemyColor;
        int[,] _boardState;
        // List<ChessMove> _possibleMoves;
        ChessBoard _currentBoard;
        HashSet<ChessLocation> _threatenedPieces, _knightCheck, _bishopCheck, _rookCheck;
        // List<FinalMove> _moveQueue;
        ChessMove _previousMove, _prePreviousMove, _previousCheck, _prePreviousCheck;

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

        /// <summary>
        /// Evaluates the chess board and decided which move to make. This is the main method of the AI.
        /// The framework will call this method when it's your turn.
        /// </summary>
        /// <param name="board">Current chess board</param>
        /// <param name="yourColor">Your color</param>
        /// <returns> Returns the best chess move the player has for the given chess board</returns>
        public ChessMove GetNextMove(ChessBoard board, ChessColor myColor)
        {
            _turnsTaken++;
            _currentBoard = board;
            _ourColor = myColor;
            _enemyColor = _ourColor == ChessColor.White ? ChessColor.Black : ChessColor.White;
            _boardState = new double[8, 8];
            // _possibleMoves = new List<ChessMove>();
            _threatenedPieces = new HashSet<ChessLocation>();
            _knightCheck = new HashSet<ChessLocation>();
            _bishopCheck = new HashSet<ChessLocation>();
            _rookCheck = new HashSet<ChessLocation>();

            List<ChessMove> moves = GenerateGravity(_currentBoard.Clone());
            printGravity();

            bool enemyKingHasMoves = GeneratePossibleKingMoves(_enemyKingLoc, _enemyColor);
            if(!enemyKingHasMoves || _numEnemyPieces < 5)
            {
                _checkValue = 10.0;
            }

            List<CalcMove> moveQueue = BuildPriorityQueue(moves);

            while (_previousMove == moveQueue[0].move || _prePreviousMove == moveQueue[0].move || (_previousCheck != null && _previousCheck.To == moveQueue[0].move.From) || (_prePreviousCheck != null && _prePreviousCheck.To == moveQueue[0].move.From))
            {
                moveQueue.Remove(moveQueue[0]);
            }

            _prePreviousMove = _previousMove;
            _previousMove = moveQueue[0].move;


            ChessBoard tempBoard = _currentBoard.Clone();
            tempBoard[moveQueue[0].move.To] = _currentBoard[moveQueue[0].move.From];
            tempBoard[moveQueue[0].move.From] = ChessPiece.Empty;

            while(checkForCheck(true, tempBoard))
            {
                moveQueue.RemoveAt(0);
                tempBoard = _currentBoard.Clone();
                tempBoard[moveQueue[0].move.To] = tempBoard[moveQueue[0].move.From];
                tempBoard[moveQueue[0].move.From] = ChessPiece.Empty;
            }

            if(tempBoard[moveQueue[0].move.To] == (_ourColor == ChessColor.White ? ChessPiece.WhitePawn : ChessPiece.BlackPawn))
            {
                if(moveQueue[0].move.To.Y == (_ourColor == ChessColor.White ? 0 : 7))
                {
                    tempBoard[moveQueue[0].move.To] = (_ourColor == ChessColor.White ? ChessPiece.WhiteQueen : ChessPiece.BlackQueen);
                }
            }

            if (checkForCheck(false, tempBoard))
            {
                if (!GeneratePossibleKingMoves(_enemyKingLoc, _enemyColor))
                {
                    moveQueue[0].move.Flag = ChessFlag.Checkmate;
                }
                else
                {
                    moveQueue[0].move.Flag = ChessFlag.Check;
                }

                _prePreviousCheck = _previousCheck;
                _previousCheck = moveQueue[0].move;
            }
            else
            {
                _prePreviousCheck = _previousCheck;
                _previousCheck = null;
            }


            _checkValue = 0.0;
            return moveQueue[0].move;
        }

        private double CalculateBoardState(ChessBoard board, ChessColor ourColor)
        {
            double total = 0;
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    if (board[x,y] != ChessPiece.Empty)
                    {
                        double val = 0;

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

        private List<ChessMove> GenerateMoves(ChessBoard board, ChessColor color)
        {
            List<ChessMove> moves = new List<ChessMove>();
            double[,] boardState = new double[8,8];

            for(int y = 0; y < 8; y++)
            {
                for(int x = 0; x < 8; x++)
                {
                    if (board[x,y] != ChessPiece.Empty)
                    {
                        ChessPiece piece = _currentBoard[x, y];

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
                                moveSet = _kingMoves;
                            }

                            ChessLocation currentLoc = new ChessLocation(x, y);

                            foreach (ChessLocation loc in moveSet)
                            {
                                ChessLocation newLoc = null;

                                if (piece == ChessPiece.WhitePawn || piece == ChessPiece.BlackPawn || piece == ChessPiece.WhiteKnight || piece == ChessPiece.BlackKnight)
                                {
                                    int sum_x, sum_y;
                                    sum_x = currentLoc.X + loc.X;
                                    if (piece == ChessPiece.WhitePawn)
                                    {
                                        sum_y = currentLoc.Y - loc.Y;
                                    }
                                    else
                                    {
                                        sum_y = currentLoc.Y + loc.Y;
                                    }

                                    newLoc = new ChessLocation(sum_x, sum_y);
                                        
                                    if (newLoc.X < 0 || newLoc.X > 7 || newLoc.Y < 0 || newLoc.Y > 7)
                                        continue;

                                    ChessMove move = new ChessMove(currentLoc, newLoc);

                                    bool valid = IsValidMove(board, move, color);
                                    bool threatened = PieceThreatened(board, newLoc, color);

                                    ChessBoard tempBoard = board.Clone();
                                    tempBoard[move.To] = tempBoard[move.From];
                                    tempBoard[move.From] = ChessPiece.Empty;

                                    if (valid)
                                    {
                                        moves.Add(move);
                                    }
                                }
                                else
                                {
                                    for (int i = 1; i < 8; i++)
                                    {
                                        newLoc = new ChessLocation(currentLoc.X + loc.X * i, currentLoc.Y + loc.Y * i);
                                        if (newLoc.X < 0 || newLoc.X > 7 || newLoc.Y < 0 || newLoc.Y > 7)
                                            continue;

                                        bool valid = IsValidMove(board, new ChessMove(currentLoc, newLoc), color);
                                        bool threatened = PieceThreatened(board, newLoc, color);

                                        if ((piece == ChessPiece.WhiteKing || piece == ChessPiece.BlackKing) && threatened)
                                        {
                                            valid = false;
                                        }

                                        if (valid)
                                        {
                                            moves.Add(new ChessMove(currentLoc, newLoc));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return moves;
        }

        private void printGravity()
        {
            for (int y = 0; y < 8; y++)
            {
                this.Log(_boardState[0, y] + "\t" + _boardState[1, y] + "\t" + _boardState[2, y] + "\t" + _boardState[3, y] + "\t" + _boardState[4, y] + "\t" + _boardState[5, y] + "\t" + _boardState[6, y] + "\t" + _boardState[7, y]);
            }
            this.Log("");
                
        }

        // Check if piece in loc is in danger
        private bool PieceThreatened(ChessBoard board, ChessLocation loc, ChessColor color)
        {
            ChessColor enemyColor = color == ChessColor.White ? ChessColor.Black : ChessColor.White;
            List<ChessMove> moves = new List<ChessMove>();

            for (int y = 0; y < 8; y++ )
            {
                for (int x = 0; x < 8; x++)
                {
                    if (board[x,y] != ChessPiece.Empty)
                    {
                        ChessMove move = new ChessMove(new ChessLocation(x, y), loc);
                        if (IsValidMove(board, move, enemyColor))
                        {
                            moves.Add(move);
                        }
                    }
                }
            }

            return moves.Count > 0;
        }

        private List<CalcMove> BuildPriorityQueue(List<ChessMove> moves)
        {
            List<CalcMove> moveQueue = new List<CalcMove>();

            foreach(ChessMove p_move in moves)
            {
                double calc_val = 0;
                if (_threatenedPieces.Contains(p_move.From))
                {
                    string pieceType = Enum.GetName(typeof(ChessPiece),_currentBoard[p_move.From]);
                    pieceType = pieceType.Remove(0,5);
                    calc_val += _pieceValues[pieceType] / 2.0;
                }

                if (_currentBoard[p_move.From] != (_ourColor == ChessColor.White ? ChessPiece.WhiteKing : ChessPiece.BlackKing))
                {
                    calc_val += 1.0;
                }
                if (_currentBoard[p_move.From] == (_ourColor == ChessColor.White ? ChessPiece.WhiteKnight : ChessPiece.BlackKnight))
                {
                    if (_knightCheck.Contains(p_move.From))
                        calc_val += _checkValue;
                }
                if (_currentBoard[p_move.From] == (_ourColor == ChessColor.White ? ChessPiece.WhiteBishop : ChessPiece.BlackBishop) || _currentBoard[p_move.From] == (_ourColor == ChessColor.White ? ChessPiece.WhiteQueen : ChessPiece.BlackQueen))
                {
                    if (_bishopCheck.Contains(p_move.From))
                        calc_val += _checkValue;
                }
                if (_currentBoard[p_move.From] == (_ourColor == ChessColor.White ? ChessPiece.WhiteRook : ChessPiece.BlackRook) || _currentBoard[p_move.From] == (_ourColor == ChessColor.White ? ChessPiece.WhiteQueen : ChessPiece.BlackQueen))
                {
                    if (_rookCheck.Contains(p_move.From))
                        calc_val += _checkValue;
                }

                calc_val += _boardState[p_move.To.X, p_move.To.Y];

                if (moveQueue.Count > 0)
                {
                    int i = 0;
                    while(i < moveQueue.Count && calc_val <= moveQueue[i].value)
                        i++;
                    moveQueue.Insert(i, new CalcMove { move = p_move, value = calc_val });
                }
                else
                {
                    moveQueue.Add(new CalcMove { move = p_move, value = calc_val });
                }
            }

            return moveQueue;
        }

        //private bool checkForCheck(bool ours, ChessBoard board)
        //{
        //    ChessLocation kingLoc;
        //    ChessColor color;
        //    List<ChessMove> moves = null;

        //    if (ours)
        //    {
        //        kingLoc = _ourKingLoc;
        //        color = _ourColor;
        //    }
        //    else
        //    {
        //        kingLoc = _enemyKingLoc;
        //        color = _ourColor == ChessColor.White ? ChessColor.Black : ChessColor.White;
        //    }

        //    if (color == ChessColor.White)
        //    {
        //        if (kingLoc.Y > 0)
        //        {
        //            if (kingLoc.X > 0)
        //            {
        //                if (board[kingLoc.X - 1, kingLoc.Y - 1] == ChessPiece.BlackPawn)
        //                {
        //                    return true;
        //                }
        //            }
        //            if (kingLoc.X < 7)
        //            {
        //                if (board[kingLoc.X + 1, kingLoc.Y - 1] == ChessPiece.BlackPawn)
        //                {
        //                    return true;
        //                }
        //            }
        //        }
        //    }
        //    else
        //    {
        //        if (kingLoc.Y < 7)
        //        {
        //            if (kingLoc.X > 0)
        //            {
        //                if (board[kingLoc.X - 1, kingLoc.Y + 1] == ChessPiece.WhitePawn)
        //                {
        //                    return true;
        //                }
        //            }
        //            if (kingLoc.X < 7)
        //            {
        //                if (board[kingLoc.X + 1, kingLoc.Y + 1] == ChessPiece.WhitePawn)
        //                {
        //                    return true;
        //                }
        //            }
        //        }
        //    }

        //    moves = GenerateKnightMoves(color == ChessColor.White ? ChessPiece.WhiteKnight : ChessPiece.BlackKnight, !ours, color, kingLoc);
        //    foreach(ChessMove p_move in moves)
        //    {
        //        //int x1 = p_move.From.X;
        //        //int y1 = p_move.From.Y;
        //        //int x2 = p_move.To.X;
        //        //int y2 = p_move.To.Y;
        //        //this.Log(String.Format("Knight Check Call: From ({0},{1}) To ({2},{3}", x1, y1, x2, y2));
        //        if (board[p_move.To] == (color == ChessColor.White ? ChessPiece.BlackKnight : ChessPiece.WhiteKnight))
        //        {
        //            return true;
        //        }
        //    }

        //    moves = GenerateBishopMoves(color == ChessColor.White ? ChessPiece.WhiteBishop : ChessPiece.BlackBishop, !ours, color, kingLoc);
        //    foreach (ChessMove p_move in moves)
        //    {
        //        //int x1 = p_move.From.X;
        //        //int y1 = p_move.From.Y;
        //        //int x2 = p_move.To.X;
        //        //int y2 = p_move.To.Y;
        //        //this.Log(String.Format("Bishop Check Call: From ({0},{1}) To ({2},{3}",x1,y1,x2,y2));
        //        if (board[p_move.To] == (color == ChessColor.White ? ChessPiece.BlackBishop : ChessPiece.WhiteBishop) || board[p_move.To] == (color == ChessColor.White ? ChessPiece.BlackQueen : ChessPiece.WhiteQueen))
        //        {
        //            return true;
        //        }
        //    }
            
        //    moves = GenerateRookMoves(color == ChessColor.White ? ChessPiece.WhiteRook : ChessPiece.BlackRook, !ours, color, kingLoc);
        //    foreach (ChessMove p_move in moves)
        //    {
        //        //int x1 = p_move.From.X;
        //        //int y1 = p_move.From.Y;
        //        //int x2 = p_move.To.X;
        //        //int y2 = p_move.To.Y;
        //        //this.Log(String.Format("Rook Check Call: From ({0},{1}) To ({2},{3}", x1, y1, x2, y2));
        //        if (board[p_move.To] == (color == ChessColor.White ? ChessPiece.BlackRook : ChessPiece.WhiteRook) || board[p_move.To] == (color == ChessColor.White ? ChessPiece.BlackQueen : ChessPiece.WhiteQueen))
        //        {
        //            return true;
        //        }
        //    }

        //    return false;
        //}

        //bool GeneratePossibleKingMoves(ChessLocation kingLoc, ChessColor color)
        //{
        //    List<ChessMove> moves = new List<ChessMove>();

        //    foreach (ChessLocation loc in _kingMoves)
        //    {
        //        ChessLocation newLoc = new ChessLocation(kingLoc.X + loc.X, kingLoc.Y + loc.Y);
        //        if (newLoc.X < 0 || newLoc.X > 7 || newLoc.Y < 0 || newLoc.Y > 7)
        //            continue;

        //        ChessMove posMove = new ChessMove(kingLoc, newLoc);
        //        bool valid = CheckKing(posMove);

        //        if (valid)
        //            valid = CheckColorAtDest(color, posMove);

        //        if (valid)
        //        {
        //            moves.Add(posMove);
        //        }
        //    }

        //    return moves.Count > 0;
        //}

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
                    validMove = CheckPawn(boardBeforeMove, moveToCheck);
                }
                else if (pieceToMove == ChessPiece.WhiteKnight)
                {
                    validMove = CheckKnight(boardBeforeMove, moveToCheck);
                }
                else if (pieceToMove == ChessPiece.WhiteBishop)
                {
                    validMove = CheckBishop(boardBeforeMove, moveToCheck);
                }
                else if (pieceToMove == ChessPiece.WhiteRook)
                {
                    validMove = CheckRook(boardBeforeMove, moveToCheck);
                }
                else if (pieceToMove == ChessPiece.WhiteQueen)
                {
                    validMove = CheckQueen(boardBeforeMove, moveToCheck);
                }
                else if (pieceToMove == ChessPiece.WhiteKing)
                {
                    validMove = CheckKing(boardBeforeMove, moveToCheck);
                }
            }
            else
            {
                if (pieceToMove == ChessPiece.BlackPawn)
                {
                    validMove = CheckPawn(boardBeforeMove, moveToCheck);
                }
                else if (pieceToMove == ChessPiece.BlackKnight)
                {
                    validMove = CheckKnight(boardBeforeMove, moveToCheck);
                }
                else if (pieceToMove == ChessPiece.BlackBishop)
                {
                    validMove = CheckBishop(boardBeforeMove, moveToCheck);
                }
                else if (pieceToMove == ChessPiece.BlackRook)
                {
                    validMove = CheckRook(boardBeforeMove, moveToCheck);
                }
                else if (pieceToMove == ChessPiece.BlackQueen)
                {
                    validMove = CheckQueen(boardBeforeMove, moveToCheck);
                }
                else if (pieceToMove == ChessPiece.BlackKing)
                {
                    validMove = CheckKing(boardBeforeMove, moveToCheck);
                }
            }

            if (validMove && !(CheckColorAtDest(boardBeforeMove, moveToCheck, colorOfPlayerMoving)))
                validMove = false;
            
            return validMove;
        }

        bool CheckPawn(ChessBoard board, ChessMove move)
        {
            bool moveable = false;
            ChessColor color = board[move.From] == ChessPiece.WhitePawn ? ChessColor.White : ChessColor.Black;

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

            return moveable;
        }

        bool CheckKnight(ChessBoard board, ChessMove move)
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

            return moveable;
        }

        bool CheckBishop(ChessBoard board, ChessMove move)
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

            return moveable;
        }

        bool CheckRook(ChessBoard board, ChessMove move)
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

            return moveable;
        }

        bool CheckQueen(ChessBoard board, ChessMove move)
        {
            int x1 = move.From.X;
            int y1 = move.From.Y;
            int x2 = move.To.X;
            int y2 = move.To.Y;
            int d_x = x2 - x1;
            int d_y = y2 - y1;

            bool moveable = true;

            if (Math.Abs(d_x) == Math.Abs(d_y))
                moveable = CheckBishop(board, move);
            else if (d_x == 0 || d_y == 0)
                moveable = CheckRook(board, move);

            return moveable;
        }

        bool CheckKing(ChessBoard board, ChessMove move)
        {
            int x1 = move.From.X;
            int y1 = move.From.Y;
            int x2 = move.To.X;
            int y2 = move.To.Y;
            int d_x = x2 - x1;
            int d_y = y2 - y1;

            bool moveable = false;

            if (Math.Abs(d_x) <= 1 && Math.Abs(d_y) <= 1)
            {
                ChessPiece piece = _currentBoard[move.From];
                ChessColor color = piece == ChessPiece.WhiteKing ? ChessColor.White : ChessColor.Black;
                
                ChessBoard tempBoard = _currentBoard.Clone();
                tempBoard[move.To] = tempBoard[move.From];
                tempBoard[move.From] = ChessPiece.Empty;
                
                if(!checkForCheck(color == _ourColor,tempBoard))
                {
                    moveable = true;
                }
                else
                {
                    moveable = false;
                }
            }
            
            return moveable;
        }

        //Checks to see if the color at the dest is the same color as the piece moving there.
        //MAKE CHANGES NECESSARY TO ALLOW DETECTING OF SAME COLOR FOR COVERAGE
        bool CheckColorAtDest(ChessBoard board, ChessMove move, ChessColor color)
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
