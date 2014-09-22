var TABLE_HEIGHT = BOARD_HEIGHT + 2;
var TABLE_WIDTH = BOARD_WIDTH + 2;
var currentSpace = null;
var previousSpace = null;
var currentPlayer = White;

function drawBoard() {
	var _user = localStorage.getItem('cs2550timestamp');
	var _userOutput = document.getElementById("logged_in_user");
	_userOutput.innerHTML = _user;
	var _board = document.getElementById("gameboard");
	_board.innerHTML = formatBoard();
	updateBoard();
	clickableBoard();
	// startMove();
}

function formatBoard() {
	var html = "<table id=\"boardTable\" border=\"0\" class=\"no-border\">";
	var num = BOARD_HEIGHT;
	for(var y = 0; y < TABLE_HEIGHT; y++) {
		if(y == 0 || y == 9) {
			html += "<tr class=\"center-align\"><td></td>";
			var character = 'A';
			for(var x = 1; x < TABLE_WIDTH - 1; x++) {
				html += "<td>" + character + "</td>";
				character = String.fromCharCode(character.charCodeAt(0) + 1);
			}
			html += "<td></td></tr>";
		}
		else {
			html += "<tr><td>" + num + "</td>";
			for(var x = 1; x < TABLE_WIDTH - 1; x++) {
				var _spaceClassType = x%2 == y%2 ? "light-space" : "dark-space";
				html += "<td class=\"" + _spaceClassType + "\"></td>";
			}
			html += "<td>" + num + "</td></tr>";
			num--;
		}
	}
	html += "</table>";
	return html;
}

function updateBoard() {
	var gridTable = document.getElementById("boardTable");
	for(var y = 0; y < BOARD_HEIGHT; y++) {
		for(var x = 0; x < BOARD_WIDTH; x++) {
			// var _spaceClassType = x%2 == y%2 ? "light-space" : "dark-space";
	var whiteScore = document.getElementById("white_score");
	var blackScore = document.getElementById("black_score");
	var curPlay = document.getElementById("cur_player");
	curPlay.innerHTML = currentPlayer.color;
	whiteScore.innerHTML = White.score;
	blackScore.innerHTML = Black.score;
			// var html = "<div class=\"" + _spaceClassType + "\">";
			var html = "<div class=\"space\">";
			if(board.boardArray[y][x].piece != null) {
				var _pieceColor = board.boardArray[y][x].piece.color;
				var _pieceType = board.boardArray[y][x].piece.type.name;
				html += "<img src=\"chess_images/" + _pieceColor + "_" + _pieceType + ".png\" alt=\"" + _pieceColor + " " + _pieceType + "\"/>";
			}
			gridTable.rows[y + 1].cells[x + 1].innerHTML = html + "</div>";
		}
	}
}

function clickableBoard() {
	var table = document.getElementById("boardTable");
	for(var y = 0; y < BOARD_HEIGHT; y++) {
		for(var x = 0; x < BOARD_WIDTH; x++) {
			table.rows[y + 1].cells[x + 1].onclick = function(){
				clickEvent(this);
			};
		}
	}
}

function clickEvent(space) {
	if(previousSpace != null) {
		previousSpace.children[0].style.backgroundColor = "transparent";
	}
	var x = space.cellIndex - 1;
	var y = space.parentNode.rowIndex - 1;
	if(previousSpace != space) {
		if(board.boardArray[y][x].piece != null) {
			if(board.boardArray[y][x].piece.color == currentPlayer.color) {
				previousSpace = space;
				previousSpace.children[0].style.backgroundColor = "rgba(72,118,255,0.5)";
			}
			else {
				if(previousSpace != null) {
					attemptMove(space);
				}
				else {
					clearSelection();
				}
			}
		}
		else {
			if(previousSpace != null) {
				attemptMove(space);
			}
			else {
				clearSelection();
			}
		}
	}
	else {
		clearSelection();
	}

	// console.log("x: " + x + ", y: " + y);
	document.getElementById("x_coord").innerHTML = x + 1;
	document.getElementById("y_coord").innerHTML = y + 1;
}

function attemptMove(space) {
	var x1 = previousSpace.cellIndex - 1;
	var y1 = previousSpace.parentNode.rowIndex - 1;
	var pieceType = board.boardArray[y1][x1].piece.type;

	if(pieceType == KING) {
		checkKing(space);
	}
	else if(pieceType == QUEEN) {
		checkQueen(space);
	}
	else if(pieceType == ROOK) {
		checkRook(space);
	}
	else if(pieceType == BISHOP) {
		checkBishop(space);
	}
	else if(pieceType == KNIGHT) {
		checkKnight(space)
	}
	else if(pieceType == PAWN) {
		checkPawn(space);
	}
}

function checkKing(space) {
	var x1 = previousSpace.cellIndex - 1;
	var y1 = previousSpace.parentNode.rowIndex - 1;
	var x2 = space.cellIndex - 1;
	var y2 = space.parentNode.rowIndex - 1;
	var d_x = x2 - x1;
	var d_y = y2 - y1;

	if(Math.abs(d_x) <= 1 && Math.abs(d_y) <= 1) {
		movePiece(space);
	}
	else {
		clearSelection();
	}
}

function checkQueen(space) {
	var x1 = previousSpace.cellIndex - 1;
	var y1 = previousSpace.parentNode.rowIndex - 1;
	var x2 = space.cellIndex - 1;
	var y2 = space.parentNode.rowIndex - 1;
	var d_x = x2 - x1;
	var d_y = y2 - y1;

	if(Math.abs(d_x) == Math.abs(d_y)) {
		checkBishop(space);
	}
	else if(d_x == 0 || d_y == 0) {
		checkRook(space);
	}
	else {
		clearSelection();
	}
}

function checkRook(space) {
	var x1 = previousSpace.cellIndex - 1;
	var y1 = previousSpace.parentNode.rowIndex - 1;
	var x2 = space.cellIndex - 1;
	var y2 = space.parentNode.rowIndex - 1;
	var d_x = x2 - x1;
	var d_y = y2 - y1;

	var movable = true;
	if(d_x == 0) {
		if(d_y > 0) {
			for(var i = 1; i < d_y; i++) {
				if(board.boardArray[y1 + i][x1].piece != null) {
					movable = false;
				}
			}
		}
		else {
			for(var i = -1; i > d_y; i--) {
				if(board.boardArray[y1 + i][x1].piece != null) {
					movable = false;
				}
			}
		}
	}
	else if(d_y == 0) {
		if(d_x > 0) {
			for(var i = 1; i < d_x; i++) {
				if(board.boardArray[y1][x1 + i].piece != null) {
					movable = false;
				}
			}
		}
		else {
			for(var i = -1; i > d_x; i--) {
				if(board.boardArray[y1][x1 + i].piece != null) {
					movable = false;
				}
			}
		}
	}
	else {
		movable = false;
	}

	if(movable == true) {
		movePiece(space);
	}
	else {
		clearSelection();
	}
}

function checkBishop(space) {
	var x1 = previousSpace.cellIndex - 1;
	var y1 = previousSpace.parentNode.rowIndex - 1;
	var x2 = space.cellIndex - 1;
	var y2 = space.parentNode.rowIndex - 1;
	var d_x = x2 - x1;
	var d_y = y2 - y1;

	var movable = true;
	if(Math.abs(d_x) == Math.abs(d_y)) {
		if(d_y > 0) {
			if(d_x > 0) {
				for(var i = 1; i < d_x; i++) {
					if(board.boardArray[y1 + i][x1 + i].piece != null) {
						movable = false;
					}
				}
			}
			else {
				for(var i = -1; i > d_x; i--) {
					if(board.boardArray[y1 - i][x1 + i].piece != null) {
						movable = false;
					}
				}
			}
		}
		else {
			if(d_x > 0) {
				for(var i = 1; i < d_x; i++) {
					if(board.boardArray[y1 - i][x1 + i].piece != null) {
						movable = false;
					}
				}
			}
			else {
				for(var i = -1; i > d_x; i--) {
					if(board.boardArray[y1 + i][x1 + i].piece != null) {
						movable = false;
					}
				}
			}
		}
	}
	else {
		movable = false;
	}

	if(movable == true) {
		movePiece(space);
	}
	else {
		clearSelection();
	}
}

function checkKnight(space) {
	var x1 = previousSpace.cellIndex - 1;
	var y1 = previousSpace.parentNode.rowIndex - 1;
	var x2 = space.cellIndex - 1;
	var y2 = space.parentNode.rowIndex - 1;
	var d_x = x2 - x1;
	var d_y = y2 - y1;

	if((Math.abs(d_x) == 2 && Math.abs(d_y) == 1) || (Math.abs(d_x) == 1 && Math.abs(d_y) == 2)) {
		movePiece(space);
	}
	else {
		clearSelection();
	}
}

function checkPawn(space) {
	var x1 = previousSpace.cellIndex - 1;
	var y1 = previousSpace.parentNode.rowIndex - 1;
	var x2 = space.cellIndex - 1;
	var y2 = space.parentNode.rowIndex - 1;
	var d_x = x2 - x1;
	var d_y = y2 - y1;

	if(currentPlayer == White) {
		if((d_x == 0 && (d_y == -1 || (d_y == -2 && board.boardArray[y1][x1].piece.moved == false)) && board.boardArray[y2][x2].piece == null) || ((d_x == 1 || d_x == -1) && d_y == -1 && board.boardArray[y2][x2].piece != null)) {
			movePiece(space);
		}
		else {
			clearSelection();
		}
	}
	else {
		if((d_x == 0 && (d_y == 1 || (d_y == 2 && board.boardArray[y1][x1].piece.moved == false)) && board.boardArray[y2][x2].piece == null) || ((d_x == 1 || d_x == -1) && d_y == 1 && board.boardArray[y2][x2].piece != null)) {
			movePiece(space);
		}
		else {
			clearSelection();
		}
	}
}

function movePiece(space) {
	var x1 = previousSpace.cellIndex - 1;
	var y1 = previousSpace.parentNode.rowIndex - 1;
	var x2 = space.cellIndex - 1;
	var y2 = space.parentNode.rowIndex - 1;

	if(board.boardArray[y2][x2].piece != null) {
		if(board.boardArray[y2][x2].piece.type == KING) {
			clearSelection();
			return;
		}
		else {
			currentPlayer.score += board.boardArray[y2][x2].piece.type.value;
		}
	}

	board.boardArray[y2][x2].piece = board.boardArray[y1][x1].piece;
	board.boardArray[y2][x2].piece.moved = true;
	board.boardArray[y1][x1].piece = null;

	clearSelection();

	if(currentPlayer == White) {
		currentPlayer = Black;
	}
	else {
		currentPlayer = White;
	}
	updateBoard();
}

function clearSelection() {
	if(previousSpace != null) {
		previousSpace.children[0].style.backgroundColor = "transparent";
		previousSpace = null;
	}
}

function loadSavedGame() {
	var ajax = new XMLHttpRequest();
	ajax.open('GET', 'savedGame.json', false);
	ajax.send(null);
	var responseJson = JSON.parse(ajax.responseText);
	White.score = responseJson["White"].score;
	Black.score = responseJson["Black"].score;
	if(responseJson["currentPlayer"] == "White") {
		currentPlayer = White;
	}
	else {
		currentPlayer = Black;
	}
	for(var y = 0; y < BOARD_HEIGHT; y++) {
		for(var x = 0; x < BOARD_WIDTH; x++) {
			if(responseJson["board"].boardArray[y][x].piece != null) {
				var tempPiece;
				if(responseJson["board"].boardArray[y][x].piece.type.name == "pawn") {
					tempPiece = new Piece(responseJson["board"].boardArray[y][x].piece.color, PAWN);
					tempPiece.moved = responseJson["board"].boardArray[y][x].piece.moved;
					board.boardArray[y][x].piece = tempPiece;
				}
				else if(responseJson["board"].boardArray[y][x].piece.type.name == "bishop") {
					tempPiece = new Piece(responseJson["board"].boardArray[y][x].piece.color, BISHOP);
					tempPiece.moved = responseJson["board"].boardArray[y][x].piece.moved;
					board.boardArray[y][x].piece = tempPiece;
				}
				else if(responseJson["board"].boardArray[y][x].piece.type.name == "knight") {
					tempPiece = new Piece(responseJson["board"].boardArray[y][x].piece.color, KNIGHT);
					tempPiece.moved = responseJson["board"].boardArray[y][x].piece.moved;
					board.boardArray[y][x].piece = tempPiece;
				}
				else if(responseJson["board"].boardArray[y][x].piece.type.name == "rook") {
					tempPiece = new Piece(responseJson["board"].boardArray[y][x].piece.color, ROOK);
					tempPiece.moved = responseJson["board"].boardArray[y][x].piece.moved;
					board.boardArray[y][x].piece = tempPiece;
				}
				else if(responseJson["board"].boardArray[y][x].piece.type.name == "queen") {
					tempPiece = new Piece(responseJson["board"].boardArray[y][x].piece.color, QUEEN);
					tempPiece.moved = responseJson["board"].boardArray[y][x].piece.moved;
					board.boardArray[y][x].piece = tempPiece;
				}
				else if(responseJson["board"].boardArray[y][x].piece.type.name == "king") {
					tempPiece = new Piece(responseJson["board"].boardArray[y][x].piece.color, KING);
					tempPiece.moved = responseJson["board"].boardArray[y][x].piece.moved;
					board.boardArray[y][x].piece = tempPiece;
				}
			}
			else {
				board.boardArray[y][x].piece = null;
			}
		}
	}
	updateBoard();
	clickableBoard();
}

var moveDist = 2;
var msPerFrame = 1;
var horse, horseDivWidth, horseLeft;

function startMove() {
	horse = document.getElementById("horse");
	horseDivWidth = horse.offsetWidth;
	horseLeft = 0;
	setTimeout(moveHorseRight, msPerFrame);
}

function moveHorseRight() {
	horseLeft += moveDist;
	horse.style.left = horseLeft + "px";
	if(horseLeft <= horseDivWidth + 1) {
		setTimeout(moveHorseRight, msPerFrame);
	}
}

function resetGame() {
	board = new Board();
	currentPlayer = White;
	White.score = 0;
	Black.score = 0;
	drawBoard();
	document.getElementById("themeSelector").value = "wood";
}

function updatePlayerName(player,name) {
	document.getElementById(player + "_name").innerHTML = name;
}

function updateTheme(theme) {
	var light = document.getElementsByClassName("light-space");
	var dark = document.getElementsByClassName("dark-space");
	if(theme == "gray") {
		for(var i = 0; i < light.length; i++) {
			light[i].style.background = "rgb(200,200,200)";
			dark[i].style.background = "rgb(100,100,100)";
		}
	}
	else
	{
		for(var i = 0; i < light.length; i++) {
			light[i].style.background = "rgb(245,222,179)";
			dark[i].style.background = "rgb(205,133,63)";
		}
	}
}