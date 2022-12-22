// Based on source: https://docs.aws.amazon.com/gamelift/latest/developerguide/realtime-script.html

//ARORA 0.0.6

//this handles player interactions on the server.




// Example Realtime Server Script
'use strict';

var util = require('util');
var Packet_pb = require('../src/proto/Packet_pb');
var gameloop = require('./gameloop.js')


// Example override configuration
const configuration = {
};


// Timing mechanism used to trigger end of game session. Defines how long, in milliseconds, between each tick in the example tick loop
const tickTime = 1000;

// Defines how to long to wait in Seconds before beginning early termination check in the example tick loop
const minimumElapsedTime = 30; //120;

var logger; // Log at appropriate level via .info(), .warn(), .error(), .debug()

let players = [];
let logicalPlayerIDs = {};
let session = null; // The Realtime server session object
let sessionTimeoutTimer = null;
const SESSION_TIMEOUT = 1 * 60 * 1000;  // milliseconds to wait for players to join (1 minute)



//messages servers sends
const OP_CODE_PLAYER_ACCEPTED_S = 113;
const OP_CODE_DISCONNECT_NOTIFICATION_S = 114;

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






let playersInfo = [];

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
function SendStringToClient(peerIds, opCode, stringToSend) {
    session.getLogger().info("[app] SendStringToClient: peerIds = " + peerIds.toString() + " opCode = " + opCode + " stringToSend = " + stringToSend);

    let gameMessage = session.newTextGameMessage(opCode, session.getServerId(), stringToSend);
    let peerArrayLen = peerIds.length;

    for (let index = 0; index < peerArrayLen; ++index) {
        session.getLogger().info("[app] SendStringToClient: sendMessageT " + gameMessage.toString() + " " + peerIds[index].toString());
        session.sendMessage(gameMessage, peerIds[index]);
    };
}

//example usage of  the above function: 
//SendStringToClient(players, START_COUNTDOWN_OP_CODE, hopTime.toString());   // signal clients to start the countdown





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

    //// Set up an example tick loop to perform server initiated actions
    //startTime = getTimeInS();
    //tickLoop();
}




//// On Player Connect is called when a player has passed initial validation
//// Return true if player should connect, false to reject
//// This is hit before onPlayerAccepted
//function onPlayerConnect(connectMsg) {
//    logger.info("onPlayerConnect: ");
//    logger.info(connectMsg);

//    let payloadRaw = new Buffer.from(connectMsg.payload);
//    let payload = JSON.parse(payloadRaw);
//    logger.info("onPlayerConnect payload: ");
//    logger.info(payload);

//    let playerConnected = {
//        peerId: connectMsg.player.peerId,
//        playerId: payload.playerId,
//        playerSessionId: connectMsg.player.playerSessionId,
//        accepted: false,
//        active: false
//    };
//    logger.info(playerConnected);

//    playersInfo.push(playerConnected);

//    // Perform any validation needed for connectMsg.payload, connectMsg.peerId
//    return true;
//}

//// Called when a Player is accepted into the game
//function onPlayerAccepted(player) {
//    logger.info("onPlayerAccepted");
//    logger.info(player);

//    playersInfo.forEach((playerInfo) => {
//        logger.info("onPlayerAccepted playersInfo checking peerId");

//        if (playerInfo.peerId == player.peerId) {
//            logger.info("onPlayerAccepted playersInfo mark active");

//            // not sure if we need to do this...
//            playerInfo.accepted = true;
//            playerInfo.active = true;
//        }
//    });

//    // This player was accepted -- let's send them a message
//    const msg = session.newTextGameMessage(OP_CODE_PLAYER_ACCEPTED, player.peerId, "Peer " + player.peerId + " accepted");
//    session.sendReliableMessage(msg, player.peerId);

//    //make the first player the host
//    if (host == null) {
//        host = player.peerId;
//    }

//    activePlayers++;

//    logger.info("onPlayerAccepted checking active player count");

//    // This would have to adjusted to handle games where players can come and go within a single match.
//    // NOTE: ActivePlayers stores active connections. If you need to test from only one computer, like you
//    // can only play one side of the match at a time, then you'll have to make this condition check if playerInfo.length > 1 instead.
//    //TODO: i changed this to >= from > for testing...
//    if (activePlayers > 1) {

//        serverConnected = true;

//        logger.info("onPlayerAccepted activePlayers > 1");

//        // getPlayers returns "a list of peer IDs for players that are currently connected to the game session"
//        // So, let's match these players to the ones stored in playersInfo
//        session.getPlayers().forEach((playerSession, playerId) => {

//            logger.info("onPlayerAccepted players loop");
//            logger.info(playerSession);
//            logger.info(playerId);

//            playersInfo.forEach((playerInfo) => {
//                logger.info("onPlayerAccepted players playerInfo loop");
//                logger.info("playerInfo.peerId: " + playerInfo.peerId + ", playerSession.peerId: " + playerSession.peerId + ", playerInfo.active: " + playerInfo.active);

//                // find the other active player
//                if (playerInfo.peerId != playerSession.peerId) {
//                    var gameStartPayload = {
//                        remotePlayerId: playerInfo.playerId
//                    };

//                    logger.info("Sending start match message...");
//                    logger.info(gameStartPayload);

//                    // send out the match has started along with the opponent's playerId
//                    const startMatchMessage = session.newTextGameMessage(GAME_START_OP, session.getServerId(), JSON.stringify(gameStartPayload));
//                    session.sendReliableMessage(startMatchMessage, playerSession.peerId);
//                }
//            });
//        });

//    }

//}

// On Player Disconnect is called when a player has left or been forcibly terminated
// Is only called for players that actually connected to the server and not those rejected by validation
// This is called before the player is removed from the player list
//function onPlayerDisconnect(peerId) {
//    logger.info("onPlayerDisconnect: " + peerId);

//    // send a message to each remaining player letting them know about the disconnect
//    const outMessage = session.newTextGameMessage(OP_CODE_DISCONNECT_NOTIFICATION, session.getServerId(), "Peer " + peerId + " disconnected");
//    session.getPlayers().forEach((player, playerId) => {
//        if (playerId != peerId) {
//            session.sendReliableMessage(outMessage, peerId);
//        }
//    });
//    activePlayers--;

//    playersInfo.forEach((playerInfo) => {
//        if (playerInfo.peerId == peerId) {
//            playerInfo.active = false;
//        }
//    });
//}


//MESSAGE FROM SERVER IS CALLED LIKE THIS

/*
 function onPlayerAccepted(player) {
    session.getLogger().info("[app] onPlayerAccepted: player.peerId = " + player.peerId);
    // store the ID. Note that the index the player is assigned will be sent
    // to the client and determines if they are "player 0" or "player 1" independent
    // of the peerId
    players.push(player.peerId);
    session.getLogger().info("[app] onPlayerAccepted: new contents of players array = " + players.toString());

    let logicalID = players.length - 1;
    session.getLogger().info("[app] onPlayerAccepted: logical ID = " + logicalID);

    logicalPlayerIDs[player.peerId] = logicalID;
    session.getLogger().info("[app] onPlayerAccepted: logicalPlayerIDs array = " + logicalPlayerIDs.toString());

    SendStringToClient([player.peerId], LOGICAL_PLAYER_OP_CODE, logicalID.toString());
}
 
 */ 


// Here the server is receiving a message from the game and then handling it
function onMessage(gameMessage) {
    logger.info("onMessage");
    logger.info(gameMessage);

    // pass data through the payload field
    var payloadRaw = new Buffer.from(gameMessage.payload);
    var payload = JSON.parse(payloadRaw);
    logger.info("payload");
    logger.info(payload);
    logger.info(payload.playerId);


    //server is sender = 0 so do not process those
    if (gameMessage.sender != 0) {

        let logicalSender = logicalPlayerIDs[gameMessage.sender];

        switch (gameMessage.opCode) {

            //REQUEST_FIND_MATCH?

            case OP_SET_PLAYER_INFO:

                SendStringToClient(players, gameMessage.opCode,"player info");  

                break;

            case OP_PLAYER_MOVEMENT:
                {
                    ////process data

                    //let movementData = {
                    //    playerXPosition: payload.playerXPosition,
                    //    playerYPosition: payload.playerYPosition,
                    //    playerZPosition: payload.playerZPosition,
                    //    playerId: payload.playerId
                    //};

                    //logger.info("movement data")
                    //logger.info(movementData);

                    ////make message
                    //const movementMSG = session.newTextGameMessage(
                    //    PLAYER_MOVEMENT_RECEIVED, session.getServerId(), JSON.stringify(movementData));

                    ////send message
                    //for (let index = 0; index < players.length; ++index) {
                    //    logger.info("Sending movement to player " + players[index].peerId);
                    //    session.sendReliableMessage(movementMSG, players[index].peerId);
                    //}

                    ////checkstate after any changes
                    ////TODO: do stuff here if needed after movement

                    break;
                }

    }
    

      
    }
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