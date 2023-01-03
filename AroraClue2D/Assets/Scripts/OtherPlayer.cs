using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OtherPlayer : MonoBehaviour
{

    //this class is used to build an object with all the things the other players need 


    //name
    public string playerName;

    //playerid
    public string playerId;

    //playernumber assigned by server
    public int playerNumber;

    //sprite
    public Sprite sprite;

    //location
    public Vector3 location;

    //network status? (friend, blocked, etc)
    public bool isFriend;
    public bool isBlocked;

    public OtherPlayer() { }

    public OtherPlayer(string playerName, string playerID, int playerNumber, Sprite sprite, Vector3 location, bool isFriend, bool isBlocked)
    {

        this.playerName = playerName;
        this.playerId = playerID;
        this.playerNumber = playerNumber;
        this.sprite = sprite;
        this.location = location;
        this.isFriend = isFriend;
        this.isBlocked = isBlocked;

    }



}
