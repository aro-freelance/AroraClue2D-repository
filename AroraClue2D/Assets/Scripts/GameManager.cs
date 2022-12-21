
using UnityEngine;
using TMPro;
using System.Net;
using Newtonsoft.Json;
using System;
using Aws.GameLift.Realtime.Types;
using System.Net.Sockets;
using System.Threading.Tasks;
using Aws;
using Amazon;
using Aws.GameLift.Realtime.Network;
using AWSSDK;
using ThirdParty.Json.LitJson;
using static UnityEditor.FilePathAttribute;


public class GameManager : MonoBehaviour
{

    public static GameManager Instance;

    string GameSessionPlacementEndpoint;

    //used to distiguish between the players in multiplayer game
    public int playerNumber;

    //this creates multiple different bools at once.  cleaner than doing each separate. especially when they are all similar. 
    public bool gameMenuOpen, dialogueActive, cutsceneActive, fadingBetweenAreas, movementDisabled;

    public string[] itemsInInventory;
    public int[] numberOfEachItem;
    public Item[] referenceItems;
    public GameObject leadDetective;
    public GameObject timerObjectOutOfMenu;
    public GameObject timerObjectInMenu;


    //the other players in the game
    public GameObject playerTwo;
    public GameObject playerThree;
    public GameObject playerFour;
    public GameObject playerFive;
    public GameObject playerSix;


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

    //this is used by the host to determine if they can fire events
    private bool ready = true; //TODO: set this to false



    
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


    //these are being used
    public const int CHECK_ANSWERS = 305;
    public const int PLAYER_MOVEMENT = 900;
    public const int PLAYER_MOVEMENT_RECEIVED = 901;

    public const int GET_HOST = 199;
    public const int START_GAME = 201;
    public const int START_GUESS_EVENT = 202;
    public const int END_GUESS_EVENT = 203;
    public const int GAMEOVER = 209;

    // Lambda opcodes
    private const string REQUEST_FIND_MATCH_OP = "1";


    private float lastSentX;
    private float lastSentY;
    private float lastSentZ;



    // Start is called before the first frame update
    void Start()
    {

        Instance = this;
        DontDestroyOnLoad(gameObject);

        GameSessionPlacementEndpoint = PrivateConsts.instance.GameSessionPlacementEndpoint;

        _apiManager = FindObjectOfType<APIManager>();
        _sqsMessageProcessing = FindObjectOfType<SQSMessageProcessing>();


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
        if (gameMenuOpen || dialogueActive || fadingBetweenAreas || cutsceneActive || movementDisabled)
        {
            PlayerController.instance.canMove = false;
        }
        else
        {
            PlayerController.instance.canMove = true;
        }

        ServerProcesses();

        if (ready)
        {
            RunGuessSystemChecksAndTimers();
        }
        
    }

    async void ServerProcesses()
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

            if (_apiManager != null)
            {
                float x = PlayerController.instance.transform.position.x;
                float y = PlayerController.instance.transform.position.y;
                float z = PlayerController.instance.transform.position.z;

                //send to server if requirements met

                if (lastSentX == null && lastSentY == null && lastSentZ == lastSentZ == null)
                {
                    SendMovement(x, y, z);
                }
                else if (Math.Abs(lastSentX - x) > 1)
                {
                    SendMovement(x, y, z);

                }
                else if (Math.Abs(lastSentY - y) > 1)
                {
                    SendMovement(x, y, z);

                }

            }
            else
            {
                Debug.Log("movement, api is null");
            }
        }


        if (isHost)
        {
            if (_prepGame)
            {
                _prepGame = false;

                Debug.Log("start game bool call in GameManager");

                HostPrepGame();

            }

            if (_startGame)
            {
                if (ready)
                {
                    _startGame = false;

                    HostStartGame();


                }

            }

            // determine match results once game is over
            if (_gameOver == true)
            {
                _gameOver = false;
                HostEndGame();
            }
        }
        
    }

    async void SendMovement(float x, float y, float z)
    {

        Debug.Log("pos : " + x + "," + y + "," + z);

        PlayerMovementData playerMovementData = new PlayerMovementData("900", x, y, z, _playerId);

        string jsonData = JsonUtility.ToJson(playerMovementData);

        lastSentX = x;
        lastSentY = y;
        lastSentZ = z;

        string response = await _apiManager.Post(GameSessionPlacementEndpoint, jsonData);

        //TODO: use response to set the position of player by id
        //example using gameSession
        //GameSessionPlacementInfo gameSessionPlacementInfo = JsonConvert.DeserializeObject<GameSessionPlacementInfo>(response);

        //example get player 2 game object. move it to position x , y , z with the data obtained from server.


        PlayerMovementData responseData = JsonConvert.DeserializeObject<PlayerMovementData>(response);

        Debug.Log("reponse movement data x = " + responseData.playerXPosition);


        //TODO: instead of popping the npc and moving them,  get the player id that sent the data and move them to that pos 
        //on the client's screen
        playerTwo.transform.position
                = new Vector3(responseData.playerXPosition, responseData.playerYPosition, responseData.playerZPosition);



    }

    void SetUIAfterMatchFound()
    {

        //TODO: use this to set the player sprites and names above head using ids


        //when loading is done
        ThisPlayerIsReadyToContinue();

    }


    //TODO: clean up redundance btwn this and the serverprocesses
    //might need to keep these because they run the CS stuff...
    //(note these are being used in DialogueManager switch in nextline autoplay)
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


        GameEventToServer triggerGuessMessage = new GameEventToServer("202", _playerId);

        string jsonData = JsonUtility.ToJson(triggerGuessMessage);

        //we only need to trigger the response here...
        //after that each player will need to receive the data about the event
        //when that data is received we should call guess event


    }

    public void GuessEvent()
    {
        //move player to location (each player will be placed at spawn based on thier player number)
        SpawnPlayerAtGuessEvent(playerNumber);

        //spawn NPC (lead detective)
        Instantiate(leadDetective, new Vector3(0, 0), Quaternion.identity);

        //show starting CS
        GuessEventStartCutscene();

    }

    public async void CheckGuess(string weapon, string suspect, string location)
    {

        PlayerGuessData playerGuessData = new PlayerGuessData("305", weapon, suspect, location, submittedAnswer, _playerId);


        string jsonData = JsonUtility.ToJson(playerGuessData);


        string response = await _apiManager.Post(GameSessionPlacementEndpoint, jsonData);

        
        AnswerCheckResponse answerCheckResponse = JsonConvert.DeserializeObject<AnswerCheckResponse>(response);

        bool isWeapon = answerCheckResponse.isWeaponCorrect;
        bool isSuspect = answerCheckResponse.isSuspectCorrect;
        bool isLocation = answerCheckResponse.isLocationCorrect;

        if(isWeapon)
        {
            Debug.Log("weapon correct. " + weapon);

        }
        else
        {
            Debug.Log("weapon incorrect. " + weapon);
        }

        if (isSuspect)
        {
            Debug.Log("suspect correct. " + suspect);

        }
        else
        {
            Debug.Log("suspect incorrect. " + suspect);
        }

        if (isLocation)
        {
            Debug.Log("location correct. " + location);

        }
        else
        {
            Debug.Log("location incorrect. " + location);
        }

        if(isWeapon && isSuspect && isLocation)
        {
            ThisPlayerHasCorrectAnswer();
        }
        else
        {
            ThisPlayerIsReadyToContinue();
        }



        submittedAnswer = true;

    }

    async void ThisPlayerHasCorrectAnswer()
    {

        GameEventToServer thisPlayerWon = new GameEventToServer("209", _playerId);
        
        string jsonData = JsonUtility.ToJson(thisPlayerWon);


        string response = await _apiManager.Post(GameSessionPlacementEndpoint, jsonData);

        //when players recieve this message they will run the method to end the game

    }

    async void ThisPlayerIsReadyToContinue()
    {

        GameEventToServer thisPlayerReady = new GameEventToServer("411", _playerId);

        string jsonData = JsonUtility.ToJson(thisPlayerReady);


        string response = await _apiManager.Post(GameSessionPlacementEndpoint, jsonData);

        //when host receives message that all players are ready they will set bool ready to true which will allow
        // the next event to fire.

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

    async void HostPrepGame()
    {
        //have the host roll the random stuff, and then send it to the server...
        RandomGameElementsManager.instance.RandomizeNewGame();

        NewGameData newGameData = new NewGameData("210", 
            RandomGameElementsManager.instance.selectedWeapon,
            RandomGameElementsManager.instance.selectedSuspect,
            RandomGameElementsManager.instance.selectedPlace
            );

        string jsonData = JsonUtility.ToJson(newGameData);

        await _apiManager.Post(GameSessionPlacementEndpoint, jsonData);


        //we need to do a ready check before we continue. server will set ready to true when ready check is done
        ready = false;

        //TODO: data received "prepare the game" response from this will tell the players to run SetUIAfterMatchFound();


        //this will not run until the ready check completes
        _startGame = true;

    }

    async void HostStartGame()
    {

        GameEventToServer startGameServerMessage = new GameEventToServer("201", _playerId);

        string jsonData = JsonUtility.ToJson(startGameServerMessage);

        await _apiManager.Post(GameSessionPlacementEndpoint, jsonData);

        //this will have a datareceived response telling the players to all LocalResume and the host to StartTimer
    }




    void StartTimer()
    {
        timerIsRunning = true;
    }

    void LocalResume()
    {
        //allow the local player to begin moving / load anything needed for the player to begin investigating
        movementDisabled = false;

    }

    async void HostEndGame()
    {

        GameEventToServer endGameServerMessage = new GameEventToServer("209", _playerId);

        string jsonData = JsonUtility.ToJson(endGameServerMessage);

        string response = await _apiManager.Post(GameSessionPlacementEndpoint, jsonData);

        //use this format to get the reponse from the server
        //PlayerMovementData responseData = JsonConvert.DeserializeObject<PlayerMovementData>(response);

        //TODO: use the response to get the name of the winner.. then run the winner cutscene (in onDataReceived)
        //on the server end tell the server connection to end in like 1 minute.


    }


    void EndGame()
    {

        //TODO: end the game... (this is not the CS)..
        //this is for kicking the players out to possibly and endscreen/ the menu

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

        //after this plays the bool for ending the game will be turned on 

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
        Debug.Log("subscribe to fullfillment");

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

            _realTimeClient.PlayerMovementEventHandler += OnPlayerMovementEvent;
            _realTimeClient.CheckAnswersEventHandler += OnCheckAnswersEvent;

            //TODO: add startgame handler

            _realTimeClient.RemotePlayerIdEventHandler += OnRemotePlayerIdEvent;
            _realTimeClient.GameOverEventHandler += OnGameOverEvent;

            Debug.Log("realtimeclient: " + _realTimeClient);

            CheckIfHost();

        }
        else
        
            Debug.Log("realtimeclient: null");
    }


    async void CheckIfHost()
    {
        //determine if player is host by asking server.. if they are isHost = true


        GameEventToServer getHost = new GameEventToServer("199", _playerId);

        string jsonData = JsonUtility.ToJson(getHost);


        string response = await _apiManager.Post(GameSessionPlacementEndpoint, jsonData);


        string responseData = JsonConvert.DeserializeObject<string>(response);

        if (responseData != null)
        {
            if (responseData == "host")
            {
                Debug.Log("you are the host");
                isHost = true;
            }
            else
            {
                //this should be an empty string if not the host
                Debug.Log("you are not the host. reponseData = " + responseData);
            }
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
public class GameEventToServer
{
    public string opCode;
    public string playerId;
    public GameEventToServer() { }
    public GameEventToServer(string opCodeIn, string playerIdIn)
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

[Serializable]
public class PlayerMovementData
{
    public string opCode;
    public float playerXPosition;
    public float playerYPosition;
    public float playerZPosition;
    public string playerId;
   

    public PlayerMovementData() { }

    public PlayerMovementData(string opCode, float playerXPosition, float playerYPosition, float playerZPosition, string playerId)
    {
        this.opCode = opCode;
        this.playerXPosition = playerXPosition;
        this.playerYPosition = playerYPosition;
        this.playerZPosition = playerZPosition;
        this.playerId = playerId;

    }

}

[Serializable]
public class PlayerGuessData
{
    public string opCode;
    public string weapon;
    public string suspect;
    public string place;
    public bool guessedOnTime;
    public string playerId;

    public PlayerGuessData() { }
    
    public PlayerGuessData(string opCode, string weapon, string suspect, string place, bool guessedOnTime, string playerId)
    {
        this.opCode = opCode;
        this.weapon = weapon;
        this.suspect = suspect;
        this.place = place;
        this.guessedOnTime= guessedOnTime;
        this.playerId = playerId;

    }

}

[Serializable]
public class AnswerCheckResponse
{

    public bool isWeaponCorrect;
    public bool isSuspectCorrect;
    public bool isLocationCorrect;

    public AnswerCheckResponse() { }

    public AnswerCheckResponse(bool isWeaponCorrect, bool isSuspectCorrect, bool isLocationCorrect)
    {
        this.isWeaponCorrect = isWeaponCorrect;
        this.isSuspectCorrect = isSuspectCorrect;
        this.isLocationCorrect = isLocationCorrect;
    }


}


public class NewGameData
{

    public string opCode;
    public string weapon;
    public string suspect;
    public string place;

    public NewGameData() { }

    public NewGameData(string opCode, string weapon, string suspect,string place)
    {

        this.opCode = opCode;
        this.weapon = weapon;
        this.suspect= suspect;
        this.place = place;

    }


}



