using UnityEngine;
using System.Collections;
using Rewired;

public class Ghost : MonoBehaviour {
    public Rigidbody2D rigid;
    public Animator anim;
    public int playerID;
    public float velocity;
    public float softVelocity;
    private Rewired.Player rePlayer;


    // Use this for initialization
    void Start ()
    {
        if (!GameManager.gManager.players[playerID].active)
        {
            gameObject.SetActive(false);
            return;
        }
        rePlayer = ReInput.players.GetPlayer(playerID);
	}
	
	// Update is called once per frame
	void Update () {
        detectControl();
	}

    void detectControl()
    {
        float xAxis = rePlayer.GetAxis("Horizontal");
        float yAxis = rePlayer.GetAxis("Vertical");
        rigid.velocity = Vector2.MoveTowards(rigid.velocity, new Vector2(velocity * xAxis, velocity * yAxis), softVelocity * Time.deltaTime);
    }
}
[System.Serializable]
public class playerData
{
    public bool active;
}
