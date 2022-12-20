using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;
using System.Net;
using Newtonsoft.Json;
using System;
using Aws.GameLift.Realtime.Types;
using System.Net.Sockets;
using System.Threading.Tasks;
using static Google.Protobuf.WellKnownTypes.Field.Types;

public class GameManager : MonoBehaviour
{

    public static GameManager Instance;

    string GameSessionPlacementEndpoint;

    //used to distiguish between the players in multiplayer game
    public int playerNumber;

    //this creates multiple different bools at once.  cleaner than doing each separate. especially when they are all similar. 
    public bool gameMenuOpen, dialogueActive, cutsceneActive, fadingBetweenAreas;

    public string[] itemsInInventory;
    public int[] numberOfEachItem;
    public Item[] referenceItems;
    public GameObject leadDetective;
    public GameObject timerObjectOutOfMenu;
    public GameObject timerObjectInMenu;

    public int currentMoney;

    //count up from this value to the guessInterval.. to trigger a guess event (minutes)
    private float timer = 0;
    public bool timerIsRunning = false;

    //countdown from this value is the time allowed to the player to make a guess during the guess event (minutes) .5
    private float timer2 = 0.5f;
    public bool secondTimerIsRunning = false;


    //how often should a guess event be triggered (minutes) 1
    public float guessInterval = 1f;

    public bool submittedAnswer = false;

    private bool isHost = false;




    
    private MatchResults _matchResults = new MatchResults();
    private MatchStats _matchStats = new MatchStats();

    private static readonly IPEndPoint DefaultLoopbackEndpoint = new IPEndPoint(IPAddress.Loopback, port: 0);
    private SQSMessageProcessing _sqsMessageProcessing;
    private RealTimeClient _realTimeClient;
    private APIManager _apiManager;

    //after CS bools
    public bool isReadyToStartCountdown;
    public bool isReadyToCheckAnswers;
    public bool isReadyToResume;
    public bool isReadyToEndGame;

    public float defaultAutoplayDelay = 4; //seconds

    public bool isGuessCorrect = false;


    private string _playerId;
    private string _remotePlayerId = "";
    private bool _processGuess = false;
    private bool _updateRemotePlayerId = false;
    private bool _findingMatch = false;
    private bool _gameOver = false;
    private bool _checkAnswers = false;
    private bool _startGame = false;
    public bool _movement = false;


    // GameLift server opcodes 
    // An opcode defined by client and your server script that represents a custom message type
    //TODO: these are not being used at the moment. remove?
    public const int OP_CODE_PLAYER_ACCEPTED = 113;
    public const int START_TIMER_OP = 301;
    public const int START_GUESS_EVENT_COUNTDOWN = 302;

    //these are being used
    public const int CHECK_ANSWERS = 305;
    public const int PLAYER_MOVEMENT = 900;
    public const int GAME_START_OP = 201;
    public const int GAMEOVER_OP = 209;

    // Lambda opcodes
    private const string REQUEST_FIND_MATCH_OP = "1";



    // Start is called before the first frame update
    void Start()
    {

        Instance = this;
        DontDestroyOnLoad(gameObject);

        GameSessionPlacementEndpoint = PrivateConsts.instance.GameSessionPlacementEndpoint;

        _apiManager = FindObjectOfType<APIManager>();
        _sqsMessageProcessing = FindObjectOfType<SQSMessageProcessing>();

        //TODO: determine if player is host by asking server.. if they are isHost = true
        isHost = true;


        _playerId = System.Guid.NewGuid().ToString();



        //TODO: this should be moved to a button on the main menu rather than called on start eventually
        OnFindMatchPressed();

        timerIsRunning = true;

    }


    void Update()
    {
        //WHILE LOADING OR IN MENU
        //if any of these are true the player cannot move. else player can move.
        // plus this lets us add in any additional things we want to do when the player is in a menu or loading
        if (gameMenuOpen || dialogueActive || fadingBetweenAreas || cutsceneActive)
        {
            PlayerController.instance.canMove = false;
        }
        else
        {
            PlayerController.instance.canMove = true;
        }

        ServerProcesses();

        RunGuessSystemChecksAndTimers();

    }

    void ServerProcesses()
    {
        if (_findingMatch)
        {
            _findingMatch = false;
            //_findMatchButton.enabled = false;
            //_findMatchButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Searching...";
        }

        if (_realTimeClient != null && _realTimeClient.GameStarted)
        {
            _realTimeClient.GameStarted = false;
        }

        if (_updateRemotePlayerId)
        {
            _updateRemotePlayerId = false;
            //update UI?
        }

        if (_movement)
        {
            _movement = false;

            if(_realTimeClient != null)
            {
                //if there is movement, send it to the server with the code to tell it that is the movement of the player with playerId
                //then we can use that one each instance of the game to set the sprite for that playerId
                RealtimePayload movement = new RealtimePayload(_playerId,
                    PlayerController.instance.transform.position.x,
                    PlayerController.instance.transform.position.y,
                    PlayerController.instance.transform.position.z);
                _realTimeClient.SendMessage(PLAYER_MOVEMENT, movement);
            }
            else
            {
                Debug.Log("movement, realtimeclient is null");
            }
        }

        if (_checkAnswers)
        {
            _checkAnswers = false;

            if (_realTimeClient != null)
            {
                Debug.Log("checkanswers bool");
                //if there is a guess, send it to the server with the code to check it locally
                //TODO: get the actual guesses and replace the hard coded strings
                RealtimePayload guess = new RealtimePayload(_playerId,
                    "weaponguess",
                    "suspectguess",
                    "locationguess");
                _realTimeClient.SendMessage(PLAYER_MOVEMENT, guess);
            }
            else
            {
                Debug.Log("movement, realtimeclient is null");
            }

        }

        if (_startGame)
        {
            _startGame = false;

            Debug.Log("start game bool call in GameManager");

            //TODO: implement
            //find the player list, assign sprites to the players, choose a host,
            //host should roll the random stuff start the timer

        }


        // determine match results once game is over
        if (this._gameOver == true)
        {
            this._gameOver = false;
            DisplayMatchResults();
        }
    }

    void RunGuessSystemChecksAndTimers()
    {
        if (isReadyToCheckAnswers)
        {
            Debug.Log("ready to check answers");
            TriggerCheckAnswers();

            isReadyToCheckAnswers = false;

        }

        if (isReadyToEndGame)
        {
            Debug.Log("ready to end game");
            EndGame();

            isReadyToEndGame = false;
        }

        if (isReadyToResume)
        {
            Debug.Log("ready to resume");
            ResumeGame();

            isReadyToResume = false;
        }

        if (isReadyToStartCountdown)
        {
            Debug.Log("ready to start countdown");
            secondTimerIsRunning = true;
            //show the timer on the screen
            timerObjectInMenu.SetActive(true);
            timerObjectOutOfMenu.SetActive(true);

            //open menu and guess interface
            GameMenu.instance.ShowMenu();
            GameMenu.instance.guessWindow.SetActive(true);
            GameMenu.instance.guessButton.SetActive(true);

            isReadyToStartCountdown = false;
        }

        if (timerIsRunning)
        {
            HandleTimer();
        }
        else
        {
            if (timer == 0)
            {
                timerIsRunning = true;
            }
        }

        if (secondTimerIsRunning)
        {
            HandleTimer2();

            TriggerCheckIfAllAnswersSubmitted();
        }
    }

    //countup to amount
    void HandleTimer()
    {
        //only host should run the timers and trigger the events
        if (isHost)
        {
            if (timerIsRunning)
            {
                timer += (Time.deltaTime / 60);

                float timeRemaining = guessInterval - timer;

                if (timeRemaining <= 0)
                {
                    TriggerGuessEvent();
                }

            }

        }
    }

    //countdown
    void HandleTimer2()
    {
        //only host should run the timers and trigger the events
        if (isHost)
        {
            if (secondTimerIsRunning)
            {

                timer2 -= (Time.deltaTime / 60);

                timerObjectInMenu.GetComponent<TMP_Text>().text = Mathf.Floor(timer2 * 60).ToString() + " Seconds Remaining";
                timerObjectOutOfMenu.GetComponent<TMP_Text>().text = Mathf.Floor(timer2 * 60).ToString() + " Seconds Remaining";

                if (timer2 <= 0)
                {
                    TriggerCheckAnswersEvent();
                }

            }
        }
    }


    async void TriggerGuessEvent()
    {
        timerIsRunning = false;

        Debug.Log("Guess event triggered");


        //TODO: await here... tell the server to call guess event for all players
        GuessEvent();



    }

    void GuessEvent()
    {
        //move player to location (each player will be placed at spawn based on thier player number)
        SpawnPlayerAtGuessEvent(playerNumber);

        //spawn NPC (lead detective)
        Instantiate(leadDetective, new Vector3(0, 0), Quaternion.identity);

        //show starting CS
        GuessEventStartCutscene();

    }





    async void TriggerCheckAnswersEvent()
    {
        secondTimerIsRunning = false;
        timerObjectInMenu.SetActive(false);
        timerObjectOutOfMenu.SetActive(false);
        timer2 = 0;


        //TODO: await check on server if any players have not submitted an answer
        bool notAllAnswered = !submittedAnswer; //TODO: set to answer from server method
        if (notAllAnswered)
        {
            NoAnswerSubmittedCutscene();
        }
        else
        {
            isReadyToCheckAnswers = true;
        }

    }


    async void TriggerCheckAnswers()
    {
        //only host should run the timers and trigger the events
        if (isHost)
        {   
            //TODO: await check if any of the submitted answers are correct
            bool correctAnswerReceived = isGuessCorrect; //TODO: set using server method.. check all users
            string winningPlayerName = "WinnerName";

            //if there is a correct answer, end the game and show that player is the winner, through dialogue
            if (correctAnswerReceived)
            {
                WinnerCutscene();
            }
            //else there is not a correct answer submitted.
            else
            {
                //show dialogue telling the players why their guesses are incorrect
                EndGuessResumeGameCutscene();
                //after this CS triggers it will resume the game 
            }

        }

    }

    void TriggerCheckIfAllAnswersSubmitted()
    {
        if(isHost)
        {
            //using the server check if all players have submittedAnswer = true
            //if they all do then TriggerEndCheckAnswerEvent

        }


    }

    void ResumeGame()
    {
        Debug.Log("GameManager: resume game");


        //close the menu
        GameMenu.instance.CloseMenu();

        //turn on player movement
        cutsceneActive = false;

        //start timer again (only when ready to start game again).  
        timer = 0;
    }



    /// <summary>
    /// Item Management
    /// </summary>

    //unused item methods
    public Item GetItemDetails(string itemToGrab)
    {
        for (int i = 0; i < referenceItems.Length; i++)
        {
            if (referenceItems[i].itemName == itemToGrab)
            {
                return referenceItems[i];
            }
        }


        return null; // if we get here and don't have an item end the function with no return
    }


    public void SortItems()
    {
        bool thereisAGap = true;

        while (thereisAGap)
        {
            thereisAGap = false;



            for (int i = 0; i < itemsInInventory.Length - 1; i++)
            {
                //if the current inventory slot is empty
                if (itemsInInventory[i] == "")
                {
                    //move the item in the next position to the current positon
                    itemsInInventory[i] = itemsInInventory[i + 1];
                    numberOfEachItem[i] = numberOfEachItem[i + 1];
                    //empty the next position, so there is not a duplicate 
                    itemsInInventory[i + 1] = "";
                    numberOfEachItem[i + 1] = 0;

                    //if you found an item after a blank space that means there is a gap, keep running the sort
                    if (itemsInInventory[i] != "")
                    {
                        thereisAGap = true;
                    }

                }
            }
        }
    }


    public void AddItem(string itemToAdd)
    {

        int newItemPosition = 0;
        bool foundPlaceToPutItem = false;

        for (int i = 0; i < itemsInInventory.Length; i++)
        {
            //so if you find a blank you are at the end of the inventory bc we sorted it. or if you find the item before that, stack it.
            if (itemsInInventory[i] == "" || itemsInInventory[i] == itemToAdd)
            {
                newItemPosition = i; // the item is being placed at the position we found
                i = itemsInInventory.Length; // we found the place to put our item. we can end the loop using this
                foundPlaceToPutItem = true;

            }
        }

        //we have a place to put item. add the item.
        if (foundPlaceToPutItem)
        {
            bool itemExists = false;
            for (int i = 0; i < referenceItems.Length; i++)
            {
                //if you find the item in the list of items
                if (referenceItems[i].itemName == itemToAdd)
                {
                    itemExists = true; // then it exists
                    i = referenceItems.Length; // end the loop
                }
            }

            if (itemExists)
            {
                itemsInInventory[newItemPosition] = itemToAdd; // put it in there
                numberOfEachItem[newItemPosition]++; //once

            }
            else
            {
                Debug.LogError("Tag: Game Manager, AddItem. " + itemToAdd + " does not exist.");
            }

            //GameMenu.instance.ShowItems();
        }



    }

    public void RemoveItem(string itemToRemove)
    {

        bool foundItem = false;
        int itemPosition = 0;

        for (int i = 0; i < itemsInInventory.Length; i++)
        {
            if (itemsInInventory[i] == itemToRemove)
            {
                foundItem = true;
                itemPosition = i; // the item is at position i.
                i = itemsInInventory.Length;//end the loop, we found it.

            }
        }

        if (foundItem)
        {
            numberOfEachItem[itemPosition]--;
            if (numberOfEachItem[itemPosition] <= 0)
            {
                itemsInInventory[itemPosition] = "";
            }

            //GameMenu.instance.ShowItems();
        }
        else
        {
            Debug.LogError("Tag: GameManager, RemoveItem. Couldn't find " + itemToRemove);
        }


    }
    async void SpawnPlayerAtGuessEvent(int playerNum)
    {
        //TODO: use player number to spawn the player at a spawnpoint in a list of spawn points

        Debug.Log("spawn player for guess event. playernumber: " + playerNum);

        fadingBetweenAreas = true;

        switch (playerNum)
        {
            case 0:

                PlayerController.instance.transform.position = new Vector3(1, -3, transform.position.z);

                break;

            case 1:

                PlayerController.instance.transform.position = new Vector3(4, -3, transform.position.z);

                break;

            case 2:

                PlayerController.instance.transform.position = new Vector3(-1, -3, transform.position.z);

                break;

            case 3:

                PlayerController.instance.transform.position = new Vector3(-4, -3, transform.position.z);

                break;


            default:
                break;


        }

        fadingBetweenAreas = false;

    }


    async void EndGame()
    {

        //TODO: end the game... (this is not the CS)..
        //this is for closing the server and kicking the players out to possibly and endscreen/ the menu

    }

    /// <summary>
    /// Cutscenes
    /// </summary>

    void GuessEventStartCutscene()
    {

        string[] lines = new string[] { "n-Detective", "submit your best guesses to me", "we are on a deadline."};

        DialogueManager.instance.AutoPlayDialogue(lines, defaultAutoplayDelay, "readyToStartCountdown");


    }

    void NoAnswerSubmittedCutscene()
    {
        submittedAnswer = true;

        //TODO: take data from the player(s) who didn't answer... values they have found and display those in cutscene...
        //TODO: this means we need to track what the player has found on the back end...

        string[] lines = new string[] { "n-Detective", "no answer submitted...", "but you found the wrench.", "everyone mark that the murder weapon is not the wrench" };

        DialogueManager.instance.AutoPlayDialogue(lines, defaultAutoplayDelay, "readyToCheckAnswers");
    }

    void EndGuessResumeGameCutscene()
    {

        string[] lines = new string[] { "n-Detective", "alright, good guesses everyone", "keep looking!" };

        DialogueManager.instance.AutoPlayDialogue(lines, defaultAutoplayDelay, "readyToResume");

        

    }

    void WinnerCutscene()
    {

        string[] lines = new string[] { "n-Detective", "playerX wins!", "good work", "end of game"};

        DialogueManager.instance.AutoPlayDialogue(lines, defaultAutoplayDelay, "readyToEndGame");

    }






    /// <summary>
    /// //Server Manager Methods
    /// </summary>


    /// <summary>
    /// 
    /// When finding a match we first are building a call to the server with apimanager.Post(api, postdata)
    /// 
    /// Then we are getting the server response back in the string called response. 
    /// This response is then converted from JSON to GameSessionPlacementEndpoint (defined in SQSMessageProcessing)
    /// which we can then use to access the string values such as PlacementId and GameSessionId
    /// we can then used these strings to make a GameSession and establish a connection 
    /// using SubscribeToFullfillmentNotifications and EstablishConnectionToRealtimeServer
    /// 
    /// Once we establish a connection we will have a realTimeClient which we can use to access the handlers setup there.
    /// 
    /// Once we have a realtimeclient and handlers:
    /// the realtimeclient can be sent codes along with playerids to tell it to call a method.
    /// 
    /// When a code is received it is processed by RealtimeClient > OnDataReceived using a switch run by the code int
    /// This switch tells it which method to use.
    /// 
    /// Then we send the data to that function to process using a handler.
    /// 
    /// </summary>
    public async void OnFindMatchPressed()
    {
        Debug.Log("Find match pressed");
        _findingMatch = true;

        FindMatch matchMessage = new FindMatch(REQUEST_FIND_MATCH_OP, _playerId);
        string jsonPostData = JsonUtility.ToJson(matchMessage);


        string response = await _apiManager.Post(GameSessionPlacementEndpoint, jsonPostData);

        Debug.Log("response: " + response);

        GameSessionPlacementInfo gameSessionPlacementInfo = JsonConvert.DeserializeObject<GameSessionPlacementInfo>(response);

        if (gameSessionPlacementInfo != null)
        {


            // GameSessionPlacementInfo is a model used to handle both game session placement and game session search results from the Lambda response.
            if (gameSessionPlacementInfo.PlacementId != null)
            {
                // The response was from a placement request
                Debug.Log("Game session placement request submitted.");

                // Debug.Log(gameSessionPlacementInfo.PlacementId);

                // subscribe to receive the player placement fulfillment notification
                await SubscribeToFulfillmentNotifications(gameSessionPlacementInfo.PlacementId);

            }
            else if (gameSessionPlacementInfo.GameSessionId != null)
            {
                // The response was for a found game session which also contains info for created player session
                Debug.Log("Game session found!");
                // Debug.Log(gameSessionPlacementInfo.GameSessionId);

                Int32.TryParse(gameSessionPlacementInfo.Port, out int portAsInt);

                // Once connected, the Realtime service moves the Player session from Reserved to Active, which means we're ready to connect.
                // https://docs.aws.amazon.com/gamelift/latest/apireference/API_CreatePlayerSession.html
                EstablishConnectionToRealtimeServer(gameSessionPlacementInfo.IpAddress, portAsInt, gameSessionPlacementInfo.PlayerSessionId);
            }
            else
            {
                Debug.Log("Game session response not valid...");
            }
        }
        else
        {
            Debug.Log("Error: GAME SESSION PLACEMENT INFO is NULL");
        }

        //_findMatchButton.gameObject.SetActive(false); // remove from UI
    }

    private async Task<bool> SubscribeToFulfillmentNotifications(string placementId)
    {
        PlayerPlacementFulfillmentInfo playerPlacementFulfillmentInfo = await _sqsMessageProcessing.SubscribeToFulfillmentNotifications(placementId);

        if (playerPlacementFulfillmentInfo != null)
        {
            Debug.Log("Player placement was fulfilled...");
            // Debug.Log("Placed Player Sessions count: " + playerPlacementFulfillmentInfo.placedPlayerSessions.Count);

            // Once connected, the Realtime service moves the Player session from Reserved to Active, which means we're ready to connect.
            // https://docs.aws.amazon.com/gamelift/latest/apireference/API_CreatePlayerSession.html
            EstablishConnectionToRealtimeServer(playerPlacementFulfillmentInfo.ipAddress, playerPlacementFulfillmentInfo.port,
                playerPlacementFulfillmentInfo.placedPlayerSessions[0].playerSessionId);

            return true;
        }
        else
        {
            Debug.Log("Player placement was null, something went wrong...");
            return false;
        }
    }

    private void EstablishConnectionToRealtimeServer(string ipAddress, int port, string playerSessionId)
    {
        int localUdpPort = GetAvailableUdpPort();

        RealtimePayload realtimePayload = new RealtimePayload(_playerId);
        string payload = JsonUtility.ToJson(realtimePayload);

        _realTimeClient = new RealTimeClient(ipAddress, port, localUdpPort, playerSessionId, payload, ConnectionType.RT_OVER_WS_UDP_UNSECURED);

        if (_realTimeClient != null)
        {

            if (_realTimeClient.Client.ConnectedAndReady)
            {
                Debug.Log("realtimeclient: not null");


                _realTimeClient.PlayerMovementEventHandler += OnPlayerMovementEvent;
                _realTimeClient.CheckAnswersEventHandler += OnCheckAnswersEvent;

                //TODO: add startgame handler

                _realTimeClient.RemotePlayerIdEventHandler += OnRemotePlayerIdEvent;
                _realTimeClient.GameOverEventHandler += OnGameOverEvent;
            }
            else
            {
                Debug.Log("client not connected and ready");
            }


        }
        else
        {
            Debug.Log("realtimeclient: null");
        }

    }

    void OnPlayerMovementEvent(object sender, PlayerMovementArgs playerMovementArgs)
    {
        UpdatePlayerMovement(playerMovementArgs);


    }

    private void UpdatePlayerMovement(PlayerMovementArgs playerMovementArgs)
    {
        //TODO: for the player sprite for the playerid received update their location to the one received?

        if(playerMovementArgs != null)
        {
            Debug.Log("Gamemanager. Update player movement: id = " + playerMovementArgs.playerId
            + " x: " + playerMovementArgs.playerXLocation + " y: " + playerMovementArgs.playerYLocation
            + " z: " + playerMovementArgs.playerZLocation);

        }
        else
        {
            Debug.Log("Gamemanager. playermovementargs null");
        }

    }

    void OnCheckAnswersEvent(object sender, CheckAnswersArgs checkAnswersArgs)
    {

        UpdateCheckAnswers(checkAnswersArgs);

    }

    private void UpdateCheckAnswers(CheckAnswersArgs checkAnswersArgs)
    {
        //TODO: check the answers against the ones that were chosen on game start


        if (checkAnswersArgs != null)
        {
            Debug.Log("Gamemanager. checkanswers weapon " + checkAnswersArgs.currentAnswerWeapon);

        }
        else
        {
            Debug.Log("Gamemanager. playermovementargs null");
        }


    }


    void OnRemotePlayerIdEvent(object sender, RemotePlayerIdEventArgs remotePlayerIdEventArgs)
    {
        Debug.Log($"Remote player id received: {remotePlayerIdEventArgs.remotePlayerId}.");
        UpdateRemotePlayerId(remotePlayerIdEventArgs);
    }

    private void UpdateRemotePlayerId(RemotePlayerIdEventArgs remotePlayerIdEventArgs)
    {
        _remotePlayerId = remotePlayerIdEventArgs.remotePlayerId;
        _updateRemotePlayerId = true;
    }

    void OnGameOverEvent(object sender, GameOverEventArgs gameOverEventArgs)
    {
        Debug.Log($"Game over event received with winner: {gameOverEventArgs.matchResults.winnerId}.");
        this._matchResults = gameOverEventArgs.matchResults;
        this._gameOver = true;
    }



    private void DisplayMatchResults()
    {
        string localPlayerResults = "";
        string remotePlayerResults = "";

        if (_matchResults.winnerId == _playerId)
        {
            localPlayerResults = "You WON! Score ";
            remotePlayerResults = "Loser. Score ";
        }
        else
        {
            remotePlayerResults = "WINNER! Score ";
            localPlayerResults = "You Lost. Score ";
        }

        if (_matchResults.playerOneId == _playerId)
        {
            // our local player matches player one data
            localPlayerResults += _matchResults.playerOneScore;
            remotePlayerResults += _matchResults.playerTwoScore;
        }
        else
        {
            // our local player matches player two data
            localPlayerResults += _matchResults.playerTwoScore;
            remotePlayerResults += _matchResults.playerOneScore;
        }

        //Player1Result.text = localPlayerResults;
        //Player2Result.text = remotePlayerResults;
        Debug.Log("player1result: " + localPlayerResults + ". player2result: " + remotePlayerResults);
    }



    public static int GetAvailableUdpPort()
    {
        using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
        {
            socket.Bind(DefaultLoopbackEndpoint);
            return ((IPEndPoint)socket.LocalEndPoint).Port;
        }
    }

    void OnApplicationQuit()
    {
        // clean up the connection if the game gets killed
        if (_realTimeClient != null && _realTimeClient.IsConnected())
        {
            _realTimeClient.Disconnect();
        }
    }

    public void OnQuitPressed()
    {
        Debug.Log("OnQuitPressed");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

}


public class MatchStats
{
    //convert this to strings/ dictionaries? of the data the players have found?

    //public List<string> localPlayerCardsPlayed = new List<string>();
    //public List<string> remotePlayerCardsPlayed = new List<string>();
}

[System.Serializable]
public class FindMatch
{
    public string opCode;
    public string playerId;
    public FindMatch() { }
    public FindMatch(string opCodeIn, string playerIdIn)
    {
        this.opCode = opCodeIn;
        this.playerId = playerIdIn;
    }
}

[System.Serializable]
public class ConnectMessage
{
    public string playerConnected;
    public ConnectMessage() { }
    public ConnectMessage(string playerConnectedIn)
    {
        this.playerConnected = playerConnectedIn;
    }
}

[System.Serializable]
public class StartMatch
{
    public string remotePlayerId;
    public StartMatch() { }
    public StartMatch(string remotePlayerIdIn)
    {
        this.remotePlayerId = remotePlayerIdIn;
    }
}

[System.Serializable]
public class RealtimePayload
{
    public string playerId;


    // Other fields you wish to pass as payload to the realtime server

    public float playerXLocation;
    public float playerYLocation;
    public float playerZLocation;

    public string weaponGuess;
    public string suspectGuess;
    public string locationGuess;

    public RealtimePayload() { }
    public RealtimePayload(string playerIdIn)
    {
        this.playerId = playerIdIn;
    }

    public RealtimePayload(string playerId, float playerXLocation, float playerYLocation, float playerZLocation)
    {
        this.playerId = playerId;
        this.playerXLocation = playerXLocation;
        this.playerYLocation = playerYLocation;
        this.playerZLocation = playerZLocation;
    }

    public RealtimePayload(string playerId, string weaponGuess, string suspectGuess, string locationGuess)
    {
        this.playerId = playerId;
        this.weaponGuess = weaponGuess;
        this.suspectGuess = suspectGuess;
        this.locationGuess = locationGuess;

    }

}



[System.Serializable]
public class MatchResults
{
    public string playerOneId;
    public string playerTwoId;

    public string playerOneScore;
    public string playerTwoScore;

    public string winnerId;

    public MatchResults() { }
    public MatchResults(string playerOneIdIn, string playerTwoIdIn, string playerOneScoreIn, string playerTwoScoreIn, string winnerIdIn)
    {
        this.playerOneId = playerOneIdIn;
        this.playerTwoId = playerTwoIdIn;
        this.playerOneScore = playerOneScoreIn;
        this.playerTwoScore = playerTwoScoreIn;
        this.winnerId = winnerIdIn;
    }
}


public class PlayerMovementData
{

    public float playerXPosition;
    public float playerYPosition;
    public float playerZPosition;
    public string playerId;

}

public class PlayerGuessData
{

    public string weapon;
    public string suspect;
    public string location;
    public bool guessedOnTime;
    public string playerId;
}



