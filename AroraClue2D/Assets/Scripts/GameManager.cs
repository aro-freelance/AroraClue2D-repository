using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    public static GameManager Instance;

    //used to distiguish between the players in multiplayer game
    public int playerNumber = 0;

    //this creates three different bools at once.  cleaner than doing each separate. especially when they are all similar. 
    public bool gameMenuOpen, dialogueActive, fadingBetweenAreas;

    public string[] itemsInInventory;
    public int[] numberOfEachItem;
    public Item[] referenceItems;

    public int currentMoney;

    //count up from this value to the guessInterval.. to trigger a guess event (minutes)
    private float timer = 0;
    private bool timerIsRunning = false;

    //count down from this value to trigger a check answers event (minutes)
    private float timer2 = 0.5f;
    private bool secondTimerIsRunning = false;
    

    //how often should a guess event be triggered (minutes)
    public int guessInterval = 1;

    public bool submittedAnswer = false;

    private bool isHost = false;


    // Start is called before the first frame update
    void Start()
    {

        Instance = this;
        DontDestroyOnLoad(gameObject);

        //TODO: determine if player is host by asking server.. if they are isHost = true
        isHost = true;

        SortItems();

        timerIsRunning = true;

    }


    void Update()
    {
        //WHILE LOADING OR IN MENU
        //if any of these are true the player cannot move. else player can move.
        // plus this lets us add in any additional things we want to do when the player is in a menu or loading
        if (gameMenuOpen || dialogueActive || fadingBetweenAreas)
        {
            PlayerController.instance.canMove = false;
        }
        else
        {
            PlayerController.instance.canMove = true;
        }

        if (isHost)
        {
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

                CheckIfAllAnswersSubmitted();
            }
        }

    }

    void HandleTimer()
    {

        if (timerIsRunning)
        {
            timer += (Time.deltaTime/60);

            float timeRemaining = guessInterval - timer;

            if (timeRemaining <= 0)
            {
                TriggerGuessEvent();
            }

        }
    }

    void HandleTimer2()
    {

        if (secondTimerIsRunning)
        {
            timer2 -= (Time.deltaTime / 60);

            if (timer2 <= 0)
            {
                TriggerCheckAnswersEvent();
            }

        }
    }


    async void TriggerGuessEvent()
    {
        timerIsRunning = false;

        Debug.Log("Guess event triggered");

        //TODO: tell the server to call guess event for all players
        GuessEvent();

        
      
    }

    void GuessEvent()
    {
        //start second timer 
        secondTimerIsRunning = true;

        //TODO: show the timer on the screen

        //move player to location (each player will be placed at spawn based on thier player number)
        SpawnPlayerAtGuessEvent(playerNumber);

        //prevent player from leaving. lock movement?
        PlayerController.instance.canMove = false;

        //TODO: spawn NPC (lead detective)

        //TODO: show lead detective dialogue... then when finished..

        //open menu and guess interface
        GameMenu.instance.ShowMenu();
        GameMenu.instance.guessWindow.SetActive(true);
        GameMenu.instance.guessButton.SetActive(true);


    }

    async void SpawnPlayerAtGuessEvent(int playerNumber)
    {
        //TODO: use player number to spawn the player at a spawnpoint in a list of spawn points

    }

    async void TriggerCheckAnswersEvent()
    {
        secondTimerIsRunning = false;

        Debug.Log("end check answers event");

        //TODO: check on server if any players have not submitted an answer
        //TODO: call method for not having answered for those players (in that method,
        //consequence of no answer and then set their submittedAnswer bool to true)

        //TODO: check if any of the submitted answers are correct
        //if there is a correct answer, end the game and show that player is the winner, through dialogue
        //else there is not a correct answer submitted.
        //show dialogue telling the players why their guesses are incorrect and then
        //spawn the players back into the map 

        //turn on player movement
        PlayerController.instance.canMove = true;

        //start timer again (only when ready to start game again).  TODO: change guess interval here?
        timer = 0;


    }

    void CheckIfAllAnswersSubmitted()
    {

        //using the server check if all players have submittedAnswer = true
             //if they all do then TriggerEndCheckAnswerEvent

    }


    public Item GetItemDetails(string itemToGrab)
    {
        for(int i = 0; i < referenceItems.Length; i++)
        {
            if(referenceItems[i].itemName == itemToGrab)
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

        for(int i = 0; i < itemsInInventory.Length; i++)
        {
            //so if you find a blank you are at the end of the inventory bc we sorted it. or if you find the item before that, stack it.
            if(itemsInInventory[i] == "" || itemsInInventory[i] == itemToAdd)
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
            for(int i = 0; i < referenceItems.Length; i++)
            {
                //if you find the item in the list of items
                if(referenceItems[i].itemName == itemToAdd)
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
                Debug.LogError("Tag: Game Manager, AddItem. " +itemToAdd + " does not exist.");
            }

            //GameMenu.instance.ShowItems();
        }



    }

    public void RemoveItem(string itemToRemove)
    {

        bool foundItem = false;
        int itemPosition = 0;

        for(int i = 0; i < itemsInInventory.Length; i++)
        {
            if(itemsInInventory[i] == itemToRemove)
            {
                foundItem = true;
                itemPosition = i; // the item is at position i.
                i = itemsInInventory.Length;//end the loop, we found it.

            }
        }

        if (foundItem)
        {
            numberOfEachItem[itemPosition]--;
            if(numberOfEachItem[itemPosition] <= 0)
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





}
