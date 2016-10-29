using UnityEngine;
using System.Collections;
using Rewired;
public class BasicPlayer : MonoBehaviour {
    public int playerID;
    public Rigidbody2D rigid;
    public float velocity;
    private Player rePlayer;
    private float horizontal;
	// Use this for initialization
	void Start () {
        rePlayer = ReInput.players.GetPlayer(playerID);
	}
	
	// Update is called once per frame
	void Update () {
        horizontal = rePlayer.GetAxisRaw("Horizontal");
        rigid.velocity = new Vector2(horizontal * velocity, rigid.velocity.y);
	}
}
