using UnityEngine;
using System.Collections;

public class Weapon : MonoBehaviour
{
    public Animator anim;
    public Rigidbody2D rigid;
    public GameObject shooter;
    public BoxCollider2D box;
    public Attack attack;
    public float spread;
    [HideInInspector]
    public bool equiped;
    public WeaponType type;
    public enum WeaponType
    {
        LongRange,
        ShortRange,
        Melee
    }
    public int fireRate;
    private float fireTimeDelay;
    private float timeSaved;

    // Use this for initialization
    void Start() {
        fireTimeDelay = 1f / fireRate;
        deactivate();
    }

    public void activate()
    {
        if (attack != null) attack.enabled = true;
    }
    public void deactivate()
    {
        if (attack != null) attack.enabled = false;
    }

    public bool canShoot()
    {
        if (Mathf.Abs(timeSaved - Time.time) > fireTimeDelay)
        {
            timeSaved = Time.time;
            return true;
        }
        return false;
    }
}
