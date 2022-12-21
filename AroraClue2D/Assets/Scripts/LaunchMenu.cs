using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LaunchMenu : MonoBehaviour
{


    public GameObject FindMatchButtonObject;
    public GameObject OptionsButtonObject;
    public GameObject ExitButtonObject;
    public GameObject statusUpdateObject;
    public TMP_Text statusUpdateText;



    public static LaunchMenu instance;



    private void Start()
    {
        
        instance = this;


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
