using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EssentialsLoader : MonoBehaviour
{

    public GameObject UIScreen;
    public GameObject player;
    public GameObject gameManager;
    //public GameObject serverManager;

    public static EssentialsLoader instance;

    void Start()
    {
        instance = this;

        if(UIFade.instance == null)
        {
            UIFade.instance = Instantiate(UIScreen).GetComponent<UIFade>();
        }

        if(PlayerController.instance == null)
        {
            PlayerController clone = Instantiate(player).GetComponent<PlayerController>();
            PlayerController.instance = clone;
        }

        if(GameManager.Instance == null)
        {
            Instantiate(gameManager);
        }

        //if (ServerManager.instance = null)
        //{
        //    Instantiate(serverManager);
        //}



    }

}
