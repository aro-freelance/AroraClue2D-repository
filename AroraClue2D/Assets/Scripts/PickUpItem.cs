using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickUpItem : MonoBehaviour
{

    private bool canPickUp;

    void Update()
    {
        //if player is near the item, and they push the button, and they can move (i.e they are not in a menu or loading or something)
        if (canPickUp && Input.GetButtonDown("Fire1") && PlayerController.instance.canMove)
        {
            //add item to inventory
            GameManager.Instance.AddItem(GetComponent<Item>().itemName);
            //remove the item from the scene
            Destroy(gameObject);


        }
        
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.tag == "Player")
        {
            canPickUp = true;
        }

    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.tag == "Player")
        {
            canPickUp = false;
        }

    }

}
