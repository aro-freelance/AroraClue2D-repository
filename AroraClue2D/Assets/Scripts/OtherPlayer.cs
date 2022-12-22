using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OtherPlayer : MonoBehaviour
{

    //this class is used to build an object with all the things the other players need 



    //set these when the player is received from the server
    public string playerName;
    //playernumber assigned by server
    public int playerNumber;
    public string spriteStringName;

    public string playerId;


    //sprite
    public Sprite sprite;

    //location
    public Vector3 location;


    public OtherPlayer() { }

    public OtherPlayer(string playerName, string playerID, int playerNumber, Sprite sprite, Vector3 location, string spriteStringName)
    {
        
        this.playerName = playerName;
        this.playerId = playerID;
        this.playerNumber = playerNumber;
        this.sprite = sprite;
        this.location = location;
        this.spriteStringName = spriteStringName;

    }



}
