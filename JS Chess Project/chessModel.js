////////// Global Variables //////////
var BOARD_HEIGHT = 8;
var BOARD_WIDTH = 8;

////////// Constant Piece Objects //////////
var KING = {
	name: "king",
	value: 100
};

var QUEEN = {
	name: "queen",
	value: 9
};

var ROOK = {
	name: "rook",
	value: 5
};

var BISHOP = {
	name: "bishop",
	value: 3
};

var KNIGHT = {
	name: "knight",
	value: 3
};

var PAWN = {
	name: "pawn",
	value: 1
};

////////// User Object //////////


////////// Player Objects //////////
var White = {
	color: "White",
	score: 0
};

var Black = {
	color: "Black",
	score: 0
}

////////// Objects //////////
function Space(piece){
	this.piece = piece;
}

function Piece(color,type){
	this.color = color;
	this.type = type;
	this.moved = false;
}

function Board(){
	this.boardArray = (function(){
		var array = [];
		for(var y = 0; y < BOARD_HEIGHT; y++){
			array.push([]);
			for(var x = 0; x < BOARD_WIDTH; x++){
				if(y == 0){
					if(x == 0 || x == 7){
						array[y].push(new Space(new Piece(Black.color,ROOK)));
					}
					else if(x == 1 || x == 6){
						array[y].push(new Space(new Piece(Black.color,KNIGHT)));
					}
					else if(x == 2 || x == 5){
						array[y].push(new Space(new Piece(Black.color,BISHOP)));
					}
					else if(x == 3){
						array[y].push(new Space(new Piece(Black.color,QUEEN)));
					}
					else if(x == 4){
						array[y].push(new Space(new Piece(Black.color,KING)));
					}
				}
				else if(y == 1){
					array[y].push(new Space(new Piece(Black.color,PAWN)));
				}
				else if(y == 6){
					array[y].push(new Space(new Piece(White.color,PAWN)));
				}
				else if(y == 7){
					if(x == 0 || x == 7){
						array[y].push(new Space(new Piece(White.color,ROOK)));
					}
					else if(x == 1 || x == 6){
						array[y].push(new Space(new Piece(White.color,KNIGHT)));
					}
					else if(x == 2 || x == 5){
						array[y].push(new Space(new Piece(White.color,BISHOP)));
					}
					else if(x == 3){
						array[y].push(new Space(new Piece(White.color,QUEEN)));
					}
					else if(x == 4){
						array[y].push(new Space(new Piece(White.color,KING)));
					}
				}
				else {
					array[y].push(new Space(null));
				}
			}
		}
		return array;
	})();
}

////////// Instantiation of Objects //////////
var board = new Board();