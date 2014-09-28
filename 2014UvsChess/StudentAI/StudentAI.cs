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

        private class FinalMove
        {
            public ChessMove move;
            public double value;
        }

        double _coverageValue = 0.5;
        double _threatValue = 1.5;
        double _checkValue = 7.0;
        ChessColor _ourColor;
        double[,] _gravity;
        List<ChessMove> _possibleMoves;
        ChessBoard _currentBoard;
        HashSet<ChessLocation> _threatenedPieces, _knightCheck, _bishopCheck, _rookCheck;
        List<FinalMove> _moveQueue;
        ChessMove _previousMove;
        ChessMove _prePreviousMove;
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
            _gravity = new double[8, 8];
            _possibleMoves = new List<ChessMove>();
            _threatenedPieces = new HashSet<ChessLocation>();
            _knightCheck = new HashSet<ChessLocation>();
            _bishopCheck = new HashSet<ChessLocation>();
            _rookCheck = new HashSet<ChessLocation>();
            _currentBoard = board;
            _ourColor = myColor;
            GenerateGravity();
            printGravity();
            BuildPriorityQueue();
            while (_previousMove == _moveQueue[0].move || _prePreviousMove == _moveQueue[0].move)
            {
                _moveQueue.Remove(_moveQueue[0]);
            }
            _prePreviousMove = _previousMove;
            _previousMove = _moveQueue[0].move;
            return _moveQueue[0].move;
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
                        if ((piece < ChessPiece.Empty && _ourColor == ChessColor.Black) || (piece > ChessPiece.Empty && _ourColor == ChessColor.White))
                        {
                            ours = true;
                        }

                        _gravity[x, y] += ours || pieceType == "King" ? 0.0 : (_pieceValues[pieceType]);
                        GenerateMoves(piece, pieceType, x, y, ours);
                    }
                }
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
                    ChessLocation newLoc = new ChessLocation(x1 + loc.X, color == ChessColor.White ? y1 - loc.Y : y1 + loc.Y);
                    if (newLoc.X < 0 || newLoc.X > 7 || newLoc.Y < 0 || newLoc.Y > 7)
                        continue;

                    bool valid = CheckMove(piece, color, new ChessMove(currentLoc, newLoc));

                    if (ours)
                    {
                        if (x1 != newLoc.X)
                            _gravity[newLoc.X, newLoc.Y] += _coverageValue;
                    }
                    else
                    {
                        if (x1 != newLoc.X)
                            _gravity[newLoc.X, newLoc.Y] -= _threatValue;
                    }

                    if (valid)
                    {
                        if (ours)
                        {
                            _possibleMoves.Add(new ChessMove(currentLoc, newLoc));

                        }
                        else
                        {
                            ifThreateningUs(newLoc, color);
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
                            _gravity[newLoc.X, newLoc.Y] += _coverageValue;
                            _possibleMoves.Add(new ChessMove(currentLoc, newLoc));
                        }
                        else
                        {
                            _gravity[newLoc.X, newLoc.Y] -= _threatValue;
                            ifThreateningUs(newLoc, color);
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
                            _gravity[newLoc.X, newLoc.Y] += _coverageValue;
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

                            bool valid = CheckMove(_ourColor == ChessColor.White ? ChessPiece.BlackQueen : ChessPiece.WhiteQueen, color, new ChessMove(currentLoc, newLoc));
                            if (valid)
                            {
                                if (i == 1)
                                {
                                    _gravity[newLoc.X, newLoc.Y] -= _threatValue;
                                    ifThreateningUs(newLoc, color);
                                }
                                else
                                {
                                    if (Math.Abs(newLoc.X - loc.X) == Math.Abs(newLoc.Y - loc.Y))
                                    {
                                        _bishopCheck.Add(newLoc);
                                    }
                                    else
                                    {
                                        _rookCheck.Add(newLoc);
                                    }
                                }
                            }
                        }
                    }
                }

                if (!ours)
                {
                    foreach (ChessLocation loc in _knightMoves)
                    {
                        for (int i = 1; i < 8; i++)
                        {
                            ChessLocation newLoc = new ChessLocation(x1 + loc.X * i, y1 + loc.Y * i);
                            if (newLoc.X < 0 || newLoc.X > 7 || newLoc.Y < 0 || newLoc.Y > 7)
                                continue;

                            bool valid = CheckMove(_ourColor == ChessColor.White ? ChessPiece.BlackKnight : ChessPiece.WhiteKnight, color, new ChessMove(currentLoc, newLoc));
                            if (valid)
                            {
                                _knightCheck.Add(newLoc);
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
                                _gravity[newLoc.X, newLoc.Y] += _coverageValue;
                                _possibleMoves.Add(new ChessMove(currentLoc, newLoc));
                            }
                            else
                            {
                                _gravity[newLoc.X, newLoc.Y] -= _threatValue;
                                ifThreateningUs(newLoc, color);
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
                                _gravity[newLoc.X, newLoc.Y] += _coverageValue;
                                _possibleMoves.Add(new ChessMove(currentLoc, newLoc));
                            }
                            else
                            {
                                _gravity[newLoc.X, newLoc.Y] -= _threatValue;
                                ifThreateningUs(newLoc, color);
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
                                _gravity[newLoc.X, newLoc.Y] += _coverageValue;
                                _possibleMoves.Add(new ChessMove(currentLoc, newLoc));
                            }
                            else
                            {
                                _gravity[newLoc.X, newLoc.Y] -= _threatValue;
                                ifThreateningUs(newLoc, color);
                            }
                        }
                    }
                }
            }
        }

        private void printGravity()
        {
            for (int y = 0; y < 8; y++)
            {
                this.Log(_gravity[0, y] + "\t" + _gravity[1, y] + "\t" + _gravity[2, y] + "\t" + _gravity[3, y] + "\t" + _gravity[4, y] + "\t" + _gravity[5, y] + "\t" + _gravity[6, y] + "\t" + _gravity[7, y]);
            }
            this.Log("");
                
        }

        // Check if piece in loc is in danger
        private void ifThreateningUs(ChessLocation loc, ChessColor enemyColor)
        {
            if (enemyColor == ChessColor.White)
            {
                if (_currentBoard[loc.X, loc.Y] < ChessPiece.Empty)
                {
                    _threatenedPieces.Add(loc);
                }
            }
            else
            {
                if (_currentBoard[loc.X, loc.Y] > ChessPiece.Empty)
                {
                    _threatenedPieces.Add(loc);
                }
            }
        }

        private void BuildPriorityQueue()
        {
            _moveQueue = new List<FinalMove>();

            foreach(ChessMove p_move in _possibleMoves)
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

                calc_val += _gravity[p_move.To.X, p_move.To.Y];

                if (_moveQueue.Count > 0)
                {
                    int i = 0;
                    while(i < _moveQueue.Count && calc_val < _moveQueue[i].value)
                        i++;
                    _moveQueue.Insert(i, new FinalMove { move = p_move, value = calc_val });
                }
                else
                {
                    _moveQueue.Add(new FinalMove { move = p_move, value = calc_val });
                }
            }
        }

        private void checkForCheck(ChessColor color, ChessMove move)
        {
            ChessPiece piece = _currentBoard[move.From];
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
            }
            else
            {
                if((d_x == 0 && (d_y == 1 || (d_y == 2 && y1 == 1)) && _currentBoard[x2,y2] == ChessPiece.Empty) || ((d_x == 1 || d_x == -1) && d_y == 1 && _currentBoard[x2,y2] != ChessPiece.Empty)) //y1 == 1 = starting pawn position for black. Probably could change to global.
                {
                    moveable = true;
                }
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

            bool moveable = true;
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

            bool moveable = true;
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
