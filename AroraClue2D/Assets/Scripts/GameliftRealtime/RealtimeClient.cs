using UnityEngine;
using System;
using System.Text;
using Aws.GameLift.Realtime;
using Aws.GameLift.Realtime.Event;
using Aws.GameLift.Realtime.Types;
using Newtonsoft.Json;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Collections;
using Aws.GameLift.Realtime.Network;

/**
 * @BatteryAcid
 * I've modified this example to demonstrate a simple two player card game.
 * 
 * The base code is sourced from the AWS GameLift Docs: 
 * https://docs.aws.amazon.com/gamelift/latest/developerguide/realtime-client.html#realtime-client-examples
 *
 * -----
 * 
 * An example client that wraps the GameLift Realtime client SDK
 * 
 * You can redirect logging from the SDK by setting up the LogHandler as such:
 * ClientLogger.LogHandler = (x) => Console.WriteLine(x);
 *
 * Based on: https://docs.aws.amazon.com/gamelift/latest/developerguide/realtime-client.html#realtime-client-examples
 */
public class RealTimeClient
{
    public Aws.GameLift.Realtime.Client Client { get; private set; }

    public bool OnCloseReceived { get; private set; }
    public bool GameStarted = false;


    //need handlers for each event handled by server
    //players connected?
    public event EventHandler<RemotePlayerIdEventArgs> RemotePlayerIdEventHandler;
    //gameover
    public event EventHandler<GameOverEventArgs> GameOverEventHandler;

    ////start timer guess event
    //public event EventHandler<StartTimerGuessEventArgs> StartTimerGuessEventHandler;
    ////start countdown for submition
    //public event EventHandler<StartGuessCountdownArgs> StartGuessCountdownEventHandler;

    //StartGame handler needs to be added... it should find the player list, assign sprites to the players, choose a host,
    //host should roll the random stuff start the timer

    //check answers
    public event EventHandler<CheckAnswersArgs> CheckAnswersEventHandler;
    //player movement/location
    public event EventHandler<PlayerMovementArgs> PlayerMovementEventHandler;



    /// <summary>
    /// Initialize a client for GameLift Realtime and connects to a player session.
    /// </summary>
    /// <param name="endpoint">The endpoint for the GameLift Realtime server to connect to</param>
    /// <param name="tcpPort">The TCP port for the GameLift Realtime server</param>
    /// <param name="localUdpPort">Local Udp listen port to use</param>
    /// <param name="playerSessionId">The player session Id in use - from CreatePlayerSession</param>
    /// <param name="connectionPayload"></param>
    /// 
    public RealTimeClient(string endpoint, int tcpPort, int localUdpPort, string playerSessionId, string connectionPayload, ConnectionType connectionType)
    {
        this.OnCloseReceived = false;

        // Create a client configuration to specify a secure or unsecure connection type
        // Best practice is to set up a secure connection using the connection type RT_OVER_WSS_DTLS_TLS12.
        ClientConfiguration clientConfiguration = new ClientConfiguration()
        {
            // C# notation to set the field ConnectionType in the new instance of ClientConfiguration
            ConnectionType = connectionType
        };


        //ConnectionToken token = new ConnectionToken(tokenUID, null);


        Client = new Aws.GameLift.Realtime.Client(clientConfiguration);


        // Create a Realtime client with the client configuration            
       // Client = new Client(clientConfiguration);
       // Client = new Aws.GameLift.Realtime.Client(clientConfiguration);


        Client.ConnectionOpen += new EventHandler(OnOpenEvent);
        Client.ConnectionClose += new EventHandler(OnCloseEvent);
        //Client.GroupMembershipUpdated += new EventHandler<GroupMembershipEventArgs>(OnGroupMembershipUpdate);
        Client.DataReceived += new EventHandler<DataReceivedEventArgs>(OnDataReceived);
        Client.ConnectionError += new EventHandler<Aws.GameLift.Realtime.Event.ErrorEventArgs>(OnConnectionErrorEvent);

        ConnectionToken token = new ConnectionToken(playerSessionId, null); //StringToBytes(connectionPayload));
        Client.Connect(endpoint, tcpPort, localUdpPort, token);


    }

   


    private void OnConnectionErrorEvent(object sender, Aws.GameLift.Realtime.Event.ErrorEventArgs e)
    {
        if (Client.ConnectedAndReady)
        {
            if (e.Exception != null)
            {
                Debug.Log($"[client] Connection Error! : " + e.Exception);
                //GameManager.QuitToMainMenu();
            }
        }
        
    }


    /// <summary>
    /// Handle data received from the Realtime server  
    /// </summary>
    private void OnDataReceived(object sender, DataReceivedEventArgs data)
    {
        Debug.Log("On data received");
        string dataString = BytesToString(data.Data);

        Debug.Log($"[server-sent] OnDataReceived - Sender: {data.Sender} OpCode: {data.OpCode} dataString: {dataString}");

        // handle message based on OpCode
        switch (data.OpCode)
        {
            case GameManager.OP_CODE_PLAYER_ACCEPTED:

                //TODO: this is not in use. remove?

                // This tells our client that the player has been accepted into the Game Session as a new player session.
                Debug.Log("Player accepted into game session!");

                // If you need to test and you don't have two computers, you can mark GameStarted true here to enable the Draw card button
                // and comment it out in the GAME_START_OP case.
                // This only works because game play is asynchronous and doesn't care if both players are active at the same time.
                //GameStarted = true;

                break;


            case GameManager.GET_HOST:
                Debug.Log("get host data received");

                break;

            case GameManager.START_GAME:
                // The game start op tells our game clients that all players have joined and the game should start
                Debug.Log("Start game op received...");

                string startGameData = BytesToString(data.Data);
                // Debug.Log(startGameData);

                // Sets the opponent's id, in production should use their public username, not id.
                StartMatch startMatch = JsonConvert.DeserializeObject<StartMatch>(startGameData);
                OnRemotePlayerIdReceived(startMatch);

                // This enables the draw card button so the game can be played.
                GameStarted = true;

                break;

            //case GameManager.DRAW_CARD_ACK_OP:

            //TODO: this is not in use. remove?

            //    // A player has drawn a card.  To be received as an acknowledgement that a card was played,
            //    // regardless of who played it, and update the UI accordingly.
            //    Debug.Log("Player draw card ack...");

            //    string data = BytesToString(e.Data);
            //    // Debug.Log(data);

            //    CardPlayed cardPlayedMessage = JsonConvert.DeserializeObject<CardPlayed>(data);
            //    // Debug.Log(cardPlayedMessage.playedBy);
            //    // Debug.Log(cardPlayedMessage.card);

            //    OnCardPlayed(cardPlayedMessage);

            //    break;

            case GameManager.GAMEOVER:
                // gives us the match results
                Debug.Log("Game over op...");
                
                string gameoverData = BytesToString(data.Data);
                // Debug.Log(gameoverData);

                MatchResults matchResults = JsonConvert.DeserializeObject<MatchResults>(gameoverData);

                OnGameOver(matchResults);

                break;




            case GameManager.START_GUESS_EVENT:

                Debug.Log("Start guess event data received");

                GameManager.Instance.GuessEvent();

                break;



            case GameManager.END_GUESS_EVENT:

                Debug.Log("end guess event data received");


                break;

            case GameManager.CHECK_ANSWERS:

                Debug.Log("Check answers op data received");

                PlayerGuessData playerGuessData = JsonConvert.DeserializeObject<PlayerGuessData>(dataString);

                OnPlayerGuess(playerGuessData);

                break;


            case GameManager.PLAYER_MOVEMENT_RECEIVED:

                Debug.Log("realtimeclient: player movement received in switch");
                PlayerMovementData playerMovementData = JsonConvert.DeserializeObject<PlayerMovementData>(dataString);

                OnPlayerMovement(playerMovementData);

                break;


            default:
                Debug.Log("OpCode not found: " + data.OpCode);
                break;
        }
    }

    /// <summary>
    /// 
    /// 
    /// Once we have a connection to the server we are initializing the handlers declared at the top of this file. 
    /// (in the manager in EstablishConnectionToRealtimeServer).  e.x. CardPlayedEventHandler
    /// 
    /// We are making a new local handler which we are setting equal to the handler on the server,
    /// If that handler exists, then we are passing the data to it. 
    /// (data received from 
    /// 
    /// 
    /// </summary>

    //protected virtual void OnCardPlayed(CardPlayed cardPlayed)
    //{
    //    Debug.Log("OnCardPlayed");

    //    CardPlayedEventArgs cardPlayedEventArgs = new CardPlayedEventArgs(cardPlayed);

    //    EventHandler<CardPlayedEventArgs> handler = CardPlayedEventHandler;
    //    if (handler != null)
    //    {
    //        handler(this, cardPlayedEventArgs);
    //    }
    //}

    protected virtual void OnRemotePlayerIdReceived(StartMatch startMatch)
    {
        Debug.Log("OnRemotePlayerIdReceived");

        RemotePlayerIdEventArgs remotePlayerIdEventArgs = new RemotePlayerIdEventArgs(startMatch);

        EventHandler<RemotePlayerIdEventArgs> handler = RemotePlayerIdEventHandler;
        if (handler != null)
        {
            handler(this, remotePlayerIdEventArgs);
        }
    }

    protected virtual void OnGameOver(MatchResults matchResults)
    {
        Debug.Log("OnGameOver");

        GameOverEventArgs gameOverEventArgs = new GameOverEventArgs(matchResults);

        EventHandler<GameOverEventArgs> handler = GameOverEventHandler;
        if (handler != null)
        {
            handler(this, gameOverEventArgs);
        }
    }

    protected virtual void OnPlayerMovement(PlayerMovementData playerMovementData)
    {
        PlayerMovementArgs playerMovementArgs = new PlayerMovementArgs(
            playerMovementData.playerXPosition,
            playerMovementData.playerYPosition,
            playerMovementData.playerZPosition,
            playerMovementData.playerId
            );

        EventHandler<PlayerMovementArgs> handler = PlayerMovementEventHandler;

        if (handler != null)
        {
            handler(this, playerMovementArgs);
        }
        else
        {
            Debug.Log("movement handler is null");
        }

    }

    protected virtual void OnPlayerGuess(PlayerGuessData playerGuessData)
    {

        CheckAnswersArgs checkAnswersArgs = new CheckAnswersArgs(
            playerGuessData.weapon, 
            playerGuessData.suspect,
            playerGuessData.place,
            playerGuessData.playerId,
            playerGuessData.guessedOnTime
            );

        EventHandler<CheckAnswersArgs> handler = CheckAnswersEventHandler;

        if (handler != null)
        {
            handler(this, checkAnswersArgs);
        }


    }

    /// <summary>
    /// Example of sending to a custom message to the server.
    /// 
    /// Server could be replaced by known peer Id etc.
    /// </summary>
    /// <param name="realtimePayload">Custom payload to send with message</param>
    public void SendMessage(int opcode, RealtimePayload realtimePayload)
    {
        // You can also pass in the DeliveryIntent depending on your message delivery requirements
        // https://docs.aws.amazon.com/gamelift/latest/developerguide/realtime-sdk-csharp-ref-datatypes.html#realtime-sdk-csharp-ref-datatypes-rtmessage

        string payload = JsonUtility.ToJson(realtimePayload);

        Client.SendMessage(Client.NewMessage(opcode)
            .WithDeliveryIntent(DeliveryIntent.Reliable)
            .WithTargetPlayer(Constants.PLAYER_ID_SERVER)
            .WithPayload(StringToBytes(payload)));
    }

    /**
     * Handle connection open events
     */
    public void OnOpenEvent(object sender, EventArgs e)
    {
    }

    /**
     * Handle connection close events
     */
    public void OnCloseEvent(object sender, EventArgs e)
    {
        OnCloseReceived = true;
    }

    /**
     * Handle Group membership update events 
     */
    public void OnGroupMembershipUpdate(object sender, GroupMembershipEventArgs e)
    {
    }

    public void Disconnect()
    {
        if (Client.Connected)
        {
            Client.Disconnect();
        }
    }

    public bool IsConnected()
    {
        return Client.Connected;
    }

    /**
     * Helper method to simplify task of sending/receiving payloads.
     */
    public static byte[] StringToBytes(string str)
    {
        return Encoding.UTF8.GetBytes(str);
    }

    /**
     * Helper method to simplify task of sending/receiving payloads.
     */
    public static string BytesToString(byte[] bytes)
    {
        return Encoding.UTF8.GetString(bytes);
    }
}


public class RemotePlayerIdEventArgs : EventArgs
{
    public string remotePlayerId { get; set; }

    public RemotePlayerIdEventArgs(StartMatch startMatch)
    {
        this.remotePlayerId = startMatch.remotePlayerId;
    }
}

public class GameOverEventArgs : EventArgs
{
    public MatchResults matchResults { get; set; }

    public GameOverEventArgs(MatchResults matchResults)
    {
        this.matchResults = matchResults;
    }
}




//timers should be handled by determining host and running them locally on host system.
//host should then trigger the events by simply passing a message with playerId



//after a player clicks submit, check to see if we have answers from all players
// if we have answers from all players, or the timer is ended, check the answers
// and return a bool with whether there is a winner and who the winner is by playerId 
public class CheckAnswersArgs : EventArgs
{


    public string currentAnswerWeapon { get; set; }
    public string currentAnswerSuspect { get; set; }
    public string currentAnswerLocation { get; set; }

    public string currentAnswerPlayerId { get; set; }

    public bool guessedOnTime { get; set; }

    public CheckAnswersArgs(
        string currentAnswerWeapon, 
        string currentAnswerSuspect,
        string currentAnswerLocation,
        string currentAnswerPlayerId,
        bool guessedOnTime)
    {
        this.currentAnswerWeapon = currentAnswerWeapon;
        this.currentAnswerSuspect = currentAnswerSuspect;
        this.currentAnswerLocation = currentAnswerLocation;
        this.currentAnswerPlayerId = currentAnswerPlayerId;
        this.guessedOnTime = guessedOnTime;
     }

}



//tracks location of each player
public class PlayerMovementArgs: EventArgs
{

    public float playerXLocation { get; set; }
    public float playerYLocation { get; set; }
    public float playerZLocation { get; set; }
    public string playerId { get; set; }


    public PlayerMovementArgs(float playerXLocation, float playerYLocation, float playerZLocation, string playerId) {

        this.playerXLocation = playerXLocation;
        this.playerYLocation = playerYLocation;
        this.playerZLocation = playerZLocation;
        this.playerId = playerId;

    }



}



