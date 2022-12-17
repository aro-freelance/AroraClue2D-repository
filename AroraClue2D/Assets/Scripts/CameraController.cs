using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CameraController : MonoBehaviour
{
    public Transform target;

    //set in unity to the largest ground tilemap (the edges for the player to walk on)
    public Tilemap theMap;

    private Vector3 bottomLeftLimit;
    private Vector3 topRightLimit;

    private float halfHeight;
    private float halfWidth;



    // Start is called before the first frame update
    void Start()
    {

        target = PlayerController.instance.transform;



        halfHeight = Camera.main.orthographicSize;
        halfWidth = halfHeight * Camera.main.aspect;


        //used to constrain the camera edges to the tilemap. if this isn't working rightclick the gear of the tilemap in inspector and compress tilemap
        bottomLeftLimit = theMap.localBounds.min + new Vector3 (halfWidth, halfHeight, 0f);
        topRightLimit = theMap.localBounds.max + new Vector3 (-halfWidth, -halfHeight, 0f);

        // this send the playercontroller the bounds of the current map
        PlayerController.instance.setBounds(theMap.localBounds.min, theMap.localBounds.max);

    }
    private void Update()
    {
        //Debug.Log("player transform position: " + target.position);
    }
    // Update is called once per frame after Update
    void LateUpdate()
    {
        if(transform.position != null) { transform.position = new Vector3(target.position.x, target.position.y, transform.position.z); }
        


        //keep the camera in the bounds
        transform.position = new Vector3(Mathf.Clamp(transform.position.x, bottomLeftLimit.x, topRightLimit.x),
            Mathf.Clamp(transform.position.y, bottomLeftLimit.y, topRightLimit.y), transform.position.z);



    }
}
