﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Meteor : Projectile {
    public GameObject FireEffect;
    public GameObject DestructionEffect;
    public int destruction_radius;
    public int Damage;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    
    private void DestroyObject()
    {
        GameObject fire = GameObject.Instantiate(FireEffect, this.transform.position, Quaternion.identity) as GameObject;
        GameObject explosion = GameObject.Instantiate(DestructionEffect, this.transform.position, Quaternion.identity) as GameObject;
        fire.transform.Rotate(-90, 0, 0);
        fire.SetActive(true);
        explosion.SetActive(true);
        Destroy(explosion, 2);
        Destroy(fire, 2);
        Destroy(gameObject);
    }

    private void DamageBuildings(Vector3 hit_position)
    {
        Collider[] hitcolliders = Physics.OverlapSphere(hit_position, destruction_radius);
        if (hitcolliders.Length == 0 ) return;

        foreach (Collider hit in hitcolliders)
        {   
            if (hit.gameObject.GetComponent<Building>() != null)
            {
                hit.gameObject.GetComponent<Building>().decrementHealth(Damage);
            } 
        } 
    }

    public  override void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.tag == "Racket")
        {
            // this will automatically bounce from it 
        }
        else 
        {
            DamageBuildings(gameObject.transform.position);
            DestroyObject();
        }
    }
}