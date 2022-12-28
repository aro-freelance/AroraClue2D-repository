
using UnityEngine;
using TMPro;
using System.Net;
using Newtonsoft.Json;
using System;
using Aws.GameLift.Realtime.Types;
using System.Net.Sockets;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

public class GameManager : MonoBehaviour
{

    public static GameManager Instance;

    string GameSessionPlacementEndpoint;

    //used to distiguish between the players in multiplayer game
    public int playerNumber;
    public PlayerData[] playerDataList;


    //this creates multiple different bools at once.  cleaner than doing each separate. especially when they are all similar. 
    public bool gameMenuOpen, dialogueActive, cutsceneActive, fadingBetweenAreas, movementDisabled;

    
    public GameObject leadDetective;
    public GameObject timerObjectOutOfMenu;
    public GameObject timerObjectInMenu;

    public GameObject launchMenu;


    //the other players in the game (in SetupAfterMatchFound these will be instantiate except for the one for the local player's number)
    public GameObject playerOneObject;
    public GameObject playerTwoObject;
    public GameObject playerThreeObject;
    public GameObject playerFourObject;
    public GameObject playerFiveObject;
    public GameObject playerSixObject;

    private Vector3 playerOneSpawnLocation = new Vector3(0, 0); // front center
    private Vector3 playerTwoSpawnLocation = new Vector3(1, 0); // front left
    private Vector3 playerThreeSpawnLocation = new Vector3(-1, 0); // front right
    private Vector3 playerFourSpawnLocation = new Vector3(0, -1); // back center
    private Vector3 playerFiveSpawnLocation = new Vector3(1, -1); // back left
    private Vector3 playerSixSpawnLocation = new Vector3(-1, -1); // back right



    public int currentMoney;

    //count up from this value to the guessInterval.. to trigger a guess event (minutes)
    private float timer = 0;
    public bool timerRunning = false;

    //countdown from this value is the time allowed to the player to make a guess during the guess event (minutes) .5
    private float countdown = 0.5f;
    public bool countdownRunning = false;


    //how often should a guess event be triggered (minutes) 1
    public float guessInterval = 1f;

    //default true, if the user doesn't submit the answer in time this is set to false in the countdown method
    public bool submittedAnswer = true;

    public bool isHost = false;

    ////this is used by each player to say they are ready
    //private bool ready = false; //TODO: set this to false

    ////this is used by the host to determine if they can fire events
    //private bool hostReady = true; //TODO: set this to false



    
    //private MatchResults _matchResults = new MatchResults();
    //private MatchStats _matchStats = new MatchStats();

    private static readonly IPEndPoint DefaultLoopbackEndpoint = new IPEndPoint(IPAddress.Loopback, port: 0);
    private SQSMessageProcessing _sqsMessageProcessing;
    private RealTimeClient _realTimeClient;
    private APIManager _apiManager;

    //after CS bools
    public bool isReadyToStartCountdown;
    public bool isReadyToCheckAnswers;
    public bool isReadyToEndGuessEvent;
    public bool isReadyToEndGame;

    public float defaultAutoplayDelay = 4; //seconds

    public bool isWinner = false;

    private string _playerId;
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
    private bool _findingMatch = false;
    private bool _prepGame = false;
    private bool _startGame = false;
    private bool _gameOver = false;

    private bool _startGuessEvent = false;
    private bool _startCountdown = false;
    private bool _endGuessEvent = false;
    private bool _resumeGameAfterGuessEvent = false;


    //messages server sends
    public const int OP_CODE_PLAYER_ACCEPTED = 113;
    public const int OP_CODE_DISCONNECT_NOTIFICATION = 114;

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
    public const int OP_HOLD_AFTER_GUESS_CHECKED_S = 13;


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


        _playerId = System.Guid.NewGuid().ToString();




    }


    //this will be called from ondatareceived OP_FIRE_MATCH_S call.
    //@Yelsa Step 6
    public void SetupAfterMatchFound(GamePrepData gamePrepData)
    {
        ///1. Set the Sprites
        //set player data list from GamePrepData received
        playerDataList = gamePrepData.playerDataList;

        //use playerDataList to setup the sprites for the other players
        //loop through all players
        for (int i = 0; i < playerDataList.Length; i++)
        {
            var player = playerDataList[i];

            //based on the player number Instantiate and set local player number
            switch (player.PlayerNumber)
            {
                //note I have set player number to start with 1 not 0
                case 1:

                    //the player id is not the same as the local player
                    if(_playerId != player.PlayerId)
                    {
                        SetSprite(playerOneObject, player.PlayerSpriteName);

                        //TODO: add a name over the player's head and set it's string here

                        Instantiate(playerOneObject, playerOneSpawnLocation, Quaternion.identity);
                    }
                    //if the local player is player one don't instantiate a sprite, but do set them as the host
                    else
                    {
                        isHost = true;
                       
                    }

                    break;

                case 2:

                    //the player id is not the same as the local player, so instantiate a sprite for the other player
                    if (_playerId != player.PlayerId)
                    {
                        SetSprite(playerTwoObject, player.PlayerSpriteName);

                        //TODO: add a name over the player's head and set it's string here

                        Instantiate(playerTwoObject, playerTwoSpawnLocation, Quaternion.identity);
                    }


                    break;

                case 3:

                    //the player id is not the same as the local player, so instantiate a sprite for the other player
                    if (_playerId != player.PlayerId)
                    {
                        SetSprite(playerThreeObject, player.PlayerSpriteName);

                        //TODO: add a name over the player's head and set it's string here

                        Instantiate(playerThreeObject, playerThreeSpawnLocation, Quaternion.identity);
                    }


                    break;

                case 4:

                    //the player id is not the same as the local player, so instantiate a sprite for the other player
                    if (_playerId != player.PlayerId)
                    {
                        SetSprite(playerFourObject, player.PlayerSpriteName);

                        //TODO: add a name over the player's head and set it's string here

                        Instantiate(playerFourObject, playerFourSpawnLocation, Quaternion.identity);
                    }


                    break;

                case 5:

                    //the player id is not the same as the local player, so instantiate a sprite for the other player
                    if (_playerId != player.PlayerId)
                    {
                        SetSprite(playerFiveObject, player.PlayerSpriteName);

                        //TODO: add a name over the player's head and set it's string here

                        Instantiate(playerFiveObject, playerFiveSpawnLocation, Quaternion.identity);
                    }


                    break;

                case 6:

                    //the player id is not the same as the local player, so instantiate a sprite for the other player
                    if (_playerId != player.PlayerId)
                    {
                        SetSprite(playerSixObject, player.PlayerSpriteName);

                        //TODO: add a name over the player's head and set it's string here

                        Instantiate(playerSixObject, playerSixSpawnLocation, Quaternion.identity);
                    }


                    break;

                default:
                    break;

            }
        }

        ///2. Set the lists of unused weapon, suspect, location using the selected answer from the server
      
        //get the correct answer from server
        string selectedWeapon = gamePrepData.selectedWeapon;
        string selectedSuspect = gamePrepData.selectedSuspect;
        string selectedPlace = gamePrepData.selectedPlace;

        //use the answer to set the lists
        RandomGameElementsManager.instance.PrepareLists(selectedWeapon, selectedSuspect, selectedPlace);



        ///3. Spawn interaction points using the lists of unused "cards"

        SpawnInteractionPoints(RandomGameElementsManager.instance.unusedWeapons,
            RandomGameElementsManager.instance.unusedSuspects, RandomGameElementsManager.instance.unusedPlaces);

        //launchMenu.SetActive(false);

        ///4. Tell the server the player is ready to start
        ThisPlayerIsReadyToStart();

    }

    public void SetSprite(GameObject playerObject, string spriteName)
    {

        switch (spriteName)
        {
            case "playerA":

                playerObject.GetComponent<SpriteRenderer>().sprite = LaunchMenu.instance.playerASprite;

                break;

            case "playerB":

                playerObject.GetComponent<SpriteRenderer>().sprite = LaunchMenu.instance.playerBSprite;

                break;


            default:
                break;
        }

    }

    void SpawnInteractionPoints(string[] unusedWeapons, string[] unusedSuspects, string[] unusedPlaces)
    {
        //TODO: use these lists to spawn the interaction points
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

        LocalProcesses();


    }


    //this function is called on update if ready is true
    void ServerProcesses()
    {
        //all players will run these functions (host only functions below)
        
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

        
        // the player is moving, send data to server to display to other players
        if (_movement)
        {
            _movement = false;

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

        
    }

    void LocalProcesses()
    {
        //@Yelsa Step 11 after the start guess cutscene ends this bool will turn on and start the countdown locally
        if (isReadyToStartCountdown)
        {
            isReadyToStartCountdown = false;

            LocalStartCountdown();

        }


        //@Yelsa Step 14a after the end guess cutscene ends this bool will turn on
        if (isReadyToEndGuessEvent)
        {
            Debug.Log("ready to resume");
            ThisPlayerIsReadyToContinueAfterGuessEvent();

            isReadyToEndGuessEvent = false;
        }

        //this will turn on after the end game cutscene ends
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

        //when the local countdown is running this will be turned on
        if (countdownRunning)
        {

            CountdownDuringGuessEvent();

        }

    }

    
    public void TimerUntilGuessEvent()
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
                    //@Yelsa Step 9
                    HostStartGuessEvent();
                }

            }

        }
    }

    void CountdownDuringGuessEvent()
    {
        countdown -= (Time.deltaTime / 60);

        timerObjectInMenu.GetComponent<TMP_Text>().text = Mathf.Floor(countdown * 60).ToString() + " Seconds Remaining";
        timerObjectOutOfMenu.GetComponent<TMP_Text>().text = Mathf.Floor(countdown * 60).ToString() + " Seconds Remaining";


        //if this countdown hits 0, then fire the submitanswer method with empty strings
        if (countdown <= 0)
        {
            countdownRunning = false;

            submittedAnswer = false;
            //@Yelsa Step 12 (countdown ended before answer submitted)
            SubmitAnswer("", "", "");
        }
    }


    //@Yelsa Step 17
    private void SendMovement(float x, float y, float z)
    {
        Debug.Log("pos : " + x + "," + y + "," + z);
        
        //send the movement to the server
        RealtimePayload realtimePayload = new RealtimePayload(_playerId, x, y, z);
        _realTimeClient.SendMessage(OP_PLAYER_MOVEMENT, realtimePayload);

        //use this to prevent spam by requiring a threshold of change from last sent to send again
        lastSentX = x;
        lastSentY = y;
        lastSentZ = z;


    }



    public void GuessEvent()
    {
        //move players (each player will be placed at spawn based on thier player number)
        LocalCallSpawnForGuessEvent();

        //TODO: test if this is the best way
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
    //@Yelsa Step 12
    public void SubmitAnswer(string weapon, string suspect, string location)
    {
        timerObjectInMenu.SetActive(false);
        timerObjectOutOfMenu.SetActive(false);
        countdown = 0;

      
        //send the server the player's answer
        RealtimePayload realtimePayload = new RealtimePayload(_playerId, weapon, suspect, location, submittedAnswer);

        _realTimeClient.SendMessage(OP_CHECK_ANSWERS, realtimePayload);


        //reset this bool to default after sending to server
        submittedAnswer = true;


    }

    //@Yelsa TODO: make the UI for this and then turn it on and off here
    public void HoldForOtherPlayersAnswersUI(bool isOn)
    {

        if (isOn)
        {
            //Turn on the UI to show message to user that they are waiting for other users to answer
        }
        else
        {
            //turn off the ui
        }


    }




    //call this in onDataReceived if the eerver returns that the player has the correct answer
    void ThisPlayerHasCorrectAnswer()
    {

        //GameEventToServer thisPlayerWon = new GameEventToServer("209", _playerId);
        
        //string jsonData = JsonUtility.ToJson(thisPlayerWon);


        //string response = await _apiManager.Post(GameSessionPlacementEndpoint, jsonData);

        RealtimePayload realtimePayload = new RealtimePayload(_playerId);

        _realTimeClient.SendMessage(OP_GAMEOVER, realtimePayload);

        //when players recieve this message they will run the method to end the game

    }

    //@Yelsa Step 14a
    void ThisPlayerIsReadyToContinueAfterGuessEvent()
    {

        //the player finished the end guess event cutscene
        //send the server a message that this player is ready to resume the investigation phase
        RealtimePayload realtimePayload = new RealtimePayload(_playerId);

        _realTimeClient.SendMessage(OP_RESUME_GAME, realtimePayload);


    }


    //TODO: @Yelsa fix this... it is still in use
    //this will be called locally by the player after the host tells them to
    void LocalCallSpawnForGuessEvent()
    {
        fadingBetweenAreas = true;

        //spawn each player on the player list
        for (int i = 0; i < playerDataList.Length; i++)
        {
            int playerNum = playerDataList[i].PlayerNumber;

            HandleGuessEventSpawn(playerNum);

        }

        fadingBetweenAreas = false;

    }


    //spawn the player based on their player number
    void HandleGuessEventSpawn(int pNum)
    {
        var isLocalPlayer = false;
        if(pNum == playerNumber)
        {
            isLocalPlayer = true;
        }

        switch (pNum)
        {
            case 1:

                if (isLocalPlayer)
                {
                    PlayerController.instance.transform.position = new Vector3(1, -3, transform.position.z);
                }
                else
                {
                    playerOneObject.transform.position = new Vector3(1, -3, transform.position.z);
                }

                break;

            case 2:
                if (isLocalPlayer)
                {
                    PlayerController.instance.transform.position = new Vector3(4, -3, transform.position.z);
                }
                else
                {
                    playerOneObject.transform.position = new Vector3(4, -3, transform.position.z);
                }

                break;

            case 3:

                if (isLocalPlayer)
                {
                    PlayerController.instance.transform.position = new Vector3(-1, -3, transform.position.z);
                }
                else
                {
                    playerOneObject.transform.position = new Vector3(-1, -3, transform.position.z);
                }

                break;

            case 4:

                if (isLocalPlayer)
                {
                    PlayerController.instance.transform.position = new Vector3(-4, -3, transform.position.z);
                }
                else
                {
                    playerOneObject.transform.position = new Vector3(-4, -3, transform.position.z);
                }

                break;

            case 5:
                if (isLocalPlayer)
                {
                    PlayerController.instance.transform.position = new Vector3(-4, -3, transform.position.z);
                }
                else
                {
                    playerOneObject.transform.position = new Vector3(-4, -3, transform.position.z);
                }

                break;

            case 6:
                if (isLocalPlayer)
                {
                    PlayerController.instance.transform.position = new Vector3(-4, -3, transform.position.z);
                }
                else
                {
                    playerOneObject.transform.position = new Vector3(-4, -3, transform.position.z);
                }

                break;


            default:
                break;
        }

    }


    //after the server call to fire the game the player will know if they are player 1,
    // this will tell them (in on data received fire match) if they are the host
    // if they are the host they will send this call with random game data
    //@Yelsa Step 4B
    public void HostPrepGame()
    {
        //set the player as the host if they get to this point
        isHost = true;

        //have the host roll the random stuff, and then send it to the server...
        RandomGameElementsManager.instance.RandomizeNewGame();

        /*NewGameData newGameData = new NewGameData("210", 
            RandomGameElementsManager.instance.selectedWeapon,
            RandomGameElementsManager.instance.selectedSuspect,
            RandomGameElementsManager.instance.selectedPlace
            );
        */

        //string jsonData = JsonUtility.ToJson(newGameData);

        //await _apiManager.Post(GameSessionPlacementEndpoint, jsonData);


        RealtimePayload realtimePayload = new RealtimePayload(RandomGameElementsManager.instance.selectedWeapon, 
            RandomGameElementsManager.instance.selectedSuspect, RandomGameElementsManager.instance.selectedPlace);

        _realTimeClient.SendMessage(OP_PREP_GAME, realtimePayload);

        //ready local stuff might not be needed... just use server calls...
        //we need to do a ready check before we continue. server will set ready to true when ready check is done
        
        //TODO: remove this local readies and only user server?
        //ready = false;
        //hostReady = false;




        //TODO: this should all be in onDataReceived as response to call to prepgame
        //TODO: data received "prepare the game" response from this will tell the players to run SetUIAfterMatchFound();
        //SetupAfterMatchFound(); //TODO: remove this to datareceived

        //this will not run until the ready check completes
        //_startGame = true;

    }

    //after the call to fire match, all players will SetupAfterMatchFound
    //when they are done with that they will call this method to tell the server they are ready to start the game
    void ThisPlayerIsReadyToStart()
    {

        RealtimePayload realtimePayload = new RealtimePayload(_playerId);

        _realTimeClient.SendMessage(OP_START_GAME, realtimePayload);

    }


    //TODO: this should just be handled in onDataReceived by a call from server.
    //server will send a call when all players are ready to start, and then the host will start the timer and the players will localresume
    void HostStartGame()
    {

        //GameEventToServer startGameServerMessage = new GameEventToServer("201", _playerId);

        //string jsonData = JsonUtility.ToJson(startGameServerMessage);

        //string response = await _apiManager.Post(GameSessionPlacementEndpoint, jsonData);

        //GameEventToServer responseData = JsonConvert.DeserializeObject<GameEventToServer>(response);
        

        //Debug.Log("hoststartgame: response code " + responseData.opCode);

        //this will have a datareceived response telling the players to all LocalResume and the host to StartTimer

        ////TODO: remove
        //if (isHost)
        //{
        //    StartTimer();
        //}
        //LocalResume(); //TODO: remove

    }


   



    void StartTimer()
    {
        timerRunning = true;
    }

    public void LocalResume()
    {
        //allow the local player to begin moving / load anything needed for the player to begin investigating
        movementDisabled = false;
        cutsceneActive = false;
        PlayerController.instance.canMove = true;

        //close the menu if it is open... TODO: test if this is annoying
        GameMenu.instance.CloseMenu();

    }

    //when the host's timer finishes they will call the server,
    //@Yelsa Step 9
    void HostStartGuessEvent()
    {

        //tell the server that the players should start the guess event
        RealtimePayload realtimePayload = new RealtimePayload(_playerId);

        _realTimeClient.SendMessage(OP_START_GUESS_EVENT, realtimePayload);



    }


    //this is unneeded because the server can just have each player
    //start a local countdown when they received the call from the server to load into the guess event
    void HostStartCountdown()
    {
        ////TODO: call server and tell users to show timer / start the timer
        //GameEventToServer startCountdownServerMessage = new GameEventToServer("901", _playerId);

        //string jsonData = JsonUtility.ToJson(startCountdownServerMessage);

        //string response = await _apiManager.Post(GameSessionPlacementEndpoint, jsonData);

        //GameEventToServer responseData = JsonConvert.DeserializeObject<GameEventToServer>(response);

        //Debug.Log("countdown start: response code " + responseData.opCode);


        //this should send a dataReceived to tell each player to LocalStartCountdown()
        //LocalStartCountdown(); //TODO remove

    }


    //this starts the local countdown for the guess event
    void LocalStartCountdown()
    {
        countdownRunning = true;

        //show the timer on the screen
        timerObjectInMenu.SetActive(true);
        timerObjectOutOfMenu.SetActive(true);

        //open menu and guess interface
        GameMenu.instance.ShowMenu();
        GameMenu.instance.guessWindow.SetActive(true);
        GameMenu.instance.guessButton.SetActive(true);
    }


    //this will be handled by the server, when all players have checked answer a server message will be sent out
    async void HostResumeGameAfterGuessEvent()
    {
        ////TODO: tell the server that the players should resume the investigation
        //GameEventToServer resumeGameAfterGuessServerMessage = new GameEventToServer("204", _playerId);

        //string jsonData = JsonUtility.ToJson(resumeGameAfterGuessServerMessage);

        //string response = await _apiManager.Post(GameSessionPlacementEndpoint, jsonData);

        //GameEventToServer responseData = JsonConvert.DeserializeObject<GameEventToServer>(response);

        //Debug.Log("resumeGameAfterGuessServerMessage: response code " + responseData.opCode);

        ////ready check should be triggered by the dataReceived response code

        ////this will not run until after ready check
        //_resumeGameAfterGuessEvent = true;

    }

    //this will be handled by the server, when all players have checked answer a server message will be sent out
    async void HostEndGame()
    {

        //GameEventToServer endGameServerMessage = new GameEventToServer("209", _playerId);

        //string jsonData = JsonUtility.ToJson(endGameServerMessage);

        //string response = await _apiManager.Post(GameSessionPlacementEndpoint, jsonData);

        //use this format to get the reponse from the server
        //PlayerMovementData responseData = JsonConvert.DeserializeObject<PlayerMovementData>(response);

        //TODO: use the response to get the name of the winner.. then run the winner cutscene (in onDataReceived)
        //on the server end tell the server connection to end in like 1 minute.


    }


    public void EndGame()
    {

        //end the game... (this is not the CS)..
        //this is for kicking the players out to possibly and endscreen/ the menu

        //destroy all game objects associated with the instance of the game
        Destroy(PlayerController.instance);
        Destroy(GameMenu.instance);
        Destroy(PlayerLoader.instance);
        Destroy(EssentialsLoader.instance);
        //TODO: @Yelsa for loop destroy other players
        //TODO: @Yelsa destroy interaction points?

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

    public void EndGuessResumeGameCutscene(EndGuessEventData endGuessEventData)
    {

        //TODO: @Yelsa use endGuessEventData to add data to the cutscene to share with the players

        string[] lines = new string[] { "n-Detective", "alright, good guesses everyone", "keep looking!" };

        DialogueManager.instance.AutoPlayDialogue(lines, defaultAutoplayDelay, "readyToResume");

        

    }

    public void WinnerCutscene(EndGameData endGameData)
    {

        string winnerName = endGameData.winnerName;
        string weapon = endGameData.weapon;
        string suspect = endGameData.suspect;
        string place = endGameData.place;

        string[] lines = new string[] { "n-Detective", winnerName +" has solved the case!",
            "It was " + suspect + " with the " + weapon + " in the " + place + "."
            , "Good work detectives!"};

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
    // @Yelsa Step 1
    public async void OnFindMatchPressed()
    {
        Debug.Log("Find match pressed");
        _findingMatch = true;

        //show UI to let player know something is loading
        LoadingMatchUI(true);


        //TODO: also send name and sprite name as part of find match object and then also add them to the playerseesionobject received
        string name = LaunchMenu.instance.nameInputText.text;
        string spriteName = LaunchMenu.instance.selectedSpriteName;

        FindMatch matchMessage = new FindMatch(REQUEST_FIND_MATCH_OP, _playerId);
        string jsonPostData = JsonUtility.ToJson(matchMessage);


        string response = await _apiManager.Post(GameSessionPlacementEndpoint, jsonPostData);

        Debug.Log("response: " + response);

        //GameSessionPlacementInfo gameSessionPlacementInfo = JsonConvert.DeserializeObject<GameSessionPlacementInfo>(response);
        /*
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
        } */

        PlayerSessionObject playerSessionObject = JsonConvert.DeserializeObject<PlayerSessionObject>(response);

        if (playerSessionObject != null)
        {

            //int port = Int32.Parse(playerSessionObject.Port);

            //playerSessionObject.IpAddress, port, playerSessionObject.PlayerSessionId

            EstablishConnectionToRealtimeServer(playerSessionObject);


            //TODO: if this doesn't work go grab this (and subscribe) from the batteryacid project... i changed it too much to keep it
        }
        else
        {
            Debug.Log("Error: player session objet is NULL");
        }



        //_findMatchButton.gameObject.SetActive(false); // remove from UI
    }


    //TODO: @HERE this function is still giving a URI issue. It is trying to start a game session and then failing to with a uri error
    // afterwards if i try to connect the session has been created but this player didn't ever get notified and connect to it...
    // could still be onDataReceived or could be that the url is wrong. trouble shoot and fix.
    //private async Task<bool> SubscribeToFulfillmentNotifications(PlayerSessionObject playerSessionObject)
    //{
    //    Debug.Log("GM: subscribe to fullfillment");


    //    /*
    //    PlayerPlacementFulfillmentInfo playerPlacementFulfillmentInfo = await _sqsMessageProcessing.SubscribeToFulfillmentNotifications(playerSessionObject);

    //    //hide the UI and take the player to a waiting screen until ready for the match start (waiting for other players)
    //    LoadingMatchUI(false);

    //    if (playerPlacementFulfillmentInfo != null)
    //    {
    //        Debug.Log("Player placement was fulfilled...");
    //        // Debug.Log("Placed Player Sessions count: " + playerPlacementFulfillmentInfo.placedPlayerSessions.Count);

    //        // Once connected, the Realtime service moves the Player session from Reserved to Active, which means we're ready to connect.
    //        // https://docs.aws.amazon.com/gamelift/latest/apireference/API_CreatePlayerSession.html
    //        EstablishConnectionToRealtimeServer(playerPlacementFulfillmentInfo.ipAddress, playerPlacementFulfillmentInfo.port,
    //            playerPlacementFulfillmentInfo.placedPlayerSessions[0].playerSessionId);

    //        return true;
    //    }
    //    else
    //    {
    //        Debug.Log("Player placement was null, something went wrong...");
    //        return false;
    //    }*/
    //}


    //use the response from the server (PlayerSessionObject) to create a Client to connect to a player session
    //@Yelsa Step 3
    private void EstablishConnectionToRealtimeServer(PlayerSessionObject playerSessionObject)
    {
        Debug.Log("establish connection to server method called");

        int localUdpPort = GetAvailableUdpPort();

        var ipAddress = playerSessionObject.IpAddress;
        var playerSessionId = playerSessionObject.PlayerSessionId;
        var port = Int32.Parse(playerSessionObject.Port);
        _playerId = playerSessionObject.PlayerId;


        RealtimePayload realtimePayload = new RealtimePayload(_playerId);
        string payload = JsonUtility.ToJson(realtimePayload);

        //show UI to let player know something is loading
        LoadingMatchUI(true);

        //this is where the connection between client and playersession is established
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


            //TODO: @YELSA implement this method with server calls that work... to the server not the lambda?
            /// note that at this point the player is connecting but reserved... they are then timing out after 1 minute. fix this issue

            

            //CheckIfHost();

            

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

        }

    }

    //this will be handled onDataRecieved after the player connects to the server
     void CheckIfHost()
    {
        ////determine if player is host by asking server.. if they are isHost = true


        //GameEventToServer getHost = new GameEventToServer("199", _playerId);

        //string jsonData = JsonUtility.ToJson(getHost);


        //string response = await _apiManager.Post(GameSessionPlacementEndpoint, jsonData);


        //GameEventToServer responseData = JsonConvert.DeserializeObject<GameEventToServer>(response);

        //if (responseData != null)
        //{
        //    string responsePlayerId = responseData.playerId;

        //    if (responsePlayerId == _playerId)
        //    {
        //        Debug.Log("you are the host");
        //        isHost = true;
        //    }
        //    else
        //    {
        //        //this should be an empty string if not the host
        //        Debug.Log("you are not the host. reponseid = " + responsePlayerId + " . your id = " + _playerId);
        //        isHost = false;
        //    }
        //}
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
        //Debug.Log($"Game over event received with winner: {gameOverEventArgs.matchResults.winnerId}.");
        //this._matchResults = gameOverEventArgs.matchResults;
        //this._gameOver = true;
    }






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


//TODO: update this and use it to prep game
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
    public bool submittedAnswer;

    public string selectedWeapon;
    public string selecedSuspect;
    public string selectedPlace;

    public RealtimePayload() { }

    //any call that only needs to send playerId..
    public RealtimePayload(string playerIdIn)
    {
        this.playerId = playerIdIn;
    }

    //movement
    public RealtimePayload(string playerId, float playerXLocation, float playerYLocation, float playerZLocation)
    {
        this.playerId = playerId;
        this.playerXLocation = playerXLocation;
        this.playerYLocation = playerYLocation;
        this.playerZLocation = playerZLocation;
    }

    //check answer
    public RealtimePayload(string playerId, string weaponGuess, string suspectGuess, string locationGuess, bool submittedAnswer)
    {
        this.playerId = playerId;
        this.weaponGuess = weaponGuess;
        this.suspectGuess = suspectGuess;
        this.locationGuess = locationGuess;
        this.submittedAnswer = submittedAnswer;

    }

    //prep game
    public RealtimePayload(string selectedWeapon, string selectedSuspect, string selectedPlace)
    {
        this.selectedWeapon = selectedWeapon;
        this.selecedSuspect = selectedSuspect;
        this.selectedPlace = selectedPlace;
    }

}


[Serializable]
public class PlayerMovementData
{
    //public string opCode;
    public float playerXPosition;
    public float playerYPosition;
    public float playerZPosition;
    public string playerId;
   

    public PlayerMovementData() { }

    public PlayerMovementData(float playerXPosition, float playerYPosition, float playerZPosition, string playerId)
    {
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

[Serializable]
public class PlayerData
{
    public string PlayerName;
    public string PlayerSpriteName;
    public int PlayerNumber;
    public string PlayerId;
    public bool ReadyToStart;
    public bool ReadyToResume;

    public PlayerData() { }

    public PlayerData(string PlayerName, string PlayerSpriteName, int PlayerNumber, string PlayerId, bool ReadyToStart, bool ReadyToResume)
    {
        this.PlayerName = PlayerName;
        this.PlayerSpriteName = PlayerSpriteName;
        this.PlayerNumber = PlayerNumber;
        this.PlayerId = PlayerId;
        this.ReadyToStart = ReadyToStart;
        this.ReadyToResume = ReadyToResume;

    }
    
}

[Serializable]
public class GamePrepData
{
    public PlayerData[] playerDataList;

    public string selectedWeapon;
    public string selectedSuspect;
    public string selectedPlace;

    public GamePrepData() { }

    public GamePrepData(PlayerData[] playerDataList, string weapon, string suspect, string place)
    {
        this.playerDataList = playerDataList;
        this.selectedWeapon = weapon;
        this.selectedSuspect = suspect;
        this.selectedPlace = place;

    }

}

[Serializable]
public class EndGameData
{
    public string winnerName;
    public string weapon;
    public string suspect;
    public string place;

    public EndGameData() { }

    public EndGameData(string winnerName, string weapon, string suspect, string place)
    {
        this.winnerName = winnerName;
        this.weapon = weapon;
        this.suspect = suspect;
        this.place = place;
    }
}

[Serializable]
public class EndGuessEventData
{
    //TODO: @Yelsa make this class with the data to share in the cutscene after the guess event

    public EndGuessEventData() { }

    //TODO: constructor with data

}


