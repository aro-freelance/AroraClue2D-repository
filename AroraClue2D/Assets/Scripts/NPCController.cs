using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCController : MonoBehaviour
{

    public Rigidbody2D theRB;
    public float moveSpeed;
    public Animator myAnimator;
    public string areaTransitionName;

    private Vector3 bottomLeftLimit;
    private Vector3 topRightLimit;

    public bool canMove = true;

    void Start()
    {
        //TODO: make a unique npc id/name for each
        //TODO: spawn a few npcs (note we also need to make spawn points for interaction points and randomize that)


        DontDestroyOnLoad(gameObject);

    }

    // Update is called once per frame
    void Update()
    {

        if (canMove)
        {
            if (!myAnimator.enabled)
            {
                myAnimator.enabled = true;
            }


            Movement();
        }
        else
        {
            //this sets speed to zero so we can't slide without control when movement is turned off
            theRB.velocity = Vector2.zero;
            myAnimator.enabled = false;
        }

    }


    //TODO: make talking AI
    void DialogueAI()
    {

    }

    //TODO: make movement AI
    void MovementAI()
    {

    }

    void Movement()
    {
        theRB.velocity = new Vector2((Input.GetAxisRaw("Horizontal") * moveSpeed), (Input.GetAxisRaw("Vertical") * moveSpeed));

        myAnimator.SetFloat("moveX", theRB.velocity.x);
        myAnimator.SetFloat("moveY", theRB.velocity.y);

        if (theRB.velocity.x > 0.1)
        {
            myAnimator.SetFloat("lastMoveX", 1);
            myAnimator.SetFloat("lastMoveY", 0);
        }
        else if (theRB.velocity.x < -0.1)
        {
            myAnimator.SetFloat("lastMoveX", -1);
            myAnimator.SetFloat("lastMoveY", 0);
        }
        else if (theRB.velocity.y > 0.1)
        {
            myAnimator.SetFloat("lastMoveX", 0);
            myAnimator.SetFloat("lastMoveY", 1);
        }
        else if (theRB.velocity.y < -0.1)
        {
            myAnimator.SetFloat("lastMoveX", 0);
            myAnimator.SetFloat("lastMoveY", -1);
        }


        //constrain the player to the bounds of the map
        transform.position = new Vector3(Mathf.Clamp(transform.position.x, bottomLeftLimit.x, topRightLimit.x),
            Mathf.Clamp(transform.position.y, bottomLeftLimit.y, topRightLimit.y), transform.position.z);
    }



    public void setBounds(Vector3 botLeft, Vector3 topRight)
    {

        bottomLeftLimit = botLeft + new Vector3(0.7f, 1f, 0f);
        topRightLimit = topRight + new Vector3(-0.7f, -0.7f, 0f);



    }


}
