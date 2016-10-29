using UnityEngine;
using System.Collections;

public class Attack : MonoBehaviour
{
    public BoxCollider2D box;
    [HideInInspector]
    public int playerID;
    public int damage;

    public void OnTriggerEnter(Collider col)
    {
        BasicPlayer bp = col.GetComponent<BasicPlayer>();
        if (bp != null)
        {
            if (bp.playerID != playerID) bp.Damage(damage);
            return;
        }
        Ghost g = col.GetComponent<Ghost>();
        if (g != null)
        {
            if (bp.playerID != playerID) g.Die();
        }
    }
}
