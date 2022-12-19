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

    public GameObject guessWindow;
    public GameObject guessButton;
    public Text guessButtonText;

    public TMP_InputField notebookTMPInputField;
    public TMP_Text playerNameHeader;
    public TMP_Text suspectListText;
    public TMP_Text weaponListText;
    public TMP_Text locationListText;

    public TMP_Dropdown weaponDropdown;
    public TMP_Dropdown suspectDropdown;
    public TMP_Dropdown locationDropdown;


    private string userAnswerWeapon;
    private string userAnswerSuspect;
    private string userAnswerLocation;

    private List<string> weaponList;
    private List<string> suspectList;
    private List<string> locationList;


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
                ShowMenu();
            }
        }

        if (guessWindow.activeInHierarchy && guessButton.activeInHierarchy)
        {
            guessButtonText.text = "Show Notes";
        }
        else if (!guessWindow.activeInHierarchy && guessButton.activeInHierarchy)
        {
            guessButtonText.text = "Show Guess";
        }

    }

    public void ShowMenu()
    {
        notebookTMPInputField.pointSize = 50;
        playerNameHeader.text = playerName;

        setListText(RandomGameElementsManager.instance.suspects, suspectListText);
        setListText(RandomGameElementsManager.instance.weapons, weaponListText);
        setListText(RandomGameElementsManager.instance.places, locationListText);

        SetGuessDropdownLists();


        if (GameManager.Instance.secondTimerIsRunning)
        {
            guessWindow.SetActive(true);
        }

        theMenu.SetActive(true);
        GameManager.Instance.gameMenuOpen = true;


    }

    //TODO: use the toggle window button to open and close the guess interface... setup in Unity Editor


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

    void SetGuessDropdownLists()
    {
        weaponList = RandomGameElementsManager.instance.weapons.ToList();
        suspectList = RandomGameElementsManager.instance.suspects.ToList();
        locationList = RandomGameElementsManager.instance.places.ToList();

        weaponDropdown.ClearOptions();
        suspectDropdown.ClearOptions();
        locationDropdown.ClearOptions();

        weaponDropdown.AddOptions(weaponList);
        suspectDropdown.AddOptions(suspectList);
        locationDropdown.AddOptions(locationList);
    }

    public void SubmitGuessButton()
    {
        userAnswerWeapon = weaponList[weaponDropdown.value];
        userAnswerSuspect = suspectList[suspectDropdown.value];
        userAnswerLocation = locationList[locationDropdown.value];

        Debug.Log("weapon: " + userAnswerWeapon + ". suspect: " + userAnswerSuspect + ". location: " + userAnswerLocation);

        string correctWeapon = RandomGameElementsManager.instance.selectedWeapon;
        string correctPerson = RandomGameElementsManager.instance.selectedSuspect;
        string correctLocation = RandomGameElementsManager.instance.selectedPlace;

        //if user guess is correct
        if( userAnswerWeapon == correctWeapon &&
            userAnswerSuspect == correctPerson &&
            userAnswerLocation == correctLocation)
        {

            //set bool in GameManager
            GameManager.Instance.isGuessCorrect = true;

        }
        else
        {
            //set bool in GameManager
            GameManager.Instance.isGuessCorrect = false;
        }

        guessButton.SetActive(false);
        guessWindow.SetActive(false);
        CloseMenu();

        GameManager.Instance.submittedAnswer = true;

    }




}
    