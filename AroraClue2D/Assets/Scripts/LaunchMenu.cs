using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using System;

public class LaunchMenu : MonoBehaviour
{


    public GameObject FindMatchButtonObject;
    public GameObject OptionsButtonObject;
    public GameObject ExitButtonObject;
    public GameObject statusUpdateObject;
    public TMP_Text statusUpdateText;
    public TMP_InputField nameInputText;
    public Image spritePreview;
    public TMP_Dropdown spriteSelectDropdown;

    //TODO: add sprites in editor, reference them here with string names for dropdown and update the cases in UpdaeSpriteToMatchDropdown
    public string[] spriteNames = { "playerA", "playerB" };
    public string selectedSpriteName = "";

    public Sprite playerASprite;
    public Sprite playerBSprite;

    public static LaunchMenu instance;



    private void Start()
    {
        
        instance = this;

        PlayerController.instance.canMove = false;

        SetDropdownOptions();

        spriteSelectDropdown.onValueChanged.AddListener(UpdateSpriteToMatchDropdown);
    }

    private void SetDropdownOptions()
    {

        //set the default sprite as the one currently selected
        selectedSpriteName = spriteNames[0];

        spriteSelectDropdown.ClearOptions();

        //put the list in the dropdown UI object
        spriteSelectDropdown.AddOptions(spriteNames.ToList());

    }

    private void UpdateSpriteToMatchDropdown(int value)
    {
        //set the selected sprite name
        selectedSpriteName = spriteNames[value];

        //set the UI image to match the selected sprite name
        switch(selectedSpriteName)
        {

            case "playerA":
                spritePreview.sprite = playerASprite;
                
                break;

            case "playerB":
                spritePreview.sprite = playerBSprite;

                break;



            default:

                break;

        }

    }




    public void OptionsButtonPressed()
    {

        Debug.Log("options button pressed");

        //TODO: open options menu


    }

    public void ExitButtonPressed()
    {
        //tell the server to stop if it is going
        GameManager.Instance.OnApplicationQuit();

        //close the game
        Application.Quit();


    }



}
