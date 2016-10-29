using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Attack : MonoBehaviour
{
    public BoxCollider2D box;
    public int playerID;
    public int damage;
    public LayerMask targets;
    public int fliped;
    [HideInInspector]
    public List<int> ignoreID;
    public bool affectStamina;

    void Start()
    {
        ignoreID = new List<int>();
    }

    void Update()
    {
        RaycastHit2D[] rch = Physics2D.BoxCastAll((Vector2)transform.position + box.offset * fliped, box.size, transform.eulerAngles.z,
                                                Vector2.down, .01f, targets);
        foreach (RaycastHit2D r in rch)
        {
            BasicPlayer bp = r.collider.GetComponent<BasicPlayer>();
            if (bp != null)
            {
                if (bp.playerID != playerID && !ignoreID.Contains(r.collider.gameObject.GetInstanceID()))
                {
                    ignoreID.Add(r.collider.gameObject.GetInstanceID());
                    if (!affectStamina) bp.Damage(damage);
                    else bp.Stamina(damage);
                    return;
                }
            }
            if (affectStamina) continue;
            Ghost g = r.collider.GetComponent<Ghost>();
            if (g != null)
            {
                if (g.playerID != playerID && !ignoreID.Contains(r.collider.GetInstanceID()))
                {
                    ignoreID.Add(r.collider.GetInstanceID());
                    g.Die();
                }
            }
        }
    }

    void OnDisable()
    {
        ignoreID = new List<int>();
    }
}
