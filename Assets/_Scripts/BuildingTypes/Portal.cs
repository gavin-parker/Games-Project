﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Portal : MonoBehaviour
{

    bool active = false;
    [System.Serializable]
    public enum MonsterType { Monster, Minataur, Harpy };

    [SerializeField]
    public Wave[] Waves;
    public GameObject spawnPos;
    public GameObject[] monsterTypes;
    // delay befire first spawn

    public float delayStart;
    Vector3 pos;
    AudioClip attackClip;
    AudioSource asource;


    GameObject temple;
    ResourceCounter resourceCounter;

    private float startTime;
    bool started = false;
    MonsterType currentType = 0;

    void Start()
    {
        temple = GameObject.FindGameObjectWithTag("Temple");
        resourceCounter = GameObject.FindGameObjectWithTag("Tablet").GetComponent<ResourceCounter>();
        pos = spawnPos.transform.position;
        asource = GetComponent<AudioSource>();
        startTime = Time.time;
    }

    void Update()
    {
        if (temple == null)
        {
            return;
        }
        if (!started && temple.GetComponent<Temple>().isPlaced())
        {
            started = true;
            StartCoroutine(spawnWaves());
        }

    }

    IEnumerator spawnWaves()
    {
        resourceCounter.beginCountDown(delayStart);
        yield return new WaitForSeconds(delayStart);
        foreach (Wave wave in Waves)
        {
            asource.Play();
            //spawn each monster with a 1 second delay
            foreach (MonsterType monsterType in wave.monsters)
            {
                Vector3 validSpawnLoc = pos;
                NavMeshHit hit;
                if (NavMesh.SamplePosition(pos, out hit, 40.0f, NavMesh.AllAreas))
                {
                    validSpawnLoc = hit.position;
                }
                else
                    Debug.LogError("Could not spawn monster");
                GameObject monster = GameObject.Instantiate(monsterTypes[(int)monsterType], validSpawnLoc, Quaternion.identity);
                monster.GetComponent<BadiesAI>().spawn(monsterType);
                yield return new WaitForSeconds(2);
            }
            resourceCounter.beginCountDown(wave.waveTime);
            Debug.Log("Waiting for " + wave.waveTime + " seconds");
            yield return new WaitForSeconds(wave.waveTime);
        }
    }

}