using UnityEngine;
using System.Collections;

public class Attack : MonoBehaviour
{
    public BoxCollider2D box;
    [HideInInspector]
    public int playerID;
    public int damage;
    public LayerMask targets;
    public int fliped;

    void Update()
    {
        RaycastHit2D[] rch = Physics2D.BoxCastAll((Vector2)transform.position + box.offset * fliped, box.size, transform.eulerAngles.z,
                                                Vector2.down, .01f, targets);
        foreach (RaycastHit2D r in rch)
        {
            BasicPlayer bp = r.collider.GetComponent<BasicPlayer>();
            if (bp != null)
            {
                if (bp.playerID != playerID)
                {
                    bp.Damage(damage);
                    return;
                }
            }
            Ghost g = r.collider.GetComponent<Ghost>();
            if (g != null)
            {
                if (g.playerID != playerID) g.Die();
            }
        }
    }
}
