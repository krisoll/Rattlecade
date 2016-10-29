using UnityEngine;
using System.Collections;

public class Bullet : MonoBehaviour {
    public Rigidbody2D rigid;
    public Animator anim;
    public BoxCollider2D box;
    public LayerMask mask;
    public float velocity;
    public float lifeTime;
    private float timeCount;
    public int damage;

	// Use this for initialization
	void Start () {
        rigid.velocity = Quaternion.Euler(0, 0, transform.eulerAngles.z) * (Vector2.right * velocity);
	}
	
    void Update()
    {
        RaycastHit2D r = Physics2D.BoxCast((Vector2)transform.position + box.offset, box.size, transform.eulerAngles.z,
                                            Vector2.down, .01f, mask);
        if (r)
        {
            rigid.velocity = Vector2.zero;
            rigid.Sleep();
            r.collider.SendMessage("Damage", damage, SendMessageOptions.DontRequireReceiver);
            r.collider.SendMessage("Die", SendMessageOptions.DontRequireReceiver);
            //anim.SetTrigger("Hit");
            Destroy(gameObject);
        }
        else if(lifeTime > 0)
        {
            timeCount += Time.deltaTime;
            if (timeCount > lifeTime) Destroy(gameObject);
        }
    }

    void DestroySelf()
    {
        Destroy(gameObject);
    }

    void OnBecameInvisible()
    {
        Destroy(gameObject);
    }
}
