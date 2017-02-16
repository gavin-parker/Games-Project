﻿using UnityEngine;
using System.Collections;

public class IronMine : ResourceBuilding
{
    private int fCost = 30;

    public override int faithCost()
    {
        return fCost;
    } 

    public override void create_building()
    {
        buildingName = "IRONMINE";
        resource_node = findNearestResourceNode();
        InvokeRepeating("incrementResource", 10.0f, 5.0f); // after 10 sec call every 5
    }

    // Use this for initialization
    void Start()
    {
        //create_building();
    }
    public override void incrementResource()
    {
        if (resource_node == null)
        {
            return;
        }
        if (Vector3.Distance(transform.position, resource_node.transform.position) < resource_node.GetComponent<ResourceNode>().range)
        {
            resourceCounter.addIron();
        }
    }

}
