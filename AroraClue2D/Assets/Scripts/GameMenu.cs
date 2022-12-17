using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameMenu : MonoBehaviour
{
    public static GameMenu instance;


    public int numberOfCharacters = 4;
    public int charNumber = 0;



    public GameObject theMenu;
    public GameObject[] windows;

    public GameObject[] statCharacterButtons;

    private CharStats[] playerStats;

    public Text[] nameText, hpText, mpText, levelText, expText, jobText;
    public Slider[] expSlider;
    public Image[] charImage;

    //this is the whole box for a single character. we can turn this off if the player has less than four characters
    public GameObject[] charStatHolder;

    //these are used to populate the statsMenu
    public Text statsHPMax, statsMPMax, statsStr, statsDex, statsVit, statsAgi, statsInt, statsMnd, statsChr,
                statsAtt, statsDef, statsMAtt, statsMDef, statsEva, statsAcc, statsMEva, statsMAcc,
                statsSpeed, statsLuck, statsElement, statsJob, statsLevel, statsName,
                statsWeaponName, statsOffhandName, statsHeadName, statsBodyName, statsHandsName, statsLegsName, statsFeetName, statsAccessoryName;

    public Image statsImage;

    public ItemButtons[] itemButtons;
    public string selectedItem;
    public Item activeItem;
    public Text itemName;
    public Text itemDescription;
    public Button useButton;
    public Text useButtonText;
    public Button discardButton;

    public GameObject itemCharChoiceMenu;
    public Text[] itemSelectCharNames   ;
    public Text goldText;

    //public Image weaponImage, offhandImage, headImage, bodyImage, handImage, legImage, feetImage, accessoryImage;



    // Start is called before the first frame update
    void Start()
    {
        instance = this;

        //this is off for testing when game is done it should be on
        //CloseMenu();
    }

    // Update is called once per frame
    void Update()
    {
        //when the player pressed right click or square? the menu will toggle open or closed
        if (Input.GetButtonDown("Fire2"))
        {
            if (theMenu.activeInHierarchy)
            {
                CloseMenu();
            }
            else
            {
                theMenu.SetActive(true);
                UpdateMainStats();
                GameManager.Instance.gameMenuOpen = true;
            }
        }
    }


    public void UpdateMainStats()
    {
        playerStats = GameManager.Instance.playerStats;

        for(int i = 0; i < playerStats.Length; i++)
        {
            if (playerStats[i].gameObject.activeInHierarchy)//if in the gamemanager prefab in unity a player's stats are set to inactive that player will not show in the menu
            {   
                charStatHolder[i].SetActive(true);

                nameText[i].text = playerStats[i].charName;
                hpText[i].text = "HP : " + playerStats[i].currentHP + " / " + playerStats[i].maxHP;
                mpText[i].text = "MP : " + playerStats[i].currentMP + " / " + playerStats[i].maxMP;
                expText[i].text = "EXP to next level : " + playerStats[i].currentEXP + " / " + playerStats[i].expToNextLevel[playerStats[i].charLevel];
                levelText[i].text = "Lvl : " + playerStats[i].charLevel;
                jobText[i].text = playerStats[i].characterJob;

                expSlider[i].maxValue = playerStats[i].expToNextLevel[playerStats[i].charLevel];
                expSlider[i].value = playerStats[i].currentEXP;

                charImage[i].sprite = playerStats[i].charImage;





            }
            else
            {
                charStatHolder[i].SetActive(false);
            }
        }

       
            for (int i = 0; i < numberOfCharacters; i++)
            {
                if (i != charNumber)
                {
                    Color color = new Color(.75f, .75f, .75f, .8f);
                   statCharacterButtons[i].GetComponentInChildren<Image>().color = color;
                }
                else
                {
                    Color color = new Color(1f, 1f, 1f, 1f);
                    statCharacterButtons[i].GetComponentInChildren<Image>().color = color;
                }
            }
      




        goldText.text = GameManager.Instance.currentMoney.ToString() + "g";
    }


    public void ToggleWindow(int windowNumber)
    {
        UpdateMainStats();

        for(int i = 0; i < windows.Length; i++)
        {
            if(i == windowNumber)
            {
                //this takes the current window in the array and checks its active status. it then switches the active status. so if it checks and it is active it deactivates and vice versa
                windows[i].SetActive(!windows[i].activeInHierarchy);
            }
            else
            {
                windows[i].SetActive(false);
            }
        }

        itemCharChoiceMenu.SetActive(false);
    }

    public void CloseMenu()
    {
        //inactivate all windows
        for (int i = 0; i < windows.Length; i++) { windows[i].SetActive(false);}

        theMenu.SetActive(false);
        GameManager.Instance.gameMenuOpen = false;
        itemCharChoiceMenu.SetActive(false);
    }

    public void OpenStatsWindow()
    {

        UpdateMainStats();

        //update the information that is shown
        FillStatWindow(0);

        for(int i = 0; i < statCharacterButtons.Length; i++)
        {
            //check if the player is active in the menu. if they are then their button should be active in this menu.
            statCharacterButtons[i].SetActive(playerStats[i].gameObject.activeInHierarchy);
            statCharacterButtons[i].GetComponentInChildren<Text>().text = playerStats[i].charName;
        }

    }


    public void FillStatWindow(int selected)
    {
        statsName.text = playerStats[selected].charName;
        statsJob.text = playerStats[selected].characterJob;
        statsLevel.text = "Level : "+ playerStats[selected].charLevel; //note this is how you get around it having an issue with int to string

        statsHPMax.text = "" + playerStats[selected].currentHP + " / " + playerStats[selected].maxHP;
        statsMPMax.text = "" + playerStats[selected].currentMP + " / " + playerStats[selected].maxMP;

        statsStr.text = ""+ playerStats[selected].charStr;
        statsDex.text = "" + playerStats[selected].charDex;
        statsVit.text = "" + playerStats[selected].charVit;
        statsAgi.text = "" + playerStats[selected].charAgi;
        statsInt.text = "" + playerStats[selected].charInt;
        statsMnd.text = "" + playerStats[selected].charMnd;
        statsChr.text = "" + playerStats[selected].charChr;

        statsAtt.text = "" + playerStats[selected].attack;
        statsDef.text = "" + playerStats[selected].defense;
        statsMAtt.text = "" + playerStats[selected].mattack;
        statsMDef.text = "" + playerStats[selected].mdefense;
        statsEva.text = "" + playerStats[selected].evasion;
        statsAcc.text = "" + playerStats[selected].accuracy;
        statsMEva.text = "" + playerStats[selected].mevasion;
        statsMAcc.text = "" + playerStats[selected].maccuracy;

        statsSpeed.text = "" + playerStats[selected].speed;
        statsLuck.text = "" + playerStats[selected].luck;
        statsElement.text = "" + playerStats[selected].element;


        //for the equipment slots they need to check if the string is empty so there is not a null error
        // not there is a bug causing an equipped item to equip to all characters after the character who equips it as well(after in the array)
        // the forums say that adding the else statement fixes that issue. IT WORKS
        if(playerStats[selected].equippedWeapon != "") { statsWeaponName.text = playerStats[selected].equippedWeapon;} else { statsWeaponName.text = "None"; }
        if(playerStats[selected].equippedOffhand != "") { statsOffhandName.text = playerStats[selected].equippedOffhand; } else { statsOffhandName.text = "None"; }
        if (playerStats[selected].equippedHead != "") { statsHeadName.text = playerStats[selected].equippedHead; } else { statsHeadName.text = "None"; }
        if (playerStats[selected].equippedBody != "") { statsBodyName.text = playerStats[selected].equippedBody; } else { statsBodyName.text = "None"; }
        if (playerStats[selected].equippedHands != "") { statsHandsName.text = playerStats[selected].equippedHands; } else { statsHandsName.text = "None"; }
        if (playerStats[selected].equippedLegs != "") { statsLegsName.text = playerStats[selected].equippedLegs; } else { statsLegsName.text = "None"; }
        if (playerStats[selected].equippedFeet != "") { statsFeetName.text = playerStats[selected].equippedFeet; } else { statsFeetName.text = "None"; }
        if (playerStats[selected].equippedAccessory != "" ) { statsAccessoryName.text = playerStats[selected].equippedAccessory; } else { statsAccessoryName.text = "None"; }


        //TODO add code for inventory images just like this. also will need to declare the images above. and of course set them in unity (which means we need more images)
        //TODO will probably want to use the equipped weapon/armor string to find the sprite


        statsImage.sprite = playerStats[selected].charImage;

        charNumber = selected;

        for (int i = 0; i < numberOfCharacters; i++)
        {
            if (i != charNumber)
            {
                Color color = new Color(.75f, .75f, .75f, .8f);
                statCharacterButtons[i].GetComponentInChildren<Image>().color = color;
            }
            else
            {
                Color color = new Color(1f, 1f, 1f, 1f);
                statCharacterButtons[i].GetComponentInChildren<Image>().color = color;
            }
        }





        goldText.text = GameManager.Instance.currentMoney.ToString() + "g";

    }


    public void ShowItems()
    {
        GameManager.Instance.SortItems();

        for(int i = 0; i < itemButtons.Length; i++)
        {

            itemButtons[i].buttonNumber = i;

            if(GameManager.Instance.itemsInInventory[i] != "")
            {
                itemButtons[i].buttonImage.gameObject.SetActive(true);
                //find the itembutton details at position i. get the details about what that item's sprite is. set the itembutton to that sprite.
                itemButtons[i].buttonImage.sprite = GameManager.Instance.GetItemDetails(GameManager.Instance.itemsInInventory[i]).itemSprite;
                itemButtons[i].amountOfItem.text = GameManager.Instance.numberOfEachItem[i].ToString();
            }
            else
            {
                itemButtons[i].buttonImage.gameObject.SetActive(false);
                itemButtons[i].amountOfItem.text = "";
            }

        }

    }


    public void SelectItem(Item newItem)
    {
        activeItem = newItem;

        if (activeItem.isClassMeldItem)
        {
            useButtonText.text = "Meld";
        }
        else if (activeItem.isArmor || activeItem.isWeapon)
        {
            useButtonText.text = "Equip";
        }
        else if (activeItem.isCraftMat)
        {
            useButtonText.text = "Craft";
        }
        else
        {
            useButtonText.text = "Use";
        }


        itemName.text = activeItem.itemName;
        itemDescription.text = activeItem.description;

    }

    public void DiscardItem()
    {

        if(activeItem != null)
        {
            GameManager.Instance.RemoveItem(activeItem.itemName);
        }

        //clear the info panel text after item is discarded
        itemName.text = "";
        itemDescription.text = "";

    }

    public void OpenSelectCharacterToUseItem()
    {
        itemCharChoiceMenu.SetActive(true);

        for(int i = 0; i < itemSelectCharNames.Length; i++)
        {
            itemSelectCharNames[i].text = GameManager.Instance.playerStats[i].charName;
            itemSelectCharNames[i].transform.parent.gameObject.SetActive(GameManager.Instance.playerStats[i].gameObject.activeInHierarchy);
        }

    }

    public void CloseSelectCharacterToUseItem()
    {
        itemCharChoiceMenu.SetActive(false);
    }

    public void UseItem(int selectChar)
    {
        activeItem.Use(selectChar);
        CloseSelectCharacterToUseItem();

        //clear the info panel text after item is used.
        itemName.text = "";
        itemDescription.text = "";

    }





}
    