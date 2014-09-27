using System;
using System.Collections.Generic;
using System.Text;
using UvsChess;

namespace StudentAI
{
    public class StudentAI : IChessAI
    {
        #region IChessAI Members that are implemented by the Student

        enum PieceValues
        {
            King = 10,
            Queen = 9,
            Rook = 5,
            Bishop = 3,
            Knight = 3,
            Pawn = 1
        }

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

        Dictionary<ChessLocation, int> gravity = new Dictionary<ChessLocation, int>();

        ChessBoard _currentBoard;

        /// <summary>
        /// Evaluates the chess board and decided which move to make. This is the main method of the AI.
        /// The framework will call this method when it's your turn.
        /// </summary>
        /// <param name="board">Current chess board</param>
        /// <param name="yourColor">Your color</param>
        /// <returns> Returns the best chess move the player has for the given chess board</returns>
        public ChessMove GetNextMove(ChessBoard board, ChessColor myColor)
        {
            _currentBoard = board;
            _ourColor = myColor;
            gravity = new Dictionary<ChessLocation, int>();
            //return new ChessMove(new ChessLocation(2,1), new ChessLocation(2,2));
        }

        // Could piece be taken on opponents next turn
        private bool isThreatened(AIChessPieces piece)
        {
            
        }

        // Checks to see if there 
        private bool isCovered(AIChessPieces piece)
        {

        }

        // Pieces that the passed piece can capture in its current location
        private void CurrentlyThreatening(AIChessPieces piece)
        {

        }

        //// Pieces that the passed piece can capture in the next given move
        //private void PossibleCaptures(AIChessPieces piece, ChessLocation loc)
        //{

        //}

        // Takes in the list of moves, and returns the move to take
        // Also calculates board gravity to determine best move
        private void Gravity(AIChessPieces piece)
        {

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
                            if (_currentBoard[x1 - i, y1 + i] != ChessPiece.Empty)
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
                    for (int i = 1; i < d_y; --i)
                    {
                        if (_currentBoard[x1 + i, y1] != ChessPiece.Empty)
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
                        if (_currentBoard[x1, y1 + i] != ChessPiece.Empty)
                        {
                            moveable = false;
                        }
                    }
                }
                else
                {
                    for (int i = -1; i > d_x; --i)
                    {
                        if (_currentBoard[x1, y1 + i] != ChessPiece.Empty)
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
