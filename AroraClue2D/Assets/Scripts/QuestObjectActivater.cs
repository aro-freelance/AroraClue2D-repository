using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestObjectActivater : MonoBehaviour
{

    public GameObject objectToActivate;
    public string questToCheck;
    public bool activeIfComplete;

    private bool initialCheckDone ;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //this is a way of making something happen once after everything is loaded
        //sometimes you want something to happen on start but can't put it in the start function because not everything is instatiated yet
        if (!initialCheckDone)
        {
            initialCheckDone = true;
            CheckCompletion();
        }
        
    }

    public void CheckCompletion()
    {
        Debug.Log("Tag:CheckCompletion");
        Debug.Log("quest to check " + questToCheck + " CheckifComplete(questToCheck) " + QuestManager.instance.CheckIfComplete(questToCheck)
                + " object to activate " + objectToActivate);
        if (QuestManager.instance.CheckIfComplete(questToCheck))
        {
            //if the quest is complete set the object to active
            objectToActivate.SetActive(activeIfComplete);
            Debug.Log("ping quest is complete");
        }
    }
}
