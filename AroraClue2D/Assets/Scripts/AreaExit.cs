using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AreaExit : MonoBehaviour{

    public string areaToLoad;
    public string areaTransitionName;
    public AreaEntrance theEntrance;

    public float timeToWaitForLoad = 1f;
    private bool shouldLoadAfterFade;


    // Start is called before the first frame update
    void Start()
    {
        theEntrance.transitionName = areaTransitionName;
        
    }

    // Update is called once per frame
    void Update()
    {
        if (shouldLoadAfterFade)
        {
            timeToWaitForLoad -= Time.deltaTime;
            if(timeToWaitForLoad <= 0)
            {
                shouldLoadAfterFade = false;
                SceneManager.LoadScene(areaToLoad);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.tag == "Player")
        {
            //moved to update to enable loading screen
            //SceneManager.LoadScene(areaToLoad);

            shouldLoadAfterFade = true;
            GameManager.Instance.fadingBetweenAreas = true;

            UIFade.instance.FadeToBlack();

            PlayerController.instance.areaTransitionName = areaTransitionName;
        }
    }
}
 