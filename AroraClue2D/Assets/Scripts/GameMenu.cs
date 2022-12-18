using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameMenu : MonoBehaviour
{
    public static GameMenu instance;

    public int numberOfCharacters = 4;
    public int charNumber = 0;

    //TODO: get this from network
    private string playerName = "Kaladin";

    public GameObject theMenu;
    public GameObject[] windows;

    public TMP_InputField notebookTMPInputField;
    public TMP_Text playerNameHeader;
    public TMP_Text suspectListText;
    public TMP_Text weaponListText;
    public TMP_Text locationListText;


    //TODO: add a section to show other players. click on other players to interact... design the sort of interactions we want.
    //also next to other players names show something about their status... previous guess by that player? 


    void Start()
    {
        instance = this;

        //this is off for testing when game is done it should be on
        CloseMenu();
    }


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
                notebookTMPInputField.pointSize = 50;
                playerNameHeader.text = playerName;

                setListText(RandomGameElementsManager.instance.suspects, suspectListText);
                setListText(RandomGameElementsManager.instance.weapons, weaponListText);
                setListText(RandomGameElementsManager.instance.places, locationListText);


                theMenu.SetActive(true);
                GameManager.Instance.gameMenuOpen = true;
            }
        }
    }


    public void ToggleWindow(int windowNumber)
    {

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

    }

    public void CloseMenu()
    {
        //inactivate all windows
        for (int i = 0; i < windows.Length; i++) { windows[i].SetActive(false);}

        theMenu.SetActive(false);
        GameManager.Instance.gameMenuOpen = false;
        //itemCharChoiceMenu.SetActive(false);
    }

 
    void setListText(string[] array, TMP_Text textLabel)
    {

        string text = "";

        for (int i = 0; i < array.Length; i++)
        {
            //if there is already something in the list, then get ready for the next item in the list with a comma and space
            if(text != "") { text = text + ", "; }

            //add the next item in the list
            text = text + array[i];


        }

        textLabel.text = text;

    }




}
    