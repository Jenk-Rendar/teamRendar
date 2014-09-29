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

        private class FinalMove
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
        double[,] _gravity;
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
            _gravity = new double[8, 8];
            // _possibleMoves = new List<ChessMove>();
            _threatenedPieces = new HashSet<ChessLocation>();
            _knightCheck = new HashSet<ChessLocation>();
            _bishopCheck = new HashSet<ChessLocation>();
            _rookCheck = new HashSet<ChessLocation>();

            List<ChessMove> moves = GenerateGravity();
            printGravity();

            bool enemyKingHasMoves = GeneratePossibleKingMoves(_enemyKingLoc, _enemyColor);
            if(!enemyKingHasMoves || _numEnemyPieces < 5)
            {
                _checkValue = 10.0;
            }

            List<FinalMove> moveQueue = BuildPriorityQueue(moves);

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
                tempBoard[moveQueue[0].move.To] = tempBoard[moveQueue[0].move.From];
                tempBoard[moveQueue[0].move.From] = ChessPiece.Empty;
            }

            if(checkForCheck(false, tempBoard))
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

            if(tempBoard[moveQueue[0].move.To] == (_ourColor == ChessColor.White ? ChessPiece.WhitePawn : ChessPiece.BlackPawn))
            {
                if(moveQueue[0].move.To.Y == (_ourColor == ChessColor.White ? 0 : 7))
                {
                    tempBoard[moveQueue[0].move.To] = (_ourColor == ChessColor.White ? ChessPiece.WhiteQueen : ChessPiece.BlackQueen);
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
                }
            }


            _checkValue = 0.0;
            return moveQueue[0].move;
        }

        private List<ChessMove> GenerateGravity()
        {
            _numEnemyPieces = 0;
            List<ChessMove> moves = new List<ChessMove>();
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

                        if (!ours)
                        {
                            _numEnemyPieces++;
                        }

                        _gravity[x, y] += ours || pieceType == "King" ? 0.0 : (_pieceValues[pieceType]);

                        if (moves.Count > 0)
                        {
                            moves.InsertRange(moves.Count - 1, GenerateMoves(piece, pieceType, new ChessLocation(x, y), ours));
                        }
                        else
                        {
                            moves = GenerateMoves(piece, pieceType, new ChessLocation(x, y), ours);
                        }
                    }
                }
            }

            return moves;
        }

        private List<ChessMove> GenerateMoves(ChessPiece piece, string type, ChessLocation currentLoc, bool ours)
        {
            ChessColor color = piece < ChessPiece.Empty ? ChessColor.Black : ChessColor.White;
            List<ChessMove> moves = null;

            if (type == "Pawn")
            {
                moves = GeneratePawnMoves(piece, ours, color, currentLoc);
            }
            else if (type == "Knight")
            {
                moves = GenerateKnightMoves(piece, ours, color, currentLoc);
            }
            else if (type == "Bishop")
            {
                moves = GenerateBishopMoves(piece, ours, color, currentLoc);
            }
            else if (type == "Rook")
            {
                moves = GenerateRookMoves(piece, ours, color, currentLoc);
            }
            else if (type == "Queen")
            {
                moves = GenerateQueenMoves(piece, ours, color, currentLoc);
            }
            else if (type == "King")
            {
                moves = GenerateKingMoves(piece, ours, color, currentLoc);
            }

            return moves;
        }

        private List<ChessMove> GeneratePawnMoves(ChessPiece piece, bool ours, ChessColor color, ChessLocation currentLoc)
        {
            List<ChessMove> moves = new List<ChessMove>();
            foreach (ChessLocation loc in _pawnMoves)
            {
                ChessLocation newLoc = new ChessLocation(currentLoc.X + loc.X, color == ChessColor.White ? currentLoc.Y - loc.Y : currentLoc.Y + loc.Y);
                if (newLoc.X < 0 || newLoc.X > 7 || newLoc.Y < 0 || newLoc.Y > 7)
                    continue;

                bool valid = CheckMove(piece, color, new ChessMove(currentLoc, newLoc));

                if (ours)
                {
                    if (currentLoc.X != newLoc.X)
                        _gravity[newLoc.X, newLoc.Y] += _coverageValue;
                }
                else
                {
                    if (currentLoc.X != newLoc.X)
                        _gravity[newLoc.X, newLoc.Y] -= _threatValue;
                }

                if (valid)
                {
                    if (ours)
                    {
                        moves.Add(new ChessMove(currentLoc, newLoc));
                    }
                    else
                    {
                        ifThreateningUs(newLoc, color);
                    }
                }
            }

            return moves;
        }

        private List<ChessMove> GenerateKnightMoves(ChessPiece piece, bool ours, ChessColor color, ChessLocation currentLoc)
        {
            List<ChessMove> moves = new List<ChessMove>();
            foreach (ChessLocation loc in _knightMoves)
            {
                ChessLocation newLoc = new ChessLocation(currentLoc.X + loc.X, currentLoc.Y + loc.Y);
                if (newLoc.X < 0 || newLoc.X > 7 || newLoc.Y < 0 || newLoc.Y > 7)
                    continue;

                bool valid = CheckMove(piece, color, new ChessMove(currentLoc, newLoc));
                if (valid)
                {
                    if (ours)
                    {
                        _gravity[newLoc.X, newLoc.Y] += _coverageValue;
                        moves.Add(new ChessMove(currentLoc, newLoc));
                    }
                    else
                    {
                        _gravity[newLoc.X, newLoc.Y] -= _threatValue;
                        ifThreateningUs(newLoc, color);
                    }
                }
            }

            return moves;
        }

        private List<ChessMove> GenerateBishopMoves(ChessPiece piece, bool ours, ChessColor color, ChessLocation currentLoc)
        {
            List<ChessMove> moves = new List<ChessMove>();
            foreach (ChessLocation loc in _bishopMoves)
            {
                for (int i = 1; i < 8; i++)
                {
                    ChessLocation newLoc = new ChessLocation(currentLoc.X + loc.X * i, currentLoc.Y + loc.Y * i);
                    if (newLoc.X < 0 || newLoc.X > 7 || newLoc.Y < 0 || newLoc.Y > 7)
                        continue;

                    bool valid = CheckMove(piece, color, new ChessMove(currentLoc, newLoc));
                    if (valid)
                    {
                        if (ours)
                        {
                            _gravity[newLoc.X, newLoc.Y] += _coverageValue;
                            moves.Add(new ChessMove(currentLoc, newLoc));
                        }
                        else
                        {
                            _gravity[newLoc.X, newLoc.Y] -= _threatValue;
                            ifThreateningUs(newLoc, color);
                        }
                    }
                }
            }

            return moves;
        }

        private List<ChessMove> GenerateRookMoves(ChessPiece piece, bool ours, ChessColor color, ChessLocation currentLoc)
        {
            List<ChessMove> moves = new List<ChessMove>();
            foreach (ChessLocation loc in _rookMoves)
            {
                for (int i = 1; i < 8; i++)
                {
                    ChessLocation newLoc = new ChessLocation(currentLoc.X + loc.X * i, currentLoc.Y + loc.Y * i);
                    if (newLoc.X < 0 || newLoc.X > 7 || newLoc.Y < 0 || newLoc.Y > 7)
                        continue;

                    bool valid = CheckMove(piece, color, new ChessMove(currentLoc, newLoc));
                    if (valid)
                    {
                        if (ours)
                        {
                            _gravity[newLoc.X, newLoc.Y] += _coverageValue;
                            moves.Add(new ChessMove(currentLoc, newLoc));
                        }
                        else
                        {
                            _gravity[newLoc.X, newLoc.Y] -= _threatValue;
                            ifThreateningUs(newLoc, color);
                        }
                    }
                }
            }

            return moves;
        }

        private List<ChessMove> GenerateQueenMoves(ChessPiece piece, bool ours, ChessColor color, ChessLocation currentLoc)
        {
            List<ChessMove> moves = new List<ChessMove>();
            foreach (ChessLocation loc in _kingMoves)
            {
                for (int i = 1; i < 8; i++)
                {
                    ChessLocation newLoc = new ChessLocation(currentLoc.X + loc.X * i, currentLoc.Y + loc.Y * i);
                    if (newLoc.X < 0 || newLoc.X > 7 || newLoc.Y < 0 || newLoc.Y > 7)
                        continue;

                    bool valid = CheckMove(piece, color, new ChessMove(currentLoc, newLoc));
                    if (valid)
                    {
                        if (ours)
                        {
                            _gravity[newLoc.X, newLoc.Y] += _coverageValue;
                            moves.Add(new ChessMove(currentLoc, newLoc));
                        }
                        else
                        {
                            _gravity[newLoc.X, newLoc.Y] -= _threatValue;
                            ifThreateningUs(newLoc, color);
                        }
                    }
                }
            }

            return moves;
        }

        private List<ChessMove> GenerateKingMoves(ChessPiece piece, bool ours, ChessColor color, ChessLocation currentLoc)
        {
            List<ChessMove> moves = new List<ChessMove>();
            foreach (ChessLocation loc in _kingMoves)
            {
                if (ours)
                {
                    _ourKingLoc = currentLoc;
                    ChessLocation newLoc = new ChessLocation(currentLoc.X + loc.X, currentLoc.Y + loc.Y);
                    if (newLoc.X < 0 || newLoc.X > 7 || newLoc.Y < 0 || newLoc.Y > 7)
                        continue;

                    bool valid = CheckMove(piece, color, new ChessMove(currentLoc, newLoc));
                    if (valid)
                    {
                        _gravity[newLoc.X, newLoc.Y] += _coverageValue;
                        moves.Add(new ChessMove(currentLoc, newLoc));
                    }
                }
                else
                {
                    _enemyKingLoc = currentLoc;
                    for (int i = 1; i < 8; i++)
                    {
                        ChessLocation newLoc = new ChessLocation(currentLoc.X + loc.X * i, currentLoc.Y + loc.Y * i);
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
                        ChessLocation newLoc = new ChessLocation(currentLoc.X + loc.X * i, currentLoc.Y + loc.Y * i);
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

            return moves;
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
        private bool ifThreateningUs(ChessLocation loc, ChessColor enemyColor)
        {
            if (enemyColor == ChessColor.White)
            {
                if (_currentBoard[loc.X, loc.Y] < ChessPiece.Empty)
                {
                    _threatenedPieces.Add(loc);
                    return true;
                }
            }
            else
            {
                if (_currentBoard[loc.X, loc.Y] > ChessPiece.Empty)
                {
                    _threatenedPieces.Add(loc);
                    return true;
                }
            }
            return false;
        }

        private List<FinalMove> BuildPriorityQueue(List<ChessMove> moves)
        {
            List<FinalMove> moveQueue = new List<FinalMove>();

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

                calc_val += _gravity[p_move.To.X, p_move.To.Y];

                if (moveQueue.Count > 0)
                {
                    int i = 0;
                    while(i < moveQueue.Count && calc_val <= moveQueue[i].value)
                        i++;
                    moveQueue.Insert(i, new FinalMove { move = p_move, value = calc_val });
                }
                else
                {
                    moveQueue.Add(new FinalMove { move = p_move, value = calc_val });
                }
            }

            return moveQueue;
        }

        private bool checkForCheck(bool ours, ChessBoard board)
        {
            ChessLocation kingLoc;
            ChessColor color;
            List<ChessMove> moves = null;

            if (ours)
            {
                kingLoc = _ourKingLoc;
                color = _ourColor;
            }
            else
            {
                kingLoc = _enemyKingLoc;
                color = _ourColor == ChessColor.White ? ChessColor.Black : ChessColor.White;
            }

            if (color == ChessColor.White)
            {
                if (kingLoc.Y > 0)
                {
                    if (kingLoc.X > 0)
                    {
                        if (board[kingLoc.X - 1, kingLoc.Y - 1] == ChessPiece.BlackPawn)
                        {
                            return true;
                        }
                    }
                    if (kingLoc.X < 7)
                    {
                        if (board[kingLoc.X + 1, kingLoc.Y - 1] == ChessPiece.BlackPawn)
                        {
                            return true;
                        }
                    }
                }
            }
            else
            {
                if (kingLoc.Y < 7)
                {
                    if (kingLoc.X > 0)
                    {
                        if (board[kingLoc.X - 1, kingLoc.Y + 1] == ChessPiece.WhitePawn)
                        {
                            return true;
                        }
                    }
                    if (kingLoc.X < 7)
                    {
                        if (board[kingLoc.X + 1, kingLoc.Y + 1] == ChessPiece.WhitePawn)
                        {
                            return true;
                        }
                    }
                }
            }

            moves = GenerateKnightMoves(color == ChessColor.White ? ChessPiece.WhiteKnight : ChessPiece.BlackKnight, !ours, color, kingLoc);
            foreach(ChessMove p_move in moves)
            {
                //int x1 = p_move.From.X;
                //int y1 = p_move.From.Y;
                //int x2 = p_move.To.X;
                //int y2 = p_move.To.Y;
                //this.Log(String.Format("Knight Check Call: From ({0},{1}) To ({2},{3}", x1, y1, x2, y2));
                if (board[p_move.To] == (color == ChessColor.White ? ChessPiece.BlackKnight : ChessPiece.WhiteKnight))
                {
                    return true;
                }
            }

            moves = GenerateBishopMoves(color == ChessColor.White ? ChessPiece.WhiteBishop : ChessPiece.BlackBishop, !ours, color, kingLoc);
            foreach (ChessMove p_move in moves)
            {
                //int x1 = p_move.From.X;
                //int y1 = p_move.From.Y;
                //int x2 = p_move.To.X;
                //int y2 = p_move.To.Y;
                //this.Log(String.Format("Bishop Check Call: From ({0},{1}) To ({2},{3}",x1,y1,x2,y2));
                if (board[p_move.To] == (color == ChessColor.White ? ChessPiece.BlackBishop : ChessPiece.WhiteBishop) || board[p_move.To] == (color == ChessColor.White ? ChessPiece.BlackQueen : ChessPiece.WhiteQueen))
                {
                    return true;
                }
            }
            
            moves = GenerateRookMoves(color == ChessColor.White ? ChessPiece.WhiteRook : ChessPiece.BlackRook, !ours, color, kingLoc);
            foreach (ChessMove p_move in moves)
            {
                //int x1 = p_move.From.X;
                //int y1 = p_move.From.Y;
                //int x2 = p_move.To.X;
                //int y2 = p_move.To.Y;
                //this.Log(String.Format("Rook Check Call: From ({0},{1}) To ({2},{3}", x1, y1, x2, y2));
                if (board[p_move.To] == (color == ChessColor.White ? ChessPiece.BlackRook : ChessPiece.WhiteRook) || board[p_move.To] == (color == ChessColor.White ? ChessPiece.BlackQueen : ChessPiece.WhiteQueen))
                {
                    return true;
                }
            }

            return false;
        }

        bool GeneratePossibleKingMoves(ChessLocation kingLoc, ChessColor color)
        {
            List<ChessMove> moves = new List<ChessMove>();

            foreach (ChessLocation loc in _kingMoves)
            {
                ChessLocation newLoc = new ChessLocation(kingLoc.X + loc.X, kingLoc.Y + loc.Y);
                if (newLoc.X < 0 || newLoc.X > 7 || newLoc.Y < 0 || newLoc.Y > 7)
                    continue;

                ChessMove posMove = new ChessMove(kingLoc, newLoc);
                bool valid = CheckKing(posMove);

                if (valid)
                    valid = CheckColorAtDest(color, posMove);

                if (valid)
                {
                    moves.Add(posMove);
                }
            }

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
            bool valid = false;
            _currentBoard = boardBeforeMove;
            
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
                if ((d_x == 0 && (d_y == -1 || (d_y == -2 && y1 == 6 && _currentBoard[x2, y2 + 1] == ChessPiece.Empty)) && _currentBoard[x2, y2] == ChessPiece.Empty) || ((d_x == 1 || d_x == -1) && d_y == -1 && _currentBoard[x2, y2] != ChessPiece.Empty)) //y1 == 6 = starting pawn position for white. Probably could change to a global.
                {
                    moveable = true;
                }
            }
            else
            {
                if((d_x == 0 && (d_y == 1 || (d_y == 2 && y1 == 1 && _currentBoard[x2, y2 - 1] == ChessPiece.Empty)) && _currentBoard[x2,y2] == ChessPiece.Empty) || ((d_x == 1 || d_x == -1) && d_y == 1 && _currentBoard[x2,y2] != ChessPiece.Empty)) //y1 == 1 = starting pawn position for black. Probably could change to global.
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
