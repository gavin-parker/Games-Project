﻿using UnityEngine;
using System.Collections;

public class Hand : MonoBehaviour {

    public enum HandStatus {Open, Close};
    HandStatus hand = HandStatus.Open;
    public bool holding = false;
    public Vector3[] fingers = new Vector3[5];

    private bool change = false;


    GameObject heldObject;

    GameObject heldScaffold;
    Scaffold heldScaffoldScript;

    public Collider[] things;

    public GameObject close_hand;
    public GameObject open_hand;
    public GameObject grab_position;
    public BodySourceView kinect_view;
    public Renderer renderer_open;
    public Renderer renderer_closed;

    public Color defaultColor;

    public bool useMouse = true;
    public bool right_hand;

    public bool wasKinematic = false;
    public bool usedGravity = false;



    BuildingType[] buildings = { BuildingType.FARM, BuildingType.HOUSE, BuildingType.IRONMINE, BuildingType.LUMBERYARD, BuildingType.QUARRY, BuildingType.TOWER };
    int buildingType;
    private Vector3[] held_object_positions;
    private float[] held_object_times;

    private Vector3 original_position;

    float rotationTimer;
    float startTime;
    Collider heldCollider;

    
	// Use this for initialization
	void Start () {
        held_object_times = new float[5];
        held_object_positions = new Vector3[5];
        held_object_positions[0] = Vector3.zero;
        held_object_positions[1] = Vector3.zero;
        held_object_positions[2] = Vector3.zero;
        held_object_positions[3] = Vector3.zero;
        held_object_positions[4] = Vector3.zero;
        if (kinect_view == null) useMouse = true;
        defaultColor = renderer_open.material.GetColor("_Color");
    }

    // Update is called once per frame
    void Update () {
   

        // MOUSE TESTING
        if (useMouse)
        {
            mouseMovement();
        }
        else
        {
            if ((right_hand && kinect_view.rightHandClosed) || (!right_hand && kinect_view.leftHandClosed))
            {
                closeHand();
            }
            else
            {
                openHand();
            }
        }
        if (change)
        {
            if (hand == HandStatus.Open)
            {
                releaseObject();
            }
            else
            {
                grabObject();
            }
            change = false;
        }

        if (holding)
        {
            for (int i = 4; i >0; i--) {
                held_object_positions[i] = held_object_positions[i - 1];
                held_object_times[i] = held_object_times[i - 1];
            }
            held_object_positions[0] = transform.position;
            held_object_times[0] = Time.deltaTime;
            //Vector3 p = grab_position.transform.position;//new Vector3(transform.position.x - 14, transform.position.y - 18, transform.position.z);
            //heldObject.transform.position = p;
        }

               
    }

    //handle player interaction using the mouse foor testing
    void mouseMovement()
    {
        Vector3 curLocation = transform.position;
        float h = Input.GetAxis("Mouse X");
        float v = Input.GetAxis("Mouse Y");
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        transform.position = new Vector3(curLocation.x - 6 * v, curLocation.y - 20 * scroll, curLocation.z + 6 * h);
        curLocation.x -= 14;
        curLocation.y -= 10;

        if (Input.GetMouseButtonDown(0))
        {
            openHand();
        }
        if (Input.GetMouseButtonDown(1))
        {
            closeHand();
        }

    }
    //changes colour of wrist band based on tracking
    void setTrackingColour()
    {
        if (right_hand)
        {
            if (!kinect_view.rightHandTracked)
            {
                renderer_open.material.SetColor("_Color", Color.red);
                renderer_closed.material.SetColor("_Color", Color.red);

            }
            else
            {
                renderer_open.material.SetColor("_Color", defaultColor);
                renderer_closed.material.SetColor("_Color", defaultColor);

            }
        }
        else
        {
            if (!kinect_view.leftHandTracked)
            {
                renderer_open.material.SetColor("_Color", Color.red);
                renderer_closed.material.SetColor("_Color", Color.red);

            }
            else
            {
                renderer_open.material.SetColor("_Color", defaultColor);
                renderer_closed.material.SetColor("_Color", defaultColor);

            }

        }

    }

    public void openHand()
    {

        open_hand.SetActive(true);
        close_hand.SetActive(false);
        if (hand == HandStatus.Close)
        {
            hand = HandStatus.Open;
            change = true;
        }
        
        
    }

    public void closeHand()
    {
        open_hand.SetActive(false);
        close_hand.SetActive(true);
        if (hand == HandStatus.Open)
        {
            hand = HandStatus.Close;
            change = true;
        }

    }
    //checks a surrounding sphere for objects, grabs them
    private void grabObject()
    {
        if (holding) return;


        GameObject closest = null;
        int layerMask = (1 << 9) | ( 1 << 10) | (1 << 14);
        Vector3 p = grab_position.transform.position;//new Vector3(transform.position.x - 14, transform.position.y - 18, transform.position.z);
        things = Physics.OverlapSphere(p, 4.0f, layerMask);
        float distance = Mathf.Infinity;
        if (things.Length > 0)
        {
            foreach (Collider thing in things)
            {
                Vector3 diff = thing.ClosestPointOnBounds(p) - p;
                float current_distance = diff.sqrMagnitude;
                if (current_distance < distance)
                {
                    distance = current_distance;
                    closest = thing.gameObject;
                }
            }
        }
        heldObject = closest;
        if (heldObject != null)
        {
            Debug.Log("GRABBED");
            if (heldObject.tag == "Shelf Object")
            {
                original_position = heldObject.transform.position;
            }
                
            holding = true;
            Grabbable placeable = heldObject.GetComponent<Grabbable>();

            if (placeable != null)
            {
                placeable.grab();
            }
            else
            {
                Debug.Log("This object is not placeable", heldObject);
            }
            heldObject.transform.parent = transform;                
        }
        else
        {
            //Debug.Log("NOTHING TO GRAB!");
        }
    }

    void OnDrawGizmosSelected()
    {
        Vector3 p = grab_position.transform.position;//new Vector3(transform.position.x - 14, transform.position.y - 18, transform.position.z);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(p, 5.0f);
    }

    private void releaseObject ()
    {
        if (holding)
        {
            holding = false;
            heldObject.transform.parent = null;
            Building building = heldObject.GetComponent<Building>();

            if(building != null)
            {
                if(building.canPlace() && building.canBuy())
                {
                    snapToGrid(heldObject);
                    building.activate();
                }
                else
                {
                    if (GameBoard.withinBounds(heldObject.transform.position))
                    {
                        Destroy(heldObject);
                    }
                    else
                    {
                        throwObject(heldObject);
                    }
                }
            }
            else
            {
                throwObject(heldObject);
            }

            heldObject = null;

        }

    }

    //function called to place an object neatly on the game board
    private void snapToGrid(GameObject placeable)
    {
        float x = placeable.transform.position.x;
        float z = placeable.transform.position.z;
        placeable.transform.position = new Vector3(Mathf.Floor(x), 0, Mathf.Floor(z));
        placeable.transform.rotation = Quaternion.LookRotation(Vector3.forward);

    }

    //function called to release any physics object from the hand
    private void throwObject(GameObject projectile)
    {
        float time = 0;
        foreach (float t in held_object_times)
        {
            time += t;
        }
        Vector3 velocity = (projectile.transform.position - held_object_positions[4]) / (time);

        Grabbable placeable = heldObject.GetComponent<Grabbable>();
        if (placeable != null)
        {
            placeable.release(velocity);

        }
        else
        {
            Debug.Log("This object is not placeable", heldObject);
        }
    }
}
