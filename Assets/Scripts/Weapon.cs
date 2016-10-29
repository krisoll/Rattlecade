using UnityEngine;
using System.Collections;

public class Weapon : MonoBehaviour
{
    public Animator anim;
    public Rigidbody2D rigid;
    public GameObject shooter;
    public GameObject bullet;
    public BoxCollider2D box;
    public Attack attack;
    public int ammo;
    public float spread;
    [HideInInspector]
    public bool equiped;
    public int flipped;
    public WeaponType type;
    public enum WeaponType
    {
        LongRange,
        ShortRange,
        Melee
    }
    public ShootType shootType;
    public enum ShootType
    {
        Automatic,
        SemiAutomatic
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
    public void Shoot()
    {
        if (anim == null) return;
        if (ammo > 0) anim.SetTrigger("Shoot");
    }

    public void fireBullet()
    {
        if (ammo <= 0) return;
        if (bullet != null && shooter != null)
        {
            GameObject go = (GameObject)Instantiate(bullet, shooter.transform.position, Quaternion.Euler(0, 0, shooter.transform.eulerAngles.z));
            Bullet b = go.GetComponent<Bullet>();
            go.transform.eulerAngles = new Vector3(0, 0, go.transform.eulerAngles.z + Random.Range(-spread, spread));
            if(flipped > 0) go.transform.eulerAngles = new Vector3(0, 0, go.transform.eulerAngles.z - 180);
            ammo--;
        }
    }

    public void destroySelf()
    {
        if(type != WeaponType.Melee && ammo <= 0)
        {
            Destroy(gameObject);
        }
    }
}
