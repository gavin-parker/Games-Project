﻿using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Threading;
using System;
using UnityEngine.AI;


/* AI LOGIC EXPLANATION
 *  PHASE 1
 *  -- Wander:
 *  The  AI wanders arround moving randomly 
 *  TODO: Smooth Rotation
 *  TODO: Avoidance
 *  
 * PHASE 2 
 * If at any moment temple detryed everyone dies
 * 
 * 
 * 
 * --Killers COMPLETED
 * Search for nearest enemy 
 * Go to its position
 * Attack it
 * When dead look for next enemy
 * If no humans remain either go for watch towers or go for temple 
 * 
 * --Defender
 * Go towards temple, if enemy near temple kill it
 * 
 * --Camper
 * 
 * 
 */



public class Agent : MonoBehaviour, Character, Grabbable
{
    // Human characteristics
    public float health = 100.0f;
    public float totalHealth = 100.0f;
    GameObject healthBar;
    Quaternion healthBarOri;
    GameObject infoText;
    Quaternion infoTextOri;
    public float strength = 10.0f;
    public float speed = 2.0f;
    private float rotationspeed = 5.0f;
    public float gravity = 9.81F;
    public float weapon_strength;
    public bool debug = false;
    private GameObject temple;
    private bool alive = true;

    // Components
    public AudioClip sacrificeClip;
    private Rigidbody rb;
    public Animation anim;


    // God interactions
    private bool sacrificeEntered = false;

    // Fighting
    enum Fighter { Killer, Defender, Camper };
    public int fighterType;

    //Killer
    private GameObject closestEnemy;
    private int maxCell;
    private float dis2Enemy = 0;

    private ResourceCounter resources;

    public bool active = true;
    //public CharacterController controller;
    private NavMeshAgent agent;
    private HumanState currentState = HumanState.Wandering;

    private Vector3 wanderPoint = Vector3.zero;
    private LineRenderer lineRenderer;

    protected Renderer[] child_materials;
    private Shader outlineShader;

    private Vector3 bL;
    private Vector3 tR;

    enum HumanState { Fighting, Wandering, Grabbed, Falling };

    void Start()
    {

        anim = GetComponent<Animation>();
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        temple = GameObject.FindGameObjectWithTag("Temple");
        maxCell = 5000;
        closestEnemy = null;
        fighterType = (int)Fighter.Killer;
        resources = GameObject.FindGameObjectWithTag("Tablet").GetComponent<ResourceCounter>();
        resources.addPop();
        agent = GetComponent<NavMeshAgent>();
        createHealthBar();
        healthBarOri = healthBar.transform.rotation;
        createInfoText();
        infoTextOri = infoText.transform.rotation;
        child_materials = GetComponentsInChildren<Renderer>();
        outlineShader = Shader.Find("Toon/Basic Outline");
        lineRenderer = GetComponent<LineRenderer>();
        tR = resources.getTopRight();
        bL = resources.getBottomLeft();
    }

    // Update is called once per frame
    void Update()
    {
        healthBar.transform.rotation = healthBarOri;
        infoText.transform.rotation = infoTextOri;
        if (active)
        {
            if (temple == null)
            {
                anim.Play("diehard");
                StartCoroutine(WaitToDestroy(2.1f));
                return;
            }
            switch (currentState)
            {
                case HumanState.Wandering:
                    if (resources.baddies > 0)
                    {
                        currentState = HumanState.Fighting;
                        infoText.SetActive(true);
                    }
                    else
                        wander();
                    break;

                case HumanState.Fighting:
                    if (resources.baddies <= 0)
                    {
                        currentState = HumanState.Wandering;
                        infoText.SetActive(false);
                    }
                    else
                        attack();
                    break;

                case HumanState.Grabbed:
                    break;

                case HumanState.Falling:
                    if (!sacrificeEntered)
                    {
                        if (resources.aboveBoard(transform.position))
                        {
                            if (transform.position.y < 0.5)
                            {
                                agent.enabled = true;
                                currentState = HumanState.Wandering;
                            }
                        }
                        else
                        {
                            sacrifice();
                        }
                    }
                    break;
            }
        }
    }

    //we should only use this method for moving -- once per frame
    void walkTo(Vector3 target)
    {
        Vector3 offset = target - transform.position;
        if (offset.magnitude > 0.1f)
        {
            agent.destination = target;
            showPath();
            //controller.Move(offset * Time.deltaTime);
            //don't spin in circles
            if (offset.magnitude > 2)
            {
                offset.y = transform.position.y;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(offset), Time.deltaTime * rotationspeed);
            }
            anim.Play("walk");
        }

    }

    private void wander()
    {
        if (wanderPoint == Vector3.zero || Vector3.Distance(transform.position, wanderPoint) < 5)
        {
            Debug.Log("finding a new target to wander to");
            wanderPoint = findNewTarget();
        }
        else
        {
            walkTo(wanderPoint);
        }
    }


    //generates a new point on the board to wander to
    private Vector3 findNewTarget()
    {
        float randX = UnityEngine.Random.Range(bL.x, tR.x);
        float randZ = UnityEngine.Random.Range(bL.z, tR.z);
        Vector3 v = new Vector3(randX, 0, randZ);
        NavMeshHit hit;
        if (NavMesh.SamplePosition(v, out hit, 20.0f, NavMesh.AllAreas))
        {
            return hit.position;
        }
        else
        {
            Debug.Log("Could not calculate a new wander target");
            return transform.position;
        }
    }

    private void showPath()
    {
        if (agent.hasPath && debug)
        {
            lineRenderer.SetPositions(agent.path.corners);
        }

    }

    void sacrifice()
    {
        if (!sacrificeEntered)
        {
            Debug.Log(transform.position);
            AudioSource source = GetComponent<AudioSource>();
            source.PlayOneShot(sacrificeClip, 0.5f);
            resources.addFaith(128);
            sacrificeEntered = true;
            anim.Play("diehard");
            StartCoroutine(WaitToDestroy(2.1f));
        }
    }

    IEnumerator WaitToDestroy(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        resources.removePop();
        GameObject.Destroy(gameObject);
    }

    private void moveStraightToTarget(GameObject target)
    {
        walkTo(target.transform.position);
    }

    private void attack()
    {
        if (closestEnemy == null)
        {
            closestEnemy = findClosestEnemy();
        }

        if (Vector3.Distance(transform.position, closestEnemy.transform.position) < 3.0f)
        {
            if (!attack(closestEnemy))
            {

                closestEnemy = null;
                currentState = HumanState.Wandering;
            }
        }
        else
        {
            moveStraightToTarget(closestEnemy);
        }

    }


    private GameObject findClosestEnemy()
    {
        GameObject[] badies;
        GameObject closest = null;
        badies = GameObject.FindGameObjectsWithTag("Badies");
        float distance = Mathf.Infinity;
        Vector3 position = transform.position;
        if (badies.Length > 0)
        {
            foreach (GameObject badie in badies)
            {
                Vector3 diff = badie.transform.position - position;
                float current_distance = diff.sqrMagnitude;
                if (current_distance < distance)
                {
                    distance = current_distance;
                    closest = badie;
                }
            }
        }

        return closest;
    }

    public void decrementHealth(float damage)
    {
        health -= damage;
        float scale = (health / totalHealth);
        float characterScale = gameObject.transform.localScale.x;
        healthBar.transform.localScale = new Vector3(0.1f / characterScale, scale / characterScale, 0.1f / characterScale);
        if (scale != 0) healthBar.GetComponent<Renderer>().material.SetColor("_Color", new Color(1.0f - scale, scale, 0));
        if (health <= 0 && alive == true)
        {
            alive = false;
            anim.Play("diehard");
            StartCoroutine(WaitToDestroy(2.0f));
        }
    }

    public void createHealthBar()
    {
        Bounds dims = gameObject.GetComponent<Collider>().bounds;
        Vector3 actualSize = dims.size;
        healthBar = GameObject.Instantiate(Resources.Load("CharacterHealthBar")) as GameObject;
        healthBar.transform.position = gameObject.GetComponent<Collider>().transform.position;
        healthBar.transform.Translate(new Vector3(0, 0, dims.size.y * -1.0f));
        healthBar.transform.SetParent(gameObject.transform);
    }

    public void createInfoText()
    {
        Bounds dims = gameObject.GetComponent<Collider>().bounds;
        Vector3 actualSize = dims.size;
        infoText = GameObject.Instantiate(Resources.Load("AttackSprite")) as GameObject;
        infoText.transform.position = gameObject.transform.position;
        infoText.transform.localScale *= 2;
        infoText.transform.Translate(new Vector3(0, actualSize.y * 1.4f, 0));
        infoText.transform.localRotation = gameObject.transform.localRotation;
        infoText.transform.Rotate(new Vector3(0, -90, 0));
        infoText.transform.SetParent(gameObject.transform);
        infoText.SetActive(false);
        //infoText.GetComponent<TextMesh>().text = currentState.ToString();
    }

    public void grab()
    {
        Debug.Log("Human Grabbed");
        agent.enabled = false;
        currentState = HumanState.Grabbed;
        rb.isKinematic = true;
        rb.useGravity = false;
        GetComponent<Collider>().enabled = false;
    }

    public void release(Vector3 vel)
    {
        currentState = HumanState.Falling;
        GetComponent<Collider>().enabled = true;
        rb.useGravity = true;
        rb.isKinematic = false;
        removeOutline();

    }

    bool attack(GameObject victim)
    {
        if (victim != null)
        {
            transform.rotation = Quaternion.LookRotation(victim.transform.position - transform.position);
            if (!anim.IsPlaying("attack"))
            {
                anim.Play("attack");
                HealthManager victimHealth = victim.GetComponent<HealthManager>();
                if (victimHealth != null)
                {
                    victimHealth.decrementHealth(strength);
                }
                else
                {
                    Debug.LogError("Trying to attack something that doesn not have health");
                }
            }

            return true;
        }
        return false;
    }


    public void activate()
    {
        active = true;
    }

    public void changeMoving(bool val)
    {
        throw new NotImplementedException();
    }

    public void changeMode(bool val)
    {
        throw new NotImplementedException();
    }


    private void setOutline()
    {
        foreach (Renderer renderer in child_materials)
        {
            renderer.material.shader = outlineShader;
            renderer.material.SetColor("_OutlineColor", Color.green);
        }
    }

    private void removeOutline()
    {
        foreach (Renderer renderer in child_materials)
        {
            renderer.material.shader = Shader.Find("Diffuse");
            //renderer.material.SetColor("_OutlineColor", Color.green);
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Hand")
        {
            setOutline();
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Hand")
        {
            removeOutline();
        }
    }

}