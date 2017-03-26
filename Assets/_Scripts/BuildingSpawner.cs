﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingSpawner : MonoBehaviour {

    public GameObject buildingToSpawn;
    public ResourceCounter resources;
    public GameObject imgCanvas;
    public ResourceGainTablet resourceCost;
    private int buildingCost;
    private int buildingMask = (1 << 10) | (1 << 14);
    public bool spawn = true;
	// Use this for initialization
	void Start () {
        InvokeRepeating("spawnBuilding", .1f, 1f);
        if (buildingCost == 0) buildingCost = 20;
        resources = GameObject.FindGameObjectWithTag("Tablet").GetComponent<ResourceCounter>();
        imgCanvas = transform.GetChild(0).gameObject;
        imgCanvas.SetActive(false);
        resourceCost.setText(buildingCost.ToString());
        resourceCost.activateThis();
        GameObject building = Instantiate(buildingToSpawn, transform.position, transform.rotation);
        Building bScript = building.GetComponent<Building>();
        if(bScript != null)
        {
            buildingCost = bScript.faithCost;
        }
        else
        {
            LightningBolt light = building.GetComponent<LightningBolt>();
            if(light != null)
            {
                buildingCost = light.faithCost;
            }
            else
            {
                Debug.LogError("OBJECT THAT IS NOT BUILDING OR LIGHTNING BOLT PLACED ON SPAWNER");
            }
        }
        resourceCost.setText(buildingCost.ToString());
        resourceCost.activateThis();
        DestroyImmediate(building);
    }

    private void spawnBuilding()
    {
        if (resources.faith < buildingCost || !resources.hasGameStarted())
        {
            imgCanvas.SetActive(true);
            resourceCost.text.color = Color.red;
        }
        else
        {
            imgCanvas.SetActive(false);
            resourceCost.text.color = Color.black;
            if (spawn && Physics.OverlapSphere(transform.position, 8.0f, buildingMask).Length == 0)
            {
                GameObject building = Instantiate(buildingToSpawn, transform.position, transform.rotation);
                building.transform.parent = transform;
                building.GetComponent<Rigidbody>().useGravity = false;
            }
        }

    }

}
