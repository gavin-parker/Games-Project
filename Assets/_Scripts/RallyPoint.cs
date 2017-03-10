﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RallyPoint : Building, Grabbable {

    public string buildingName;
    private int fCost = 0;

    GameObject rangeHighlight;
    private float radius = 25.0f;

    public float timer1;
    private float StartTime1;
    private float placementTime = 0;


    bool active = false;
    public bool held = false;
    private bool badplacement = false;
    bool grabbedOnce = false;
    bool humanIsHeld = false;

    public override void changeTextColour(Color colour)
    {
        //infoText.GetComponent<TextMesh>().GetComponent<Renderer>().material.SetColor("_Color", colour);
    }

    //private GameObject infoText;

    // Use this for initialization
    void Start() {
        rangeHighlight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rangeHighlight.GetComponent<Renderer>().material.SetColor("_Color", new Color(0.0f, 0.6f, 0.0f, 0.2f));
        rangeHighlight.transform.localScale = new Vector3(radius, 0.1f, radius);
        rangeHighlight.transform.position = new Vector3(gameObject.transform.position.x, 0.1f, gameObject.transform.position.z);
        rangeHighlight.transform.rotation = Quaternion.LookRotation(Vector3.forward);

        rangeHighlight.GetComponent<Collider>().enabled = false;
        rangeHighlight.GetComponent<Renderer>().enabled = true;
        rangeHighlight.SetActive(false);
        InvokeRepeating("HumanHeld", .1f, .1f);
    }

    // Update is called once per frame
    void Update() {
        rangeHighlight.transform.position = new Vector3(gameObject.transform.position.x, 0.1f, gameObject.transform.position.z);

        if (held)
        {
            if (highlight != null)
            {
                highlightCheck();
            }
        }
        else if (badplacement)
        {
            if (Time.time - placementTime > 5.0f)
            {
                DestroyObject(gameObject);
            }
        }
        else if (humanIsHeld)
        {
            showRange();
        }
        else if (!humanIsHeld)
        {
            hideRange();
        }
    }

    public void grab()
    {
        showRange();
        grabbedOnce = true;
        held = true;
        badplacement = false;
        //Destroy(infoText);

        // Deactivate  collider and gravity
        if (highlight != null)
        {
            DestroyImmediate(highlight);
        }

        // highlight where object wiould place if falling straight down
        createHighlight();

        GetComponent<Rigidbody>().useGravity = false;
        GetComponent<Rigidbody>().isKinematic = true;
        GetComponent<Collider>().enabled = false;

    }

    public override void create_building()
    {
        //
    }

    public override string getName()
    {
        return buildingName;
    }

    public override Vector3 getLocation()
    {
        return transform.position;
    }

    public override bool canBuy()
    {
        if (resourceCounter.faith >= fCost)
        {
            resourceCounter.removeFaith(fCost);
            return true;
        }
        return false;
    }

    public override void die()
    {
        Destroy(gameObject);
    }

    public override void activate()
    {
        active = true;
        if (highlight != null) Destroy(highlight);
        highlight = null;
        held = false;
        buildingName = "RALLY";
        timer1 = 0f;
        StartTime1 = Time.time;
        hideRange();

        
    }


    public override void deactivate()
    {
        active = false;
    }

    public void showRange()
    {
        rangeHighlight.SetActive(true);

    }

    public void hideRange()
    {
        rangeHighlight.SetActive(false);

    }

    new void OnTriggerEnter(Collider other)
    {
        base.OnTriggerEnter(other);
        if (other.gameObject.tag == "Hand" && grabbedOnce)
        {
            showRange();
        }
    }

    new void release(Vector3 vel)
    {
        base.release(vel);
        hideRange();
    }

    public void HumanHeld()
    {
        GameObject[] humans;
        humans = GameObject.FindGameObjectsWithTag("Human");
        foreach (GameObject human in humans)
        {
            if (human.GetComponent<Agent>().humanHeld)
            {
                humanIsHeld = true;
                break;
            }
            else
            {
                humanIsHeld = false;
            }
        }
    }


}

/*
 IEnumerator HumanHeld()
    {
        yield return new WaitForFixedUpdate();
        GameObject[] humans;
        humans = GameObject.FindGameObjectsWithTag("Human");
        foreach (GameObject human in humans)
        {
            if (human.GetComponent<Agent>().humanHeld)
            {
                Debug.Log("IF");
                humanIsHeld = true;
            }
        }
    }
 */