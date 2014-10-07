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

        //int _turnsTaken;
        // List<ChessMove> _possibleMoves;
        // List<FinalMove> _moveQueue;
        //Dictionary<ChessBoard, int> _calculatedBoards = new Dictionary<ChessBoard, int>();
        ChessColor _ourColor;
        ChessMove _previousMove, _prePreviousMove, _previousCheck, _prePreviousCheck;
        double _startTime;
        const int MAX_TIME = 4900;
        const int CHECKMATE_VAL = 10000000;

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
            _startTime = DateTime.Now.TimeOfDay.TotalMilliseconds;
            //_turnsTaken++;
            _ourColor = myColor;
            ChessColor enemyColor = myColor == ChessColor.White ? ChessColor.Black : ChessColor.White;

            List<CalcMove> moves = StartAlphaBeta(board, myColor);

            while (moves.Count == 0)
            {
                moves = StartAlphaBeta(board, myColor);
            }

            while (moves[0].move == _prePreviousMove || moves[0].move == _prePreviousCheck)
            {
                moves.Remove(moves[0]);
            }

            ChessMove selectedMove = moves[0].move.Clone();

            List<CalcMove> maxMoves = GetMaxMoves(moves);

            if (maxMoves.Count > 0)
            {
                Random rnd = new Random();
                int rndIndex = rnd.Next() % maxMoves.Count;
                selectedMove = maxMoves[rndIndex].move.Clone();
            }

            _prePreviousMove = _previousMove;
            _previousMove = selectedMove;

            ChessBoard tempBoard = board.Clone();
            tempBoard[selectedMove.To] = tempBoard[selectedMove.From];
            tempBoard[selectedMove.From] = ChessPiece.Empty;

            if ((tempBoard[selectedMove.To] == ChessPiece.WhitePawn && selectedMove.To.Y == 0) || (tempBoard[selectedMove.To] == ChessPiece.BlackPawn && selectedMove.To.Y == 7))
            {
                tempBoard[selectedMove.To] = myColor == ChessColor.White ? ChessPiece.WhiteQueen : ChessPiece.BlackQueen;
            }

            ChessLocation enemyKingLoc = findKing(tempBoard, enemyColor);

            // Checks if their king is threatened after our move
            int numPiecesThreateningEnemyKing = PieceThreatened(tempBoard, enemyKingLoc, enemyColor).Count;
            if (numPiecesThreateningEnemyKing > 0)
            {
                selectedMove.Flag = ChessFlag.Check;
                _prePreviousCheck = _previousCheck;
                _previousCheck = selectedMove;

                if (!CanKingMove(tempBoard, enemyKingLoc, enemyColor))
                {
                    this.Log("Enemy king cannot move");
                    if (numPiecesThreateningEnemyKing > 1)
                    {
                        this.Log("Multiple threats to enemy king");
                        selectedMove.Flag = ChessFlag.Checkmate;
                    }
                    else if (!CanPieceThreateningKingBeTaken(tempBoard, enemyKingLoc, enemyColor))
                    {
                        this.Log("Piece checking king cannot be taken");
                        if (!CanKingBeBlocked(tempBoard, enemyKingLoc, enemyColor))
                        {
                            this.Log("Piece checking king cannot be blocked");
                            selectedMove.Flag = ChessFlag.Checkmate;
                        }
                    }
                }
            }

            return selectedMove;
        }

        private List<CalcMove> GetMaxMoves(List<CalcMove> moves)
        {
            List<CalcMove> maxMoves = new List<CalcMove>();

            int curMax = 0;

            for (int i = 0; i < moves.Count; i++)
            {
                if (moves[i].value > curMax)
                {
                    curMax = moves[i].value;
                    maxMoves.Clear();
                    CalcMove tempMove = new CalcMove();
                    tempMove.move = moves[i].move.Clone();
                    tempMove.value = moves[i].value;
                    maxMoves.Add(tempMove);
                }
                else if (moves[i].value == curMax)
                {
                    CalcMove tempMove = new CalcMove();
                    tempMove.move = moves[i].move.Clone();
                    tempMove.value = moves[i].value;
                    maxMoves.Add(tempMove);
                }
            }

            return maxMoves;
        }

        private List<CalcMove> StartAlphaBeta(ChessBoard board, ChessColor color)
        {
            ChessColor enemyColor = color == ChessColor.White ? ChessColor.Black : ChessColor.White;

            List<ChessMove> possibleMoves = GenerateMoves(board, color);
            List<CalcMove> finishedMoves = new List<CalcMove>();

            int alpha = Int32.MinValue;
            int beta = Int32.MaxValue;

            foreach (ChessMove p_move in possibleMoves)
            {
                ChessBoard tempBoard = board.Clone();
                tempBoard[p_move.To] = tempBoard[p_move.From];
                tempBoard[p_move.From] = ChessPiece.Empty;

                CalcMove newMove = new CalcMove();
                newMove.move = new ChessMove(p_move.From, p_move.To);

                int score = AlphaBetaMin(tempBoard, enemyColor, alpha, beta, 2);

                if (score >= beta)
                {
                    newMove.value = score;
                }
                else
                {
                    if (score > alpha)
                    {
                        alpha = score;
                    }

                    newMove.value = alpha;
                }

                //if (board[p_move.From] == ChessPiece.BlackKnight || board[p_move.From] == ChessPiece.WhiteKnight)
                //{
                //    if(GetNumPawns(board, color) > 4)
                //    {
                //        newMove.value += 1;
                //    }
                //}
                //else if (board[p_move.From] == ChessPiece.BlackBishop || board[p_move.From] == ChessPiece.WhiteBishop)
                //{
                //    if (GetNumPawns(board, color) <= 4)
                //    {
                //        newMove.value += 1;
                //    }
                //}

                //if (board[p_move.To] != ChessPiece.Empty && newMove.value < CHECKMATE_VAL)
                //{
                //    if (GetPieceValue(board[p_move.To]) > GetPieceValue(board[p_move.From]))
                //        newMove.value += 1000;
                //    else
                //        newMove.value += 100;
                //}

                //if (PieceThreatened(tempBoard, p_move.To, color).Count > 0)
                //{
                //    newMove.value -= 100;
                //}

                //if (newMove.value < CHECKMATE_VAL && PieceThreatened(tempBoard, findKing(tempBoard, enemyColor), enemyColor).Count > 0)
                //{
                //    if (PieceThreatened(tempBoard, p_move.To, color).Count == 0)
                //        newMove.value += 10000;
                //}

                //if (PieceThreatened(board, findKing(board, color), color).Count > 0 && newMove.value < CHECKMATE_VAL)
                //{
                //    if (PieceThreatened(tempBoard, findKing(board, color), color).Count == 0)
                //    {
                //        newMove.value += 100000;
                //        this.Log("Piece can get king out of check, val = " + newMove.value);
                //    }
                //}

                if (finishedMoves.Count > 0)
                {
                    int i = 0;

                    if (color == ChessColor.White)
                        while (i < finishedMoves.Count && newMove.value <= finishedMoves[i].value) i++;
                    else
                        while (i < finishedMoves.Count && newMove.value < finishedMoves[i].value) i++;
                    finishedMoves.Insert(i, newMove);
                }
                else
                {
                    finishedMoves.Add(newMove);
                }
            }

            return finishedMoves;
        }

        private int AlphaBetaMax(ChessBoard board, ChessColor color, int alpha, int beta, int depth)
        {
            ChessColor enemyColor = color == ChessColor.White ? ChessColor.Black : ChessColor.White;

            double timeSpent = DateTime.Now.TimeOfDay.TotalMilliseconds - _startTime;

            if (Checkmate(board, findKing(board, enemyColor), enemyColor))
            {
                return CHECKMATE_VAL;
            }

            if (timeSpent >= MAX_TIME || depth == 0)
            {
                return CalculateBoardState(board, color);
            }

            List<ChessMove> possibleMoves = GenerateMoves(board, color);

            foreach (ChessMove move in possibleMoves)
            {
                ChessBoard tempBoard = board.Clone();
                tempBoard[move.To] = tempBoard[move.From];
                tempBoard[move.From] = ChessPiece.Empty;

                int score = AlphaBetaMin(tempBoard, enemyColor, alpha, beta, depth - 1);

                if (score >= beta)
                {
                    return score;
                }

                if (score > alpha)
                {
                    alpha = score;
                }
            }

            return alpha;
        }

        private int AlphaBetaMin(ChessBoard board, ChessColor color, int alpha, int beta, int depth)
        {
            ChessColor enemyColor = color == ChessColor.White ? ChessColor.Black : ChessColor.White;

            double timeSpent = DateTime.Now.TimeOfDay.TotalMilliseconds - _startTime;

            if (Checkmate(board, findKing(board, enemyColor), enemyColor))
            {
                return CHECKMATE_VAL;
            }

            if (timeSpent >= MAX_TIME || depth == 0)
            {
                return CalculateBoardState(board, color);
            }

            List<ChessMove> possibleMoves = GenerateMoves(board, color);

            foreach (ChessMove move in possibleMoves)
            {
                ChessBoard tempBoard = board.Clone();
                tempBoard[move.To] = tempBoard[move.From];
                tempBoard[move.From] = ChessPiece.Empty;

                int score = AlphaBetaMax(tempBoard, enemyColor, alpha, beta, depth - 1);

                if (score <= alpha)
                {
                    return score;
                }

                if (score < beta)
                {
                    beta = score;
                }
            }

            return beta;
        }

        private int CalculateBoardState(ChessBoard board, ChessColor color)
        {
            ChessColor enemyColor = color == ChessColor.White ? ChessColor.Black : ChessColor.White;

            //if (_calculatedBoards.ContainsKey(board))
            //{
            //    if (color == ChessColor.White)
            //    {
            //        return _calculatedBoards[board];
            //    }
            //    else
            //    {
            //        return -_calculatedBoards[board];
            //    }

            //}

            int total = 0;

            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    if (board[x, y] != ChessPiece.Empty)
                    {
                        int val = GetPieceValue(board[x, y]);

                        if (color == ChessColor.White)
                        {
                            if (board[x, y] < ChessPiece.Empty)
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

            //if (!_calculatedBoards.ContainsKey(board))
            //{
            //    if (color == ChessColor.White)
            //    {
            //        _calculatedBoards.Add(board, total);
            //    }
            //    else
            //    {
            //        _calculatedBoards.Add(board, -total);
            //    }

            //}

            return total;
        }

        private List<ChessMove> GenerateMoves(ChessBoard board, ChessColor color)
        {
            List<ChessMove> moves = new List<ChessMove>();
            double[,] boardState = new double[8, 8];

            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    if (board[x, y] != ChessPiece.Empty && PieceIsSameColor(board[x, y], color))
                    {
                        ChessPiece piece = board[x, y];

                        HashSet<ChessLocation> moveSet = GetMoveSet(piece);

                        ChessLocation currentLoc = new ChessLocation(x, y);

                        foreach (ChessLocation loc in moveSet)
                        {
                            if (_slowPieces.Contains(piece))
                            {
                                ChessMove posMove = GetPieceMove(board, currentLoc, loc, color, 1);
                                if (posMove != null)
                                {
                                    moves.Add(posMove);
                                }
                            }
                            else
                            {
                                for (int i = 1; i < 8; i++)
                                {
                                    ChessMove posMove = GetPieceMove(board, currentLoc, loc, color, i);
                                    if (posMove != null)
                                    {
                                        moves.Add(posMove);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (moves.Count == 0)
            {
                this.Log("No moves generated >_<");
            }

            return moves;
        }

        private ChessMove GetPieceMove(ChessBoard board, ChessLocation fromLoc, ChessLocation toLocDelta, ChessColor color, int deltaMod)
        {
            ChessPiece piece = board[fromLoc];
            int sum_x = fromLoc.X + toLocDelta.X * deltaMod;
            int sum_y = piece == ChessPiece.WhitePawn ? fromLoc.Y - toLocDelta.Y : fromLoc.Y + toLocDelta.Y * deltaMod;

            ChessLocation toLoc = new ChessLocation(sum_x, sum_y);

            if (toLoc.X < 0 || toLoc.X > 7 || toLoc.Y < 0 || toLoc.Y > 7)
                return null;

            ChessMove move = new ChessMove(fromLoc, toLoc);
            bool valid = IsValidMove(board, move, color);

            if (valid)
            {
                ChessBoard tempBoard = board.Clone();
                tempBoard[move.To] = tempBoard[move.From];
                tempBoard[move.From] = ChessPiece.Empty;

                if ((piece == ChessPiece.WhitePawn && move.To.Y == 0) || (piece == ChessPiece.BlackPawn && move.To.Y == 7))
                {
                    tempBoard[move.To] = color == ChessColor.White ? ChessPiece.WhiteQueen : ChessPiece.BlackQueen;
                }

                int threatened = PieceThreatened(tempBoard, toLoc, color).Count;

                if ((piece == ChessPiece.WhiteKing || piece == ChessPiece.BlackKing) && threatened > 0)
                {
                    valid = false;
                }

                if (PieceThreatened(tempBoard, findKing(tempBoard, color), color).Count > 0)
                {
                    valid = false;
                }

                if (_ourColor == color && ((_previousCheck != null && _previousCheck.To == move.From) || (_prePreviousCheck != null && _prePreviousCheck.To == move.From)))
                {
                    valid = false;
                }
            }



            if (valid)
            {
                //val += CalculateBoardState(tempBoard, color);

                //if (PieceThreatened(board, _ourKingLoc, color) > 0)
                //{
                //    if (PieceThreatened(tempBoard, _ourKingLoc, color) == 0)
                //    {
                //        val += 1000000;
                //        this.Log("Piece can get king out of check, val = " + val);
                //    }
                //}

                //if (board[toLoc] != ChessPiece.Empty)
                //{
                //    val += GetPieceValue(board[toLoc]);
                //}

                //if (threatened > 0)
                //{
                //    val -= GetPieceValue(piece) * 2;
                //}

                return move;
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

        private bool PieceIsSameColor(ChessPiece piece, ChessColor color)
        {
            if (color == ChessColor.White)
            {
                if (piece == ChessPiece.WhitePawn || piece == ChessPiece.WhiteKnight || piece == ChessPiece.WhiteBishop || piece == ChessPiece.WhiteRook || piece == ChessPiece.WhiteQueen || piece == ChessPiece.WhiteKing)
                {
                    return true;
                }
            }
            else
            {
                if (piece == ChessPiece.BlackPawn || piece == ChessPiece.BlackKnight || piece == ChessPiece.BlackBishop || piece == ChessPiece.BlackRook || piece == ChessPiece.BlackQueen || piece == ChessPiece.BlackKing)
                {
                    return true;
                }
            }

            return false;
        }

        // Check if piece in loc is in danger
        private List<ChessMove> PieceThreatened(ChessBoard board, ChessLocation currentLoc, ChessColor color)
        {
            ChessColor enemyColor = color == ChessColor.White ? ChessColor.Black : ChessColor.White;
            List<ChessMove> moves = new List<ChessMove>();

            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    ChessPiece piece = board[x, y];

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

            return moves;
        }

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

            return moves.Count > 0;
        }

        private bool CanPieceThreateningKingBeTaken(ChessBoard board, ChessLocation currentLoc, ChessColor color)
        {
            ChessColor enemyColor = color == ChessColor.White ? ChessColor.Black : ChessColor.White;
            this.Log("Can checking piece be taken");

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
                            if (PieceThreatened(board, pieceLoc, enemyColor).Count > 0)
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        private bool CanKingBeBlocked(ChessBoard board, ChessLocation currentLoc, ChessColor color)
        {
            ChessColor enemyColor = color == ChessColor.White ? ChessColor.Black : ChessColor.White;
            this.Log("Can king be blocked");

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
                            this.Log("Piece checking king found");
                            if (piece != ChessPiece.WhiteKnight && piece != ChessPiece.BlackKnight)
                            {
                                int d_x = pieceLoc.X - currentLoc.X;
                                int d_y = pieceLoc.Y - currentLoc.Y;

                                this.Log("d_x = " + d_x + ", d_y = " + d_y);

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

                                int dist = Math.Abs(d_x) > Math.Abs(d_y) ? Math.Abs(d_x) : Math.Abs(d_y);

                                this.Log("dist = " + dist);

                                for (int i = 1; i < dist; i++)
                                {
                                    int xLoc = currentLoc.X + xDir * i;
                                    int yLoc = currentLoc.Y + yDir * i;

                                    this.Log("(xLoc, yLoc) = (" + xLoc + ", " + yLoc + ")");

                                    ChessLocation blockLoc = new ChessLocation(xLoc, yLoc);
                                    for (int y2 = 0; y2 < 8; y2++)
                                    {
                                        for (int x2 = 0; x2 < 8; x2++)
                                        {
                                            ChessPiece blockPiece = board[x2, y2];

                                            if (blockPiece != ChessPiece.Empty)
                                            {
                                                //prints name of piece
                                                this.Log(Enum.GetName(typeof(ChessPiece), blockPiece));

                                                ChessLocation blockPieceLoc = new ChessLocation(x2, y2);
                                                ChessMove blockMove = new ChessMove(blockPieceLoc, blockLoc);
                                                if (IsValidMove(board, blockMove, color))
                                                {
                                                    this.Log("valid piece to block: " + Enum.GetName(typeof(ChessPiece), piece) + " at (" + x2 + ", " + y2 + ")");
                                                    ChessBoard tempBoard = board.Clone();
                                                    tempBoard[blockMove.To] = tempBoard[blockMove.From];
                                                    tempBoard[blockMove.From] = ChessPiece.Empty;

                                                    if (PieceThreatened(tempBoard, currentLoc, color).Count == 0)
                                                    {
                                                        this.Log("move is valid and can block the check");
                                                        return true;
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

            return false;
        }

        private ChessLocation findKing(ChessBoard board, ChessColor color)
        {
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    if ((color == ChessColor.White && board[x, y] == ChessPiece.WhiteKing) || (color == ChessColor.Black && board[x, y] == ChessPiece.BlackKing))
                    {
                        return new ChessLocation(x, y);
                    }
                }
            }

            return null;
        }

        private bool Checkmate(ChessBoard board, ChessLocation kingLoc, ChessColor color)
        {
            int numPiecesThreateningEnemyKing = PieceThreatened(board, kingLoc, color).Count;

            if (numPiecesThreateningEnemyKing > 0)
            {
                if (!CanKingMove(board, kingLoc, color))
                {
                    if (numPiecesThreateningEnemyKing > 1)
                    {
                        return true;
                    }
                    else if (!CanPieceThreateningKingBeTaken(board, kingLoc, color))
                    {
                        if (!CanKingBeBlocked(board, kingLoc, color))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private int GetNumPawns(ChessBoard board, ChessColor color)
        {
            ChessPiece colorPawn = color == ChessColor.White ? ChessPiece.WhitePawn : ChessPiece.BlackPawn;

            int numPawns = 0;

            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    if (board[x, y] == colorPawn)
                    {
                        numPawns++;
                    }
                }
            }

            return numPawns;
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

            if (color == ChessColor.White)
            {
                if ((d_x == 0 && (d_y == -1 || (d_y == -2 && y1 == 6 && board[x2, y2 + 1] == ChessPiece.Empty)) && board[x2, y2] == ChessPiece.Empty) || ((d_x == 1 || d_x == -1) && d_y == -1 && board[x2, y2] != ChessPiece.Empty)) //y1 == 6 = starting pawn position for white. Probably could change to a global.
                {
                    moveable = true;
                }
            }
            else
            {
                if ((d_x == 0 && (d_y == 1 || (d_y == 2 && y1 == 1 && board[x2, y2 - 1] == ChessPiece.Empty)) && board[x2, y2] == ChessPiece.Empty) || ((d_x == 1 || d_x == -1) && d_y == 1 && board[x2, y2] != ChessPiece.Empty)) //y1 == 1 = starting pawn position for black. Probably could change to global.
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

        bool CheckKnight(ChessBoard board, ChessMove move, ChessColor color)
        {
            bool moveable = false;

            int x1 = move.From.X;
            int y1 = move.From.Y;
            int x2 = move.To.X;
            int y2 = move.To.Y;
            int d_x = x2 - x1;
            int d_y = y2 - y1;

            if (((Math.Abs(d_x) == 1 && Math.Abs(d_y) == 2) || (Math.Abs(d_x) == 2 && Math.Abs(d_y) == 1)))
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
                if (d_y > 0)
                {
                    if (d_x > 0)
                    {
                        for (int i = 1; i < d_x; ++i)
                        {
                            if (board[x1 + i, y1 + i] != ChessPiece.Empty)
                                moveable = false;
                        }
                    }
                    else
                    {
                        for (int i = -1; i > d_x; --i)
                        {
                            if (board[x1 + i, y1 - i] != ChessPiece.Empty)
                                moveable = false;
                        }
                    }
                }
                else
                {
                    if (d_x > 0)
                    {
                        for (int i = 1; i < d_x; ++i)
                        {
                            if (board[x1 + i, y1 - i] != ChessPiece.Empty)
                                moveable = false;
                        }
                    }
                    else
                    {
                        for (int i = -1; i > d_x; --i)
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

                if (PieceThreatened(tempBoard, move.To, color).Count == 0)
                {
                    moveable = true;
                }
            }

            if (!DiffColorAtDest(board, move, color))
            {
                moveable = false;
            }

            List<ChessLocation> overlapingKingMoves = areKingsTooClose(board, move.From, color);
            if (overlapingKingMoves.Contains(move.To))
            {
                moveable = false;
            }

            return moveable;
        }

        private List<ChessLocation> areKingsTooClose(ChessBoard board, ChessLocation currentLoc, ChessColor color)
        {
            List<ChessLocation> ourKingMoves = new List<ChessLocation>();
            List<ChessLocation> enemyKingMoves = new List<ChessLocation>();
            List<ChessLocation> badMoves = new List<ChessLocation>();

            ChessLocation enemyKingLoc = (color == ChessColor.White ? findKing(board, ChessColor.Black) : findKing(board, ChessColor.White));

            if (enemyKingLoc != null)
            {
                foreach (ChessLocation kingmove in _kingMoves)
                {
                    ourKingMoves.Add(new ChessLocation(currentLoc.X + kingmove.X, currentLoc.Y + kingmove.Y));
                    enemyKingMoves.Add(new ChessLocation(enemyKingLoc.X + kingmove.X, enemyKingLoc.Y + kingmove.Y));
                }

                foreach (ChessLocation kingmove in ourKingMoves)
                {
                    if (enemyKingMoves.Contains(kingmove))
                    {
                        badMoves.Add(kingmove);
                    }
                }
            }

            return badMoves;
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
