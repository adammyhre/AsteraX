﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.CrossPlatformInput;

[RequireComponent(typeof(Rigidbody))]
public class PlayerShip : MonoBehaviour
{
 
	// This is a somewhat protected private singleton for PlayerShip
    static private PlayerShip   _S;
    static public PlayerShip    S
    {
        get
        {
            return _S;
        }
        private set
        {
            if (_S != null)
            {
                Debug.LogWarning("Second attempt to set PlayerShip singleton _S.");
            }
            _S = value;
        }
    }

	static public int JUMPS;
	static Text JUMPS_TEXT;

    [Header("Set in Inspector")]
    public float        shipSpeed = 10f;
    public GameObject   bulletPrefab;
	[Tooltip("Starting number of jumps allowed.")]
	public int			maxJumps = 3;
	[Tooltip("Time to wait before after a player ship is destroyed.")]
	static public float respawnTimer = 2f;

    Rigidbody           rigid;


    void Awake()
    {
        S = this;

		JUMPS = maxJumps;
		JUMPS_TEXT = GameObject.Find ("JumpsText").GetComponent<Text> ();
		JUMPS_TEXT.text = "Jumps: " + JUMPS;

        // NOTE: We don't need to check whether or not rigid is null because of [RequireComponent()] above
        rigid = GetComponent<Rigidbody>();
    }


	// Setting this function to public so it can be called from the Asteroid's OnCollisionEnter function
	public void Jump() 
	{
		JUMPS--;
		JUMPS_TEXT.text = "Jumps: " + JUMPS;
		if (JUMPS <= 0) {
			gameObject.SetActive(false);
			AsteraX.GameOver();
		} else {
			Respawn();
		}
	}


	void Respawn() 
	{
		StartCoroutine(AsteraX.PlotRespawnPointCoroutine(RespawnCallback));
		// Move the player off screen, disable OffScreenWrapper until respawn
		OffScreenWrapper wrapper = GetComponent<OffScreenWrapper>();
		if (wrapper != null) {
			wrapper.enabled = false;
		}
		transform.position = new Vector3(10000,10000,0);
	}

	void RespawnCallback(Vector3 respawnPoint) 
	{
		OffScreenWrapper wrapper = GetComponent<OffScreenWrapper>();
		if (wrapper != null) {
			wrapper.enabled = true;
		}
		transform.position = respawnPoint;
	}


    void Update()
    {
        // Using Horizontal and Vertical axes to set velocity
        float aX = CrossPlatformInputManager.GetAxis("Horizontal");
        float aY = CrossPlatformInputManager.GetAxis("Vertical");

        Vector3 vel = new Vector3(aX, aY);
        if (vel.magnitude > 1)
        {
            // Avoid speed multiplying by 1.414 when moving at a diagonal
            vel.Normalize();
        }

        rigid.velocity = vel * shipSpeed;

        // Mouse input for firing
        if (CrossPlatformInputManager.GetButtonDown("Fire1"))
        {
            Fire();
        }
    }


    void Fire()
    {
        // Get direction to the mouse
        Vector3 mPos = Input.mousePosition;
        mPos.z = -Camera.main.transform.position.z;
        Vector3 mPos3D = Camera.main.ScreenToWorldPoint(mPos);

        // Instantiate the Bullet and set its direction
        GameObject go = Instantiate<GameObject>(bulletPrefab);
        go.transform.position = transform.position;
        go.transform.LookAt(mPos3D);
    }

    static public float MAX_SPEED
    {
        get
        {
            return S.shipSpeed;
        }
    }
    
	static public Vector3 POSITION
    {
        get
        {
            return S.transform.position;
        }
    }
}
