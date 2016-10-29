using UnityEngine;
using System.Collections;
using Rewired;
public class BasicPlayer : MonoBehaviour {
    public int playerID;
    public Rigidbody2D rigid;
    public BoxCollider2D box;
    public LayerMask ground;
    public Ghost gost;
    public float velocity;
    public float jumpVelocity;
    private RaycastHit2D grounded;
    private Player rePlayer;
    private float horizontal;
    private int flipped = -1;
	// Use this for initialization
	void Start () {
        rePlayer = ReInput.players.GetPlayer(playerID);
	}
	
	// Update is called once per frame
	void Update () {
        grounded = Physics2D.BoxCast(new Vector2(transform.position.x + box.offset.x, transform.position.y + box.offset.y), box.size, 0, Vector2.down, 0.01f, ground);
        if (grounded)
        {
            if (rePlayer.GetAxis("Vertical")>0)
            {
                rigid.velocity = new Vector2(rigid.velocity.x, jumpVelocity);
            }
        }
        Flip();
        horizontal = rePlayer.GetAxisRaw("Horizontal");
        rigid.velocity = new Vector2(horizontal * velocity, rigid.velocity.y);
	}

    void Flip()
    {
        if (rigid.velocity.x > 0 && flipped == 1) flipped = -1;
        if (rigid.velocity.x < 0 && flipped == -1) flipped = 1;
        transform.localScale = new Vector3(flipped, transform.localScale.y, transform.localScale.z);
    }
}
