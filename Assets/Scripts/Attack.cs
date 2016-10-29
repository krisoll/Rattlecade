using UnityEngine;
using System.Collections;

public class Attack : MonoBehaviour
{
    public BoxCollider2D box;
    [HideInInspector]
    public int playerID;
    public int damage;

    public void doAttack()
    {
        RaycastHit2D[] rch = Physics2D.BoxCastAll((Vector2)transform.position + box.offset, box.size, transform.eulerAngles.z,
                              Vector2.down, .01f, LayerMask.GetMask("Player", "Ignore Raycast"));
        foreach(RaycastHit2D r in rch)
        {
            BasicPlayer bp = r.collider.GetComponent<BasicPlayer>();
            if(bp!= null)
            {
                if (bp.playerID != playerID) bp.Damage(damage);
                continue;
            }
            Ghost g = r.collider.GetComponent<Ghost>();
            if (g != null)
            {
                if (bp.playerID != playerID) g.Die();
            }
        }
    }
}
