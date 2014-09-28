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
            get { return "StudentAI (Debug)"; }
#else
            get { return "StudentAI"; }
#endif
        }

        ChessColor _ourColor;
        double[,] _gravity;
        List<ChessMove> _possibleMoves;
        ChessBoard _currentBoard;
        Dictionary<string, int> _pieceValues = new Dictionary<string, int>()
        {
            { "King", 10 },
            { "Queen", 9 },
            { "Rook", 5 },
            { "Bishop", 3 },
            { "Knight", 3 },
            { "Pawn", 1 }
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
            _gravity = new double[8, 8];
            _possibleMoves = new List<ChessMove>();
            _currentBoard = board;
            _ourColor = myColor;
            GenerateGravity();
            return _possibleMoves[0];
        }

        private void GenerateGravity()
        {
            for(int y = 0; y < 8; y++)
            {
                for(int x = 0; x < 8; x++)
                {
                    if (_currentBoard[x,y] != ChessPiece.Empty)
                    {
                        bool ours = false;
                        ChessPiece piece = _currentBoard[x, y];
                        string pieceType = Enum.GetName(typeof(ChessPiece),piece);
                        pieceType = pieceType.Remove(0,5);

                        // If the piece is our piece
                        if (!((piece < ChessPiece.Empty && _ourColor == ChessColor.Black) || (piece > ChessPiece.Empty && _ourColor == ChessColor.White)))
                        {
                            ours = true;
                        }

                        

                        _gravity[x, y] += ours || pieceType == "King" ? 0 : (_pieceValues[pieceType]);
                        GenerateMoves(piece, pieceType, x, y, ours);
                    }
                }
                this.Log(_gravity[0, y] + " " + _gravity[1, y] + " " + _gravity[2, y] + " " + _gravity[3, y] + " " + _gravity[4, y] + " " + _gravity[5, y] + " " + _gravity[6, y] + " " + _gravity[7, y]);
            }
        }

        private void GenerateMoves(ChessPiece piece, string type, int x1, int y1, bool ours)
        {
            ChessColor color = piece < ChessPiece.Empty ? ChessColor.Black : ChessColor.White;
            ChessLocation currentLoc = new ChessLocation(x1, y1);

            if (type == "Pawn")
            {
                foreach (ChessLocation loc in _pawnMoves)
                {
                    ChessLocation newLoc = new ChessLocation(x1 + loc.X, y1 + color == ChessColor.White ? -(loc.Y) : loc.Y);
                    if (newLoc.X < 0 || newLoc.X > 7 || newLoc.Y < 0 || newLoc.Y > 7)
                        continue;

                    bool valid = CheckMove(piece, color, new ChessMove(currentLoc, newLoc));
                    if (valid)
                    {
                        if (ours)
                        {
                            if (y1 != newLoc.Y)
                                _gravity[newLoc.X, newLoc.Y] += 1;
                            _possibleMoves.Add(new ChessMove(currentLoc, newLoc));
                        }
                        else
                        {
                            if (y1 != newLoc.Y)
                                _gravity[newLoc.X, newLoc.Y] -= 1;
                        }
                    }
                }
            }
            else if (type == "Knight")
            {
                foreach (ChessLocation loc in _knightMoves)
                {
                    ChessLocation newLoc = new ChessLocation(x1 + loc.X, y1 + loc.Y);
                    if (newLoc.X < 0 || newLoc.X > 7 || newLoc.Y < 0 || newLoc.Y > 7)
                        continue;

                    bool valid = CheckMove(piece, color, new ChessMove(currentLoc, newLoc));
                    if (valid)
                    {
                        if (ours)
                        {
                            _gravity[newLoc.X, newLoc.Y] += 1;
                            _possibleMoves.Add(new ChessMove(currentLoc, newLoc));
                        }
                        else
                        {
                            _gravity[newLoc.X, newLoc.Y] -= 1;
                        }
                    }
                }
            }
            else if (type == "King")
            {
                foreach (ChessLocation loc in _kingMoves)
                {
                    if (ours)
                    {
                        ChessLocation newLoc = new ChessLocation(x1 + loc.X, y1 + loc.Y);
                        if (newLoc.X < 0 || newLoc.X > 7 || newLoc.Y < 0 || newLoc.Y > 7)
                            continue;

                        bool valid = CheckMove(piece, color, new ChessMove(currentLoc, newLoc));
                        if (valid)
                        {
                            _gravity[newLoc.X, newLoc.Y] += 1;
                            _possibleMoves.Add(new ChessMove(currentLoc, newLoc));
                        }
                    }
                    else
                    {
                        for (int i = 1; i < 8; i++)
                        {
                            ChessLocation newLoc = new ChessLocation(x1 + loc.X * i, y1 + loc.Y * i);
                            if (newLoc.X < 0 || newLoc.X > 7 || newLoc.Y < 0 || newLoc.Y > 7)
                                continue;

                            bool valid = CheckMove(piece, color, new ChessMove(currentLoc, newLoc));
                            if (valid)
                            {
                                if (i == 1)
                                {
                                    _gravity[newLoc.X, newLoc.Y] -= 1;
                                }
                                else
                                {
                                    _gravity[newLoc.X, newLoc.Y] += 10;
                                }
                            }
                        }
                    }
                    
                }
            }
            else if (type == "Bishop")
            {
                foreach (ChessLocation loc in _bishopMoves)
                {
                    for(int i = 1; i < 8; i++)
                    {
                        ChessLocation newLoc = new ChessLocation(x1 + loc.X * i, y1 + loc.Y * i);
                        if (newLoc.X < 0 || newLoc.X > 7 || newLoc.Y < 0 || newLoc.Y > 7)
                            continue;

                        bool valid = CheckMove(piece, color, new ChessMove(currentLoc, newLoc));
                        if (valid)
                        {
                            if (ours)
                            {
                                _gravity[newLoc.X, newLoc.Y] += 1;
                                _possibleMoves.Add(new ChessMove(currentLoc, newLoc));
                            }
                            else
                            {
                                _gravity[newLoc.X, newLoc.Y] -= 1;
                            }
                        }
                    }
                }
            }
            else if (type == "Rook")
            {
                foreach (ChessLocation loc in _rookMoves)
                {
                    for (int i = 1; i < 8; i++)
                    {
                        ChessLocation newLoc = new ChessLocation(x1 + loc.X * i, y1 + loc.Y * i);
                        if (newLoc.X < 0 || newLoc.X > 7 || newLoc.Y < 0 || newLoc.Y > 7)
                            continue;

                        bool valid = CheckMove(piece, color, new ChessMove(currentLoc, newLoc));
                        if (valid)
                        {
                            if (ours)
                            {
                                _gravity[newLoc.X, newLoc.Y] += 1;
                                _possibleMoves.Add(new ChessMove(currentLoc, newLoc));
                            }
                            else
                            {
                                _gravity[newLoc.X, newLoc.Y] -= 1;
                            }
                        }
                    }
                }
            }
            else if (type == "Queen")
            {
                foreach (ChessLocation loc in _kingMoves)
                {
                    for (int i = 1; i < 8; i++)
                    {
                        ChessLocation newLoc = new ChessLocation(x1 + loc.X * i, y1 + loc.Y * i);
                        if (newLoc.X < 0 || newLoc.X > 7 || newLoc.Y < 0 || newLoc.Y > 7)
                            continue;

                        bool valid = CheckMove(piece, color, new ChessMove(currentLoc, newLoc));
                        if (valid)
                        {
                            if (ours)
                            {
                                _gravity[newLoc.X, newLoc.Y] += 1;
                                _possibleMoves.Add(new ChessMove(currentLoc, newLoc));
                            }
                            else
                            {
                                _gravity[newLoc.X, newLoc.Y] -= 1;
                            }
                        }
                    }
                }
            }
        }

        //// Could piece be taken on opponents next turn
        //private bool isThreatened(AIChessPieces piece)
        //{
            
        //}

        //// Checks to see if there 
        //private bool isCovered(AIChessPieces piece)
        //{

        //}

        //// Pieces that the passed piece can capture in its current location
        //private void CurrentlyThreatening(AIChessPieces piece)
        //{

        //}

        ////// Pieces that the passed piece can capture in the next given move
        ////private void PossibleCaptures(AIChessPieces piece, ChessLocation loc)
        ////{

        ////}

        //// Takes in the list of moves, and returns the move to take
        //// Also calculates board gravity to determine best move
        //private void Gravity(AIChessPieces piece)
        //{

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
            _currentBoard = boardBeforeMove;
            bool valid = false;
            ChessPiece pieceToMove = boardBeforeMove[moveToCheck.From];

            
            valid = CheckMove(pieceToMove, colorOfPlayerMoving, moveToCheck);
            
            return valid;
        }

        bool CheckMove(ChessPiece piece, ChessColor color, ChessMove move)
        {
            bool validMove = false;

            if (move.To.X < 0 || move.To.X > 7 || move.To.Y < 0 || move.To.Y > 7)
                return false;

            if (piece == ChessPiece.WhitePawn || piece == ChessPiece.BlackPawn)
            {
                validMove = CheckPawn(move, color);
            }
            else if (piece == ChessPiece.WhiteKnight || piece == ChessPiece.BlackKnight)
            {
                validMove = CheckKnight(move);
            }
            else if (piece == ChessPiece.WhiteBishop || piece == ChessPiece.BlackBishop)
            {
                validMove = CheckBishop(move);
            }
            else if (piece == ChessPiece.WhiteRook || piece == ChessPiece.BlackRook)
            {
                validMove = CheckRook(move);
            }
            else if (piece == ChessPiece.WhiteQueen || piece == ChessPiece.BlackQueen)
            {
                validMove = CheckQueen(move);
            }
            else if (piece == ChessPiece.WhiteKing || piece == ChessPiece.BlackKing)
            {
                validMove = CheckKing(move);
            }

            if (!(CheckColorAtDest(color, move)))
                validMove = false;

            return validMove;
        }

        bool CheckPawn(ChessMove move, ChessColor color)
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
                if((d_x == 0 && (d_y == -1 || (d_y == -2 && y1 == 6)) && _currentBoard[x2,y2] == ChessPiece.Empty) || ((d_x == 1 || d_x == -1) && d_y == -1 && _currentBoard[x2,y2] != ChessPiece.Empty)) //y1 == 6 = starting pawn position for white. Probably could change to a global.
                {
                    moveable = true;
                }
                moveable = false;
            }
            else
            {
                if((d_x == 0 && (d_y == 1 || (d_y == 2 && y1 == 1)) && _currentBoard[x2,y2] == ChessPiece.Empty) || ((d_x == 1 || d_x == -1) && d_y == 1 && _currentBoard[x2,y2] != ChessPiece.Empty)) //y1 == 1 = starting pawn position for black. Probably could change to global.
                {
                    moveable = true;
                }
                moveable = false;
            }

            return moveable;
        }

        bool CheckKnight(ChessMove move)
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

        bool CheckBishop(ChessMove move)
        {
            int x1 = move.From.X;
            int y1 = move.From.Y;
            int x2 = move.To.X;
            int y2 = move.To.Y;
            int d_x = x2 - x1;
            int d_y = y2 - y1;

            bool moveable = false;
            if (Math.Abs(d_x) == Math.Abs(d_y))
            {
                if(d_y > 0)
                {
                    if(d_x > 0)
                    {
                        for(int i = 1; i < d_x; ++i)
                        {
                            if (_currentBoard[x1 + i, y1 + i] != ChessPiece.Empty)
                                moveable = false;
                        }
                    }
                    else
                    {
                        for(int i = -1; i > d_x; --i)
                        {
                            if (_currentBoard[x1 + i, y1 - i] != ChessPiece.Empty)
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
                            if (_currentBoard[x1 + i, y1 - i] != ChessPiece.Empty)
                                moveable = false;
                        }
                    }
                    else
                    {
                        for(int i = -1; i > d_x; --i)
                        {
                            if (_currentBoard[x1 + i, y1 + i] != ChessPiece.Empty)
                                moveable = false;
                        }
                    }
                }
            }
            else moveable = false;

            return moveable;
        }

        bool CheckRook(ChessMove move)
        {
            int x1 = move.From.X;
            int y1 = move.From.Y;
            int x2 = move.To.X;
            int y2 = move.To.Y;
            int d_x = x2 - x1;
            int d_y = y2 - y1;

            bool moveable = false;
            if (d_x == 0)
            {
                if (d_y > 0)
                {
                    for (int i = 1; i < d_y; ++i)
                    {
                        if (_currentBoard[x1, y1 + i] != ChessPiece.Empty)
                            moveable = false;
                    }
                }
                else
                {
                    for (int i = -1; i > d_y; --i)
                    {
                        if (_currentBoard[x1, y1 + i] != ChessPiece.Empty)
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
                        if (_currentBoard[x1 + i, y1] != ChessPiece.Empty)
                        {
                            moveable = false;
                        }
                    }
                }
                else
                {
                    for (int i = -1; i > d_x; --i)
                    {
                        if (_currentBoard[x1 + i, y1] != ChessPiece.Empty)
                            moveable = false;
                    }
                }
            }
            else moveable = false;

            return moveable;
        }

        bool CheckQueen(ChessMove move)
        {
            int x1 = move.From.X;
            int y1 = move.From.Y;
            int x2 = move.To.X;
            int y2 = move.To.Y;
            int d_x = x2 - x1;
            int d_y = y2 - y1;

            bool moveable = true;

            if (Math.Abs(d_x) == Math.Abs(d_y))
                moveable = CheckBishop(move);
            else if (d_x == 0 || d_y == 0)
                moveable = CheckRook(move);

            return moveable;
        }

        bool CheckKing(ChessMove move)
        {
            int x1 = move.From.X;
            int y1 = move.From.Y;
            int x2 = move.To.X;
            int y2 = move.To.Y;
            int d_x = x2 - x1;
            int d_y = y2 - y1;

            bool moveable = false;

            if (Math.Abs(d_x) <= 1 && Math.Abs(d_y) <= 1)
                moveable = true;

            return moveable;
        }

        //Checks to see if the color at the dest is the same color as the piece moving there.
        //MAKE CHANGES NECESSARY TO ALLOW DETECTING OF SAME COLOR FOR COVERAGE
        bool CheckColorAtDest(ChessColor color, ChessMove move)
        {
            if (color == ChessColor.White)
            {
                if (_currentBoard[move.To] > ChessPiece.Empty)
                    return false;
            }
            else
            {
                if (_currentBoard[move.To] < ChessPiece.Empty)
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
