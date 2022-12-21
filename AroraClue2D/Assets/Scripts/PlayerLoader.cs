using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLoader : MonoBehaviour
{

    public GameObject player;


    public static PlayerLoader instance;    


    // Start is called before the first frame update
    void Start()
    {
        instance= this;

        if(PlayerController.instance == null)
        {
            Instantiate(player);
        }
    }

}
