﻿using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Threading;

public class Agent : MonoBehaviour {
 
	public bool fightMode;
	public float radius;

	private Thread t1;
	private bool threadRunning = false;

	List<int> waypoints;

	public float health;
	public float strenght;
	public float speed;
	public float gravity = 9.81F;
	public float weapon_strength;

	private Vector3 initialPosition;
	private PathFinding pathFinder;
	private Vector3 nextNode;
	private int targetCell;
	private CharacterController controller;
	private Vector3 cracyVector = new Vector3 (0.0f,-50.0f,0.0f);

	private GameObject closestBadie;
	private bool noMoreEnemies;
	private int maxCell;

	private Rigidbody rb;

	void Start () {
		radius = 50;
		strenght = 10;
		health = 100;
		nextNode =  new Vector3 (0.0f,-50.0f,0.0f);
		waypoints = new List<int>();
		noMoreEnemies = false;
		initialPosition = transform.position;
		controller = GetComponent<CharacterController>();
		rb = GetComponent<Rigidbody>();
		pathFinder =(PathFinding) GameObject.FindGameObjectWithTag("PathFinder").GetComponent(typeof(PathFinding));
		maxCell =pathFinder.getMaxCell();
		closestBadie = null;

	}
	
	// Update is called once per frame
	void FixedUpdate () {
		wanderNav();
	}
	void OnMouseDown(){
		Debug.Log("clicekd");
		if(!threadRunning){
			Debug.Log("Here");
			Vector3 target = new Vector3(0.0f,0.05f,-20.0f);
			int srcCell = coord2cellID(transform.position);
			int targetCell = coord2cellID(target);
			threadRunning = true;
			t1 = new Thread(()=>astar(srcCell,targetCell));
			t1.Start();
		}
	}

	void  astar(int srcCell, int targetCell){
		Debug.Log("Entered");
		waypoints = new List<int>(pathFinder.Astar(srcCell,targetCell));
		Debug.Log("Done");
		threadRunning = false;
	}

	private void wanderNav(){
		if(nextNode.y  == -50f){
			if (waypoints.Count > 0){
				int n = waypoints[0];
				waypoints.Remove(n);
				Vector3 p = pathFinder.getCellPosition(n);
				p.y = 0.5f;
				nextNode = p;
			}
			else{
				//GetComponent<Rigidbody>().velocity = Vector3.zero;
				//GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
				int srcCell = coord2cellID(transform.position);
				int targetCell = calculateNewTarget();
				if(!threadRunning){
					threadRunning = true;
					t1 = new Thread(()=>astar(srcCell,targetCell));
					t1.Start();
				}
			}
		}
		else if (Vector3.Distance(transform.position,nextNode) < 0.3f){
			if(waypoints.Count < 1){
				rb.velocity /= 10;
				nextNode.y = -50f ;
			}
			else{
				int n = waypoints[0];
				waypoints.Remove(n);
				Vector3 p = pathFinder.getCellPosition(n);
				p.y = 0.5f;
				nextNode = p;
			}
		}
		else{
			rb.velocity =((nextNode -transform.position) * speed);
		}
	}

	// lazy function cuz i can not be bothered to find the correct mathematical approach tonight
	private int coord2cellID(Vector3 coords){
		int layerMask = 1 << 8;
		Vector3 target = new Vector3(coords.x,0.05f,coords.z);
		
		Collider[] obstacles = Physics.OverlapSphere(target, 0.05f, layerMask);
		if (obstacles.Length != 0){
			return obstacles[0].gameObject.GetComponent<Cell>().id;
		}
		return -1;
	}

	
	private GameObject findClosestEnemy(){
		GameObject[] badies;
		GameObject closest = null;
		badies = GameObject.FindGameObjectsWithTag("Badies");
		float distance = Mathf.Infinity;
		Vector3 position = transform.position;
		foreach (GameObject badie in badies){
			Vector3 diff = badie.transform.position - position;
			float current_distance = diff.sqrMagnitude;
			if (current_distance < distance){
				distance = current_distance;
				closest = badie;
			}
		}
		// if you win and all badies are dead what you do?
		return closest;
	}
	
	public void decrementHealth(float damage){
		health -= damage;
		if (health <= 0){
			Destroy(gameObject);
		}
	}

	int calculateNewTarget(){
		int target = (int) Random.Range(0.0f,pathFinder.getMaxCell());
		string status = pathFinder.checkCell(target);
		while (status != "empty"){
			target = (int) Random.Range(0.0f,maxCell);
			status= pathFinder.checkCell(target);
			Debug.Log("Hit");
		}
		return target;
	}
}