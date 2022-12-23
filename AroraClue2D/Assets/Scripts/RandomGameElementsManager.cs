
using SuperSocket.ClientEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomGameElementsManager : MonoBehaviour
{
    public static RandomGameElementsManager instance;

    public string[] weapons;
    public string[] suspects;
    public string[] places;

    public string selectedWeapon = "";
    public string selectedSuspect = "";
    public string selectedPlace = "";

    [HideInInspector] public string[] unusedWeapons;
    [HideInInspector] public string[] unusedSuspects;
    [HideInInspector] public string[] unusedPlaces;


    private void Start()
    {
        instance = this;
        

    }

    public void RandomizeNewGame()
    {
        selectedWeapon = weapons[Random.Range(0, weapons.Length)];

        selectedSuspect = suspects[Random.Range(0, suspects.Length)];

        selectedPlace = places[Random.Range(0, places.Length)];

        Debug.Log("RandomizeNewGame: weapon: " + selectedWeapon + ". suspect: " + selectedSuspect + ". place: " + selectedPlace);



        //unusedWeapons = CreateUnusedList(weapons, selectedWeapon).RandomOrder();
        //unusedSuspects = CreateUnusedList(suspects, selectedSuspect).RandomOrder();
        //unusedPlaces = CreateUnusedList(places, selectedPlace).RandomOrder();


    }

    public void PrepareLists(string selectedWeapon, string selectedSuspect, string selectedLocation)
    {

        unusedWeapons = CreateUnusedList(weapons, selectedWeapon).RandomOrder();
        unusedSuspects = CreateUnusedList(suspects, selectedSuspect).RandomOrder();
        unusedPlaces = CreateUnusedList(places, selectedLocation).RandomOrder();


    }


    public string[] CreateUnusedList(string[] array, string selectedString)
    {
        List<string> newList = new List<string>();
        int index = 0;

        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] != selectedString)
            {
                newList.Add(array[i]);
                index++;
            }

        }

        string[] newArray = newList.ToArray();

        return newArray;
    }








}
