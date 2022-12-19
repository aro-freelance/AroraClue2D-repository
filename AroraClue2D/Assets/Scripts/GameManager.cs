using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;

public class GameManager : MonoBehaviour
{

    public static GameManager Instance;

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

    //after CS bools
    public bool isReadyToStartCountdown;
    public bool isReadyToCheckAnswers;
    public bool isReadyToResume;
    public bool isReadyToEndGame;

    public float defaultAutoplayDelay = 4; //seconds

    public bool isGuessCorrect = false;


    // Start is called before the first frame update
    void Start()
    {

        Instance = this;
        DontDestroyOnLoad(gameObject);

        //TODO: determine if player is host by asking server.. if they are isHost = true
        isHost = true;

        //SortItems();

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

        RunGuessSystemChecksAndTimers();

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






}
