using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharStats : MonoBehaviour
{
    public static CharStats instance;

    public string charName;
    public int charLevel = 1;
    public int currentEXP;
    public int[] expToNextLevel;
    public int maxLevel = 100;
    public int baseExp = 1000;

    public string characterJob;


    public int currentHP;
    public int maxHP;
    public int currentMP;
    public int maxMP;

    public int charStr, charDex, charVit, charAgi, charInt, charMnd, charChr;


    public int attack, defense, mattack, mdefense, evasion, accuracy, mevasion, maccuracy, speed, luck, element;

    public string equippedWeapon, equippedOffhand, equippedHead, equippedBody, equippedHands, equippedLegs, equippedFeet, equippedAccessory;


    public int weaponPower = 1;
    public int offhandPower = 1;
    public int headArmorPower = 1;
    public int bodyArmorPower = 1;
    public int handsArmorPower = 1;
    public int legsArmorPower = 1;
    public int feetArmorPower = 1;
    public int accessoryArmorPower = 1;

    public int weaponMPower = 1;
    public int weaponAccuracy = 1;
    public int weaponMAccuracy = 1;
    public int weaponSpeed = 1;
    public string equippedArmor;
    public int armorDefense = 1;
    public int armorMDefense = 1;
    public int armorEvasion = 1;
    public int armorMEvasion = 1;


    public int luckFactor = 0;

    public Sprite charImage;


    // this is the sequence of ABCD which is used to designate the character's class
    public string[] jobMeldString;




    // Start is called before the first frame update
    void Start()
    {

        instance = this;

        if(charLevel == 1)
        {
            charStr = 3;
            charDex = 3;
            charVit = 3;
            charAgi = 3;
            charInt = 3;
            charMnd = 3;
            charChr = 3;
            maxHP = 30;
            currentHP = maxHP;
            maxMP = 20;
            currentMP = maxMP;

            CalculateStats();
        }

        //setJobStringArray();

        expToNextLevel = new int[maxLevel];
        expToNextLevel[1] = baseExp;

        for(int i=2; i < expToNextLevel.Length; i++){

            expToNextLevel[i] = baseExp + (i * i * 20);
            //alternately could use: Mathf.FloorToInt(expToNextLevel[i-1]*1.05f);
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            AddExp(500);
        }
    }

    public void setJobStringArray()
    {
        if (characterJob == "Fighter")
        {
            if (jobMeldString.Length == 0)
            {
                jobMeldString[0] = "A";
            }
        }
        if (characterJob == "Healer")
        {
            if (jobMeldString.Length == 0)
            {
                jobMeldString[0] = "B";
            }
        }
        if (characterJob == "Rogue")
        {
            if (jobMeldString.Length == 0)
            {
                jobMeldString[0] = "C";
            }
        }
        if (characterJob == "Wizard")
        {
            if (jobMeldString.Length == 0)
            {
                jobMeldString[0] = "D";
            }
        }
    }

    public void AddExp(int expToAdd)
    {
        currentEXP += expToAdd;

        if(charLevel < maxLevel)
        {
            //level up
            if (currentEXP > expToNextLevel[charLevel])
            {
                currentEXP -= expToNextLevel[charLevel];
                charLevel++;



                //level up bonuses


                //returns a value between the lower bound and (upperbound - 1) example: (0,5) returns 0, 1, 2, 3, or 4
                int bonusStr = Random.Range(0, 5);
                int bonusDex = Random.Range(0, 5);
                int bonusVit = Random.Range(3, 5);
                int bonusAgi = Random.Range(0, 5);
                int bonusInt = Random.Range(0, 2);
                int bonusMnd = Random.Range(0, 2);
                int bonusChr = Random.Range(0, 2);

                charStr = Mathf.FloorToInt(charStr + bonusStr);
                charDex = Mathf.FloorToInt(charDex + bonusDex);
                charVit = Mathf.FloorToInt(charVit + bonusVit);
                charAgi = Mathf.FloorToInt(charAgi + bonusAgi);
                charInt = Mathf.FloorToInt(charInt + bonusInt);
                charMnd = Mathf.FloorToInt(charMnd + bonusMnd);
                charChr = Mathf.FloorToInt(charChr + bonusChr);

                maxHP = Mathf.FloorToInt((maxHP + (charVit / 4)) * 1.05f);
                maxMP = Mathf.FloorToInt((maxMP + ((charInt + charMnd + charChr) / 12) ) * 1.05f);


                CalculateStats();


                currentHP = maxHP;
                currentMP = maxMP;



                //note to add a manually set bonus add an array at the top, fill in the values in unity, 
                //and then here in the level up area add maxMp = maxMp + mpLvlBonus[playerLevel] where mpLevelBonus is the name of the array


            }
        }
        

        // if character is max level
        if(charLevel >= maxLevel)
        {
            currentEXP = 0;
        }


    }


    public void CalculateStats()
    {
        

        attack = Mathf.FloorToInt(charStr * 2.8f + weaponPower);
        defense = Mathf.FloorToInt(charVit * 2.8f + armorDefense);
        evasion = Mathf.FloorToInt(charAgi * 1.3f + armorEvasion);
        accuracy = Mathf.FloorToInt(charDex * 1.3f + weaponAccuracy);

        mattack = Mathf.FloorToInt(charInt * 2.8f + weaponMPower);
        mdefense = Mathf.FloorToInt(charMnd * 2.8f + armorMDefense);
        mevasion = Mathf.FloorToInt(charInt * 1.3f + armorMEvasion);
        maccuracy = Mathf.FloorToInt(charMnd * 1.3f + weaponMAccuracy);

        speed = Mathf.FloorToInt(1 + ((charDex + weaponSpeed) / 20));
        luck = Mathf.FloorToInt(1 + (charChr / 10) + luckFactor);
    }


}
