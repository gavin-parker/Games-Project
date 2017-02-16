﻿using UnityEngine;
using System.Collections;
using System;

public class StoneQuarry : ResourceBuilding
{
    public int fCost = 20;

    public override int faithCost()
    {
        return fCost;
    }

    public override void create_building()
    {
        buildingName = "QUARRY";
        InvokeRepeating("incrementResource", 10.0f, 4.0f); // after 10 sec call every 4 
    }

    // Use this for initialization
    void Start()
    {
        create_building();
    }
    public override void incrementResource()
    {
        //resourceCounter.addStone();
    }
}
