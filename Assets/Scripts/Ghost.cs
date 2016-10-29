using UnityEngine;
using System.Collections;
using Rewired;
using Com.LuisPedroFonseca.ProCamera2D;

public class Ghost : MonoBehaviour {
    public Rigidbody2D rigid;
    public Animator anim;
    public LayerMask skelletonLayer;
    public BoxCollider2D box;
    public SpriteRenderer sprite;
    public SpriteRenderer lightCircle;
    public int playerID;
    public float boxCastDistance;
    public float velocity;
    public float softVelocity;
    private Rewired.Player rePlayer;
    bool alive = true;
    private int flipped = -1;
    [HideInInspector]
    public bool free;
    [HideInInspector]
    public float lightTime;
    [HideInInspector]
    public float invulnerabilityTime;
    public float maxInv;


    // Use this for initialization
    void Start ()
    {
        if (playerID >= GameManager.gManager.players.Length || !GameManager.gManager.players[playerID].active)
        {
            gameObject.SetActive(false);
            return;
        }
        rePlayer = ReInput.players.GetPlayer(playerID);
        Camera.main.GetComponent<ProCamera2D>().AddCameraTarget(transform);
        free = true;
        lightTime = 1;
    }
	// Update is called once per frame
	void Update () {
        if (!alive)
        {
            return;
        }
        invulnerabilityTime = Mathf.Clamp(invulnerabilityTime - Time.deltaTime, 0, maxInv);
        detectControl();
        savePosition();
	}

    void LateUpdate()
    {
        if (!free) return;
        if (alive)
        {
            lightTime = Mathf.Clamp(lightTime - Time.deltaTime, 0, 1);
            sprite.color = new Color(sprite.color.r, sprite.color.g, sprite.color.b,
                                        Mathf.Max(GameManager.gManager.players[playerID].alpha, lightTime));
            GameManager.gManager.players[playerID].alpha = 0;
            lightCircle.color = new Color(lightCircle.color.r, lightCircle.color.g, lightCircle.color.b, lightTime / 2);
        }
        else
        {
            sprite.color = new Color(sprite.color.r, sprite.color.g, sprite.color.b, 1);
            lightCircle.color = new Color(lightCircle.color.r, lightCircle.color.g, lightCircle.color.b, 0);
        }
    }

    void detectControl()
    {
        float xAxis = rePlayer.GetAxis("Horizontal");
        float yAxis = rePlayer.GetAxis("Vertical");
        rigid.velocity = Vector2.MoveTowards(rigid.velocity, new Vector2(velocity * xAxis, velocity * yAxis), softVelocity * Time.deltaTime);
        Flip();
        if (rePlayer.GetButtonDown("Leave") || rePlayer.GetButtonDown("PickUp"))
        {
            RaycastHit2D[] rch = Physics2D.BoxCastAll((Vector2)transform.position + box.offset, box.size, 0, Vector3.down, boxCastDistance,
                                               skelletonLayer);
            foreach(RaycastHit2D r in rch)
            {
                BasicPlayer bp = r.collider.GetComponent<BasicPlayer>();
                if (bp == null || bp.ghost != null || bp.health <= 0 || bp.stamina < 3) continue;
                bp.ghost = this;
                transform.SetParent(bp.gameObject.transform);
                transform.localPosition = Vector3.zero;
                anim.SetTrigger("Possess");
                bp.anim.SetTrigger("Alive");
                this.enabled = false;
                rigid.velocity = Vector2.zero;
                free = false;
                return;
            }
            lightTime = 1;
        }
    }

    public void savePosition()
    {
        GameManager.gManager.players[playerID].position = transform.position;
    }

    void EndPossession()
    {
        gameObject.SetActive(false);
    }
    void EndEscape()
    {
        enabled = true;
    }
    void Flip()
    {
        if (rigid.velocity.x > 0 && flipped == 1) flipped = -1;
        if (rigid.velocity.x < 0 && flipped == -1) flipped = 1;
        transform.localScale = new Vector3(flipped, transform.localScale.y, transform.localScale.z);
    }
    public void Die()
    {
        if (invulnerabilityTime > 0) return;
        anim.SetTrigger("Die");
        alive = false;
        GameManager.gManager.players[playerID].dead = true;
    }
    public void destroySelf()
    {
        Destroy(gameObject);
    }
}
[System.Serializable]
public class playerData
{
    public bool active;
    public bool dead;
    public Vector3 position;
    public float alpha;
}
