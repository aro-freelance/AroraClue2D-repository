using UnityEngine;
using System;
using System.Text;
using Aws.GameLift.Realtime;
using Aws.GameLift.Realtime.Event;
using Aws.GameLift.Realtime.Types;
using Newtonsoft.Json;

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


        Client = new Client(clientConfiguration);


        Client.ConnectionOpen += new EventHandler(OnOpenEvent);
        Client.ConnectionClose += new EventHandler(OnCloseEvent);
        //Client.GroupMembershipUpdated += new EventHandler<GroupMembershipEventArgs>(OnGroupMembershipUpdate);
        Client.DataReceived += new EventHandler<DataReceivedEventArgs>(OnDataReceived);
        Client.ConnectionError += new EventHandler<Aws.GameLift.Realtime.Event.ErrorEventArgs>(OnConnectionErrorEvent);

        //@Yelsa  the token had null as payload parameter which is likely what was preventing player from being accepted as ACTIVE. Check back here whether RESERVED player status is still an issue.
        ConnectionToken token = new ConnectionToken(playerSessionId, StringToBytes(connectionPayload)); 
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

        // handle message based on OpCode the server sent
        switch (data.OpCode)
        {

            case Constants.PLAYER_CONNECT_OP_CODE:

                Debug.Log("on data received player connect op code");

                break;

            case 200:
                //TODO: use this to establish connection to the game session after it is created

                Debug.Log("opcode 200. hello from the server " + dataString);

                break;

                //@Yelsa Step 4.5
            case GameManager.OP_CODE_PLAYER_ACCEPTED:


                // This tells our client that the player has been accepted into the Game Session as a new player session.
                Debug.Log("op code: Player accepted into game session!");

                string numberOfPlayers = BytesToString(data.Data);

                //tell player that they are accepted and waiting for match to start
                GameManager.Instance.LoadingMatchUI(false);

                //if the player is first to join, they are the host and they should prep the game
                if(numberOfPlayers == "1")
                {
                    GameManager.Instance.HostPrepGame();
                }


                break;

            case GameManager.OP_FIRE_MATCH_S:

                Debug.Log("op code: call to fire match");

                //TODO: get data from the call in the form of GamePrepData. contains weapon, suspect, place strings and a PlayerData[]
                string jsonDataFromServer = BytesToString(data.Data);
                //TODO: this should be set to the data from server
                GamePrepData gamePrepData = JsonConvert.DeserializeObject<GamePrepData>(jsonDataFromServer);

                GameManager.Instance.SetupAfterMatchFound(gamePrepData);


                break;


            case GameManager.OP_SET_PLAYER_INFO_S:
                Debug.Log("get host data received");

                break;


            case GameManager.OP_PREP_GAME_S:
                Debug.Log("prep game data received");

                break;

            //TODO: I stopped here for christmas... code from here forward hasn't been checked, just labeled
            //@Yelsa Step 8
            case GameManager.OP_START_GAME_S:
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


            //@Yelsa Step 14b
            case GameManager.OP_GAMEOVER_S:
                // gives us the match results
                Debug.Log("Game over op...");
                
                string gameoverData = BytesToString(data.Data);
                // Debug.Log(gameoverData);

                //MatchResults matchResults = JsonConvert.DeserializeObject<MatchResults>(gameoverData);

                //OnGameOver(matchResults);

                break;



            //@Yelsa Step 11
            case GameManager.OP_START_GUESS_EVENT_S:

                Debug.Log("Start guess event data received");

                GameManager.Instance.GuessEvent();

                break;

            //@Yelsa Step 14a
            case GameManager.OP_END_GUESS_EVENT_S:

                Debug.Log("end guess event data received");


                break;

            //@YELSA Step 16  (should be same as back to step 8)
            case GameManager.OP_RESUME_GAME_S:

                //TODO:

                break;

            //Receive movement data from server for another player's location
            //@Yelsa Step 18
            case GameManager.OP_PLAYER_MOVEMENT_S:

                Debug.Log("realtimeclient: player movement received in switch");
                PlayerMovementData playerMovementData = JsonConvert.DeserializeObject<PlayerMovementData>(dataString);

                //OnPlayerMovement(playerMovementData);

                //TODO: use this data to set the position of player whose id was received 
                //... unless it is this player's id... should update server code to not send it to the player whose id it is

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

    //protected virtual void OnGameOver(MatchResults matchResults)
    //{
    //    Debug.Log("OnGameOver");

    //    GameOverEventArgs gameOverEventArgs = new GameOverEventArgs(matchResults);

    //    EventHandler<GameOverEventArgs> handler = GameOverEventHandler;
    //    if (handler != null)
    //    {
    //        handler(this, gameOverEventArgs);
    //    }
    //}

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
    //public MatchResults matchResults { get; set; }

    //public GameOverEventArgs(MatchResults matchResults)
    //{
    //    this.matchResults = matchResults;
    //}
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



