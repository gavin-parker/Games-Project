﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine.AI;

/* AI LOGIC EXPLANATION
 * 
 * --Rushers COMPLETED
 * Search fastest path to temple using A*
 * When found move a node at a time
 * If buildings in the way destroy building
 * When temple in range attack 
 * 
 * --Defenders COMPLETED
 * Go towards temple same way as rushers
 * Once on temple range 
 * If humans or watch towers nears attack them to protect rusher
 * If nothing near attack temple
 * 
 * 
 * --Killers
 * Search for nearest human 
 * Go to its position (not sure how this will be done yet)
 * Attack it
 * When dead look for next human
 * If no humans remain either go for watch towers or go for temple 
 * 
 */

public class BadiesAI : MonoBehaviour, Character {

    /* Three type of badies will exist
     * Rushers: They'll go for the temple
     * Defenders: They'll follow the rushers and attack the temple if no humans or defend towers around
     * Killers: They'll attack humans
     *
     * Starting implementation of rushers
     */

    // Badie characteristic

    public float strenght = 20.0f;
    public float health = 100.0f;
    private float speed = 7.0f;
    public float rotationSpeed = 6.0f;
    private bool alive = false;

    // Components
    private Rigidbody rb;
    public Animation anim;

    // Movement
    private bool moving= true;
    public Vector3 startMarker;
    public Vector3 endMarker;
    private float startTime;
    private float journeyLength;

    //Pathfinding
    private PathFinding pathFinder;
    public List<int> waypoints;
    public Vector3 nextNode;
    public int targetCell;
    private bool threadRunning = false;
    private Thread t1;


    private int maxCell;


    // Fighting
    public enum Fighter {  Rusher, Defender, Killer, Training };
    public int fighterType;

    // Rusher
    private GameObject temple;
    private Vector3 templeAttackPoint; // nearest bound on temple
    public bool templeInRange = false;
    public bool pathToTempleFound = false;


    // Killer
    public GameObject closestEnemy;
    private bool noMoreEnemies = false;
    private float dis2Enemy = 0;

    //Defender
    private float radius = 25.0f;
    private float dis2Temple;
    public bool enemyInrange = false;
    public bool defending = false;
    public bool path2Enemy = false;


    // Training
    private bool enemyFound;
    private bool buildingsLeft = true;
    public GameObject[] buildings;

    private ResourceCounter resources;
    private NavMeshAgent agent;

    //the nearest position on the navmesh to the desired target point
    private Vector3 nextBestTarget;

    enum MonsterState {AttackTemple, AttackHumans, AttackBuildings, Idle };

    MonsterState currentState = MonsterState.AttackTemple;
    void Start()
    {
        resources = GameObject.FindGameObjectWithTag("Tablet").GetComponent<ResourceCounter>();
        resources.addBaddie();
        agent = GetComponent<NavMeshAgent>();

    }

    //can we make the spawn type an enum please xoxo
    public void spawn(int type)
    {
        
        moving = true;
        nextNode = new Vector3(0.0f, -50.0f, 0.0f);
        waypoints = new List<int>();
        noMoreEnemies = false;

        anim = GetComponent<Animation>();
        rb = GetComponent<Rigidbody>();
        //rb.interpolation = RigidbodyInterpolation.Interpolate;
        pathFinder = (PathFinding)GameObject.FindGameObjectWithTag("PathFinder").GetComponent(typeof(PathFinding));
        maxCell = 5000;
        closestEnemy = null;

        fighterType = type;

        temple = GameObject.FindGameObjectWithTag("Temple");
        alive = true;
        Debug.Log(string.Format("Spawn at: {0} with Type: {1}", transform.position, fighterType));
        currentState = MonsterState.AttackTemple;

        //maybe we want to do this regularly in case the monsters behaviour changes

        templeAttackPoint = temple.GetComponent<Collider>().ClosestPointOnBounds(transform.position);
    }

    void Update()
    {   if (alive)
        {

            switch (currentState) {
                case MonsterState.AttackTemple:
                    walkTowards(templeAttackPoint);
                    break;
                case MonsterState.AttackHumans:

                    break;
                case MonsterState.AttackBuildings:

                    break;
                case MonsterState.Idle:

                    break;
            }

            

            //if (false/*temple == null*/)  anim.Play("rage");
            //else if (!moving) anim.Play("idle");
            //else if (fighterType == (int)Fighter.Rusher) rusherNav();
            //else if (fighterType == (int)Fighter.Defender) defenderNav();
            //else if (fighterType == (int)Fighter.Killer) killerNav();
            //else if (fighterType == (int)Fighter.Training) trainingNav();
        }
    }


    private void defenderNav()
    {
        if (templeInRange && !defending)
        {
            // Check surroundings First towers next humans
            int layerMask = 1 << 10;
            List<Collider> hitColliders; hitColliders = new List<Collider>(Physics.OverlapSphere(transform.position, radius, layerMask));
            Collider tower = hitColliders.Find(x => x.tag == "Tower");
            if (tower != null)
            {
                closestEnemy = tower.gameObject;
                if (!threadRunning)
                {
                    nextNode.y = -50.0f;
                    moving = false;
                    threadRunning = true;
                    defending = true;
                    path2Enemy = false;
                    int srcCell = coord2cellID(transform.position);
                    targetCell = coord2cellID(tower.ClosestPointOnBounds(transform.position));
                    t1 = new Thread(() => astarD(srcCell, targetCell));
                    t1.Start();
                }
            }
            else
            {
                attack(temple);
            }
        }
        else if (defending)
        {
            if (path2Enemy)
            {

                if (enemyInrange)
                {
                    if (!attack(closestEnemy))
                    {
                        defending = false;
                        enemyInrange = false;
                        closestEnemy = null;
                        pathToTempleFound = false;
                        templeInRange = false;
                        path2Enemy = false;
                        nextNode.y = -50.0f;
                    }
                }
                else if (nextNode.y == -50.0f) getnextWaypoint();
                else if (pathFinder.checkCell(targetCell) == "empty")
                {
                    Vector3 offset = nextNode - transform.position;
                    if (offset.magnitude > 0.2f)
                    {
                        walkTowards(endMarker);

                        anim.Play("walk");
                    }
                    else getnextWaypoint();
                }
                else
                {
                    int layermask = 1 << 10;
                    List<Collider> hitColliders = new List<Collider>(Physics.OverlapSphere(nextNode, 2.0f, layermask));
                    if (hitColliders.Count > 0)
                    {
                        if ( hitColliders.Exists(x => x.Equals(closestEnemy.GetComponent<Collider>()))) {
                            enemyInrange = true;
                        }
                        attack(hitColliders[0].gameObject);
                    }
                }

            }
        }
        else rusherNav();
    }

    private void rusherNav()
    {
        if (templeInRange) attack(temple);
        else
        {
            if (nextNode.y == -50.0f) getnextWaypoint();
            else if (pathFinder.checkCell(targetCell) == "empty")
            {
                Vector3 offset = nextNode - transform.position;
                if (offset.magnitude > 0.2f)
                {
                    walkTowards(endMarker);

                    anim.Play("walk");
                }
                else getnextWaypoint();
            }
            else
            {
                int layermask = 1 << 10;
                List<Collider> hitColliders = new List<Collider>(Physics.OverlapSphere(nextNode, 2.0f, layermask));
                if(hitColliders.Count > 0)
                {
                    if (hitColliders.Exists(x => x.tag == "Temple")) {
                        templeInRange = true;
                        attack(temple);
                    }
                    else attack(hitColliders[0].gameObject);
                }
                else
                {
                    Debug.Log("PROBLEM");
                }
            }
        }

    }

    private void trainingNav()
    {
        //Debug.Log("Here!");
        if (closestEnemy != null)
        {
            if(Vector3.Distance(closestEnemy.GetComponent<Collider>().ClosestPointOnBounds(transform.position), transform.position) < 2.0f)
            {
                attack(closestEnemy);
            }
            else
            {
              if( nextNode.y == -50.0f) getnextWaypoint();
                else if (pathFinder.checkCell(targetCell) == "empty")
                {
                    Vector3 offset = nextNode - transform.position;
                    if (offset.magnitude > 0.2f)
                    {
                        walkTowards(endMarker);
                    }
                    else getnextWaypoint();
                }
                else
                {
                    int layermask = 1 << 10;
                    List<Collider> hitColliders = new List<Collider>(Physics.OverlapSphere(nextNode, 2.0f, layermask));
                    if (hitColliders.Count > 0) attack(hitColliders[0].gameObject);
                    
                }
            }
        }
        else
        {
            //Debug.Log("FINDING BUILDING!");
            findBuidling();
            nextNode.y = -50.0f;
            waypoints = new List<int>();
        }
        
    }

    private void findBuidling()
    {
        buildings = GameObject.FindGameObjectsWithTag("Building");
        if( buildings.Length > 0)
        {
            
            closestEnemy = null;
            float distance = Mathf.Infinity;
            foreach (GameObject b in buildings)
            {
                Vector3 diff = b.GetComponent<Collider>().ClosestPointOnBounds(transform.position) - transform.position;
                float curDistance = diff.sqrMagnitude;
                if (curDistance < distance && b.transform.position.y >= -1.0f)
                {
                    closestEnemy = b;
                    distance = curDistance;
                }
            }
        }
    }
    private void killerNav()
    {
        if (noMoreEnemies)
        {
            fighterType = (int)Fighter.Rusher;
            pathToTempleFound = false;
            nextNode.y = -50.0f;


        }
        else if (closestEnemy != null)
        {
            if (Vector3.Distance(transform.position, closestEnemy.transform.position) < 2.0f)
            {
                if (!attack(closestEnemy)) closestEnemy = null;
            }
            else
            {
                if (nextNode.y == -50.0f)
                {
                    killerNextWaypoint();
                }
                else if (pathFinder.checkCell(targetCell) == "empty")
                {
                    Vector3 offset = nextNode - transform.position;
                    if (offset.magnitude > 0.2f)
                    {
                        walkTowards(endMarker);

                        anim.Play("walk");
                    }
                    else killerNextWaypoint();
                }
                else
                {
                    int layermask = 1 << 10;
                    List<Collider> hitColliders = new List<Collider>(Physics.OverlapSphere(nextNode, 2.0f, layermask));
                    if (hitColliders.Count > 0)
                    {
                        attack(hitColliders[0].gameObject);
                    }
                }
            }
        }
        else
        {
            closestEnemy = findClosestEnemy();
        }
    }

    void astarR(int srcCell, int targetCell)
    {
        waypoints = new List<int>(pathFinder.AstarB(srcCell, targetCell));
        threadRunning = false;
        pathToTempleFound = true;
        moving = true;
    }

    void astarD(int srcCell, int targetCell)
    {
        waypoints = new List<int>(pathFinder.AstarB(srcCell, targetCell));
        threadRunning = false;
        path2Enemy = true;
        moving = true;
    }


    private Vector3 getClosestPointToTarget(Vector3 target)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(target, out hit, 20.0f, NavMesh.AllAreas))
        {
            return hit.position;
        }
        else
        {
            Debug.LogError("Cannot walk here!");
            return transform.position;
        }

    }


    private void walkTowards(Vector3 target)
    {
        if (target != nextBestTarget)
        {
            nextBestTarget = getClosestPointToTarget(target);
            target = nextBestTarget;
        }
        Vector3 offset = target - transform.position;
        offset = offset.normalized * speed;
        agent.destination = target;
        //controller.Move(offset * Time.deltaTime);
        //don't spin in circles
        if (offset.magnitude > 5)
        {
            offset.y = transform.position.y;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(offset), Time.deltaTime * rotationSpeed);
            anim.Play("walk");
        }
        else
        {

            reachedTarget();

        }
    }
    private void reachedTarget()
    {
        switch (currentState)
        {
            case MonsterState.AttackTemple:
                attack(temple);
                break;
            case MonsterState.AttackHumans:

                break;
            case MonsterState.AttackBuildings:

                break;
            case MonsterState.Idle:

                break;
        }


    }


    private void killerNextWaypoint()
    {
        if (waypoints.Count > 0)
        {
            if (dis2Enemy - 0.2f > Vector3.Distance(transform.position, closestEnemy.transform.position))
            {
                int srcCell = coord2cellID(transform.position);
                targetCell = coord2cellID(closestEnemy.transform.position);
                dis2Enemy = Vector3.Distance(transform.position, closestEnemy.transform.position);
                if (!threadRunning)
                {
                    moving = false;
                    threadRunning = true;
                    t1 = new Thread(() => astarD(srcCell, targetCell));
                    t1.Start();
                }
            }
            else
            {
                targetCell = waypoints[0];
                waypoints.Remove(targetCell);
                Vector3 p = pathFinder.getCellPosition(targetCell);
                p.y = 0.0f;
                nextNode = p;
                startTime = Time.time;
                startMarker = transform.position;
                endMarker = p;
                journeyLength = Vector3.Distance(startMarker, endMarker);
            }
        }
        else
        {
            if (!threadRunning)
            {
                nextNode.y = -50.0f;
                moving = false;
                dis2Enemy = Vector3.Distance(transform.position, closestEnemy.transform.position);
                int srcCell = coord2cellID(transform.position);
                targetCell = coord2cellID(closestEnemy.transform.position);
                t1 = new Thread(() => astarD(srcCell, targetCell));
                t1.Start();
            }
        }
    }

    private void getnextWaypoint()
    {
        if (fighterType == (int)Fighter.Rusher || fighterType == (int)Fighter.Defender)
        {
            if (!pathToTempleFound && !threadRunning)
            {
                if (temple != null)
                {
                    moving = false;
                    threadRunning = true;
                    Debug.Log("Searching for path to temple");
                    int srcCell = coord2cellID(transform.position);
                    targetCell = coord2cellID(temple.GetComponent<Collider>().ClosestPointOnBounds(transform.position));
                    t1 = new Thread(() => astarR(srcCell, targetCell));
                    t1.Start();

                }

            }
            else if (pathToTempleFound)
            {
                if (waypoints.Count > 0)
                {
                    targetCell = waypoints[0];
                    waypoints.Remove(targetCell);
                    Vector3 p = pathFinder.getCellPosition(targetCell);
                    p.y = 0.0f;
                    nextNode = p;
                    startTime = Time.time;
                    startMarker = transform.position;
                    endMarker = p;
                    journeyLength = Vector3.Distance(startMarker, endMarker);
                }
                else
                {
                    if (defending == false)
                    {
                        if (Vector3.Distance(temple.GetComponent<Collider>().ClosestPointOnBounds(transform.position), transform.position) > 2.0f)
                        {
                            Debug.Log("problem in Path finding");
                        }
                        else templeInRange = true;
                    }
                    else
                    {
                        if (Vector3.Distance(closestEnemy.GetComponent<Collider>().ClosestPointOnBounds(transform.position), transform.position) > 2.0f)
                        {
                            Debug.Log("problem in Path finding");
                        }
                        else enemyInrange = true;
                    }
                }
            }
        }
        else if (fighterType == (int)Fighter.Training)
        {
            if (waypoints.Count > 0)
            {
                targetCell = waypoints[0];
                waypoints.Remove(targetCell);
                Vector3 p = pathFinder.getCellPosition(targetCell);
                p.y = 0.0f;
                nextNode = p;
                startTime = Time.time;
                startMarker = transform.position;
                endMarker = p;
                journeyLength = Vector3.Distance(startMarker, endMarker);
            }
            else
            {
                if (closestEnemy != null && !threadRunning)
                {
                    moving = false;
                    threadRunning = true;
                    int srcCell = coord2cellID(transform.position);
                    targetCell = coord2cellID(closestEnemy.GetComponent<Collider>().ClosestPointOnBounds(transform.position));
                    t1 = new Thread(() => astarD(srcCell, targetCell));
                    t1.Start();
                }
            }
        }
    }

    bool attack(GameObject victim)
    {
        if (victim != null)
        {
            anim.Play("hit");
            transform.rotation = Quaternion.LookRotation(victim.transform.position- transform.position);
            //if it's a building
            HealthManager victimHealth = victim.GetComponent<HealthManager>();
            if (victimHealth != null)
            {
                victimHealth.decrementHealth(strenght * Time.deltaTime);
            } else {
                Debug.LogError("Trying to attack something that doesn not have health");
            }
            return true;
        }
        return false;
    }

    private int coord2cellID(Vector3 coords)
    {
        int layerMask = 1 << 8;
        Vector3 target = new Vector3(coords.x, 0.05f, coords.z);

        Collider[] obstacles = Physics.OverlapSphere(target, 0.05f, layerMask);
        if (obstacles.Length != 0)
        {
            return obstacles[0].gameObject.GetComponent<Cell>().id;
        }
        return -1;
    }

    public void decrementHealth(float damage)
    {
        health -= damage;
        if (health <= 0)
        {
            alive = false;
            anim.Play("die");
            StartCoroutine(WaitToDestroy(0.7f));
        }
    }
    private GameObject findClosestEnemy()
    {
        GameObject[] humans;
        GameObject closest = null;
        humans = GameObject.FindGameObjectsWithTag("Human");
        float distance = Mathf.Infinity;
        Vector3 position = transform.position;
        if (humans.Length > 0)
        {
            foreach (GameObject badie in humans)
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
        else
        {
            noMoreEnemies = true;
        }
        return closest;
    }
    public void changeMoving(bool val)
    {
        moving = val;
    }

    IEnumerator WaitToDestroy(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        resources.removeBaddie();
        GameObject.Destroy(gameObject);
    }

    public void changeMode(bool val)
    {
        //Not used for baddies atm
    }

}