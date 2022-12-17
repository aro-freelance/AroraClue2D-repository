using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;


// I need to decide how to get values of less than 3 letters. As the logic is currently designed it will always output 3. But the system calls for this progression:
//  A -> AB -> ABC -> AABC -> AAABC -> AAABCC -> AAABCCD etc (note exact letters are only examples)
// So what we actually need to to be only adding one letter on each time. (could be randomly determined from the set of options)
// Also we need to account for the possiblity of AA or AAA. This could simply be reduced do to an A however this punishes the player for specializing which is not really intended.
// So perhaps we do need to make tables for that. Or perhaps we need to reroll it. For now reroll.

//note due to an error I found out that A is working find. so no need to worry about that

//unusual combination. TODO: Decide if I want to implement something for these. Currently I am rerolling them. 
//Note one problem with this is that it is randomly assigning another value to the end. it is not checking if the play has chosen this value at any point 
//so you could end up with a player who has only chosen fighter and rogue getting some mage thrown in randomly

//TODO Alphabetical doesn't work for example jobsDDA... will need to either redo the table names and all code refering to them to alphabetical
//... or find some way to sort double letters to the front


public class JobManager : MonoBehaviour
{
    public static JobManager instance;

    public int charNumber = 0;
    public int numberOfCharacters = 4;
    private bool setUp;
    string[] letterArray = { "A", "B", "C", "D" };

    public string[] jobStringArray;
    public string[] valuesForTable;

    public int fighterCount, healerCount, rogueCount, mageCount;

    public int classTier;

    public string currentMeldFirstPart;
    public string currentMeldSecondPart;

    //get a single A,B,C,D from the job string (3 times for three choices)
    public string currentJobDominantEssenceRoll1, currentJobDominantEssenceRoll2, currentJobDominantEssenceRoll3;

    public string currentClassName;
    public string newClassChoiceOne, newClassChoiceTwo, newClassChoiceThree;


    public Button meld1FighterButton, meld1HealerButton, meld1RogueButton, meld1MageButton, meld2FighterButton, meld2HealerButton, meld2RogueButton, meld2MageButton;
    public Button choiceOneButton, choiceTwoButton, choiceThreeButton;
    public Button meldButtonPanelOne, cancelButtonPanelOne, meldButtonPanelTwo, cancelButtonPanelTwo;
    public Button[] characterButtons;

    public Text choiceOneDescription, choiceTwoDescripton, choiceThreeDescription;
    public Text choiceOneButtonText, choiceTwoButtonText, choiceThreeButtonText;
    public Text choicePanelHeader;

    public bool meldOneIsSelected;
    public bool meldTwoIsSelected;

    public bool classChoiceIsSelected;

    public bool isTitle, isPrefix, isMainJob, isPlayerChoice;

    //job 0, prefix 1, title 2
    public int jobPrefixOrTitleInt;
    public string whatIsBeingUpdated;
    public bool showReRollPanel = false;

    //fill out these tables with strings in unity 

    public string[] jobA, jobB, jobC, jobD,
                    jobAB, jobAC, jobAD, jobBC, jobBD, jobCD,
                    jobAAB, jobAAC, jobAAD, jobBBA, jobBBC, jobBBD, jobCCA, jobCCB, jobCCD, jobDDA, jobDDB, jobDDC, jobABC, jobABD, jobACD, jobBCD;


    public string[] titlesA, titlesB, titlesC, titlesD,
                    titlesAB, titlesAC, titlesAD, titlesBC, titlesBD, titlesCD,
                    titlesAAB, titlesAAC, titlesAAD, titlesBBA, titlesBBC, titlesBBD, titlesCCA, titlesCCB, titlesCCD, titlesDDA, titlesDDB, titlesDDC, titlesABC, titlesABD, titlesACD, titlesBCD;

    

    public string[] prefixesA, prefixesB, prefixesC, prefixesD,
                    prefixesAB, prefixesAC, prefixesAD, prefixesBC, prefixesBD, prefixesCD,
                    prefixesAAB, prefixesAAC, prefixesAAD, prefixesBBA, prefixesBBC, prefixesBBD, prefixesCCA, prefixesCCB, prefixesCCD, prefixesDDA, prefixesDDB, prefixesDDC, prefixesABC, prefixesABD, prefixesACD, prefixesBCD;

    public string[] jobDescriptions;
    public string[] titleDescriptions;
    public string[] prefixDescriptions;
    



    // Start is called before the first frame update
    void Start()
    {
        instance = this;


    }

    // Update is called once per frame
    void Update()
    {
        

        if (Input.GetKeyDown(KeyCode.B))
        {

            string testValue1 = letterArray[Random.Range(0,4)];
            string testValue2 = letterArray[Random.Range(0, 4)];
            string testValue3 = letterArray[Random.Range(0, 4)];
            string[] testValueArray = { testValue1, testValue2, testValue3 };

            jobStringArray = testValueArray;
           
            currentMeldFirstPart = letterArray[Random.Range(0, 4)]; ;
            currentMeldSecondPart = letterArray[Random.Range(0, 4)]; ;
            Meld();
        }
        if (Input.GetKeyDown(KeyCode.N))
        {

            string testValue1 = letterArray[Random.Range(0, 4)];
            string testValue2 = letterArray[Random.Range(0, 4)];
            string testValue3 = letterArray[Random.Range(0, 4)];
            string testValue4 = letterArray[Random.Range(0, 4)];
            string testValue5 = letterArray[Random.Range(0, 4)];
            string testValue6 = letterArray[Random.Range(0, 4)];
            string[] testValueArray = { testValue1, testValue2, testValue3, testValue4, testValue5, testValue6};

            jobStringArray = testValueArray;

            currentMeldFirstPart = letterArray[Random.Range(0, 4)];
            currentMeldSecondPart = letterArray[Random.Range(0, 4)];
            Meld();
        }
        if (Input.GetKeyDown(KeyCode.M))
        {

            string testValue1 = letterArray[Random.Range(0, 4)];
            string testValue2 = letterArray[Random.Range(0, 4)];
            string testValue3 = letterArray[Random.Range(0, 4)];
            string testValue4 = letterArray[Random.Range(0, 4)];
            string testValue5 = letterArray[Random.Range(0, 4)];
            string testValue6 = letterArray[Random.Range(0, 4)];
            string testValue7 = letterArray[Random.Range(0, 4)];
            string testValue8 = letterArray[Random.Range(0, 4)];
            string testValue9 = letterArray[Random.Range(0, 4)];
            string[] testValueArray = { testValue1, testValue2, testValue3, testValue4, testValue5, testValue6, testValue7, testValue8, testValue9};

            jobStringArray = testValueArray;

            currentMeldFirstPart = letterArray[Random.Range(0, 4)];
            currentMeldSecondPart = letterArray[Random.Range(0, 4)];
            Meld();
        }

        //controlled by the bool showReRollPanel
        OpenRerollChoicePanel();

        if (!setUp)
        {
            setUp = true;
            charNumber = 0;
            setButtonsToShowActiveCharacter();
            for (int i = 0; i < characterButtons.Length; i++)
            {
                characterButtons[i].GetComponentInChildren<Text>().text = GameManager.Instance.playerStats[i].charName;
            }
        }

        if (meldOneIsSelected && meldTwoIsSelected)
        {
            meldOneIsSelected = false;
            meldTwoIsSelected = false;

            
            Meld();

        }
    }

  

    public void Meld()
    {



        getValuesFromJobString();
        getClassTier();
        SelectRandomString();

    }

    


    public void GetMeldButtonPressedValue(int positionPressed)
    {

        if(positionPressed < 4)
        {
            if(positionPressed == 0)
            {
                currentMeldFirstPart = "A";
                meldOneIsSelected = true;
            }
            if (positionPressed == 1)
            {
                currentMeldFirstPart = "B";
                meldOneIsSelected = true;
            }
            if (positionPressed == 2)
            {
                currentMeldFirstPart = "C";
                meldOneIsSelected = true;
            }
            if (positionPressed == 3)
            {
                currentMeldFirstPart = "D";
                meldOneIsSelected = true;
            }


        }

        if(positionPressed >= 4)
        {
            if (positionPressed == 4)
            {
                currentMeldFirstPart = "A";
                meldTwoIsSelected = true;
            }
            if (positionPressed == 5)
            {
                currentMeldFirstPart = "B";
                meldTwoIsSelected = true;
            }
            if (positionPressed == 6)
            {
                currentMeldFirstPart = "C";
                meldTwoIsSelected = true;
            }
            if (positionPressed == 7)
            {
                currentMeldFirstPart = "D";
                meldTwoIsSelected = true;
            }
        }

    }

   public void getClassTier()
    {
        classTier = jobStringArray.Length;
        if (classTier <= 3)
        {
            isMainJob = true;
            isPrefix = false;
            isTitle = false;
            whatIsBeingUpdated = "Class";
        }
        if (classTier >= 4 && classTier <= 6)
        {
            isMainJob = false;
            isPrefix = true;
            isTitle = false;
            whatIsBeingUpdated = "Prefix";
        }
        if (classTier >= 7 && classTier <= 10)
        {
            isMainJob = false;
            isPrefix = false;
            isTitle = true;
            whatIsBeingUpdated = "Title";
        }
        if(classTier >= 11)
        {
            showReRollPanel = true;
        }
    }

    public void OpenRerollChoicePanel()
    {
        //TODO 

        //opened by GetClassTier if class tier is >= 11
        if (showReRollPanel)
        {
            //activate the panel
        }
        // closed by GetChoiceOfReroll when a button is pressed. GetChoiceOfReroll is attached to the buttons on the Reroll Panel in Unity
        else
        {
            //deactivate the panel
        }

    }

    public void selectNewCharacter(int charSelected)
    {
        charNumber = charSelected;
        setButtonsToShowActiveCharacter();
    }


    public void setButtonsToShowActiveCharacter()
    {
        for(int i = 0; i < characterButtons.Length; i++)
        {
            if (i != charNumber)
            {
                Color color = new Color(.75f, .75f, .75f, .8f);
                characterButtons[i].GetComponentInChildren<Image>().color = color;
            }
            else
            {
                Color color = new Color(1f, 1f, 1f, 1f);
                characterButtons[i].GetComponentInChildren<Image>().color = color;
            }
        }
    }


    public void getValuesFromJobString()
    {
        //note Random.Range min is inclusive but max is exclusive. it can roll the min but not up to the max
        int roll1 = Random.Range(0, (jobStringArray.Length));
        int roll2 = Random.Range(0, (jobStringArray.Length));
        int roll3 = Random.Range(0, (jobStringArray.Length));

        currentJobDominantEssenceRoll1 = jobStringArray[roll1];
        currentJobDominantEssenceRoll2 = jobStringArray[roll2];
        currentJobDominantEssenceRoll3 = jobStringArray[roll3];

        classTier = jobStringArray.Length;

    }

    public void OpenMeld(int charToUseOn)
    {
        
        charNumber = charToUseOn;

        //open meld window
        GameMenu.instance.ToggleWindow(2);



    }

    public void GetChoiceOfReroll(int choiceInt)
    {
        if(choiceInt == 0)
        {
            isMainJob = true;
            isPrefix = false;
            isTitle = false;
           
            showReRollPanel = false;
            
        }
        if (choiceInt == 1)
        {
            isMainJob = false;
            isPrefix = true;
            isTitle = false;

            showReRollPanel = false;
        }
        if (choiceInt == 2)
        {
            isMainJob = false;
            isPrefix = false;
            isTitle = true;

            showReRollPanel = false;
        }
    }

    public void SelectRandomString()
    {

        //combine the value from the current job with the two selected value to make a string tag
        string value1 = currentJobDominantEssenceRoll1 + currentMeldFirstPart + currentMeldSecondPart;
        string value2 = currentJobDominantEssenceRoll2 + currentMeldFirstPart + currentMeldSecondPart;
        string value3 = currentJobDominantEssenceRoll3 + currentMeldFirstPart + currentMeldSecondPart;


        //sort value 1
        //sort duplicates to front
        if(currentJobDominantEssenceRoll1 == currentMeldFirstPart || currentJobDominantEssenceRoll1 == currentMeldSecondPart || currentMeldFirstPart == currentMeldSecondPart)
        {

            string[] localArray = { currentJobDominantEssenceRoll1, currentMeldFirstPart, currentMeldSecondPart };
            //if the first two letters are the same 1,2,3
            if (localArray[0] == localArray[1])
            {
                value1 = localArray[0] + localArray[1] + localArray[2];
            }
            //if the first letter is the same as the last letter 1,3,2
            else if (localArray[0] == localArray[2])
            {
                value1 = localArray[0] + localArray[2] + localArray[1];
            }
            //if the second 2 letters are the same 2,3,1
            else
            {
                value1 = localArray[1] + localArray[2] + localArray[0];
            }

        }
        //else sort alphabetically
        else
        {
            value1 = SortString.instance.AlphabeticallySortCharOfString(value1);
        }

        //sort value 2
        //sort duplicates to front
        if (currentJobDominantEssenceRoll2 == currentMeldFirstPart || currentJobDominantEssenceRoll2 == currentMeldSecondPart || currentMeldFirstPart == currentMeldSecondPart)
        {
            string[] localArray = { currentJobDominantEssenceRoll2, currentMeldFirstPart, currentMeldSecondPart };
            //if the first two letters are the same 1,2,3
            if (localArray[0] == localArray[1])
            {
                value2 = localArray[0] + localArray[1] + localArray[2];
            }
            //if the first letter is the same as the last letter 1,3,2
            else if (localArray[0] == localArray[2])
            {
                value2 = localArray[0] + localArray[2] + localArray[1];
            }
            //if the second 2 letters are the same 2,3,1
            else
            {
                value2= localArray[1] + localArray[2] + localArray[0];
            }

            
        }
        //else sort alphabetically
        else
        {
            value2 = SortString.instance.AlphabeticallySortCharOfString(value2);
        }

        //sort value 3
        //sort duplicates to front
        if (currentJobDominantEssenceRoll3 == currentMeldFirstPart || currentJobDominantEssenceRoll3 == currentMeldSecondPart || currentMeldFirstPart == currentMeldSecondPart)
        {

            Debug.Log("ping value 3 dup sort");
            string[] localArray = { currentJobDominantEssenceRoll3, currentMeldFirstPart, currentMeldSecondPart };
            //if the first two letters are the same 1,2,3
            if (localArray[0] == localArray[1])
            {
                Debug.Log("ping value 3 dup sort XXY");
                value3 = localArray[0] + localArray[1] + localArray[2];
            }
            //if the first letter is the same as the last letter 1,3,2
            else if (localArray[0] == localArray[2])
            {
                Debug.Log("ping value 3 dup sort XYX");
                value3 = localArray[0] + localArray[2] + localArray[1];
            }
            //if the second 2 letters are the same 2,3,1
            else
            {
                Debug.Log("ping value 3 dup sort XYY");
                value3 = localArray[1] + localArray[2] + localArray[0];
            }
        }
        //else sort alphabetically
        else
        {
            value3 = SortString.instance.AlphabeticallySortCharOfString(value3);
        }

        
        //put the values in an array. 3 values because we want 3 random choices
        valuesForTable[0] = value1;
        valuesForTable[1] = value2;
        valuesForTable[2] = value3;


        //use a bool to identify which part of the class is being updated

        if (isMainJob)
        {
            //clear the values of the class choices
            newClassChoiceOne = "";
            newClassChoiceTwo = "";
            newClassChoiceThree = "";

            //for each of the 3 three letter identifiers find the correct table and roll a random value from it
            for (int i = 0; i < valuesForTable.Length; i++)
            {
                string jobCodeABCD = valuesForTable[i];
                Debug.Log("job code = " + jobCodeABCD);



                //unusual combination. TODO: Decide if I want to implement something for these. Currently I am rerolling them.
                if (jobCodeABCD == "AA")
                {
                    Debug.Log("jobCode was invalid : " + jobCodeABCD);
                    string[] localArray = { "B", "C", "D" };
                    int r = Random.Range(0, 3);
                    jobCodeABCD = "A" + localArray[r];
                    Debug.Log("new jobCode is " + jobCodeABCD);
                }
                if (jobCodeABCD == "BB")
                {
                    Debug.Log("jobCode was invalid : " + jobCodeABCD);
                    string[] localArray = { "A", "C", "D" };
                    int r = Random.Range(0, 3);
                    jobCodeABCD = "B" + localArray[r];
                    Debug.Log("new jobCode is " + jobCodeABCD);
                }
                if (jobCodeABCD == "CC")
                {
                    Debug.Log("jobCode was invalid : " + jobCodeABCD);
                    string[] localArray = { "A", "B", "D" };
                    int r = Random.Range(0, 3);
                    jobCodeABCD = "CC" + localArray[r];
                    Debug.Log("new jobCode is " + jobCodeABCD);
                }
                if (jobCodeABCD == "DD")
                {
                    Debug.Log("jobCode was invalid : " + jobCodeABCD);
                    string[] localArray = { "A", "B", "C" };
                    int r = Random.Range(0, 3);
                    jobCodeABCD = "DD" + localArray[r];
                    Debug.Log("new jobCode is " + jobCodeABCD);
                }
                if (jobCodeABCD == "AAA")
                {
                    Debug.Log("jobCode was invalid : " + jobCodeABCD);
                    string[] localArray = { "B", "C", "D"};
                    int r = Random.Range(0, 3);
                    jobCodeABCD = "AA" + localArray[r];
                    Debug.Log("new jobCode is " + jobCodeABCD);
                }
                if (jobCodeABCD == "BBB")
                {
                    Debug.Log("jobCode was invalid : " + jobCodeABCD);
                    string[] localArray = { "A", "C", "D" };
                    int r = Random.Range(0, 3);
                    jobCodeABCD = "BB" + localArray[r];
                    Debug.Log("new jobCode is " + jobCodeABCD);
                }
                if (jobCodeABCD == "CCC")
                {
                    Debug.Log("jobCode was invalid : " + jobCodeABCD);
                    string[] localArray = { "A", "B", "D" };
                    int r = Random.Range(0, 3);
                    jobCodeABCD = "CC" + localArray[r];
                    Debug.Log("new jobCode is " + jobCodeABCD);
                }
                if (jobCodeABCD == "DDD")
                {
                    Debug.Log("jobCode was invalid : " + jobCodeABCD);
                    string[] localArray = { "A", "B", "C" };
                    int r = Random.Range(0, 3);
                    jobCodeABCD = "DD" + localArray[r];
                    Debug.Log("new jobCode is " + jobCodeABCD);
                }
                

                //A note on Random.Range: It only goes from min to (max - 1). However we still use the array.Length as the max because array starts with 0. So array.Length is actually
                // the correct value for the last position in the array.

                if (jobCodeABCD == "A")
                {
                    int x = Random.Range(0, jobA.Length);
                    string y = jobA[x];

                    if(newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "B")
                {
                    int x = Random.Range(0, jobB.Length);
                    string y = jobB[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "C")
                { 
                    int x = Random.Range(0, jobC.Length);
                    string y = jobC[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "D")
                {
                    int x = Random.Range(0, jobD.Length);
                    string y = jobD[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "AB")
                {
                    int x = Random.Range(0, jobAB.Length);
                    string y = jobAB[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "AC")
                {
                    int x = Random.Range(0, jobAC.Length);
                    string y = jobAC[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "AD")
                {
                    int x = Random.Range(0, jobAD.Length);
                    string y = jobAD[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "BC")
                {
                    int x = Random.Range(0, jobABC.Length);
                    string y = jobBC[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "BD")
                {
                    int x = Random.Range(0, jobBD.Length);
                    string y = jobBD[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "CD")
                {
                    int x = Random.Range(0, jobCD.Length);
                    string y = jobCD[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "AAB")
                {
                    int x = Random.Range(0, jobAAB.Length);
                    string y = jobAAB[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "AAC")
                {
                    int x = Random.Range(0, jobAAC.Length);
                    string y = jobAAC[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "AAD")
                {
                    int x = Random.Range(0, jobAAD.Length);
                    string y = jobAAD[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "BBA")
                {
                    int x = Random.Range(0, jobBBA.Length);
                 
                    string y = jobBBA[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "BBC")
                {
                    int x = Random.Range(0, jobBBC.Length);

                    string y = jobBBC[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "BBD")
                {
                    int x = Random.Range(0, jobBBD.Length);

                    string y = jobBBD[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "CCA")
                {
                    int x = Random.Range(0, jobCCA.Length);

                    string y = jobCCA[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "CCB")
                {
                    int x = Random.Range(0, jobCCB.Length);

                    string y = jobCCB[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "CCD")
                {
                    int x = Random.Range(0, jobCCD.Length);

                    string y = jobCCD[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "DDA")
                {
                    int x = Random.Range(0, jobDDA.Length);

                    string y = jobDDA[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "DDB")
                {
                    int x = Random.Range(0, jobDDB.Length);

                    string y = jobDDB[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "DDC")
                {
                    int x = Random.Range(0, jobDDC.Length);

                    string y = jobDDC[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "ABC")
                {
                    int x = Random.Range(0, jobABC.Length);

                    string y = jobABC[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "ABD")
                {
                    int x = Random.Range(0, jobABD.Length);

                    string y = jobABD[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "ACD")
                {
                    int x = Random.Range(0, jobACD.Length);

                    string y = jobACD[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "BCD")
                {
                    int x = Random.Range(0, jobBCD.Length);

                    string y = jobBCD[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }


                //if choices are duplicates removes the duplicates 
                //TODO and disable the buttons within the if statements below
                if(newClassChoiceOne == newClassChoiceThree)
                {
                    newClassChoiceThree = "";
                }
                if(newClassChoiceOne == newClassChoiceTwo)
                {
                    newClassChoiceTwo = newClassChoiceThree;
                }
                if(newClassChoiceTwo == newClassChoiceThree)
                {
                    newClassChoiceThree = "";
                }



            }
            
            //TODO use those values to update the choice panel

        }

        if (isPrefix)
        {

            //clear the values of the class choices
            newClassChoiceOne = "";
            newClassChoiceTwo = "";
            newClassChoiceThree = "";

            //for each of the 3 three letter identifiers find the correct table and roll a random value from it
            for (int i = 0; i < valuesForTable.Length; i++)
            {
                string jobCodeABCD = valuesForTable[i];
                Debug.Log("job code = " + jobCodeABCD);


                //unusual combination. TODO: Decide if I want to implement something for these. Currently I am rerolling them.
                if (jobCodeABCD == "AA")
                {
                    Debug.Log("jobCode was invalid : " + jobCodeABCD);
                    string[] localArray = { "B", "C", "D" };
                    int r = Random.Range(0, 3);
                    jobCodeABCD = "A" + localArray[r];
                    Debug.Log("new jobCode is " + jobCodeABCD);
                }
                if (jobCodeABCD == "BB")
                {
                    Debug.Log("jobCode was invalid : " + jobCodeABCD);
                    string[] localArray = { "A", "C", "D" };
                    int r = Random.Range(0, 3);
                    jobCodeABCD = "B" + localArray[r];
                    Debug.Log("new jobCode is " + jobCodeABCD);
                }
                if (jobCodeABCD == "CC")
                {
                    Debug.Log("jobCode was invalid : " + jobCodeABCD);
                    string[] localArray = { "A", "B", "D" };
                    int r = Random.Range(0, 3);
                    jobCodeABCD = "CC" + localArray[r];
                    Debug.Log("new jobCode is " + jobCodeABCD);
                }
                if (jobCodeABCD == "DD")
                {
                    Debug.Log("jobCode was invalid : " + jobCodeABCD);
                    string[] localArray = { "A", "B", "C" };
                    int r = Random.Range(0, 3);
                    jobCodeABCD = "DD" + localArray[r];
                    Debug.Log("new jobCode is " + jobCodeABCD);
                }
                if (jobCodeABCD == "AAA")
                {
                    Debug.Log("jobCode was invalid : " + jobCodeABCD);
                    string[] localArray = { "B", "C", "D" };
                    int r = Random.Range(0, 3);
                    jobCodeABCD = "AA" + localArray[r];
                    Debug.Log("new jobCode is " + jobCodeABCD);
                }
                if (jobCodeABCD == "BBB")
                {
                    Debug.Log("jobCode was invalid : " + jobCodeABCD);
                    string[] localArray = { "A", "C", "D" };
                    int r = Random.Range(0, 3);
                    jobCodeABCD = "BB" + localArray[r];
                    Debug.Log("new jobCode is " + jobCodeABCD);
                }
                if (jobCodeABCD == "CCC")
                {
                    Debug.Log("jobCode was invalid : " + jobCodeABCD);
                    string[] localArray = { "A", "B", "D" };
                    int r = Random.Range(0, 3);
                    jobCodeABCD = "CC" + localArray[r];
                    Debug.Log("new jobCode is " + jobCodeABCD);
                }
                if (jobCodeABCD == "DDD")
                {
                    Debug.Log("jobCode was invalid : " + jobCodeABCD);
                    string[] localArray = { "A", "B", "C" };
                    int r = Random.Range(0, 3);
                    jobCodeABCD = "DD" + localArray[r];
                    Debug.Log("new jobCode is " + jobCodeABCD);
                }


                //A note on Random.Range: It only goes from min to (max - 1). However we still use the array.Length as the max because array starts with 0. So array.Length is actually
                // the correct value for the last position in the array.

                if (jobCodeABCD == "A")
                {
                    int x = Random.Range(0, prefixesA.Length);
                    string y = prefixesA[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "B")
                {
                    int x = Random.Range(0, prefixesB.Length);
                    string y = prefixesB[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "C")
                {
                    int x = Random.Range(0, prefixesC.Length);
                    string y = prefixesC[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "D")
                {
                    int x = Random.Range(0, prefixesD.Length);
                    string y = prefixesD[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "AB")
                {
                    int x = Random.Range(0, prefixesAB.Length);
                    string y = prefixesAB[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "AC")
                {
                    int x = Random.Range(0, prefixesAC.Length);
                    string y = prefixesAC[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "AD")
                {
                    int x = Random.Range(0, prefixesAD.Length);
                    string y = prefixesAD[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "BC")
                {
                    int x = Random.Range(0, prefixesABC.Length);
                    string y = prefixesBC[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "BD")
                {
                    int x = Random.Range(0, prefixesBD.Length);
                    string y = prefixesBD[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "CD")
                {
                    int x = Random.Range(0, prefixesCD.Length);
                    string y = prefixesCD[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "AAB")
                {
                    int x = Random.Range(0, prefixesAAB.Length);
                    string y = prefixesAAB[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "AAC")
                {
                    int x = Random.Range(0, prefixesAAC.Length);
                    string y = prefixesAAC[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "AAD")
                {
                    int x = Random.Range(0, prefixesAAD.Length);
                    string y = prefixesAAD[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "BBA")
                {
                    int x = Random.Range(0, prefixesBBA.Length);

                    string y = prefixesBBA[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "BBC")
                {
                    int x = Random.Range(0, prefixesBBC.Length);

                    string y = prefixesBBC[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "BBD")
                {
                    int x = Random.Range(0, prefixesBBD.Length);

                    string y = prefixesBBD[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "CCA")
                {
                    int x = Random.Range(0, prefixesCCA.Length);

                    string y = prefixesCCA[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "CCB")
                {
                    int x = Random.Range(0, prefixesCCB.Length);

                    string y = prefixesCCB[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "CCD")
                {
                    int x = Random.Range(0, prefixesCCD.Length);

                    string y = prefixesCCD[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "DDA")
                {
                    int x = Random.Range(0, prefixesDDA.Length);

                    string y = prefixesDDA[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "DDB")
                {
                    int x = Random.Range(0, prefixesDDB.Length);

                    string y = prefixesDDB[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "DDC")
                {
                    int x = Random.Range(0, prefixesDDC.Length);

                    string y = prefixesDDC[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "ABC")
                {
                    int x = Random.Range(0, prefixesABC.Length);

                    string y = prefixesABC[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "ABD")
                {
                    int x = Random.Range(0, prefixesABD.Length);

                    string y = prefixesABD[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "ACD")
                {
                    int x = Random.Range(0, prefixesACD.Length);

                    string y = prefixesACD[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "BCD")
                {
                    int x = Random.Range(0, prefixesBCD.Length);

                    string y = prefixesBCD[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }


                //if choices are duplicates removes the duplicates 
                //TODO and disable the buttons within the if statements below
                if (newClassChoiceOne == newClassChoiceThree)
                {
                    newClassChoiceThree = "";
                }
                if (newClassChoiceOne == newClassChoiceTwo)
                {
                    newClassChoiceTwo = newClassChoiceThree;
                }
                if (newClassChoiceTwo == newClassChoiceThree)
                {
                    newClassChoiceThree = "";
                }

            }

            //TODO use those values to update the choice panel
        }

        if (isTitle)
        {
            //clear the values of the class choices
            newClassChoiceOne = "";
            newClassChoiceTwo = "";
            newClassChoiceThree = "";

            //for each of the 3 three letter identifiers find the correct table and roll a random value from it
            for (int i = 0; i < valuesForTable.Length; i++)
            {
                string jobCodeABCD = valuesForTable[i];
                Debug.Log("job code = " + jobCodeABCD);

                //unusual combination. TODO: Decide if I want to implement something for these. Currently I am rerolling them. 
                //Note one problem with this is that it is randomly assigning another value to the end. it is not checking if the play has chosen this value at any point 
                //so you could end up with a player who has only chosen fighter and rogue getting some mage thrown in randomly
                if (jobCodeABCD == "AA")
                {
                    Debug.Log("jobCode was invalid : " + jobCodeABCD);
                    string[] localArray = { "B", "C", "D" };
                    int r = Random.Range(0, 3);
                    jobCodeABCD = "A" + localArray[r];
                    Debug.Log("new jobCode is " + jobCodeABCD);
                }
                if (jobCodeABCD == "BB")
                {
                    Debug.Log("jobCode was invalid : " + jobCodeABCD);
                    string[] localArray = { "A", "C", "D" };
                    int r = Random.Range(0, 3);
                    jobCodeABCD = "B" + localArray[r];
                    Debug.Log("new jobCode is " + jobCodeABCD);
                }
                if (jobCodeABCD == "CC")
                {
                    Debug.Log("jobCode was invalid : " + jobCodeABCD);
                    string[] localArray = { "A", "B", "D" };
                    int r = Random.Range(0, 3);
                    jobCodeABCD = "CC" + localArray[r];
                    Debug.Log("new jobCode is " + jobCodeABCD);
                }
                if (jobCodeABCD == "DD")
                {
                    Debug.Log("jobCode was invalid : " + jobCodeABCD);
                    string[] localArray = { "A", "B", "C" };
                    int r = Random.Range(0, 3);
                    jobCodeABCD = "DD" + localArray[r];
                    Debug.Log("new jobCode is " + jobCodeABCD);
                }
                if (jobCodeABCD == "AAA")
                {
                    Debug.Log("jobCode was invalid : " + jobCodeABCD);
                    string[] localArray = { "B", "C", "D" };
                    int r = Random.Range(0, 3);
                    jobCodeABCD = "AA" + localArray[r];
                    Debug.Log("new jobCode is " + jobCodeABCD);
                }
                if (jobCodeABCD == "BBB")
                {
                    Debug.Log("jobCode was invalid : " + jobCodeABCD);
                    string[] localArray = { "A", "C", "D" };
                    int r = Random.Range(0, 3);
                    jobCodeABCD = "BB" + localArray[r];
                    Debug.Log("new jobCode is " + jobCodeABCD);
                }
                if (jobCodeABCD == "CCC")
                {
                    Debug.Log("jobCode was invalid : " + jobCodeABCD);
                    string[] localArray = { "A", "B", "D" };
                    int r = Random.Range(0, 3);
                    jobCodeABCD = "CC" + localArray[r];
                    Debug.Log("new jobCode is " + jobCodeABCD);
                }
                if (jobCodeABCD == "DDD")
                {
                    Debug.Log("jobCode was invalid : " + jobCodeABCD);
                    string[] localArray = { "A", "B", "C" };
                    int r = Random.Range(0, 3);
                    jobCodeABCD = "DD" + localArray[r];
                    Debug.Log("new jobCode is " + jobCodeABCD);
                }

                //A note on Random.Range: It only goes from min to (max - 1). However we still use the array.Length as the max because array starts with 0. So array.Length is actually
                // the correct value for the last position in the array.

                if (jobCodeABCD == "A")
                {
                    int x = Random.Range(0, titlesA.Length);
                    string y = titlesA[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "B")
                {
                    int x = Random.Range(0, titlesB.Length);
                    string y = titlesB[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "C")
                {
                    int x = Random.Range(0, titlesC.Length);
                    string y = titlesC[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "D")
                {
                    int x = Random.Range(0, titlesD.Length);
                    string y = titlesD[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "AB")
                {
                    int x = Random.Range(0, titlesAB.Length);
                    string y = titlesAB[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "AC")
                {
                    int x = Random.Range(0, titlesAC.Length);
                    string y = titlesAC[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "AD")
                {
                    int x = Random.Range(0, titlesAD.Length);
                    string y = titlesAD[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "BC")
                {
                    int x = Random.Range(0, titlesABC.Length);
                    string y = titlesBC[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "BD")
                {
                    int x = Random.Range(0, titlesBD.Length);
                    string y = titlesBD[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "CD")
                {
                    int x = Random.Range(0, titlesCD.Length);
                    string y = titlesCD[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "AAB")
                {
                    int x = Random.Range(0, titlesAAB.Length);
                    string y = titlesAAB[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "AAC")
                {
                    int x = Random.Range(0, titlesAAC.Length);
                    string y = titlesAAC[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "AAD")
                {
                    int x = Random.Range(0, titlesAAD.Length);
                    string y = titlesAAD[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "BBA")
                {
                    int x = Random.Range(0, titlesBBA.Length);

                    string y = titlesBBA[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "BBC")
                {
                    int x = Random.Range(0, titlesBBC.Length);

                    string y = titlesBBC[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "BBD")
                {
                    int x = Random.Range(0, titlesBBD.Length);

                    string y = titlesBBD[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "CCA")
                {
                    int x = Random.Range(0, titlesCCA.Length);

                    string y = titlesCCA[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "CCB")
                {
                    int x = Random.Range(0, titlesCCB.Length);

                    string y = titlesCCB[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "CCD")
                {
                    int x = Random.Range(0, titlesCCD.Length);

                    string y = titlesCCD[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "DDA")
                {
                    int x = Random.Range(0, titlesDDA.Length);

                    string y = titlesDDA[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "DDB")
                {
                    int x = Random.Range(0, titlesDDB.Length);

                    string y = titlesDDB[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "DDC")
                {
                    int x = Random.Range(0, titlesDDC.Length);

                    string y = titlesDDC[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "ABC")
                {
                    int x = Random.Range(0, titlesABC.Length);

                    string y = titlesABC[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "ABD")
                {
                    int x = Random.Range(0, titlesABD.Length);

                    string y = titlesABD[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "ACD")
                {
                    int x = Random.Range(0, titlesACD.Length);

                    string y = titlesACD[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }
                if (jobCodeABCD == "BCD")
                {
                    int x = Random.Range(0, titlesBCD.Length);

                    string y = titlesBCD[x];

                    if (newClassChoiceOne == "")
                    {
                        newClassChoiceOne = y;
                    }
                    else if (newClassChoiceTwo == "")
                    {
                        newClassChoiceTwo = y;
                    }
                    else
                    {
                        newClassChoiceThree = y;
                    }
                }


                //if choices are duplicates removes the duplicates 
                //TODO and disable the buttons within the if statements below
                if (newClassChoiceOne == newClassChoiceThree)
                {
                    newClassChoiceThree = "";
                }
                if (newClassChoiceOne == newClassChoiceTwo)
                {
                    newClassChoiceTwo = newClassChoiceThree;
                }
                if (newClassChoiceTwo == newClassChoiceThree)
                {
                    newClassChoiceThree = "";
                }



            }

            //TODO use those values to update the choice panel
        }

        
        //set text to the choice buttons
        if(newClassChoiceOne != "")
        {
            choiceOneButtonText.text = newClassChoiceOne;
            //TODO: update character information with new job class title
            //close the panel and go to stats
        }
        else
        {
            //this shouldn't happen
            choiceOneButtonText.text = "Cancel";
            //TODO: close the panel
        }
        if(newClassChoiceTwo != "")
        {
            choiceTwoButtonText.text = newClassChoiceTwo;
            //TODO: update character information with new job class title
            //close the panel and go to stats
        }
        else
        {
            //make button 2 inactive
            choiceTwoButtonText.text = "";
        }
        if (newClassChoiceThree != "")
        {
            choiceThreeButtonText.text = newClassChoiceThree;
            //TODO: update character information with new job class title
            //close the panel and go to stats
        }
        else
        {
            //make button three inactive
            choiceThreeButtonText.text = "";
        }

        //set text to the header
        choicePanelHeader.text = "Select Your Tier " + classTier + " " + whatIsBeingUpdated + " Option";

    }

    



}
