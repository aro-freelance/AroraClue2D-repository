using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueActivater : MonoBehaviour
{

    public string[] lines;

    private bool canActivate;

    public bool isPerson = true;

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
            DialogueManager.instance.ShowDialogue(lines, isPerson);
            DialogueManager.instance.shouldActivateQuestAtEnd(questToMark, markComplete);

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
