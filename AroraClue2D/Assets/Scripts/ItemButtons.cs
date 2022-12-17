using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemButtons : MonoBehaviour
{

    public Image buttonImage;
    public Text amountOfItem;
    public int buttonNumber;





    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Press()
    {
        if(GameManager.Instance.itemsInInventory[buttonNumber] != "")
        {
            GameMenu.instance.SelectItem(GameManager.Instance.GetItemDetails(GameManager.Instance.itemsInInventory[buttonNumber]));
        }

    }


}
