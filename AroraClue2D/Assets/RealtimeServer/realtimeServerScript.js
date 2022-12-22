﻿// Based on source: https://docs.aws.amazon.com/gamelift/latest/developerguide/realtime-script.html

//ARORA 0.0.7

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
let sessionTimeoutTimer = null;
const SESSION_TIMEOUT = 1 * 60 & 1000; // mins to wait * secs/min * milliseconds/second ....  1 minute

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
let logicalPlayerIDs = {};




let serverConnected = false;

let timeForStart = false;
let timeForGuessEvent = false;
let timeForResume = false;

let weapon = null;
let suspect = null;
let location = null;
let guessedOnTime = null;

let hintWeapon = null;
let hintSuspect = null;
let hintLocation = null;


let host = null;
let winner = null;
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

// Called when a new game session is started on the process
function onStartGameSession(gameSession) {
    logger.info("onStartGameSession: ");
    logger.info(gameSession);
    // Complete any game session set-up

    // Set up an example tick loop to perform server initiated actions
    //startTime = getTimeInS();
    //tickLoop();
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
        accepted: false,
        active: false
    };
    logger.info(playerConnected);

    players.push(playerConnected);

    // Perform any validation needed for connectMsg.payload, connectMsg.peerId
    return true;
}

// Called when a Player is accepted into the game
function onPlayerAccepted(player) {
    logger.info("onPlayerAccepted");
    logger.info(player);

    players.forEach((playerInfo) => {
        logger.info("onPlayerAccepted players checking peerId");

        if (playerInfo.peerId == player.peerId) {
            logger.info("onPlayerAccepted players mark active");

            // not sure if we need to do this...
            playerInfo.accepted = true;
            playerInfo.active = true;
        }
    });

    // This player was accepted -- let's send them a message
    const msg = session.newTextGameMessage(OP_CODE_PLAYER_ACCEPTED, player.peerId, "Peer " + player.peerId + " accepted");
    session.sendReliableMessage(msg, player.peerId);

    //make the first player the host
    if (host == null) {
        host = player.peerId;
    }

    activePlayers++;

    logger.info("onPlayerAccepted checking active player count");

    // This would have to adjusted to handle games where players can come and go within a single match.
    // NOTE: ActivePlayers stores active connections. If you need to test from only one computer, like you
    // can only play one side of the match at a time, then you'll have to make this condition check if playerInfo.length > 1 instead.
    //TODO: i changed this to >= from > for testing...
    if (activePlayers > 1) {

        serverConnected = true;

        logger.info("onPlayerAccepted activePlayers > 1");

        // getPlayers returns "a list of peer IDs for players that are currently connected to the game session"
        // So, let's match these players to the ones stored in players
        session.getPlayers().forEach((playerSession, playerId) => {

            logger.info("onPlayerAccepted players loop");
            logger.info(playerSession);
            logger.info(playerId);

            players.forEach((playerInfo) => {
                logger.info("onPlayerAccepted players playerInfo loop");
                logger.info("playerInfo.peerId: " + playerInfo.peerId + ", playerSession.peerId: " + playerSession.peerId + ", playerInfo.active: " + playerInfo.active);

                // find the other active player
                if (playerInfo.peerId != playerSession.peerId) {
                    var gameStartPayload = {
                        remotePlayerId: playerInfo.playerId
                    };

                    logger.info("Sending start match message...");
                    logger.info(gameStartPayload);

                    // send out the match has started along with the opponent's playerId
                    
                    
                    //TODO: start the prep phase if we have enough players
                    const startMatchMessage = session.newTextGameMessage(OP_PREP_GAME, session.getServerId(), JSON.stringify(gameStartPayload));
                    session.sendReliableMessage(startMatchMessage, playerSession.peerId);
                }
            });
        });

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

    //connect
        case 1:

        //todo if this is called then update it with initial connect stuff
        //OTHERWISE REMOVE THIS BLOCK

        let movementData = {
                    playerXPosition: 15,
                    playerYPosition: 15,
                    playerZPosition: 15,
                    playerId: payload.playerId
                };
                
                const movementMSG = session.newTextGameMessage(
                    OP_PLAYER_MOVEMENT_S, session.getServerId(), JSON.stringify(movementData));

                    for (let index = 0; index < allPlayersLength; ++index) {

                    logger.info("Sending movement data to player " + players[index].peerId);

                    session.sendReliableMessage(movementMSG, player[index].peerId);

                }


                //TODO: remove
                //this is for testing... just one message and then ending
                logger.info("ENDING BECAUSE YOU PUT A TEST END HERE. Completed process ending with: " + outcome);
                
                gameoverCleanup();
                
                break;
       
        case OP_PLAYER_MOVEMENT:
            {
                //when the server receives movement data from the player 
               
                
                //1. the server make a PlayerMovementData object to send back to players
                // this object will contain location and id data from the player who sent it
                

                let movementData = {
                    playerXPosition: payload.playerXPosition,
                    playerYPosition: payload.playerYPosition,
                    playerZPosition: payload.playerZPosition,
                    playerId: payload.playerId
                };

                logger.info("movement data")
                logger.info(movementData);


                //2. Then it make a message 

                let allPlayersLength = players.length;

                const movementMSG = session.newTextGameMessage(
                    OP_PLAYER_MOVEMENT_S, session.getServerId(), JSON.stringify(movementData));

                //3. Then it should send that message to each player

                //this sends a string, and we are sending an object... so use the other method for now?
                //SendStringToClient(players, OP_PLAYER_MOVEMENT_S, movementMSG);

                for (let index = 0; index < allPlayersLength; ++index) {

                    logger.info("Sending movement data to player " + players[index].peerId);

                    session.sendReliableMessage(movementMSG, player[index].peerId);

                }


                //TODO: remove
                //this is for testing... just one message and then ending
                logger.info("ENDING BECAUSE YOU PUT A TEST END HERE. Completed process ending with: " + outcome);
                
                gameoverCleanup();

                break;
            }

       
            //leave this here for now as an example PLAYCARDOP removed so i hardcoded a number for example (unused)
             case 800:
            {
                logger.info("PLAY_CARD_OP hit");

                const cardDrawn = randomIntFromInterval(1, 10);
                var cardDrawnSuccess = addCardDraw(cardDrawn, payload.playerId);

                if (cardDrawnSuccess) {

                    let allPlayersLength = playersInfo.length;
                    let cardDrawData = {
                        card: cardDrawn,
                        playedBy: payload.playerId,
                        plays: cardPlays[payload.playerId].length
                    };
                    logger.info(cardDrawData);

                    const cardDrawMsg = session.newTextGameMessage(DRAW_CARD_ACK_OP, session.getServerId(), JSON.stringify(cardDrawData));

                    for (let index = 0; index < allPlayersLength; ++index) {
                        logger.info("Sending draw card message to player " + playersInfo[index].peerId);
                        session.sendReliableMessage(cardDrawMsg, playersInfo[index].peerId);
                    }

                    //if something needs to happen after this data is sent...
                    checkGameOver();

                } else {
                    // ignore action as the player has already played max allowed cards
                    logger.info("Player " + payload.playerId + " attempted extra card!");
                }

                break;
            }


    }
}

function checkGameOver() {

    var gameCompletedPlayers = 0;

    for (const [key, value] of Object.entries(cardPlays)) {
        // has player made two plays
        if (value.length == 2) {
            gameCompletedPlayers++;
        }
    }

    logger.info(gameCompletedPlayers);

    // If at least two players completed two turns, signal game over.
    // This partially handles the case where a player joins but leaves the game after one play or something,
    // and another joins and plays two turns. Update for your game requirements.
    if (gameCompletedPlayers >= 2) {
        logger.info("setting game over...");
        determineWinner();
        gameover = true;
    }
}

// assumes both players played two cards
function determineWinner() {

    var result = {
        playerOneId: "",
        playerTwoId: "",
        playerOneScore: "",
        playerTwoScore: "",
        winnerId: ""
    }

    var playersExamined = 0;
    for (const [key, value] of Object.entries(cardPlays)) {
        // make sure we're only looking at players with two plays
        if (value.length == 2) {
            if (playersExamined == 0) {
                result.playerOneId = key;
                result.playerOneScore = value[0] + value[1];
            } else if (playersExamined == 1) {
                result.playerTwoId = key;
                result.playerTwoScore = value[0] + value[1];
            }
            playersExamined++;
        }
    }

    if (result.playerOneScore > result.playerTwoScore) {
        result.winnerId = result.playerOneId;
    } else if (result.playerOneScore < result.playerTwoScore) {
        result.winnerId = result.playerTwoId;
    } else if (result.playerOneScore == result.playerTwoScore) {
        result.winnerId = "tie";
    }

    logger.info(result);

    // send out game over messages with winner
    const gameoverMsg = session.newTextGameMessage(GAMEOVER_OP, session.getServerId(), JSON.stringify(result));

    for (let index = 0; index < players.length; ++index) {
        logger.info("Sending game over message to player " + players[index].peerId);
        session.sendReliableMessage(gameoverMsg, players[index].peerId);
    }
}

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
    onStartGameSession: onStartGameSession,
    onProcessTerminate: onProcessTerminate,
    onHealthCheck: onHealthCheck
};