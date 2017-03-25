﻿using UnityEngine;
using System.Collections;
using System;
using UnityEngine.AI;

public class Wall : Building, Grabbable
{

    // Use this for initialization
    public int cost_per_meter = 10;
    public bool held = false;
    public float turretRadius = 20f;

    public GameObject turretB = null;
    public GameObject turretA = null;
    public GameObject wallSegment = null;
    private Vector3 originalScale;
    private Quaternion originalRotation;
    public float adjustRange = 15.0f;
    private GameObject turretHighlightA;
    private GameObject turretHighlightB;
    private Vector3 initialTurretA;
    private Vector3 initialTurretB;

    void Start()
    {
        originalScale = wallSegment.transform.localScale;
        originalRotation = wallSegment.transform.rotation;
        create_turretHighlight();
        initialTurretA = new Vector3(10000,10000, 10000);
        initialTurretB = new Vector3(10000, 10000, 10000);

    }

    // Update is called once per frame
    //modify highlight so that there is a range highlihgt for the turret 
    void Update()
    {
        turretHighlightA.transform.position = new Vector3(turretA.transform.position.x, 0.1f, turretA.transform.position.z);
        turretHighlightB.transform.position = new Vector3(turretB.transform.position.x, 0.1f, turretB.transform.position.z);
        if (held)
        {
            if (highlight != null)
            {
                highlightCheck();
                showTurretHighlight();
            }
            else
            {
                hideTurretHighlight();
            }
        }
        else
        {
            hideTurretHighlight();
        }
    }

    public override string getName()
    {
        return "Wall";
    }


    public override Vector3 getLocation()
    {
        return this.gameObject.transform.position;
    }

    public override void highlightDestroy()
    {
        if (turretHighlightA != null)
        {
            turretHighlightA.SetActive(false);
            turretHighlightB.SetActive(false);
            highlight.SetActive(false);
        }
    }

    public override bool canBuy()
    {
        if (!bought && (resourceCounter.faith >= faithCost()))
        {
            bought = true;
            resourceCounter.removeFaith(faithCost());
            return true;
        }
        return false;
    }

    private int faithCost()
    {
        return cost_per_meter;
    }

    public override void die()
    {
        Destroy(turretHighlightA);
        Destroy(turretHighlightB);
        Destroy(gameObject);  
    }

    private void create_turretHighlight()
    {
        turretHighlightA = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        turretHighlightA.GetComponent<Renderer>().material.SetColor("_Color", Color.yellow);
        turretHighlightA.transform.localScale = new Vector3(turretRadius, 0.1f, turretRadius);
        turretHighlightA.transform.position = new Vector3(turretA.transform.position.x, 0.1f, turretA.transform.position.z);
        turretHighlightA.transform.rotation = Quaternion.LookRotation(Vector3.forward);
        turretHighlightA.GetComponent<Collider>().enabled = false;
        turretHighlightA.GetComponent<Renderer>().enabled = true;
        turretHighlightA.SetActive(false);

        turretHighlightB = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        turretHighlightB.GetComponent<Renderer>().material.SetColor("_Color", Color.yellow);
        turretHighlightB.transform.localScale = new Vector3(turretRadius, 0.1f, turretRadius);
        turretHighlightB.transform.position = new Vector3(turretB.transform.position.x, 0.1f, turretB.transform.position.z);
        turretHighlightB.transform.rotation = Quaternion.LookRotation(Vector3.forward);
        turretHighlightB.GetComponent<Collider>().enabled = false;
        turretHighlightB.GetComponent<Renderer>().enabled = true;
        turretHighlightB.SetActive(false);
    }

    public override void create_building()
    {
        // WALL SNAPPING WITH EACH OTHER CODE HERE I FAILED MISERABLY
    }

    //Don't need this 
    public override void activate()
    {
        create_building();
        if (highlight != null) highlightDestroy();
        held = false;
        int buildingLayer = 1 << 18;
        Collider[] turretAPoints = Physics.OverlapSphere(turretA.transform.position, turretRadius/2, buildingLayer);
        Collider[] turretBPoints = Physics.OverlapSphere(turretB.transform.position, turretRadius/2, buildingLayer);

        foreach (Collider collider in turretAPoints)
        {
            if (collider.gameObject.tag == "Turret" && collider.gameObject != turretA && collider.gameObject != turretB)
            {
                Debug.Log("Joining wall segments");
                initialTurretB = turretB.transform.position;
                initialTurretA = turretA.transform.position;
                turretA.transform.position = collider.transform.position;
                turretA.transform.rotation = collider.transform.rotation;    
                alignWall(turretB.transform.position, turretA.transform.position);
            }
        }
        foreach (Collider collider in turretBPoints)
        {
            if (collider.gameObject.tag == "Turret" && collider.gameObject != turretA && collider.gameObject != turretB)
            {
                Debug.Log("Joining wall segments");
                initialTurretA = turretA.transform.position;
                initialTurretB = turretB.transform.position;
                turretB.transform.position = collider.transform.position;
                turretB.transform.rotation = collider.transform.rotation;
                alignWall(turretA.transform.position, collider.transform.position);

            }
        }

    }

    //point = turret pos
    private void alignWall(Vector3 pointA, Vector3 pointB)
    {
        Vector3 midPoint = pointA + (pointB - pointA) /2f; 
        float height = wallSegment.transform.localScale.y;

        wallSegment.transform.position = new Vector3(midPoint.x, height/2, midPoint.z);    
        wallSegment.transform.localScale = new Vector3(wallSegment.transform.localScale.x, wallSegment.transform.localScale.y, (pointB - pointA).magnitude);
        wallSegment.transform.LookAt(pointB + Vector3.up*(height/2));

        BoxCollider wallCollider = this.GetComponent<BoxCollider>();
        NavMeshObstacle obstacle = GetComponent<NavMeshObstacle>();

        obstacle.size = new Vector3(obstacle.size.x, obstacle.size.y, 1+(pointB - pointA).magnitude);
        wallCollider.center = wallSegment.transform.localPosition;
        obstacle.center = wallSegment.transform.localPosition;  
    }

    //Don't need this
    public override void deactivate()
    {
    }

    private void resetWall()
    {
        turretA.transform.position = initialTurretA;
        turretB.transform.position = initialTurretB;
        float height = wallSegment.transform.localScale.y;
        Vector3 midPoint = turretA.transform.position + (turretB.transform.position - turretA.transform.position) / 2f;
        wallSegment.transform.position = new Vector3(midPoint.x, height / 2, midPoint.z);
        wallSegment.transform.localScale = new Vector3(wallSegment.transform.localScale.x, wallSegment.transform.localScale.y, (turretB.transform.position - turretA.transform.position).magnitude);
        wallSegment.transform.LookAt(turretB.transform.position + Vector3.up * (height / 2));
    }

    public void grab()
    {
        //if I have already placed the wall once 
        if (initialTurretA != new Vector3(10000, 10000, 10000))
        {
            resetWall();
        }

        turretA.SetActive(true);
        turretB.SetActive(true);
        // Deactivate  collider and gravity
        if (highlight != null)
        {
            DestroyImmediate(highlight);
        }
        held = true;
        // highlight where object wiould place if falling straight down
        createHighlight();
        GetComponent<Rigidbody>().useGravity = false;
        GetComponent<Rigidbody>().isKinematic = true;
        GetComponent<Collider>().enabled = false;

    }

    private void showTurretHighlight()
    {
        turretHighlightA.SetActive(true);
        turretHighlightB.SetActive(true);
    }

    private void hideTurretHighlight()
    {
        turretHighlightA.SetActive(false);
        turretHighlightB.SetActive(false);
    }

}
