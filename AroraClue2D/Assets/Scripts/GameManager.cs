using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    public static GameManager Instance;

    public CharStats[] playerStats;

    //this creates three different bools at once.  cleaner than doing each separate. especially when they are all similar. 
    public bool gameMenuOpen, dialogueActive, fadingBetweenAreas;

    public string[] itemsInInventory;
    public int[] numberOfEachItem;
    public Item[] referenceItems;

    public int currentMoney;



    // Start is called before the first frame update
    void Start()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SortItems();

    }

    // Update is called once per frame
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

        if (Input.GetKeyDown(KeyCode.J))
        {
            AddItem("Health Potion");
            AddItem("Mana Potion");
            
        }
        
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

            GameMenu.instance.ShowItems();
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

            GameMenu.instance.ShowItems();
        }
        else
        {
            Debug.LogError("Tag: GameManager, RemoveItem. Couldn't find " + itemToRemove);
        }


    }


}
