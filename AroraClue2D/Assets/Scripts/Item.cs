using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    //an item is an object that can have an effect on the player

    //example items: weapon, armor, potion, level up component, crafting component, quest item, scroll of magic, item that unlocks an ability

    //some items will be destroyed after 1 to x uses

    //some items can be moved from character to character and some cannot. some can be sold and some cannot

    //some items can be used by some characters but not others

    public Sprite itemSprite;
    public string itemName;
    //for inventory description for player
    public string description;
    //how much does it sell for?

    [Header("Monetary Value")]
    public int value;
    public bool isNotForSale;
    //this stores the value of the effect of the item. for a potion it is how much hp+. for a hill giant potion it is how much str+. etc...


    //since items come in many kinds lets make some tags for types of items
    [Header("Item Tags")]
    public bool isWeapon;
    public bool isArmor;
    public bool isPotion;
    public bool isCraftMat;
    public bool isQuestItem;
    public bool isAbilityUnlocker;
    public bool isClassMeldItem;
    public bool isScroll;
    public bool isClassLocked;
    public bool isTeleport;
    public bool affectsStats;

    [Header("Number of Uses")]
    public bool isDisposable;
    public int numberOfUses;




    [Header("Stat Changing Items")]
    public bool affectHP;
    public bool affectMP;
    public bool affectMaxHP;
    public bool affectMaxMP;
    public bool affectStr;
    public bool affectDex;
    public bool affectVit;
    public bool affectAgi;
    public bool affectInt;
    public bool affectMnd;
    public bool affectChr;
    public bool affectAttack;
    public bool affectDefense;
    public bool affectMAttack;
    public bool affectMDefense;
    public bool affectEvasion;
    public bool affectAccuracy;
    public bool affectMEvasion;
    public bool affectMAccuracy;
    public bool affectSpeed;
    public bool affectLuck;
    public bool affectJob;
    public bool affectElement;
    public bool affectXP;
    public int amountToChange;
    public bool isTempBuff;
    public int durationOfTempAffect;
    public string newStringValue;

    [Header("Equipment Information")]
    public int weaponPower;
    public int armorPower;

    public bool isHead;
    public bool isBody;
    public bool isHands;
    public bool isLegs;
    public bool isFeet;
    public bool isAccessory;

    public bool isMainHand;
    public bool isOffHand;
    public bool isTwoHanded;
    public bool wieldingTwoHandedWeapon;


    public void Use(int charToUseOn)
    {
        CharStats selectedchar = GameManager.Instance.playerStats[charToUseOn];

        if (isPotion)
        {
            if (affectHP)
            {
                selectedchar.currentHP += amountToChange;
                if(selectedchar.currentHP > selectedchar.maxHP)
                {
                    selectedchar.currentHP = selectedchar.maxHP;
                }
            }
            if (affectMP)
            {
                selectedchar.currentMP += amountToChange;
                if (selectedchar.currentMP > selectedchar.maxMP)
                {
                    selectedchar.currentMP = selectedchar.maxMP;
                }
            }
        }

        if (isWeapon)
        {
            if(isMainHand && isTwoHanded)
            {
                //if there is currently a weapon equipped
                if (selectedchar.equippedWeapon != "")
                {
                    GameManager.Instance.AddItem(selectedchar.equippedWeapon);
                }
                if(selectedchar.equippedOffhand != "")
                {
                    GameManager.Instance.AddItem(selectedchar.equippedOffhand);
                }

                selectedchar.equippedWeapon = itemName;
                selectedchar.weaponPower = weaponPower;
                //the item is now equipped and not in inventory. it will return to inventory if/when it is unequipped. for now we remove it from inventory
                GameManager.Instance.RemoveItem(itemName);

            }
            if(isMainHand && !isOffHand)
            {
                //if there is currently a weapon equipped
                if (selectedchar.equippedWeapon != "")
                {
                    GameManager.Instance.AddItem(selectedchar.equippedWeapon);
                }

                selectedchar.equippedWeapon = itemName;
                selectedchar.weaponPower = weaponPower;
                //the item is now equipped and not in inventory. it will return to inventory if/when it is unequipped. for now we remove it from inventory
                GameManager.Instance.RemoveItem(itemName);

            }
            if(isOffHand && !isMainHand)
            {
                //if there is currently a weapon equipped
                if (selectedchar.equippedOffhand != "")
                {
                    GameManager.Instance.AddItem(selectedchar.equippedOffhand);
                }

                selectedchar.equippedOffhand = itemName;
                selectedchar.offhandPower = weaponPower;
                //the item is now equipped and not in inventory. it will return to inventory if/when it is unequipped. for now we remove it from inventory
                GameManager.Instance.RemoveItem(itemName);

            }
            if(isMainHand && isOffHand)
            {
                //open menu to select where to equip
                //if player selects mainhand
                //run mainhand equip code
                //if player selects offhand
                //run offhand equip code

                //the item is now equipped and not in inventory. it will return to inventory if/when it is unequipped. for now we remove it from inventory
                GameManager.Instance.RemoveItem(itemName);

            }

            
        }

        if (isArmor)
        {

            if (isHead)
            {
                //if there is currently armor equipped in that slot
                if (selectedchar.equippedHead != "")
                {
                    GameManager.Instance.AddItem(selectedchar.equippedHead);
                }

                selectedchar.equippedHead = itemName;
                selectedchar.headArmorPower = armorPower;
                //the item is now equipped and not in inventory. it will return to inventory if/when it is unequipped. for now we remove it from inventory
                GameManager.Instance.RemoveItem(itemName);
            }
            if (isBody)
            {
                //if there is currently armor equipped in that slot
                if (selectedchar.equippedBody != "")
                {
                    GameManager.Instance.AddItem(selectedchar.equippedBody);
                }

                selectedchar.equippedBody= itemName;
                selectedchar.bodyArmorPower = armorPower;
                //the item is now equipped and not in inventory. it will return to inventory if/when it is unequipped. for now we remove it from inventory
                GameManager.Instance.RemoveItem(itemName);

            }
            if (isHands)
            {
                //if there is currently armor equipped in that slot
                if (selectedchar.equippedHands != "")
                {
                    GameManager.Instance.AddItem(selectedchar.equippedHands);
                }

                selectedchar.equippedHands = itemName;
                selectedchar.handsArmorPower = armorPower;
                //the item is now equipped and not in inventory. it will return to inventory if/when it is unequipped. for now we remove it from inventory
                GameManager.Instance.RemoveItem(itemName);
            }
            if (isLegs)
            {
                //if there is currently armor equipped in that slot
                if (selectedchar.equippedLegs != "")
                {
                    GameManager.Instance.AddItem(selectedchar.equippedLegs);
                }

                selectedchar.equippedLegs = itemName;
                selectedchar.legsArmorPower = armorPower;
                //the item is now equipped and not in inventory. it will return to inventory if/when it is unequipped. for now we remove it from inventory
                GameManager.Instance.RemoveItem(itemName);

            }
            if (isFeet)
            {
                //if there is currently armor equipped in that slot
                if (selectedchar.equippedFeet != "")
                {
                    GameManager.Instance.AddItem(selectedchar.equippedFeet);
                }

                selectedchar.equippedFeet = itemName;
                selectedchar.feetArmorPower = armorPower;
                //the item is now equipped and not in inventory. it will return to inventory if/when it is unequipped. for now we remove it from inventory
                GameManager.Instance.RemoveItem(itemName);

            }
            if (isAccessory)
            {
                //if there is currently armor equipped in that slot
                if (selectedchar.equippedAccessory != "")
                {
                    GameManager.Instance.AddItem(selectedchar.equippedAccessory);
                }

                selectedchar.equippedAccessory = itemName;
                selectedchar.accessoryArmorPower = armorPower;
                //the item is now equipped and not in inventory. it will return to inventory if/when it is unequipped. for now we remove it from inventory
                GameManager.Instance.RemoveItem(itemName);

            }
            
        }

        if (affectJob)
        {
            //change job to new job
        }

        if (affectsStats)
        {
            if (affectStr)
            {
                selectedchar.charStr += amountToChange;

                if (isTempBuff)
                {
                    //wait for durationOfTempAffect time

                    selectedchar.charStr -= amountToChange;
                }

            }
            if (affectDex)
            {

            }
            if (affectVit)
            {

            }
            if (affectAgi)
            {

            }
            if (affectInt)
            {

            }
            if (affectMnd)
            {

            }
            if (affectChr)
            {

            }
            if (affectAttack)
            {

            }
            if (affectDefense)
            {

            }
            if (affectEvasion)
            {

            }
            if (affectAccuracy)
            {

            }
            if (affectMAttack)
            {

            }
            if (affectMDefense)
            {

            }
            if (affectMEvasion)
            {

            }
            if (affectMAccuracy)
            {

            }
            if (affectSpeed)
            {

            }
            if (affectLuck)
            {

            }
            if (affectElement)
            {

            }
            if (affectXP)
            {

            }

        }

        if (isCraftMat)
        {
            //open the crafting menu
            //TODO make a crafting system
        }

        if (isClassMeldItem)
        {

            //open class meld system
            JobManager.instance.OpenMeld(charToUseOn);

        }

        if (isScroll)
        {
            //use ability on scroll
            //destroy item
        }

        if (isAbilityUnlocker)
        {
            //unlock ability for character who used if they have ability to learn
        }

        if (isDisposable)
        {
            numberOfUses = numberOfUses - 1;
            if(numberOfUses <= 0)
            {
                GameManager.Instance.RemoveItem(itemName);
            }
        }

        

    }


}
