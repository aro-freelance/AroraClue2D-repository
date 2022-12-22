
using UnityEngine;
using TMPro;
using System.Net;
using System;
using Aws.GameLift.Realtime.Types;
using System.Threading.Tasks;
using System.Linq;
using Amazon.CognitoIdentity;
using Amazon;
using Amazon.Lambda.Model;
using Amazon.Lambda;
using System.Text;
using Newtonsoft.Json;
using System.Net.Sockets;
using System.Collections.Generic;
using Unity.VisualScripting;
using Aws.GameLift.Realtime;
using System.Collections;
using System.IO;
using System.Net.NetworkInformation;
using UnityEngine.Networking;
using System.Net.Http;

public class GameManager : MonoBehaviour
{

    public static GameManager Instance;

    string GameSessionPlacementEndpoint;

    //used to distiguish between the players in multiplayer game
    public int playerNumber;
    public OtherPlayer[] otherPlayers;


    //this creates multiple different bools at once.  cleaner than doing each separate. especially when they are all similar. 
    public bool gameMenuOpen, dialogueActive, cutsceneActive, fadingBetweenAreas, movementDisabled;

    
    public GameObject leadDetective;
    public GameObject timerObjectOutOfMenu;
    public GameObject timerObjectInMenu;

    public GameObject launchMenu;


    //the other players in the game
    public GameObject playerTwo;
    public GameObject playerThree;
    public GameObject playerFour;
    public GameObject playerFive;
    public GameObject playerSix;


    public int currentMoney;

    //count up from this value to the guessInterval.. to trigger a guess event (minutes)
    private float timer = 0;
    public bool timerRunning = false;

    //countdown from this value is the time allowed to the player to make a guess during the guess event (minutes) .5
    private float countdown = 0.5f;
    public bool countdownRunning = false;


    //how often should a guess event be triggered (minutes) 1
    public float guessInterval = 1f;

    public bool submittedAnswer = false;

    public bool isHost = false;

    //this is used by each player to say they are ready
    private bool ready = false; //TODO: set this to false

    //this is used by the host to determine if they can fire events
    private bool hostReady = true; //TODO: set this to false



    
    //private MatchResults _matchResults = new MatchResults();
    //private MatchStats _matchStats = new MatchStats();

    private static readonly IPEndPoint DefaultLoopbackEndpoint = new IPEndPoint(IPAddress.Loopback, port: 0);
    private SQSMessageProcessing _sqsMessageProcessing;
    private APIManager _apiManager;

    private RealTimeClient _realTimeClient;


    //after CS bools
    public bool isReadyToStartCountdown;
    public bool isReadyToCheckAnswers;
    public bool isReadyToEndGuessEvent;
    public bool isReadyToEndGame;

    public float defaultAutoplayDelay = 4; //seconds

    public bool isWinner = false;

    public string _playerId;
    private string _remotePlayerId = "";

    //TODO: figure out wtf this is for
    private bool _updateRemotePlayerId = false; 

    // turned on in the player controller when the player is moving.
    // used to send player location to server so it can be sent to other players
    public bool _movement = false;
    //used to prevent spam
    private float lastSentX;
    private float lastSentY;
    private float lastSentZ;
    //used to allow the first movement to be sent
    private bool _firstMovement = true;


    //gamestates
    public bool _findingMatch = false;
    public bool _hostFireMatch = false;
    public bool _prepGame = false;
    public bool _startGame = false;
    public bool _gameOver = false;

    public bool _startGuessEvent = false;
    public bool _startCountdown = false;
    public bool _endGuessEvent = false;
    public bool _resumeGameAfterGuessEvent = false;


    // GameLift server opcodes 
    public const int OP_CODE_PLAYER_ACCEPTED_S = 113;

    public const int OP_REQUEST_FIND_MATCH_S = 1;
    public const int OP_FIRE_MATCH_S = 2;
    public const int OP_PREP_GAME_S = 3;
    public const int OP_RESUME_GAME_S = 4;
    public const int OP_GAMEOVER_S = 5;
    public const int OP_START_GAME_S = 6;
    public const int OP_START_GUESS_EVENT_S = 7;
    public const int OP_START_COUNTDOWN_S = 8;
    public const int OP_END_GUESS_EVENT_S = 9;
    public const int OP_SET_PLAYER_INFO_S = 10;
    public const int OP_CHECK_ANSWERS_S = 11;
    public const int OP_PLAYER_MOVEMENT_S = 12;



    //messages player sends
    public const int OP_REQUEST_FIND_MATCH = 501;
    public const int OP_FIRE_MATCH = 502;
    public const int OP_PREP_GAME = 503;
    public const int OP_RESUME_GAME = 504;
    public const int OP_GAMEOVER = 505;
    public const int OP_START_GAME = 506;
    public const int OP_START_GUESS_EVENT = 507;
    public const int OP_START_COUNTDOWN = 508;
    public const int OP_END_GUESS_EVENT = 509;
    public const int OP_SET_PLAYER_INFO = 510;
    public const int OP_CHECK_ANSWERS = 511;
    public const int OP_PLAYER_MOVEMENT = 512;


    private Aws.GameLift.Realtime.Client _client;

    private Queue<Action> _mainThreadQueue = new Queue<Action>();



    // Start is called before the first frame update
    void Start()
    {

        Instance = this;
        DontDestroyOnLoad(gameObject);

        GameSessionPlacementEndpoint = PrivateConsts.instance.GameSessionPlacementEndpoint;

        _apiManager = FindObjectOfType<APIManager>();
        _sqsMessageProcessing = FindObjectOfType<SQSMessageProcessing>();


        _playerId = System.Guid.NewGuid().ToString();


        
        
    }

    async void HostFireMatch()
    {

        //TODO: tell the server that the players should resume the investigation
        GameEventToServer fireMatchServerMessage = new GameEventToServer("2", _playerId);

        string jsonData = JsonUtility.ToJson(fireMatchServerMessage);

        string response = await _apiManager.Post(GameSessionPlacementEndpoint, jsonData);

        GameEventToServer responseData = JsonConvert.DeserializeObject<GameEventToServer>(response);

        Debug.Log("firematchservermessage: response code " + responseData.opCode);

        //on dataReceived response fire prep game...

        //this will not run until after ready check
        _prepGame = true;


    }


    //this will be called from ondatareceived after host preps game
    void SetupAfterMatchFound()
    {

        launchMenu.SetActive(false);



        //TODO: use this to set the player sprites and names above head using ids

        //TODO: get the correct answer from server and use it to make lists in random
        //TODO: use those lists to spawn points

        //TODO: also get player number from the server.. this will be used for spawning 
        playerNumber = 0;


        //when loading is done
        ThisPlayerIsReadyToContinue();

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
            LocalProcesses();
        }
        
    }


    //this function is called on update if ready is true
    void ServerProcesses()
    {
        //all players will run these functions (host only functions below)
        
        //TODO: check this... or remove
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
                float minMovementToSendToServer = 2;

                //if first move send to server
                if (_firstMovement)
                {
                    _firstMovement = false;

                    SendMovement(x, y, z);
                }
                //or if enough movement is significant enough send to server
                else if (Math.Abs(lastSentX - x) > minMovementToSendToServer)
                {
                    SendMovement(x, y, z);

                }
                else if (Math.Abs(lastSentY - y) > minMovementToSendToServer)
                {
                    SendMovement(x, y, z);

                }

            }
            else
            {
                Debug.Log("movement, api is null");
            }
        }


        //Only called if the player is the host of the server
        if (isHost)
        {
            //this is handled by ready checks to all players through server
            if (hostReady)
            {
                if(_hostFireMatch)
                {
                    _hostFireMatch = false;

                    Debug.Log("_hostFireMatch");

                    HostFireMatch();
                }

                if (_prepGame)
                {
                    _prepGame = false;

                    Debug.Log("_prepGame");

                    HostPrepGame();

                }

                if (_startGame)
                {
                    _startGame = false;

                    Debug.Log("_startGame");

                    HostStartGame();

                }

                if (_startGuessEvent)
                {
                    _startGuessEvent = false;

                    Debug.Log("_startGuessEvent");

                    HostStartGuessEvent();

                }

                if (_startCountdown)
                {
                    _startCountdown = false;

                    Debug.Log("_startCountdown");

                    HostStartCountdown();
                }

                if (_endGuessEvent)
                {
                    _endGuessEvent = false;

                    Debug.Log("_endGuessEvent");

                    HostEndGuessEvent();
                }

                if (_resumeGameAfterGuessEvent)
                {
                    _resumeGameAfterGuessEvent = false;

                    Debug.Log("_resumeGameAfterGuessEvent");

                    HostResumeGameAfterGuessEvent();
                }

                if (_gameOver == true)
                {
                    _gameOver = false;

                    Debug.Log("_gameOver");

                    HostEndGame();
                }

            }
            
        }
        
    }

    void LocalProcesses()
    {


        if (isReadyToStartCountdown)
        {
            Debug.Log("ready to start countdown");
            ThisPlayerIsReadyToContinue();

            isReadyToStartCountdown = false;
        }

        //shouldn't need this 
        //if (isReadyToCheckAnswers)
        //{
        //    //TODO: check this... and remove?
        //    Debug.Log("ready to check answers");

        //    isReadyToCheckAnswers = false;
        //}

        if (isReadyToEndGuessEvent)
        {
            Debug.Log("ready to resume");
            ThisPlayerIsReadyToContinue();

            isReadyToEndGuessEvent = false;
        }

        if (isReadyToEndGame)
        {
            Debug.Log("ready to end game");
            EndGame();

            isReadyToEndGame = false;
        }

       

        if (timerRunning)
        {
            TimerUntilGuessEvent();
        }
        else
        {
            if (timer == 0)
            {
                timerRunning = true;
            }
        }

        if (countdownRunning)
        {

            CountdownDuringGuessEvent();

        }

    }

    
    void TimerUntilGuessEvent()
    {
        //only host should run the timers and trigger the events
        if (isHost)
        {
            if (timerRunning)
            {
                timer += (Time.deltaTime / 60);

                //this timer counts up until the endpoint of guessInterval
                float timeRemaining = guessInterval - timer;
                if (timeRemaining <= 0)
                {
                    HostStartGuessEvent();
                }

            }

        }
    }

    void CountdownDuringGuessEvent()
    {
        if (countdownRunning)
        {

            countdown -= (Time.deltaTime / 60);

            timerObjectInMenu.GetComponent<TMP_Text>().text = Mathf.Floor(countdown * 60).ToString() + " Seconds Remaining";
            timerObjectOutOfMenu.GetComponent<TMP_Text>().text = Mathf.Floor(countdown * 60).ToString() + " Seconds Remaining";


            //if this countdown hits 0, then fire the submitanswer method with empty strings
            if (countdown <= 0)
            {
                countdownRunning = false;

                submittedAnswer = false;
                SubmitAnswer("","","");
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


        //TODO: get the player id that sent the data and move them to that pos 
        //on the client's screen


        //playerTwo.transform.position
        //   = new Vector3(responseData.playerXPosition, responseData.playerYPosition, responseData.playerZPosition);



    }



    public void GuessEvent()
    {
        //move players (each player will be placed at spawn based on thier player number)
        LocalCallSpawnForGuessEvent();


        //TODO: test that this is working
        //spawn NPC (lead detective)
        if(GameObject.Find("leadDetective") == null)
        {
            Instantiate(leadDetective, new Vector3(0, 0), Quaternion.identity);
        }
        else
        {
            leadDetective.SetActive(true);
        }

        //show starting CS
        GuessEventStartCutscene();

    }

    

    public void EndGuessEvent()
    {
        leadDetective.SetActive(false);

        LocalResume();
    }


    //called from submit button or by countdown timer ending
    public void SubmitAnswer(string weapon, string suspect, string location)
    {
        timerObjectInMenu.SetActive(false);
        timerObjectOutOfMenu.SetActive(false);
        countdown = 0;

        if (!submittedAnswer)
        {
            NoAnswerSubmittedCutscene();

            //TODO: send server some data from this player to share

        }
        else
        {
            PlayerCheckGuess(weapon, suspect, location);
            
        }
    }


    //this will be called by the player when they end their own countdown timer...
    public async void PlayerCheckGuess(string weapon, string suspect, string location)
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

        //after they call server, they will be able to check if they won or are ready to keep playing

        submittedAnswer = false;

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



    //this will be called locally by the player after the host tells them to
    void LocalCallSpawnForGuessEvent()
    {
        fadingBetweenAreas = true;

        //handle spawn for local player
        HandleGuessEventSpawn(playerNumber);

        //handle spawn for other players
        for (int i = 0; i < otherPlayers.Length; i++)
        {
            int playerNum = otherPlayers[i].playerNumber;

            HandleGuessEventSpawn(playerNum);

        }

        fadingBetweenAreas = false;

    }


    //spawn the player based on their player number
    void HandleGuessEventSpawn(int pNum)
    {
        switch (pNum)
        {
            case 1:

                PlayerController.instance.transform.position = new Vector3(1, -3, transform.position.z);

                break;

            case 2:

                PlayerController.instance.transform.position = new Vector3(4, -3, transform.position.z);

                break;

            case 3:

                PlayerController.instance.transform.position = new Vector3(-1, -3, transform.position.z);

                break;

            case 4:

                PlayerController.instance.transform.position = new Vector3(-4, -3, transform.position.z);

                break;

            case 5:

                PlayerController.instance.transform.position = new Vector3(-4, -3, transform.position.z);

                break;

            case 6:

                PlayerController.instance.transform.position = new Vector3(-4, -3, transform.position.z);

                break;


            default:
                break;
        }

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
        hostReady = false;

        //TODO: data received "prepare the game" response from this will tell the players to run SetUIAfterMatchFound();
        SetupAfterMatchFound(); //TODO: remove this to datareceived

        //this will not run until the ready check completes
        _startGame = true;

    }

    async void HostStartGame()
    {

        GameEventToServer startGameServerMessage = new GameEventToServer("201", _playerId);

        string jsonData = JsonUtility.ToJson(startGameServerMessage);

        string response = await _apiManager.Post(GameSessionPlacementEndpoint, jsonData);

        GameEventToServer responseData = JsonConvert.DeserializeObject<GameEventToServer>(response);
        

        Debug.Log("hoststartgame: response code " + responseData.opCode);

        //this will have a datareceived response telling the players to all LocalResume and the host to StartTimer

        //TODO: remove
        if (isHost)
        {
            StartTimer();
        }
        LocalResume(); //TODO: remove

    }




    void StartTimer()
    {
        timerRunning = true;
    }

    void LocalResume()
    {
        //allow the local player to begin moving / load anything needed for the player to begin investigating
        movementDisabled = false;
        cutsceneActive = false;
        PlayerController.instance.canMove = true;

        //close the menu if it is open... TODO: test if this is annoying
        GameMenu.instance.CloseMenu();

    }

    async void HostStartGuessEvent()
    {

        //tell the server that the players should start the guess event
        GameEventToServer startGuessEventServerMessage = new GameEventToServer("202", _playerId);

        string jsonData = JsonUtility.ToJson(startGuessEventServerMessage);

        string response = await _apiManager.Post(GameSessionPlacementEndpoint, jsonData);

        GameEventToServer responseData = JsonConvert.DeserializeObject<GameEventToServer>(response);

        Debug.Log("hoststartguessevent: response code " + responseData.opCode);


        //this should send a dataReceived to tell each player to GuessEvent();

        GuessEvent(); //TODO remove

    }

    async void HostStartCountdown()
    {
        //TODO: call server and tell users to show timer / start the timer
        GameEventToServer startCountdownServerMessage = new GameEventToServer("901", _playerId);

        string jsonData = JsonUtility.ToJson(startCountdownServerMessage);

        string response = await _apiManager.Post(GameSessionPlacementEndpoint, jsonData);

        GameEventToServer responseData = JsonConvert.DeserializeObject<GameEventToServer>(response);

        Debug.Log("countdown start: response code " + responseData.opCode);


        //this should send a dataReceived to tell each player to LocalStartCountdown();

        LocalStartCountdown(); //TODO remove

    }

    void LocalStartCountdown()
    {
        //TODO: we probably want to just handle the countdown locally
        countdownRunning = true;
        //show the timer on the screen
        timerObjectInMenu.SetActive(true);
        timerObjectOutOfMenu.SetActive(true);

        //open menu and guess interface
        GameMenu.instance.ShowMenu();
        GameMenu.instance.guessWindow.SetActive(true);
        GameMenu.instance.guessButton.SetActive(true);
    }


    async void HostEndGuessEvent()
    {
        //TODO: tell the server that the players should end the guess event
        GameEventToServer endGuessEventServerMessage = new GameEventToServer("203", _playerId);

        string jsonData = JsonUtility.ToJson(endGuessEventServerMessage);

        string response = await _apiManager.Post(GameSessionPlacementEndpoint, jsonData);

        GameEventToServer responseData = JsonConvert.DeserializeObject<GameEventToServer>(response);

        Debug.Log("hostendguessevent: response code " + responseData.opCode);


        //this should send a dataReceived to tell each player to EndGuessResumeGameCutscene();

        EndGuessResumeGameCutscene(); //TODO remove

    }

    async void HostResumeGameAfterGuessEvent()
    {
        //TODO: tell the server that the players should resume the investigation
        GameEventToServer resumeGameAfterGuessServerMessage = new GameEventToServer("204", _playerId);

        string jsonData = JsonUtility.ToJson(resumeGameAfterGuessServerMessage);

        string response = await _apiManager.Post(GameSessionPlacementEndpoint, jsonData);

        GameEventToServer responseData = JsonConvert.DeserializeObject<GameEventToServer>(response);

        Debug.Log("resumeGameAfterGuessServerMessage: response code " + responseData.opCode);

        //ready check should be triggered by the dataReceived response code

        //this will not run until after ready check
        _resumeGameAfterGuessEvent = true;

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


    public void EndGame()
    {

        //TODO: end the game... (this is not the CS)..
        //this is for kicking the players out to possibly and endscreen/ the menu

        //destroy all game objects associated with the instance of the game
        Destroy(PlayerController.instance);
        Destroy(GameMenu.instance);
        Destroy(PlayerLoader.instance);
        Destroy(EssentialsLoader.instance);
        //TODO: for loop destroy other players
        //TODO: destroy interaction points?

        //show the main menu
        launchMenu.SetActive(true);

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
    /// 

    // calls our game service Lambda function to get connection info for the Realtime server


    private bool IsConnectedToServer = false;
    private const string DEFAULT_ENDPOINT = "127.0.0.1";
    private const int DEFAULT_TCP_PORT = 3001;
    private const int DEFAULT_UDP_PORT = 8921;

    // given a starting and ending range, finds an open UDP port to use as the listening port
    private int FindAvailableUDPPort(int firstPort, int lastPort)
    {
        var UDPEndPoints = IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners();
        List<int> usedPorts = new List<int>();
        usedPorts.AddRange(from n in UDPEndPoints where n.Port >= firstPort && n.Port <= lastPort select n.Port);
        usedPorts.Sort();
        for (int testPort = firstPort; testPort <= lastPort; ++testPort)

        {
            if (!usedPorts.Contains(testPort))
            {
                return testPort;
            }
        }
        return -1;
    }

    // common code whether we are connecting to a GameLift hosted server or
    // a local server
    private IEnumerator ConnectToServer(string ipAddr, int port, string tokenUID)
    {
        ClientLogger.LogHandler = (x) => Debug.Log(x);
        ConnectionToken token = new ConnectionToken(tokenUID, null);

        ClientConfiguration clientConfiguration = ClientConfiguration.Default();

        _client = new Aws.GameLift.Realtime.Client(clientConfiguration);
        //_client.ConnectionOpen += new EventHandler(OnOpenEvent);
        //_client.ConnectionClose += new EventHandler(OnCloseEvent);
        //_client.DataReceived += new EventHandler<DataReceivedEventArgs>(OnDataReceived);
        //_client.ConnectionError += new EventHandler<Aws.GameLift.Realtime.Event.ErrorEventArgs>(OnConnectionErrorEvent);

        int UDPListenPort = FindAvailableUDPPort(DEFAULT_UDP_PORT, DEFAULT_UDP_PORT + 20);
        if (UDPListenPort == -1)
        {
            Debug.Log("Unable to find an open UDP listen port");
            yield break;
        }
        else
        {
            Debug.Log($"UDP listening on port: {UDPListenPort}");
        }

        Debug.Log($"[client] Attempting to connect to server ip: {ipAddr} TCP port: {port} Player Session ID: {tokenUID}");
        _client.Connect(string.IsNullOrEmpty(ipAddr) ? DEFAULT_ENDPOINT : ipAddr, port, UDPListenPort, token);

        while (true)
        {
            if (_client.ConnectedAndReady)
            {
                IsConnectedToServer = true;
                Debug.Log("[client] Connected to server");
                break;
            }
            yield return null;
        }
    }

    private void ConnectToLocalServer()
    {
        var ipAddrPath = Application.dataPath + "/serverip.txt";
        var ipAddrReader = new StreamReader(ipAddrPath);
        var ipAddr = ipAddrReader.ReadToEnd();
        ipAddrReader.Close();

        // typically you'd use the player ID from an authentication system like GameLift,
        // however for our demo purposes, just pick a random unique ID
        StartCoroutine(ConnectToServer(ipAddr, DEFAULT_TCP_PORT, Guid.NewGuid().ToString()));
    }

    public void ActionConnectToServer(string ipAddr, int port, string tokenUID)
    {
        StartCoroutine(ConnectToServer(ipAddr, port, tokenUID));
    }

    // calls our game service Lambda function to get connection info for the Realtime server
    private async void ConnectToGameLiftServer()
    {
        Debug.Log("Reaching out to client service Lambda function");

        //AWSConfigs.AWSRegion = "us-east-1"; // Your region here
        //AWSConfigs.HttpClient = AWSConfigs.HttpClientOption.UnityWebRequest;
        // paste this in from the Amazon Cognito Identity Pool console
        
        CognitoAWSCredentials credentials = new CognitoAWSCredentials(
            PrivateConsts.instance.IdentityPool, // Your identity pool ID here
            RegionEndpoint.USEast1 // Your region here
        );

        AmazonLambdaClient client = new AmazonLambdaClient(credentials, RegionEndpoint.USEast1);


        InvokeRequest request = new InvokeRequest
        {
            FunctionName = "ConnectClientToServer",
            //Payload = "hello world",
            //InvocationType = "Event",
            InvocationType = InvocationType.RequestResponse,
            
            
        };



        InvokeResponse response = await client.InvokeAsync(request);


        if (response.FunctionError == null)
        {
            if (response.StatusCode == 200)
            {
                var payload = Encoding.ASCII.GetString(response.Payload.ToArray()) + "\n";
                PlayerSessionObject playerSessionObj = JsonUtility.FromJson<PlayerSessionObject>(payload);

                if (playerSessionObj.FleetId == null)
                {
                    Debug.Log($"Error in Lambda: {payload}");
                }
                else
                {
                    QForMainThread(ActionConnectToServer, playerSessionObj.IpAddress, int.Parse(playerSessionObj.Port), playerSessionObj.PlayerSessionId);
                }
            }
        }
        else
        {
            //might need better error message?
            Debug.LogError(response.FunctionError.ToString());
        }





    }




    public async void OnFindMatchPressed()
    { 

        Debug.Log("Find match pressed");
        _findingMatch = true;

        string name = LaunchMenu.instance.nameInputText.text;
        string spriteName = LaunchMenu.instance.selectedSpriteName;

        FindMatch matchMessage = new FindMatch("1", _playerId);
        string jsonPostData = JsonUtility.ToJson(matchMessage);


        //string response = await _apiManager.Post(GameSessionPlacementEndpoint, jsonPostData);

        

        




        //GameSessionPlacementInfo gameSessionPlacementInfo = JsonConvert.DeserializeObject<GameSessionPlacementInfo>(response);

        //if (gameSessionPlacementInfo != null)
        //{

        //    //if the player is looking for a game to join
        //    if (gameSessionPlacementInfo.PlacementId != null)
        //    {
        //        // The response was from a placement request
        //        Debug.Log("Game session placement request submitted.  placementId = " + gameSessionPlacementInfo.PlacementId);


        //        // subscribe to receive the player placement fulfillment notification
        //        await SubscribeToFulfillmentNotifications(gameSessionPlacementInfo.PlacementId);

        //    }
        //    //if the player has found a game to join
        //    else if (gameSessionPlacementInfo.GameSessionId != null)
        //    {
        //        // The response was for a found game session which also contains info for created player session
        //        Debug.Log("Game session found!");
        //        // Debug.Log(gameSessionPlacementInfo.GameSessionId);

        //        Int32.TryParse(gameSessionPlacementInfo.Port, out int portAsInt);



        //        // Once connected, the Realtime service moves the Player session from Reserved to Active, which means we're ready to connect.
        //        // https://docs.aws.amazon.com/gamelift/latest/apireference/API_CreatePlayerSession.html
        //        EstablishConnectionToRealtimeServer(gameSessionPlacementInfo.IpAddress, portAsInt,
        //            gameSessionPlacementInfo.PlayerSessionId);
        //    }
        //    else
        //    {
        //        Debug.Log("Game session response not valid...");
        //    }
        //}
        //else
        //{
        //    Debug.Log("Error: GAME SESSION PLACEMENT INFO is NULL");
        //}

        //_findMatchButton.gameObject.SetActive(false); // remove from UI
    }





    //TODO: @HERE this function is still giving a URI issue. It is trying to start a game session and then failing to with a uri error
    // afterwards if i try to connect the session has been created but this player didn't ever get notified and connect to it...
    // could still be onDataReceived or could be that the url is wrong. trouble shoot and fix.
    private async Task<bool> SubscribeToFulfillmentNotifications(string placementId)
    {
        Debug.Log("subscribe to fullfillment");

        //show UI to let player know something is loading
        LoadingMatchUI(true);

        PlayerPlacementFulfillmentInfo playerPlacementFulfillmentInfo = 
            await SQSMessageProcessing.instance.SubscribeToFulfillmentNotifications(placementId);

        //hide the UI and take the player to a waiting screen until ready for the match start (waiting for other players)
        LoadingMatchUI(false);

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

        //show UI to let player know something is loading
        LoadingMatchUI(true);

        _realTimeClient = new RealTimeClient(ipAddress, port, localUdpPort, playerSessionId, payload, ConnectionType.RT_OVER_WS_UDP_UNSECURED);

        if (_realTimeClient != null)
        {
            //hide the UI and take the player to a waiting screen until ready for the match start (waiting for other players)
            LoadingMatchUI(false);

            _realTimeClient.PlayerMovementEventHandler += OnPlayerMovementEvent;
            _realTimeClient.CheckAnswersEventHandler += OnCheckAnswersEvent;

            //TODO: add startgame handler

            _realTimeClient.RemotePlayerIdEventHandler += OnRemotePlayerIdEvent;

            //_realTimeClient.GameOverEventHandler += OnGameOverEvent;

            Debug.Log("realtimeclient: " + _realTimeClient);

            SendPlayerInfo();

            //we should turn this on... but it will not run until ready to true.. therefore we can
            //add a ready make sure that all the players have joined? before we fire this off in Update.
            _prepGame = true;

        }
        else
        
            Debug.Log("realtimeclient: null");
    }

    public void LoadingMatchUI(bool isLoadingMatch)
    {
        if(isLoadingMatch)
        {
            LaunchMenu.instance.statusUpdateObject.SetActive(true);
            LaunchMenu.instance.statusUpdateText.text = "Loading Match....";
        }
        else
        {
            LaunchMenu.instance.statusUpdateText.text = "Waiting for other players.";

            // need to be careful with "ready" because if host then ... host needs a hostReady ..
            ready = true;
            ThisPlayerIsReadyToContinue();
        }

    }


    async void SendPlayerInfo()
    {
        //determine if player is host by asking server.. if they are isHost = true

        //at this point the player number will be set... so also send the name and sprite name
        string name = LaunchMenu.instance.nameInputText.text;
        string spriteName = LaunchMenu.instance.selectedSpriteName;

        SetPlayerInfo setPlayerInfo = new SetPlayerInfo("199", _playerId, name, spriteName, 0);

        string jsonData = JsonUtility.ToJson(setPlayerInfo);


        string response = await _apiManager.Post(GameSessionPlacementEndpoint, jsonData);


        SetPlayerInfo responseData = JsonConvert.DeserializeObject<SetPlayerInfo>(response);

        //TODO: on data received this needs to set the host if the player number = 1

        
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

    //void OnGameOverEvent(object sender, GameOverEventArgs gameOverEventArgs)
    //{
    //    Debug.Log($"Game over event received with winner: {gameOverEventArgs.matchResults.winnerId}.");
    //    this._matchResults = gameOverEventArgs.matchResults;
    //    this._gameOver = true;
    //}






    public static int GetAvailableUdpPort()
    {
        using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
        {
            socket.Bind(DefaultLoopbackEndpoint);
            return ((IPEndPoint)socket.LocalEndPoint).Port;
        }
    }

    public void OnApplicationQuit()
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




    

    private void QForMainThread(Action fn)
    {
        lock (_mainThreadQueue)
        {
            _mainThreadQueue.Enqueue(() => { fn(); });
        }
    }

    private void QForMainThread<T1>(Action<T1> fn, T1 p1)
    {
        lock (_mainThreadQueue)
        {
            _mainThreadQueue.Enqueue(() => { fn(p1); });
        }
    }

    private void QForMainThread<T1, T2>(Action<T1, T2> fn, T1 p1, T2 p2)
    {
        lock (_mainThreadQueue)
        {
            _mainThreadQueue.Enqueue(() => { fn(p1, p2); });
        }
    }

    private void QForMainThread<T1, T2, T3>(Action<T1, T2, T3> fn, T1 p1, T2 p2, T3 p3)
    {
        lock (_mainThreadQueue)
        {
            _mainThreadQueue.Enqueue(() => { fn(p1, p2, p3); });
        }
    }


    private void RunMainThreadQueueActions()
    {
        // as our server messages come in on their own thread
        // we need to queue them up and run them on the main thread
        // when the methods need to interact with Unity
        lock (_mainThreadQueue)
        {
            while (_mainThreadQueue.Count > 0)
            {
                _mainThreadQueue.Dequeue().Invoke();
            }
        }
    }





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

[Serializable]
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

[Serializable]
public class SetPlayerInfo
{
    public string opCode;
    public string playerId;
    public string playerName;
    public string spriteName;
    public int playerNumber;

    public SetPlayerInfo() { }

    public SetPlayerInfo(string opCode, string playerId, string playerName, string spriteName, int playerNumber)
    {
        this.opCode = opCode;
        this.playerId = playerId;
        this.playerName = playerName;
        this.spriteName = spriteName;
        this.playerNumber = playerNumber;
    }
}

// This data structure is returned by the client service when a game match is found
[System.Serializable]
public class PlayerSessionObject
{
    public string PlayerSessionId;
    public string PlayerId;
    public string GameSessionId;
    public string FleetId;
    public string CreationTime;
    public string Status;
    public string IpAddress;
    public string Port;
}



