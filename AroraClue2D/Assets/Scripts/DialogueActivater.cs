
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueActivater : MonoBehaviour
{
    //Test laptop github

    public RandomGameElementsManager randomGameElementsManager;

    public string[] lines;

    private bool canActivate;

    public bool isPerson = true;
    public bool isInteractionPoint = false;

    public bool isSuspectInfo = false;
    public bool isWeaponInfo = false;
    public bool isPlaceInfo = false;


    //TODO: create spawn points for interaction points and create a method of randomly selecting spawn points and placing them there.
    //Each instance of the interaction point should be given a spawnNumber
    public int spawnNumber;

    public bool shouldActivateAQuest;
    public string questToMark;
    [Header("Should the Quest be Marked Complete after the Dialogue?")]
    public bool markComplete;

    private void Start()
    {
        Debug.Log("dialogue activater");
    }

    void Update()
    {

        if (canActivate && Input.GetButtonDown("Fire1") && !DialogueManager.instance.dialogueBox.activeInHierarchy)
        {
            if (isInteractionPoint)
            {
                PopulateInteractiveDialogue();
            }
            DialogueManager.instance.ShowDialogue(lines, isPerson);
            DialogueManager.instance.shouldActivateQuestAtEnd(questToMark, markComplete);

        }
        
        
    }

    void PopulateInteractiveDialogue()
    {
        if (isWeaponInfo)
        {
            string weaponString = randomGameElementsManager.unusedWeapons[spawnNumber];

            int random = Random.Range(0, 2);

            Debug.Log(weaponString + " " + random);

            switch (random)
            {
                case 0:
                    lines = new System.String[] { 
                        "I found the " + weaponString};
                    break;
                case 1:
                    lines = new System.String[] {
                         weaponString};
                    break;
                case 2:
                    lines = new System.String[] {
                        "Found the " + weaponString};
                    break;

                default:
                    break;
            }

            //remove the item from the scene (If we decide to let the player pick it up)...
            //TODO: game design decision.. should multiple players be able to ifnd the same clue?
            //Destroy(gameObject);

            isWeaponInfo = false;

        }

        if (isSuspectInfo)
        {
            string personString = randomGameElementsManager.unusedSuspects[spawnNumber];

            int random = Random.Range(0, 2);

            Debug.Log($"{personString} {random}");

            switch (random)
            {
                case 0:
                    lines = new System.String[] {
                        "I found the " + personString};
                    break;
                case 1:
                    lines = new System.String[] {
                         personString};
                    break;
                case 2:
                    lines = new System.String[] {
                        "Found the " + personString};
                    break;

                default:
                    break;
            }

            isSuspectInfo = false;

        }

        //TODO: NOTE.... place information is likely to be determined by the room the player is standing in... not interaction points.
        if (isPlaceInfo)
        {
            string placeString = randomGameElementsManager.unusedPlaces[spawnNumber];

            int random = Random.Range(0, 2);

            Debug.Log($"{placeString} {random}");

            switch (random)
            {
                case 0:
                    lines = new System.String[] {
                        "I found the " + placeString};
                    break;
                case 1:
                    lines = new System.String[] {
                         placeString};
                    break;
                case 2:
                    lines = new System.String[] {
                        "Found the " + placeString};
                    break;

                default:
                    break;
            }

            isPlaceInfo = false;

        }
        
        
        



        

    }


    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.tag == "Player")
        {
            canActivate = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.tag == "Player")
        {
            canActivate = false;
        }
    }

}
