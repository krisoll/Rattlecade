using UnityEngine;
using System.Collections;
using Rewired;

public class Ghost : MonoBehaviour {
    public Rigidbody2D rigid;
    public Animator anim;
    public int playerID;
    private Rewired.Player rePlayer;


    // Use this for initialization
    void Start () {
        rePlayer = ReInput.players.GetPlayer(playerID);
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
