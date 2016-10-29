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
        if(rePlayer.GetButtonDown("Shoot"))
        {
            RaycastHit2D[] rch = Physics2D.BoxCastAll((Vector2)transform.position + box.offset, box.size, 0, Vector3.down, boxCastDistance,
                                               skelletonLayer);
            foreach(RaycastHit2D r in rch)
            {
                BasicPlayer bp = r.collider.GetComponent<BasicPlayer>();
                if (bp == null || bp.gost != null) continue;
                bp.gost = this;
                transform.SetParent(bp.gameObject.transform);
                transform.localPosition = Vector3.zero;
                anim.SetTrigger("Possess");
                return;
            }
        }
    }

    public void savePosition()
    {
        GameManager.gManager.players[playerID].position = transform.position;
    }
}
[System.Serializable]
public class playerData
{
    public bool active;
    public bool dead;
    public Vector3 position;
}
