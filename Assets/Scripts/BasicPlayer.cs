using UnityEngine;
using System.Collections;
using Rewired;
using System.Collections.Generic;

public class BasicPlayer : MonoBehaviour {
    public int playerID;
    public Rigidbody2D rigid;
    public BoxCollider2D box;
    public Animator anim;
    public LayerMask ground;
    [HideInInspector]
    public Ghost ghost;
    public GameObject pivotPoint;
    private List<SpriteRenderer> sprites;
    public int weapon;
    public int health;
    public int stamina;
    public float velocity;
    public float jumpVelocity;
    private RaycastHit2D grounded;
    private Player rePlayer;
    private float horizontal;
    private int flipped = -1;
    private bool canMove;
    private float TimeCount;
    private float TimeCount2;
    // Use this for initialization
    void Start () {
        rePlayer = ReInput.players.GetPlayer(playerID);
        sprites = new List<SpriteRenderer>();
        getSR(gameObject, ref sprites);
    }
	
	// Update is called once per frame
	void Update () {
        if (health <= 0)
        {
            DestroySelf();
        }
        if (!canMove) return;
        grounded = Physics2D.BoxCast(new Vector2(transform.position.x + box.offset.x, transform.position.y + box.offset.y), box.size, 0, Vector2.down, 0.01f, ground);
        if (grounded)
        {
            if (rePlayer.GetAxis("Vertical")>0)
            {
                rigid.velocity = new Vector2(rigid.velocity.x, jumpVelocity);
            }
        }
        //FlipToMouse();
        horizontal = rePlayer.GetAxisRaw("Horizontal");
        anim.SetFloat("Velocity", Mathf.Abs(horizontal));
        rigid.velocity = new Vector2(horizontal * velocity, rigid.velocity.y);
        if (rePlayer.GetButtonDown("Shoot"))
        {

        }
        else if (rePlayer.GetButtonDown("Leave") || health <= 0)
        {
            this.canMove = false;
            ghost.transform.SetParent(null);
            ghost.gameObject.SetActive(true);
            ghost.anim.SetTrigger("Escaping");
            anim.SetTrigger("Die");
            ghost = null;
            anim.SetFloat("Velocity", 0);
        }
	}

    public void LateUpdate()
    {
        if (!canMove) return;
        FlipToMouse();
    }
    
    private void getSR(GameObject g, ref List<SpriteRenderer> list)
    {
        SpriteRenderer sr = g.GetComponent<SpriteRenderer>();
        if (sr != null) list.Add(sr);
        for (int i = 0; i < g.transform.childCount; i++)
        {
            getSR(g.transform.GetChild(i).gameObject, ref list);
        }
    }

    void DestroySelf()
    {
        TimeCount += Time.deltaTime;
        TimeCount2 += Time.deltaTime;
        if (TimeCount > .05f + (2 - TimeCount2) / 20)
        {
            TimeCount = 0;
            foreach(SpriteRenderer sr in sprites)
            {
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, sr.color.a < .01f ? 1 : 0);
            }
        }
        if(TimeCount2 > 2)
        {
            Destroy(gameObject);
        }
    }

    void Flip()
    {
        if (rigid.velocity.x > 0 && flipped == 1) flipped = -1;
        if (rigid.velocity.x < 0 && flipped == -1) flipped = 1;
        transform.localScale = new Vector3(flipped, transform.localScale.y, transform.localScale.z);
    }

    public void FlipToMouse()
    {
        Vector3 v = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (transform.position.x < v.x && flipped == 1) flipped = -1;
        if (transform.position.x > v.x && flipped == -1) flipped = 1;
        transform.localScale = new Vector3(flipped, transform.localScale.y, transform.localScale.z);
        Vector3 diff = Camera.main.ScreenToWorldPoint(Input.mousePosition) - pivotPoint.transform.position;
        diff.Normalize();

        float rot_z = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        pivotPoint.transform.rotation = Quaternion.Euler(0f, 0f, rot_z - (flipped == 1 ? 180 : 0));
    }

    public void CanMove()
    {
        canMove = true;
        rePlayer = ReInput.players.GetPlayer(ghost.playerID);
    }

    public void Damage(int i)
    {
        if (!canMove) return;
        health -= i;
    }

}
