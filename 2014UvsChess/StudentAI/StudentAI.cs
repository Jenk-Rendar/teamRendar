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

        enum AIChessPieces
        {
            Pawn0,
            Pawn1,
            Pawn2,
            Pawn3,
            Pawn4,
            Pawn5,
            Pawn6,
            Pawn7,
            Knight0,
            Knight1,
            Bishop0,
            Bishop1,
            Rook0,
            Rook1,
            Queen0,
            Queen1,
            Queen2,
            Queen3,
            Queen4,
            Queen5,
            Queen6,
            Queen7,
            King
        }

        ChessColor _ourColor;

        Dictionary<AIChessPieces, ChessLocation> _blackPieces = new Dictionary<AIChessPieces, ChessLocation>();
        Dictionary<AIChessPieces, ChessLocation> _whitePieces = new Dictionary<AIChessPieces, ChessLocation>();

        ChessBoard _currentBoard;

        public StudentAI()
        {
            _blackPieces[AIChessPieces.Rook0] = new ChessLocation(0, 0);
            _blackPieces[AIChessPieces.Knight0] = new ChessLocation(1, 0);
            _blackPieces[AIChessPieces.Bishop0] = new ChessLocation(2, 0);
            _blackPieces[AIChessPieces.Queen0] = new ChessLocation(3, 0);
            _blackPieces[AIChessPieces.King] = new ChessLocation(4, 0);
            _blackPieces[AIChessPieces.Bishop1] = new ChessLocation(5, 0);
            _blackPieces[AIChessPieces.Knight1] = new ChessLocation(6, 0);
            _blackPieces[AIChessPieces.Rook1] = new ChessLocation(7, 0);
            _blackPieces[AIChessPieces.Pawn0] = new ChessLocation(0, 1);
            _blackPieces[AIChessPieces.Pawn1] = new ChessLocation(1, 1);
            _blackPieces[AIChessPieces.Pawn2] = new ChessLocation(2, 1);
            _blackPieces[AIChessPieces.Pawn3] = new ChessLocation(3, 1);
            _blackPieces[AIChessPieces.Pawn4] = new ChessLocation(4, 1);
            _blackPieces[AIChessPieces.Pawn5] = new ChessLocation(5, 1);
            _blackPieces[AIChessPieces.Pawn6] = new ChessLocation(6, 1);
            _blackPieces[AIChessPieces.Pawn7] = new ChessLocation(7, 1);

            _whitePieces[AIChessPieces.Rook0] = new ChessLocation(0, 7);
            _whitePieces[AIChessPieces.Knight0] = new ChessLocation(1, 7);
            _whitePieces[AIChessPieces.Bishop0] = new ChessLocation(2, 7);
            _whitePieces[AIChessPieces.Queen0] = new ChessLocation(3, 7);
            _whitePieces[AIChessPieces.King] = new ChessLocation(4, 7);
            _whitePieces[AIChessPieces.Bishop1] = new ChessLocation(5, 7);
            _whitePieces[AIChessPieces.Knight1] = new ChessLocation(6, 7);
            _whitePieces[AIChessPieces.Rook1] = new ChessLocation(7, 7);
            _whitePieces[AIChessPieces.Pawn0] = new ChessLocation(0, 6);
            _whitePieces[AIChessPieces.Pawn1] = new ChessLocation(1, 6);
            _whitePieces[AIChessPieces.Pawn2] = new ChessLocation(2, 6);
            _whitePieces[AIChessPieces.Pawn3] = new ChessLocation(3, 6);
            _whitePieces[AIChessPieces.Pawn4] = new ChessLocation(4, 6);
            _whitePieces[AIChessPieces.Pawn5] = new ChessLocation(5, 6);
            _whitePieces[AIChessPieces.Pawn6] = new ChessLocation(6, 6);
            _whitePieces[AIChessPieces.Pawn7] = new ChessLocation(7, 6);
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
            _currentBoard = board;
            if (_ourColor == null)
            {
                _ourColor = myColor;
            }
            return new ChessMove(new ChessLocation(2,1), new ChessLocation(2,2));
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

            AIChessPieces pieceToMove = ChessPieceEnumConverter(moveToCheck, colorOfPlayerMoving);

            valid = CheckWhichPieceMoved(pieceToMove, colorOfPlayerMoving, moveToCheck.To.X, moveToCheck.To.Y);

            if (colorOfPlayerMoving != _ourColor)
            {
                if (colorOfPlayerMoving == ChessColor.White)
                {
                    _whitePieces[pieceToMove] = moveToCheck.To;
                }
                else
                {
                    _blackPieces[pieceToMove] = moveToCheck.To;
                }
            }
            
            return valid;
        }

        AIChessPieces ChessPieceEnumConverter(ChessMove move, ChessColor color)
        {
            Dictionary<AIChessPieces, ChessLocation> dict = color == ChessColor.White ? _whitePieces : _blackPieces;

            foreach(AIChessPieces piece in Enum.GetValues(typeof(AIChessPieces)))
            {
                if(dict.ContainsKey(piece))
                {
                    if(dict[piece] == move.From)
                    {
                        return piece;
                    }
                }
            }

            return 0;
        }

        bool CheckWhichPieceMoved(AIChessPieces piece, ChessColor color, int x2, int y2)
        {
            bool validMove = false;

            string type = Enum.GetName(typeof(AIChessPieces), piece);

            if (type.Contains("Pawn"))
            {
                validMove = CheckPawn(piece, color, x2, y2);
            }
            else if (type.Contains("Knight"))
            {
                validMove = CheckKnight(piece, color, x2, y2);
            }
            else if (type.Contains("Bishop"))
            {
                validMove = CheckBishop(piece, color, x2, y2);
            }
            else if (type.Contains("Rook"))
            {
                validMove = CheckRook(piece, color, x2, y2);
            }
            else if (type.Contains("Queen"))
            {
                validMove = CheckQueen(piece, color, x2, y2);
            }
            else if (type.Contains("King"))
            {
                validMove = CheckKing(piece, color, x2, y2);
            }
            return validMove;
        }

        bool CheckQueen(AIChessPieces piece, ChessColor color, int x2, int y2)
        {
            Dictionary<AIChessPieces, ChessLocation> dict = color == ChessColor.White ? _whitePieces : _blackPieces;

            int x1 = dict[piece].X;
            int y1 = dict[piece].Y;
            int d_x = x2 - x1;
            int d_y = y2 - y1;

            bool movement = true;
            if (Math.Abs(d_x) == Math.Abs(d_y))
                movement = CheckBishop(piece, color, x2, y2);
            else if (d_x == 0 || d_y == 0)
                movement = CheckRook(piece, color, x2, y2);
            return movement;
        }

        bool CheckPawn(AIChessPieces piece, ChessColor color, int x2, int y2)
        {
            Dictionary<AIChessPieces, ChessLocation> dict = color == ChessColor.White ? _whitePieces : _blackPieces;

            int x1 = dict[piece].X;
            int y1 = dict[piece].Y;
            int d_x = x2 - x1;
            int d_y = y2 - y1;

            if (!(CheckColorAtDest(color, x2, y2)))
                return false;

            if(color == ChessColor.White)
            {
                if((d_x == 0 && (d_y == -1 || (d_y == -2 && y1 == 6)) && _currentBoard[x2,y2] == ChessPiece.Empty) || ((d_x ==1 || d_x == -1) && d_y == -1 && _currentBoard[x2,y2] != ChessPiece.Empty)) //y1 == 6 = starting pawn position for white. Probably could change to a global.
                {
                    return true;
                }
                return false;
            }
            else
            {
                if((d_x == 0 && (d_y == 1 || (d_y == 2 && y1 == 1)) && _currentBoard[x2,y2] == ChessPiece.Empty) || ((d_x == 1 || d_x == -1) && d_y == -1 && _currentBoard[x2,y2] != ChessPiece.Empty)) //y1 == 1 = starting pawn position for black. Probably could change to global.
                {
                    return true;
                }
                return false;
            }
        }

        bool CheckKnight(AIChessPieces piece, ChessColor color, int x2, int y2)
        {
            Dictionary<AIChessPieces, ChessLocation> dict = color == ChessColor.White ? _whitePieces : _blackPieces;

            int x1 = dict[piece].X;
            int y1 = dict[piece].Y;
            int d_x = x2 - x1;
            int d_y = y2 - y1;
            
            if(!(CheckColorAtDest(color, x2, y2)))
                return false;

            if ( ((Math.Abs(d_x) == 1 && Math.Abs(d_y) == 2) || (Math.Abs(d_x) == 2 && Math.Abs(d_y) == 1)))
                return true;
            return false;
        }

        bool CheckKing(AIChessPieces piece, ChessColor color, int x2, int y2)
        {
            Dictionary<AIChessPieces, ChessLocation> dict = color == ChessColor.White ? _whitePieces : _blackPieces;

            int x1 = dict[piece].X;
            int y1 = dict[piece].Y;
            int d_x = x2 - x1;
            int d_y = y2 - y1;

            if (!(CheckColorAtDest(color, x2, y2)))
                return false;
            if (Math.Abs(d_x) <= 1 && Math.Abs(d_y) <= 1)
                return true;
            return false;
        }

        bool CheckRook(AIChessPieces piece, ChessColor color, int x2, int y2)
        {
            Dictionary<AIChessPieces, ChessLocation> dict = color == ChessColor.White ? _whitePieces : _blackPieces;

            int x1 = dict[piece].X;
            int y1 = dict[piece].Y;
            int d_x = x2 - x1;
            int d_y = y2 - y1;

            bool moveable = true;
            if (d_x == 0)
            {
                if(d_y > 0)
                {
                    for (int i = 1; i < d_y; --i)
                    {
                        if (_currentBoard[x1 + i,y1] != ChessPiece.Empty)
                            moveable = false;
                    }
                }
                else
                {
                    for(int i = -1; i > d_y; --i)
                    {
                        if (_currentBoard[x1, y1+i] != ChessPiece.Empty)
                            moveable = false;
                    }
                }
            }
            else if (d_y == 0)
            {
                if(d_x > 0)
                {
                    for(int i = 1; i < d_x; ++i)
                    {
                        if(_currentBoard[x1,y1+i] != ChessPiece.Empty)
                        {
                            moveable = false;
                        }
                    }
                }
                else
                {
                    for(int i = -1; i > d_x; --i)
                    {
                        if (_currentBoard[x1, y1 + i] != ChessPiece.Empty)
                            moveable = false;
                    }
                }
            }
            else moveable = false;
            return moveable;
        }

        bool CheckBishop(AIChessPieces piece, ChessColor color, int x2, int y2)
        {
            Dictionary<AIChessPieces, ChessLocation> dict = color == ChessColor.White ? _whitePieces : _blackPieces;

            int x1 = dict[piece].X;
            int y1 = dict[piece].Y;
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

        //Checks to see if the color at the dest is the same color as the piece moving there.
        bool CheckColorAtDest(ChessColor color, int x2, int y2)
        {
            if (color == ChessColor.White)
            {
                if (_currentBoard[x2, y2] > ChessPiece.Empty)
                    return false;
            }
            else
            {
                if (_currentBoard[x2, y2] < ChessPiece.Empty)
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
