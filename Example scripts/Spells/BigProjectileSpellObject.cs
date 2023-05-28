using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BigProjectileSpellObject : ProjectileObject
{
    public GameObject hitParticle;
    public GameObject trailParticle;
    public GameObject vanishParticle;
    public AudioClip hitSound;
    public float soundVolume = 0.7f;

    readonly float damageModifier = 2f;
    readonly float effectStrength = 1.5f;
    readonly float maxTrackingSpeed = 10f;
    readonly float maxLifespan = 20f;


    float lifespan = 0;
    EnemyStats trackTarget;
    bool isTracking = false;

    bool canHit = false;

    DamageInstance dInstance;
    List<StatusEffect> statusList = new List<StatusEffect>();
    List<TriggerEffect> triggerList = new List<TriggerEffect>();

    DamageInstance dInstanceCrit;
    List<StatusEffect> statusListCrit = new List<StatusEffect>();
    List<TriggerEffect> triggerListCrit = new List<TriggerEffect>();

    Rune r1, r2;

    public void initializeSpell(Rune rune1, Rune rune2)
    {
        r1 = rune1;
        r2 = rune2;

        //calculate modified damage from player stats
        float modifiedDamage = PlayerStat.instance.attack * damageModifier * RuneInfo.GetDamageValueModifier(rune1, rune2);
        int modifiedDamageInt = Mathf.RoundToInt(modifiedDamage);
        Damage damage = new Damage("player_bigProjectile", modifiedDamageInt, rune1, rune2, false);
        statusList.AddRange(RuneInfo.GetStatusEffects(modifiedDamageInt, rune1, rune2, effectStrength));
        triggerList.AddRange(RuneInfo.GetTriggerEffect(modifiedDamageInt, rune1, rune2, effectStrength));
        dInstance = new DamageInstance(damage, statusList, triggerList);

        int modifiedDamageIntCrit = Mathf.RoundToInt(modifiedDamage * PlayerStat.instance.critDamage);
        Damage damageCrit = new Damage("player_bigProjectileCrit", modifiedDamageIntCrit, rune1, rune2, false);
        statusListCrit.AddRange(RuneInfo.GetStatusEffects(modifiedDamageIntCrit, rune1, rune2, effectStrength));
        triggerListCrit.AddRange(RuneInfo.GetTriggerEffect(modifiedDamageIntCrit, rune1, rune2, effectStrength));
        dInstanceCrit = new DamageInstance(damageCrit, statusListCrit, triggerListCrit);

        trailParticle.GetComponent<ProjectileTrail>().Initialize(rune1, rune2);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (canHit)
        {
            EnemyStats enemy = other.gameObject.GetComponentInParent<EnemyStats>();
            if (enemy != null)
            {
                if (Random.Range(Mathf.Epsilon, 1f) <= PlayerStat.instance.critChance)
                    CombatController.instance.CollisionEnter(enemy, dInstanceCrit);
                else
                    CombatController.instance.CollisionEnter(enemy, dInstance);

                var particle = Instantiate(hitParticle, other.ClosestPoint(transform.position), Quaternion.identity);
                particle.GetComponent<BigProjectileParticle>().Initialize(r1, r2);
                AudioSource.PlayClipAtPoint(hitSound, transform.position, soundVolume);

                gameObject.SetActive(false);
                Destroy(gameObject, 1);
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (canHit)
        {
            EnemyStats enemy = other.gameObject.GetComponentInParent<EnemyStats>();
            if (enemy != null)
            {
                if (Random.Range(Mathf.Epsilon, 1f) <= PlayerStat.instance.critChance)
                    CombatController.instance.CollisionEnter(enemy, dInstanceCrit);
                else
                    CombatController.instance.CollisionEnter(enemy, dInstance);

                var particle = Instantiate(hitParticle, other.ClosestPoint(transform.position), Quaternion.identity);
                particle.GetComponent<BigProjectileParticle>().Initialize(r1, r2);
                AudioSource.PlayClipAtPoint(hitSound, transform.position, soundVolume);

                gameObject.SetActive(false);
                Destroy(gameObject, 1);
            }
        }
    }

    public override void TrackLocation(EnemyStats target)
    {
        lifespan += 1f;
        if (target == null) return;
        if (!isTracking)
        {
            //adds velocity to push projectile away from player initially, force is depending on distance between proj and player
            float distanceScaler = 1.5f - Mathf.Clamp(Vector3.Distance(transform.position, ContinousMovement.rig.centerEyeAnchor.position), 0f, 1.5f);
            GetComponent<Rigidbody>().AddForce((transform.position - ContinousMovement.rig.centerEyeAnchor.position).normalized * distanceScaler, ForceMode.VelocityChange);
        }
        trackTarget = target;
        isTracking = true;
        GetComponent<Rigidbody>().drag = 0.5f;
    }

    private void OnDestroy()
    {
        SpellCastingController.instance.projectileSpell.Remove(this);
    }


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (canHit) lifespan += Time.deltaTime;
        else lifespan += Time.deltaTime / 2;
        if (lifespan > maxLifespan)
        {
            Instantiate(vanishParticle, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }

        if (lifespan > 0.25f) //is actually 0.5s
        {
            canHit = true;
        }
    }

    void FixedUpdate()
    {
        if (isTracking && trackTarget != null)
        {
            Vector3 pointToTarget = (trackTarget.gameObject.transform.position - transform.position + new Vector3(0f, 1f, 0f)).normalized * 2f;
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb.velocity.magnitude < maxTrackingSpeed) rb.AddForce(pointToTarget);
        }
    }
}
