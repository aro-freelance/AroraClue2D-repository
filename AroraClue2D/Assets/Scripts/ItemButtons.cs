using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemButtons : MonoBehaviour
{

    public Image buttonImage;
    public Text amountOfItem;
    public int buttonNumber;




    public void Press()
    {
        if(GameManager.Instance.itemsInInventory[buttonNumber] != "")
        {
            Debug.Log("pressed Item Button number " + buttonNumber);

            //GameMenu.instance.SelectItem(GameManager.Instance.GetItemDetails(GameManager.Instance.itemsInInventory[buttonNumber]));
        }

    }


}
