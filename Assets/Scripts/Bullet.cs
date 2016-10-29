using UnityEngine;
using System.Collections;

public class Bullet : MonoBehaviour {
    public Rigidbody2D rigid;
    public Animator anim;
    public float velocity;
    public int damage;

	// Use this for initialization
	void Start () {
        rigid.velocity = Quaternion.Euler(0, 0, transform.eulerAngles.z) * (Vector2.right * velocity);
	}
	
    void OnTriggerEnter(Collider col)
    {
        rigid.velocity = Vector2.zero;
        col.SendMessage("Damage", damage, SendMessageOptions.DontRequireReceiver);
        col.SendMessage("Die", SendMessageOptions.DontRequireReceiver);
        anim.SetTrigger("Hit");
    }

    void DestroySelf()
    {
        Destroy(gameObject);
    }
}
