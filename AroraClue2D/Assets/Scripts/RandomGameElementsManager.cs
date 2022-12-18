using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomGameElementsManager : MonoBehaviour
{

    public string[] weapons;
    public string[] suspects;
    public string[] places;

    string selectedWeapon = "";
    string selectedSuspect = "";
    string selectedPlace = "";


    private void Start()
    {


        RandomizeNewGame();


    }

    void RandomizeNewGame()
    {
        selectedWeapon = weapons[Random.Range(0, weapons.Length)];

        selectedSuspect = suspects[Random.Range(0, suspects.Length)];

        selectedPlace = places[Random.Range(0, places.Length)];

        Debug.Log("RandomizeNewGame: weapon: " + selectedWeapon + ". suspect: " + selectedSuspect + ". place: " + selectedPlace);

    }






}
