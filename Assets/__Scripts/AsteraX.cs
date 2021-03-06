//#define DEBUG_AsteraX_LogMethods
// Used to predict collisions on the respawn point.
#define DEBUG_Asteroid_PlotCollision

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AsteraX : MonoBehaviour
{
    // Private Singleton-style instance. Accessed by static property S later in script
    static private AsteraX _S;

    static List<Asteroid>           ASTEROIDS;
    static List<Bullet>             BULLETS;
	static int 						SCORE = 0;
	private GameObject 				GAME_OVER_SCREEN;


    const float MIN_ASTEROID_DIST_FROM_PLAYER_SHIP = 5;
	const int MAX_JUMPS = 3;

    // System.Flags changes how eGameStates are viewed in the Inspector and lets multiple 
    //  values be selected simultaneously (similar to how Physics Layers are selected).
    // It's only valid for the game to ever be in one state, but I've added System.Flags
    //  here to demonstrate it and to make the ActiveOnlyDuringSomeGameStates script easier
    //  to view and modify in the Inspector.
    // When you use System.Flags, you still need to set each enum value so that it aligns 
    //  with a power of 2. You can also define enums that combine two or more values,
    //  for example the all value below that combines all other possible values.
    [System.Flags]
    public enum eGameState
    {
        // Decimal      // Binary
        none = 0,       // 00000000
        mainMenu = 1,   // 00000001
        preLevel = 2,   // 00000010
        level = 4,      // 00000100
        postLevel = 8,  // 00001000
        gameOver = 16,  // 00010000
        all = 0xFFFFFFF // 11111111111111111111111111111111
    }

    [Header("Set in Inspector")]
    [Tooltip("This sets the AsteroidsScriptableObject to be used throughout the game.")]
    public AsteroidsScriptableObject asteroidsSO;

	public delegate void CallbackDelegateV3(Vector3 respawnPoint);
	private CallbackDelegateV3 respawnCallback;


	private void Awake()
    {
#if DEBUG_AsteraX_LogMethods
        Debug.Log("AsteraX:Awake()");
#endif

        S = this;
    }


    void Start()
    {
#if DEBUG_AsteraX_LogMethods
        Debug.Log("AsteraX:Start()");
#endif

		GAME_OVER_SCREEN = GameObject.Find ("GameOverBG");
		GAME_OVER_SCREEN.SetActive (false);

		ASTEROIDS = new List<Asteroid>();
        
        // Spawn the parent Asteroids, child Asteroids are taken care of by them
        for (int i = 0; i < 3; i++)
        {
            SpawnParentAsteroid(i);
        }
 

	}


    void SpawnParentAsteroid(int i)
    {
#if DEBUG_AsteraX_LogMethods
        Debug.Log("AsteraX:SpawnParentAsteroid("+i+")");
#endif

        Asteroid ast = Asteroid.SpawnAsteroid();
        ast.gameObject.name = "Asteroid_" + i.ToString("00");
        // Find a good location for the Asteroid to spawn
        Vector3 pos;
        do
        {
            pos = ScreenBounds.RANDOM_ON_SCREEN_LOC;
        } while ((pos - PlayerShip.POSITION).magnitude < MIN_ASTEROID_DIST_FROM_PLAYER_SHIP);

        ast.transform.position = pos;
        ast.size = asteroidsSO.initialSize;
    }

	public void EndGame()
	{
		Debug.Log ("GAME OVER");
		GAME_OVER_SCREEN.SetActive (true);
		GameObject gameOverText = GameObject.Find ("InfoText");
		gameOverText.GetComponent<Text> ().text = "Score: " + SCORE;
		Invoke("ReloadScene", 4);
	}

	void ReloadScene()
	{
		UnityEngine.SceneManagement.SceneManager.LoadScene(0);
	}



    // ---------------- Static Section ---------------- //

    /// <summary>
    /// <para>This static public property provides some protection for the Singleton _S.</para>
    /// <para>get {} does return null, but throws an error first.</para>
    /// <para>set {} allows overwrite of _S by a 2nd instance, but throws an error first.</para>
    /// <para>Another advantage of using a property here is that it allows you to place
    /// a breakpoint in the set clause and then look at the call stack if you fear that 
    /// something random is setting your _S value.</para>
    /// </summary>
    static private AsteraX S
    {
        get
        {
            if (_S == null)
            {
                Debug.LogError("AsteraX:S getter - Attempt to get value of S before it has been set.");
                return null;
            }
            return _S;
        }
        set
        {
            if (_S != null)
            {
                Debug.LogError("AsteraX:S setter - Attempt to set S when it has already been set.");
            }
            _S = value;
        }
    }


    static public AsteroidsScriptableObject AsteroidsSO
    {
        get
        {
            if (S != null)
            {
                return S.asteroidsSO;
            }
            return null;
        }
    }
    
	static public void AddAsteroid(Asteroid asteroid)
    {
        if (ASTEROIDS.IndexOf(asteroid) == -1)
        {
            ASTEROIDS.Add(asteroid);
        }

	}
    static public void RemoveAsteroid(Asteroid asteroid)
    {
        if (ASTEROIDS.IndexOf(asteroid) != -1)
        {
            ASTEROIDS.Remove(asteroid);
        }
    }
	static public void AddScore(int points) 
	{
		// Clean this up and check null on scoreText - or find a way to set it in the inspector, avoid using the .Find method
		SCORE += points;
		GameObject scoreText = GameObject.Find ("ScoreText");
		scoreText.GetComponent<Text> ().text = "Score: " + SCORE;
	}

	static public void GameOver()
	{
		_S.EndGame();
	}

	/// ---------------- Respawn Point Collision Avoidance Section ---------------- ///

	/// <summary>
	/// <para>The dot product of vector A and second vector B results in
	/// the length of vector A projected in the direction of vector B.
	/// Here the dot product is the projection of an asteroid's velocity onto the relative position
	/// of the potential respawn point.  Divide the dot product by the relative speed squared 
	/// (magnitude of the relative velocity) to calculate 
	/// the time when the objects will be closest to each other during their trajectories.  
	/// minSeparation becomes the distance between the two objects at their closest approach.  
	/// If this distance is small enough they will collide.</para>
	/// <para>I have set it to check for a minSeparation value of < .3 and if the asteroid is 
	/// within 3 seconds of approaching this point.</para>
	/// </summary>
	/// <returns>A safe respawn point.</returns>

	static public IEnumerator PlotRespawnPointCoroutine (CallbackDelegateV3 callback) 
	{
		Vector3 respawnPoint = Vector3.zero;
		bool isSafePoint;
		// Attempt to find a safe point 20 times max to avoid possible infinite loop
		// if there are no possible safe spots
		int attempts = 20;

		do
		{
			// Choose a random location within 80% of the play area;
			respawnPoint = ScreenBounds.RANDOM_RESPAWN_LOC;
			isSafePoint = true;
			attempts--;

			foreach (Asteroid a in ASTEROIDS) {
				// Only predict collisions with parent asteroids
				if (a.transform.parent == null) {
					Vector3 relativePos = a.transform.position - respawnPoint;
					Vector3 relativeVel = a.GetComponent<Rigidbody> ().velocity;
					float relativeSpeed = relativeVel.magnitude;

					// Calculate the time of the closest approach of this asteroid to the respawn point
					float timeToClosestApproach = Vector3.Dot(relativePos, relativeVel);
					timeToClosestApproach /= relativeSpeed * relativeSpeed * -1;

					float distance = relativePos.magnitude;
					// The minimal Separation is the distance between the two objects at the time of the closest approach. 
					float minSeparation = distance - relativeSpeed * timeToClosestApproach;

					// If a collision is likely within 3 seconds flag this point as false
					// Increase minSeparation if you want to give the player a bit more room to move after respawn.
					if (minSeparation < .3 && timeToClosestApproach <= 3) {
						#if DEBUG_Asteroid_PlotCollision
						Debug.DrawLine (respawnPoint, a.transform.position, Color.white, 3f);
						#endif
						isSafePoint = false;
					}
				}
			}
		} while (attempts > 0 && isSafePoint == false);

		yield return new WaitForSeconds (PlayerShip.respawnTimer);

		callback (respawnPoint);

	}

}
