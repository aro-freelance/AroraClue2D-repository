using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public string[] questMarkerNames;
    public bool[] questMarkerComplete; //invisible to player but keeps track of triggers based on player progress
    public static QuestManager instance;



    // Start is called before the first frame update
    void Start()
    {
        instance = this;
        questMarkerComplete = new bool[questMarkerNames.Length];
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Debug.Log("enter the cave completion is " + CheckIfComplete("enter cave"));
            MarkQuestComplete("enter cave");
            MarkQuestComplete("Fight the Dragon");
            MarkQuestIncomplete("meld a new job");

            Debug.Log("enter the cave completion is NOW " + CheckIfComplete("enter cave"));
        }
        
    }

    //if the quest exists return its int value
    public int GetQuestNumber(string questToFind)
    {
        for(int i = 0; i < questMarkerNames.Length; i++)
        {

            if(questMarkerNames[i] == questToFind)
            {
                return i;
            }

        }

        Debug.LogError("Quest " + questToFind + " does not exist");
        return 0; // this is why you leave the first quest blank in the unity editor. we want it to be equal to null for testing and errors.
    }

    //check array to see if quest is complete
    public bool CheckIfComplete(string questToCheck)
    {
        //use int value of quest to check if the quest is complete
        if(GetQuestNumber(questToCheck) != 0)
        {
            return questMarkerComplete[GetQuestNumber(questToCheck)];
        }

        return false;
    }


    public void MarkQuestComplete(string questToMark)
    {
        questMarkerComplete[GetQuestNumber(questToMark)] = true;
        UpdateLocalQuestObjects();
    }

    public void MarkQuestIncomplete(string questToMark)
    {
        questMarkerComplete[GetQuestNumber(questToMark)] = false;
        UpdateLocalQuestObjects();

    }

    //call this after any quest is marked complete or incomplete
    public void UpdateLocalQuestObjects()
    {
        QuestObjectActivater[] questObjects = FindObjectsOfType<QuestObjectActivater>();

        if(questObjects.Length > 0)
        {
            for(int i = 0; i < questObjects.Length; i++)
            {
                questObjects[i].CheckCompletion();
            }
        }
    }

}
