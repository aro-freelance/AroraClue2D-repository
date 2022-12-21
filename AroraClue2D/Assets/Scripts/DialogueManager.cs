
using System.Collections;
using System.Collections.Generic;
using System.Threading;
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
    private bool justStarted = true;
    private bool autoplayActive = false;

    public static DialogueManager instance;


    private float timer = 0;
    private float autoplayDelay = 5; //seconds

    private string questToMark;
    private bool markQuestComplete;
    private bool shouldMarkQuest;

    public string cutsceneEndType = "";


    void Start()
    {
        instance = this;
 

        //dialogueText.text = dialogueLines[currentLine];
        
    }

    void Update()
    {
        if (dialogueBox.activeInHierarchy)
        {
            GameMenu.instance.CloseMenu();

            //player progressed
            if(!autoplayActive)
            {
                if (Input.GetButtonUp("Fire1"))
                {
                    if (!justStarted)
                    {
                        NextLine();
                    }
                    else
                    {
                        justStarted = false;
                    }

                }
            }
            //autoplay cutscene
            else
            {
                if (timer > autoplayDelay)
                {
                    NextLine();
                    timer = 0;
                }

                timer += Time.deltaTime;
            }
            
        }
        
    }

    private void NextLine()
    {
        currentLine++;


        if (currentLine >= dialogueLines.Length)
        {
            dialogueBox.SetActive(false);
            GameManager.Instance.dialogueActive = false;


            switch (cutsceneEndType)
            {
                case "readyToResume":
                    GameManager.Instance.isReadyToEndGuessEvent = true;
                    break;

                case "readyToStartCountdown":
                    GameManager.Instance.isReadyToStartCountdown = true;
                    break;

                case "readyToCheckAnswers":
                    GameManager.Instance.isReadyToCheckAnswers = true;
                    break;

                case "readyToEndGame":
                    GameManager.Instance.isReadyToEndGame = true;
                    break;

                default:
                    break;

            }
            

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

    public void AutoPlayDialogue(string[] newLines, float delay_Sec, string cutsceneEndTypeString)
    {

        dialogueBox.SetActive(true);

        //set the string that determines what happens after the cutscene
        cutsceneEndType = cutsceneEndTypeString;

        //plugging the array in directly to a placeholder array here allows it to be of an adaptable length and content easily
        dialogueLines = newLines;
        currentLine = 0;

        CheckIfName();

        dialogueText.text = dialogueLines[currentLine];

        autoplayDelay = delay_Sec;
        timer = 0;

        nameBox.SetActive(true);

        autoplayActive = true;
        GameManager.Instance.cutsceneActive = true;

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
