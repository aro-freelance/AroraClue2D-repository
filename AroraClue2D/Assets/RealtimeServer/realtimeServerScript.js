// Based on source: https://docs.aws.amazon.com/gamelift/latest/developerguide/realtime-script.html

//ARORA 0.0.904

//this handles player interactions on the server.  

// Example Realtime Server Script
'use strict';


// Example override configuration
const configuration = {
    pingIntervalTime: 30000
};


// Timing mechanism used to trigger end of game session. Defines how long, in milliseconds, between each tick in the example tick loop
const tickTime = 1000;

// Defines how to long to wait in Seconds before beginning early termination check in the example tick loop
const minimumElapsedTime = 30; //120;

var session; // The Realtime server session object


var logger; // Log at appropriate level via .info(), .warn(), .error(), .debug()


var startTime; // Records the time the process started
var activePlayers = 0; // Records the number of connected players
var onProcessStartedCalled = false; // Record if onProcessStarted has been called

//messages server sends
const OP_CODE_PLAYER_ACCEPTED = 113;
const OP_CODE_DISCONNECT_NOTIFICATION = 114;

const OP_REQUEST_FIND_MATCH_S = 1;
const OP_FIRE_MATCH_S = 2;
const OP_PREP_GAME_S = 3;
const OP_RESUME_GAME_S = 4;
const OP_GAMEOVER_S = 5;
const OP_START_GAME_S = 6;
const OP_START_GUESS_EVENT_S = 7;
const OP_START_COUNTDOWN_S = 8;
const OP_END_GUESS_EVENT_S = 9;
const OP_SET_PLAYER_INFO_S = 10;
const OP_CHECK_ANSWERS_S = 11;
const OP_PLAYER_MOVEMENT_S = 12;
const OP_HOLD_AFTER_GUESS_CHECKED_S = 13;


//messages player sends

const OP_REQUEST_FIND_MATCH = 501;
const OP_FIRE_MATCH = 502;
const OP_PREP_GAME = 503;
const OP_RESUME_GAME = 504;
const OP_GAMEOVER = 505;
const OP_START_GAME = 506;
const OP_START_GUESS_EVENT = 507;
const OP_START_COUNTDOWN = 508;
const OP_END_GUESS_EVENT = 509;
const OP_SET_PLAYER_INFO = 510;
const OP_CHECK_ANSWERS = 511;
const OP_PLAYER_MOVEMENT = 512;


let players = [];
let playerDataList = [];

//the game will start after there are this many active players connected
let numberOfPlayersDesiredToFireMatch = 2;


let serverConnected = false;

let weapon = null;
let suspect = null;
let location = null;

let winnerName = "Player";
let gameover = false;


// note that the strings will be Base64 encoded, so they can't contain colon, comma or double quote
// This function takes a list of peers and then send the opcode and string to the peer
/*
function SendStringToClient(peerIds, opCode, stringToSend) {
    session.getLogger().info("[app] SendStringToClient: peerIds = " + peerIds.toString() + " opCode = " + opCode + " stringToSend = " + stringToSend);

    let gameMessage = session.newTextGameMessage(opCode, session.getServerId(), stringToSend);
    let peerArrayLen = peerIds.length;

    for (let index = 0; index < peerArrayLen; ++index) {
        session.getLogger().info("[app] SendStringToClient: sendMessageT " + gameMessage.toString() + " " + peerIds[index].toString());
        session.sendMessage(gameMessage, peerIds[index]);
    };
} */




// Called when game server is initialized, passed server's object of current session
function init(rtSession) {
    session = rtSession;
    logger = session.getLogger();
    logger.info("init");

    console.log("hello world. this is the realtimeserverscript");

}

// On Process Started is called when the process has begun and we need to perform any
// bootstrapping.  This is where the developer should insert any code to prepare
// the process to be able to host a game session, for example load some settings or set state
//
// Return true if the process has been appropriately prepared and it is okay to invoke the
// GameLift ProcessReady() call.
function onProcessStarted(args) {
    onProcessStartedCalled = true;
    logger.info("Starting process with args: " + args);
    logger.info("Ready to host games...");

    return true;
}



// Handle process termination if the process is being terminated by GameLift
// You do not need to call ProcessEnding here
function onProcessTerminate() {
    // Perform any clean up
}

// Return true if the process is healthy
function onHealthCheck() {
    return true;
}

// On Player Connect is called when a player has passed initial validation
// Return true if player should connect, false to reject
// This is hit before onPlayerAccepted
//@Yelsa Step 4A
function onPlayerConnect(connectMsg) {
    logger.info("onPlayerConnect: ");
    logger.info(connectMsg);

    let payloadRaw = new Buffer.from(connectMsg.payload);
    let payload = JSON.parse(payloadRaw);
    logger.info("onPlayerConnect payload: ");
    logger.info(payload);

    let playerConnected = {
        peerId: connectMsg.player.peerId,
        playerId: payload.playerId,
        playerSessionId: connectMsg.player.playerSessionId,
        accepted: true,
        active: true
    };
    logger.info(playerConnected);

    //do this in onPlayerAccepted where we already have a player object ?
    players.push(playerConnected);

    let numberOfPlayers = players.length.toString;

    const msg = session.newTextGameMessage(OP_CODE_PLAYER_ACCEPTED, connectMsg.player.peerId, numberOfPlayers);
    session.sendReliableMessage(msg, connectMsg.player.peerId);


    // Perform any validation needed for connectMsg.payload, connectMsg.peerId
    return true;
}

// Called when a Player is accepted into the game
//@Yelsa Step 5b
function onPlayerAccepted(player) {
    logger.info("onPlayerAccepted");
    logger.info(player);

    //this is done in onplayeraccepted ?
    //players.push(player)

    players.forEach((playerInfo) => {
        logger.info("onPlayerAccepted players checking peerId");

        if (playerInfo.peerId == player.peerId) {
            logger.info("onPlayerAccepted players mark active");

            // not sure if we need to do this...
            playerInfo.accepted = true;
            playerInfo.active = true;
        }
    });

    // This OP code is used in onPlayerConnected
    //const msg = session.newTextGameMessage(OP_CODE_PLAYER_ACCEPTED, player.peerId, "Peer " + player.peerId + " accepted");
    //session.sendReliableMessage(msg, player.peerId);
    
    activePlayers++;

    //@Yelsa TODO: get the actual name and sprite name data in this by sending it to the server when the player client is made
    //make a PlayerData object for each player and add it to the list playerDataList
    var playerData = {

        PlayerName: "playerName",
        PlayerSpriteName: "playerA",
        PlayerNumber: activePlayers.length,
        PlayerId: player.peerId,
        ReadyToStart: false,
        ReadyToResume: false,
        AnswerChecked: false,
        IsWinner: false

    };

    playerDataList.push(playerData);

    logger.info("onPlayerAccepted checking active player count");

    //@Yelsa if we are having problems we may need to make the host prep message async?
    //if there are enough active players and the server has received a message with random data from host to prep the game
    if (activePlayers == numberOfPlayersDesiredToFireMatch) {
        if (selectedWeapon != null && selectedSuspect != null && selectedPlace != null) {

            serverConnected = true;

            logger.info("onPlayerAccepted activePlayers > 1");

            // getPlayers returns "a list of peer IDs for players that are currently connected to the game session"
            // So, let's match these players to the ones stored in players
            session.getPlayers().forEach((playerSession, playerId) => {

                logger.info("onPlayerAccepted players loop 5b");
                logger.info(playerSession);
                logger.info(playerId);

                players.forEach((playerInfo) => {
                    logger.info("onPlayerAccepted players playerInfo loop");
                    logger.info("playerInfo.peerId: " + playerInfo.peerId + ", playerSession.peerId: " + playerSession.peerId + ", playerInfo.active: " + playerInfo.active);

                    //make a GamePrepData object to send the player
                    var gameStartPayload = {
                        playerDataList: playerDataList,
                        selectedWeapon: selectedWeapon,
                        selectedSuspect: selectedSuspect,
                        selectedPlace: selectedPlace
                    };

                    logger.info("Sending prep match message...");
                    logger.info(gameStartPayload);


                    //@Yelsa Step 5b message sent here
                    //start the prep phase if we have enough players
                    const startMatchMessage = session.newTextGameMessage(OP_FIRE_MATCH_S, session.getServerId(), JSON.stringify(gameStartPayload));
                    session.getPlayers().forEach((playerId) => {
                        session.sendReliableMessage(startMatchMessage, playerId);
                    });

                });
            });
        }
    }

}

// On Player Disconnect is called when a player has left or been forcibly terminated
// Is only called for players that actually connected to the server and not those rejected by validation
// This is called before the player is removed from the player list
function onPlayerDisconnect(peerId) {
    logger.info("onPlayerDisconnect: " + peerId);

    // send a message to each remaining player letting them know about the disconnect
    const outMessage = session.newTextGameMessage(OP_CODE_DISCONNECT_NOTIFICATION, session.getServerId(), "Peer " + peerId + " disconnected");
    session.getPlayers().forEach((player, playerId) => {
        if (playerId != peerId) {
            session.sendReliableMessage(outMessage, peerId);
        }
    });
    activePlayers--;

    players.forEach((playerInfo) => {
        if (playerInfo.peerId == peerId) {
            playerInfo.active = false;
        }
    });
}


// here  the server is receiving a message from the game and handling it
function onMessage(gameMessage) {
    logger.info("onMessage");
    logger.info(gameMessage);

    // pass data through the payload field
    var payloadRaw = new Buffer.from(gameMessage.payload);
    var payload = JSON.parse(payloadRaw);
    logger.info("payload")
    logger.info(payload);
    logger.info(payload.playerId);

    switch (gameMessage.opCode) {


        //@Yelsa Step 5A
        case OP_PREP_GAME:

            //server receives random data from host

            weapon = payload.selectedWeapon;

            suspect = payload.selectedSuspect;

            location = payload.selectedPlace;


            break;

        //@Yelsa Step 7
        case OP_START_GAME:

            var allReady = true;

            //look for the player who sent the message in the playerDataList by id,
            //then set them ready to start
            // then check if anyone in the list is not ready to start, if so end,  if all are ready send start game msg
            playerDataList.forEach((player) => {
                if (player.playerId == payload.playerId) {
                    player.ReadyToStart = true;
                }
                if (player.ReadyToStart == false) {
                    allReady = false;
                }
            });

            //if all players are ready send a message telling them to start the game
            if (allReady) {

                const readyMessage = session.newTextGameMessage(OP_START_GAME_S, session.getServerId(), "All Ready. Start Game.");
                session.getPlayers().forEach((playerId) => {
                    session.sendReliableMessage(readyMessage, playerId);
                });

            }

            break;

        //@Yelsa Step 10
        case OP_START_GUESS_EVENT:

            //send all players the call to start the guess event
            const startGuessEventMessage = session.newTextGameMessage(OP_START_GUESS_EVENT_S, session.getServerId(), "Start Guess Event");
            session.getPlayers().forEach((playerId) => {
                session.sendReliableMessage(startGuessEventMessage, playerId);
            });

            break;


        //@Yelsa Step 13
        case OP_CHECK_ANSWERS:

            var weaponIsCorrect = false;
            var suspectIsCorrect = false;
            var placeIsCorrect = false;

            var allAnswersChecked = true;

            //server receives a guess (weapon, suspect, place) from player, and checks it against the correct answer
            if (payload.submittedAnswer) {
                if (payload.weaponGuess == selectedWeapon) {
                    weaponIsCorrect = true;
                }
                if (payload.suspectGuess == selectedSuspect) {
                    suspectIsCorrect = true;
                }
                if (payload.locationGuess == selectedPlace) {
                    placeIsCorrect = true;
                }


                //TODO: also store some of this data to be shared among the players at the end of the guess event

            }

            //TODO: else statement to get information the player should know to share with the rest of the players

            //if all aspects of the guess are correct, mark the player as the winner
            if (weaponIsCorrect && suspectIsCorrect && placeIsCorrect) {

                playerDataList.forEach((player) => {
                    if (player.PlayerId == payload.playerId) {
                        player.IsWinner = true;
                        player.AnswerChecked = true;
                        winnerName = player.PlayerName;
                        gameover = true;
                    }
                });
            }
            //otherwise just mark the player as having answered
            else {
                playerDataList.forEach((player) => {
                    if (player.PlayerId == payload.playerId) {
                        player.AnswerChecked = true;
                    }
                });
            }

            //if there is a winner, send a message to all players to end the game. Include the correct answer and the winner name.
            //@Yelsa Step 14B
            if (gameover) {

                let endGameData = {
                    winnerName: winnerName,
                    weapon: selectedWeapon,
                    suspect: selectedSuspect,
                    place: selectedPlace
                };

                const gameOverMessage = session.newTextGameMessage(OP_GAMEOVER_S, session.getServerId(), JSON.stringify(endGameData));
                session.getPlayers().forEach((playerId) => {
                    session.sendReliableMessage(gameOverMessage, playerId);
                });
            }
            //if there is no winner, send a message to player to enter waiting phase
            else {
                //if any players have not answered set allAnswersChecked to false
                playerDataList.forEach((player) => {
                    if (player.AnswerChecked == false) {
                        allAnswersChecked = false;
                    }
                });

                //if all have answered send message to all to end guess event
                //@Yelsa Step 14A
                if (allAnswersChecked) {

                    //TODO: each time a player checks an answer store the data
                    //and then make an object here to send some of that data back to the players to use in the end guess cutscene
                    //to share data among the players

                    const guessEventEndMessage = session.newTextGameMessage(OP_END_GUESS_EVENT_S, session.getServerId(), "End Guess Event.");
                    session.getPlayers().forEach((playerId) => {
                        session.sendReliableMessage(guessEventEndMessage, playerId);
                    });

                }
                //otherwise send a message to the player who just had their answers checked to let them know they are waiting for other players
                //@Yelsa Step 14C
                else {

                    const holdAfterAnswerSubmitMessage =
                        session.newTextGameMessage(OP_HOLD_AFTER_GUESS_CHECKED_S, session.getServerId(), "Hold for other players to end Guess Event.");
                    session.getPlayers().forEach((playerId) => {
                        if (playerId == payload.playerId) {
                            session.sendReliableMessage(holdAfterAnswerSubmitMessage, playerId);
                        }
                    });

                }

            }


            break;

        //@Yelsa Step 16
        case OP_RESUME_GAME:

            //server received a message from player that they are ready to continue after the guess event end cutscene

            var allReadyToResume = true;

            //set the player who sent the message to ready to resume
            playerDataList.forEach((player) => {
                if (player.playerId == payload.playerId) {
                    player.ReadyToResume = true;
                }
            });

            //check if all players are ready to resume
            playerDataList.forEach((player) => {
                if (player.ReadyToResume == false) {
                    allReadyToResume = false;
                }
            });


            //if all are ready to resume, send a message to all players to resume
            if (allReadyToResume) {

                const resumeMessage = session.newTextGameMessage(OP_RESUME_GAME_S, session.getServerId(), "Resume Game.");
                session.getPlayers().forEach((playerId) => {
                    session.sendReliableMessage(resumeMessage, playerId);
                });

            }
            

            break;

        //@Yelsa Step 19
        case OP_PLAYER_MOVEMENT:
            {
                //when the server receives movement data from the player


                //1. the server make a PlayerMovementData object to send back to players
                // this object will contain location and id data from the player who sent it

                var playerNumber = null;

                playerDataList.forEach((player) => {

                    if (player.PlayerId == payload.playerId) {
                        playerNumber = player.PlayerNumber;
                    }

                });


                let movementData = {
                    playerXPosition: payload.playerXPosition,
                    playerYPosition: payload.playerYPosition,
                    playerZPosition: payload.playerZPosition,
                    playerNumber: playerNumber
                };

                logger.info("movement data")
                logger.info(movementData);


                //2. Then it make a message 

                const movementMSG = session.newTextGameMessage(
                    OP_PLAYER_MOVEMENT_S, session.getServerId(), JSON.stringify(movementData));

                //3. Then it should send that message to each player

                session.getPlayers().forEach((playerId) => {
                    if (playerId != payload.playerId) {
                        session.sendReliableMessage(movementMSG, playerId);
                    }
                });

                //for (let index = 0; index < players.length; ++index) {

                //    logger.info("Sending movement data to player " + players[index].peerId);

                //    session.sendReliableMessage(movementMSG, player[index].peerId);

                //}


                break;
            }

    }
}

//function checkGameOver() {

//    var gameCompletedPlayers = 0;

//    /*
//    for (const [key, value] of Object.entries(cardPlays)) {
//        // has player made two plays
//        if (value.length == 2) {
//            gameCompletedPlayers++;
//        }
//    }*/

//    logger.info(gameCompletedPlayers);

//    // If at least two players completed two turns, signal game over.
//    // This partially handles the case where a player joins but leaves the game after one play or something,
//    // and another joins and plays two turns. Update for your game requirements.
//    if (gameCompletedPlayers >= 2) {
//        logger.info("setting game over...");
//        determineWinner();
//        gameover = true;
//    }
//}

//// assumes both players played two cards
//function determineWinner() {

//    var result = {
//        playerOneId: "",
//        playerTwoId: "",
//        playerOneScore: "",
//        playerTwoScore: "",
//        winnerId: ""
//    }

//    var playersExamined = 0;
//    /*
//    for (const [key, value] of Object.entries(cardPlays)) {
//        // make sure we're only looking at players with two plays
//        if (value.length == 2) {
//            if (playersExamined == 0) {
//                result.playerOneId = key;
//                result.playerOneScore = value[0] + value[1];
//            } else if (playersExamined == 1) {
//                result.playerTwoId = key;
//                result.playerTwoScore = value[0] + value[1];
//            }
//            playersExamined++;
//        }
//    }

//    if (result.playerOneScore > result.playerTwoScore) {
//        result.winnerId = result.playerOneId;
//    } else if (result.playerOneScore < result.playerTwoScore) {
//        result.winnerId = result.playerTwoId;
//    } else if (result.playerOneScore == result.playerTwoScore) {
//        result.winnerId = "tie";
//    }
//    */

//    logger.info(result);

//    // send out game over messages with winner
//    const gameoverMsg = session.newTextGameMessage(OP_GAMEOVER_S, session.getServerId(), JSON.stringify(result));

//    for (let index = 0; index < players.length; ++index) {
//        logger.info("Sending game over message to player " + players[index].peerId);
//        session.sendReliableMessage(gameoverMsg, players[index].peerId);
//    }
//}

// The cardPlays object looks like this:
// {"eb051e15-1337-4071-b8a9-b9b0da32d7e2":[1,5],"27f87c33-c6f8-45f2-b403-801eaf4f4a2d":[5,6]}
// Where each player's uuid acts as the key for an array of their card play numbers
/*
function addCardDraw(cardNumber, playerId) {
    logger.info("addCardDraw " + cardNumber + " to player " + playerId);

    if (cardPlays[playerId]) {
        if (cardPlays[playerId].length < 2) {
            cardPlays[playerId].push(cardNumber);
        } else {
            logger.info("Player " + playerId + " has played maximum amount of cards.");
            return false;
        }
    } else {
        cardPlays[playerId] = [];
        cardPlays[playerId].push(cardNumber);
    }
    logger.info(cardPlays);
    return true;
}*/



// Called when a new game session is started on the process
function onStartGameSession(gameSession) {
    logger.info("onStartGameSession: ");
    logger.info(gameSession);
    // Complete any game session set-up

    // Set up an example tick loop to perform server initiated actions
    startTime = getTimeInS();
    tickLoop();
}



// A simple tick loop example

//TODO: could use something like this to run timer on server?
async function tickLoop() {
    // const elapsedTime = getTimeInS() - startTime;
    // logger.info("Tick... " + elapsedTime + " activePlayers: " + activePlayers);

    if (!gameover) {

        // If we had 2 players that are no longer active, end game.
        // You can add a minimum elapsed time check here if you'd like
        if (players.length == 2 && activePlayers == 0) { // && (elapsedTime > minimumElapsedTime)) {
            logger.info("All players disconnected. Ending game");

            gameoverCleanup();
        }
        else if(serverConnected && activePlayers == 0) {
            logger.info("All players disconnected. Ending game");

            gameoverCleanup();
        }
        else {
            setTimeout(tickLoop, tickTime);
        }

        

    } else {
        logger.info("game over");
        gameoverCleanup();
    }
}

async function gameoverCleanup() {
    // Call processEnding() to terminate the process and quit
    const outcome = await session.processEnding();
    logger.info("Completed process ending with: " + outcome);
    process.exit(0);
}

function randomIntFromInterval(min, max) { // min and max included 
    return Math.floor(Math.random() * (max - min + 1) + min)
}

// Return true if the send should be allowed
function onSendToPlayer(gameMessage) {
    logger.info("onSendToPlayer: ");
    logger.info(gameMessage);

    // This example rejects any payloads containing "Reject"
    return (!gameMessage.getPayloadAsText().includes("Reject"));
}

// Return true if the send to group should be allowed
// Use gameMessage.getPayloadAsText() to get the message contents
function onSendToGroup(gameMessage) {
    logger.info("onSendToGroup: " + gameMessage);
    return true;
}

// Return true if the player is allowed to join the group
function onPlayerJoinGroup(groupId, peerId) {
    logger.info("onPlayerJoinGroup: " + groupId + ", " + peerId);
    return true;
}

// Return true if the player is allowed to leave the group
function onPlayerLeaveGroup(groupId, peerId) {
    logger.info("onPlayerLeaveGroup: " + groupId + ", " + peerId);
    return true;
}

// Calculates the current time in seconds
function getTimeInS() {
    return Math.round(new Date().getTime() / 1000);
}




exports.ssExports = {
    configuration: configuration,
    init: init,
    onProcessStarted: onProcessStarted,
    onMessage: onMessage,
    onPlayerConnect: onPlayerConnect,
    onPlayerAccepted: onPlayerAccepted,
    onPlayerDisconnect: onPlayerDisconnect,
    onSendToPlayer: onSendToPlayer,
    onSendToGroup: onSendToGroup,
    onPlayerJoinGroup: onPlayerJoinGroup,
    onPlayerLeaveGroup: onPlayerLeaveGroup,
    onProcessTerminate: onProcessTerminate,
    onHealthCheck: onHealthCheck,
    onStartGameSession: onStartGameSession
};