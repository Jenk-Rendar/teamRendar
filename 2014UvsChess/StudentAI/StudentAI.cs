using System;
using System.Collections.Generic;
using System.Text;
using UvsChess;

namespace StudentAI
{
    public class StudentAI : IChessAI
    {
        #region IChessAI Members that are implemented by the Student

        enum Pieces
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

        /// <summary>
        /// Evaluates the chess board and decided which move to make. This is the main method of the AI.
        /// The framework will call this method when it's your turn.
        /// </summary>
        /// <param name="board">Current chess board</param>
        /// <param name="yourColor">Your color</param>
        /// <returns> Returns the best chess move the player has for the given chess board</returns>
        public ChessMove GetNextMove(ChessBoard board, ChessColor myColor)
        {

            return new ChessMove(new ChessLocation(2,6), new ChessLocation(2,4));
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
            ChessPiece cp = boardBeforeMove[moveToCheck.From];
            valid = CheckWhichPieceMoved(boardBeforeMove,cp, moveToCheck, colorOfPlayerMoving);
            return valid;
        }

        bool CheckWhichPieceMoved(ChessBoard boardBeforeMove, ChessPiece cp, ChessMove moveToCheck, ChessColor color)
        {
            bool validMove = false;
            if(cp == ChessPiece.BlackPawn || cp == ChessPiece.WhitePawn)
            {
                validMove = CheckPawn(boardBeforeMove, cp, moveToCheck, color);
            }
            else if(cp == ChessPiece.BlackKnight || cp == ChessPiece.WhiteKnight)
            {
                validMove = CheckKnight(boardBeforeMove, cp, moveToCheck, color);
            }
            else if(cp == ChessPiece.BlackBishop || cp == ChessPiece.WhiteBishop)
            {

            }
            else if(cp == ChessPiece.BlackRook || cp == ChessPiece.WhiteRook)
            {

            }
            else if(cp == ChessPiece.BlackQueen || cp == ChessPiece.WhiteQueen)
            {

            }
            else if(cp == ChessPiece.BlackKing || cp == ChessPiece.WhiteKing)
            {

            }
            return validMove;
        }


        bool CheckPawn(ChessBoard board, ChessPiece cp, ChessMove moveToCheck, ChessColor color)
        {
            int x1 = moveToCheck.From.X;
            int x2 = moveToCheck.To.X;
            int y1 = moveToCheck.From.Y;
            int y2 = moveToCheck.To.Y;
            int d_x = x2 - x1;
            int d_y = y2 - y1;

            if (!(CheckColorAtDest(board, color, x2, y2)))
                return false;

            if(color == ChessColor.White)
            {
                if((d_x == 0 && (d_y == -1 || (d_y == -2 && y1 == 6)) && board[x2,y2] == ChessPiece.Empty) || ((d_x ==1 || d_x == -1) && d_y == -1 && board[x2,y2] != ChessPiece.Empty)) //y1 == 6 = starting pawn position for white. Probably could change to a global.
                {
                    return true;
                }
                return false;
            }
            else
            {
                if((d_x == 0 && (d_y == 1 || (d_y == 2 && y1 == 1)) && board[x2,y2] == ChessPiece.Empty) || ((d_x == 1 || d_x == -1) && d_y == -1 && board[x2,y2] != ChessPiece.Empty)) //y1 == 1 = starting pawn position for black. Probably could change to global.
                {
                    return true;
                }
                return false;
            }
        }

        bool CheckKnight(ChessBoard board, ChessPiece cp, ChessMove moveToCheck, ChessColor color)
        {
            int x1 = moveToCheck.From.X;
            int x2 = moveToCheck.To.X;
            int y1 = moveToCheck.From.Y;
            int y2 = moveToCheck.To.Y;
            int d_x = x2 - x1;
            int d_y = y2 - y1;
            
            if(!(CheckColorAtDest(board, color, x2, y2)))
                return false;

            if ( ((Math.Abs(d_x) == 1 && Math.Abs(d_y) == 2) || (Math.Abs(d_x) == 2 && Math.Abs(d_y) == 1)))
                return true;
            return false;
        }

        //Checks to see if the color at the dest is the same color as the piece moving there.
        bool CheckColorAtDest(ChessBoard board, ChessColor color, int x2, int y2)
        {
            if (color == ChessColor.White)
            {
                if (board[x2, y2] > ChessPiece.Empty)
                    return false;
            }
            else
            {
                if (board[x2, y2] < ChessPiece.Empty)
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
