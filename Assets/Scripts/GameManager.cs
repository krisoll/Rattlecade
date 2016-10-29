using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour {
    public static GameManager gManager;

	// Use this for initialization
	void Awake () {
        Application.targetFrameRate = 30;
        if (gManager == null)
        {
            gManager = this;
        }
        else if (gManager != this)
        {
            //Then destroy this. This enforces our singleton pattern, meaning there can only ever be one instance of a GameManager.
            Destroy(gManager.gameObject);
            gManager = this;
        }
        //Sets this to not be destroyed when reloading scene
        DontDestroyOnLoad(gameObject);
    }
	
	// Update is called once per frame
	void Update () {
	
	}
}
