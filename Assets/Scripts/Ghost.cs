using UnityEngine;
using System.Collections;
using Rewired;

public class Ghost : MonoBehaviour {
    public Rigidbody2D rigid;
    public Animator anim;
    public LayerMask skelletonLayer;
    public BoxCollider2D box;
    public int playerID;
    public float boxCastDistance;
    public float velocity;
    public float softVelocity;
    private Rewired.Player rePlayer;
    bool alive = true;
    private int flipped = -1;


    // Use this for initialization
    void Start ()
    {
        if (playerID >= GameManager.gManager.players.Length || !GameManager.gManager.players[playerID].active)
        {
            gameObject.SetActive(false);
            return;
        }
        rePlayer = ReInput.players.GetPlayer(playerID);
	}
	
	// Update is called once per frame
	void Update () {
        if (!alive) return;
        
        detectControl();
        savePosition();
	}

    void detectControl()
    {
        float xAxis = rePlayer.GetAxis("Horizontal");
        float yAxis = rePlayer.GetAxis("Vertical");
        rigid.velocity = Vector2.MoveTowards(rigid.velocity, new Vector2(velocity * xAxis, velocity * yAxis), softVelocity * Time.deltaTime);
        Flip();
        if (rePlayer.GetButtonDown("Leave"))
        {
            RaycastHit2D[] rch = Physics2D.BoxCastAll((Vector2)transform.position + box.offset, box.size, 0, Vector3.down, boxCastDistance,
                                               skelletonLayer);
            foreach(RaycastHit2D r in rch)
            {
                BasicPlayer bp = r.collider.GetComponent<BasicPlayer>();
                if (bp == null || bp.ghost != null) continue;
                bp.ghost = this;
                transform.SetParent(bp.gameObject.transform);
                transform.localPosition = Vector3.zero;
                anim.SetTrigger("Possess");
                bp.anim.SetTrigger("Alive");
                this.enabled = false;
                rigid.velocity = Vector2.zero;
                return;
            }
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
}
[System.Serializable]
public class playerData
{
    public bool active;
    public bool dead;
    public Vector3 position;
}
