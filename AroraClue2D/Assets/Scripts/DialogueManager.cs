using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    

    public Text dialogueText;
    public Text nameText;


    public GameObject dialogueBox;
    public GameObject nameBox;

    
    //string array of dialogue lines. each array position will have a line of text we can call at different points in the game
    public string[] dialogueLines;

    public int currentLine;
    private bool justStarted;

    public static DialogueManager instance;


    private string questToMark;
    private bool markQuestComplete;
    private bool shouldMarkQuest;


    void Start()
    {
        instance = this;
 

        //dialogueText.text = dialogueLines[currentLine];
        
    }

    void Update()
    {
        if (dialogueBox.activeInHierarchy)
        {
            if (Input.GetButtonUp("Fire1"))
            {
                if (!justStarted)
                {
                    currentLine++;
                    

                    if (currentLine >= dialogueLines.Length)
                    {
                        dialogueBox.SetActive(false);
                        GameManager.Instance.dialogueActive = false;

                        if (shouldMarkQuest)
                        {
                            shouldMarkQuest = false;
                            if (markQuestComplete)
                            {
                                QuestManager.instance.MarkQuestComplete(questToMark);
                            }
                            else
                            {
                                QuestManager.instance.MarkQuestIncomplete(questToMark);
                            }
                        }
                    }
                    else
                    {
                        CheckIfName();
                        dialogueText.text = dialogueLines[currentLine];
                    }
                }
                else
                {
                    justStarted = false;
                }
                
            }
        }
        
    }

    public void ShowDialogue(string[] newLines, bool isPerson)
    {

        dialogueBox.SetActive(true);

        //plugging the array in directly to a placeholder array here allows it to be of an adaptable length and content easily
        dialogueLines = newLines;
        currentLine = 0;

        CheckIfName();

        dialogueText.text = dialogueLines[currentLine];
        justStarted = true;

        //if the dialogue is coming from a person, show the name box. otherwise hide the namebox
        nameBox.SetActive(isPerson);

        GameManager.Instance.dialogueActive = true;

    }

    public void CheckIfName()
    {
        if (dialogueLines[currentLine].StartsWith("n-")){

            nameText.text = dialogueLines[currentLine].Replace("n-", "");
            //put name in box

            currentLine++;
        }

    }

    public void shouldActivateQuestAtEnd(string questName, bool markComplete)
    {
        questToMark = questName;
        markQuestComplete = markComplete;
        shouldMarkQuest = true;

    }

}
