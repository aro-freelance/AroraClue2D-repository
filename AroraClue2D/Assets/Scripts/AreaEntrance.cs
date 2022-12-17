using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AreaEntrance : MonoBehaviour{

    public string transitionName;

    // Start is called before the first frame update
    void Start()
    {
        if(transitionName == PlayerController.instance.areaTransitionName)
        {
            //set the player position to the position of the object with this script on it
            PlayerController.instance.transform.position = transform.position; 
        }

        UIFade.instance.FadeFromBlack();
        GameManager.Instance.fadingBetweenAreas = false;
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
