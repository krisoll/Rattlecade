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
    public LayerMask weaponLayer;
    [HideInInspector]
    public Ghost ghost;
    private Attack[] ats;
    public GameObject pivotPoint;
    private List<SpriteRenderer> sprites;
    public Weapon weapon;
    public GameObject weaponContainer;
    public int health;
    public int stamina;
    public float velocity;
    public float jumpVelocity;
    public float aimSensibility;
    private RaycastHit2D grounded;
    private Player rePlayer;
    private float horizontal;
    private int flipped = -1;
    private bool canMove;
    private float TimeCount;
    private float TimeCount2;
    private Vector2 savedAim;

    void OnDrawGizmos()
    {
        Vector2 v;
        if (playerID == 0) v = Camera.main.ScreenToWorldPoint(Input.mousePosition) - pivotPoint.transform.position;
        else v = new Vector3(rePlayer.GetAxis("HAim") * 10, rePlayer.GetAxis("VAim") * 10);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere((Vector2)pivotPoint.transform.position + v.normalized, .03f);
    }

    // Use this for initialization
    void Start () {
        rePlayer = ReInput.players.GetPlayer(playerID);
        sprites = new List<SpriteRenderer>();
        getSR(gameObject, ref sprites);
        anim.SetBool("Grounded", true);
        ats = GetComponentsInChildren<Attack>();
    }
	
	// Update is called once per frame
	void Update () {
        if (health <= 0)
        {
            DestroySelf();
        }
        if (!canMove) return;
        grounded = Physics2D.BoxCast(new Vector2(transform.position.x + box.offset.x, transform.position.y + box.offset.y), 
                                     box.size, 0, Vector2.down, 0.05f, ground);
        anim.SetBool("Grounded", grounded);
        if (grounded)
        {
            if (rePlayer.GetButtonDown("Jump"))
            {
                rigid.velocity = new Vector2(rigid.velocity.x, jumpVelocity);
            }
        }
        horizontal = rePlayer.GetAxisRaw("Horizontal");
        anim.SetFloat("Velocity", Mathf.Abs(horizontal));
        rigid.velocity = new Vector2(horizontal * velocity, rigid.velocity.y);
        if (rePlayer.GetButtonDown("Shoot"))
        {
            anim.SetTrigger("Attack");
        }
        else if (rePlayer.GetButtonDown("PickUp"))
        {
            if (weapon == null)
            {
                RaycastHit2D[] rch = Physics2D.BoxCastAll((Vector2)transform.position + box.offset, box.size, 0, Vector2.down, .1f,
                                                   weaponLayer);
                foreach (RaycastHit2D r in rch)
                {
                    Weapon w = r.collider.GetComponent<Weapon>();
                    if (w == null || w.equiped) continue;
                    weapon = w;
                    w.equiped = true;
                    w.transform.SetParent(weaponContainer.transform);
                    w.transform.localPosition = Vector2.zero;
                    w.transform.localEulerAngles = Vector2.zero;
                    w.rigid.velocity = Vector2.zero;
                    w.rigid.Sleep();
                    w.rigid.isKinematic = true;
                    w.box.enabled = false;
                    switch (w.type)
                    {
                        case Weapon.WeaponType.LongRange: anim.SetInteger("Weapon", 1); break;
                        case Weapon.WeaponType.ShortRange: anim.SetInteger("Weapon", 2); break;
                        case Weapon.WeaponType.Melee: anim.SetInteger("Weapon", 3); break;
                    }
                    ats = GetComponentsInChildren<Attack>();
                    foreach (Attack a in ats)
                    {
                        a.playerID = playerID;
                    }
                    break;
                }
            }
            else
            {
                anim.SetInteger("Weapon", 0);
                weapon.equiped = false;
                weapon.transform.SetParent(null);
                weapon.rigid.WakeUp();
                weapon.box.enabled = true;
                weapon.rigid.isKinematic = false;
                weapon = null;
                ats = GetComponentsInChildren<Attack>();
                foreach (Attack a in ats)
                {
                    a.playerID = playerID;
                }
            }
        }
        else if (rePlayer.GetButtonDown("Leave") || health <= 0)
        {
            this.canMove = false;
            ghost.transform.SetParent(null);
            ghost.gameObject.SetActive(true);
            anim.SetBool("Grounded", true);
            ghost.anim.SetTrigger("Escaping");
            anim.SetTrigger("Die");
            anim.ResetTrigger("Attack");
            anim.ResetTrigger("Shoot");
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
        if (playerID == 0) savedAim = Camera.main.ScreenToWorldPoint(Input.mousePosition) - pivotPoint.transform.position;
        else if(new Vector3(rePlayer.GetAxis("HAim"), rePlayer.GetAxis("VAim")).magnitude > aimSensibility)
                savedAim = (new Vector3(rePlayer.GetAxis("HAim"), rePlayer.GetAxis("VAim"))).normalized * 2;
        Vector2 v = savedAim + (Vector2)pivotPoint.transform.position;
        if (transform.position.x < v.x - 0.01 && flipped == 1) flipped = -1;
        if (transform.position.x > v.x + 0.01 && flipped == -1) flipped = 1;
        foreach (Attack a in ats)
        {
            a.fliped = flipped;
        }
        transform.localScale = new Vector3(flipped, transform.localScale.y, transform.localScale.z);
        Vector3 diff = v - (Vector2)pivotPoint.transform.position;
        diff.Normalize();

        float rot_z = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        pivotPoint.transform.rotation = Quaternion.Euler(0f, 0f, rot_z - (flipped == 1 ? 180 : 0));
    }

    public void CanMove()
    {
        if (ghost == null) return;
        canMove = true;
        rePlayer = ReInput.players.GetPlayer(ghost.playerID);
        playerID = ghost.playerID;
        foreach(Attack a in ats)
        {
            a.playerID = playerID;
        }
    }

    public void Damage(int i)
    {
        if (!canMove) return;
        health -= i;
    }

}
