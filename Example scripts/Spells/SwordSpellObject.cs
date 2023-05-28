using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwordSpellObject : MonoBehaviour
{
    public GameObject hitParticle;
    public GameObject SummonEffect;
    public GameObject vanishParticle;
    public AudioClip hitSound;
    public float soundVolume = 0.7f;

    readonly float damageModifier = 0.75f;
    readonly float effectStrength = 0.5f;
    readonly float hitCooldown = 0.5f;

    float lifespan = 30f;
    Vector3 lastPosition;
    bool canStationaryDamage;

    DamageInstance dInstance;
    List<StatusEffect> statusList = new List<StatusEffect>();
    List<TriggerEffect> triggerList = new List<TriggerEffect>();
    
    DamageInstance dInstanceCrit;
    List<StatusEffect> statusListCrit = new List<StatusEffect>();
    List<TriggerEffect> triggerListCrit = new List<TriggerEffect>();

    List<EnemyStats> cooldownList = new List<EnemyStats>();

    float cooldown = 0;

    Rune r1, r2;

    public void initializeSpell(Rune rune1, Rune rune2)
    {
        r1 = rune1;
        r2 = rune2;

        //calculate modified damage from player stats
        float modifiedDamage = PlayerStat.instance.attack * damageModifier * RuneInfo.GetDamageValueModifier(rune1, rune2);
        int modifiedDamageInt = Mathf.RoundToInt(modifiedDamage);
        Damage damage = new Damage("player_sword", modifiedDamageInt, rune1, rune2, false);
        statusList.AddRange(RuneInfo.GetStatusEffects(modifiedDamageInt, rune1, rune2, effectStrength));
        triggerList.AddRange(RuneInfo.GetTriggerEffect(modifiedDamageInt, rune1, rune2, effectStrength));
        dInstance = new DamageInstance(damage, statusList, triggerList);

        int modifiedDamageIntCrit = Mathf.RoundToInt(modifiedDamage * PlayerStat.instance.critDamage);
        Damage damageCrit = new Damage("player_swordCrit", modifiedDamageIntCrit, rune1, rune2, false);
        statusListCrit.AddRange(RuneInfo.GetStatusEffects(modifiedDamageIntCrit, rune1, rune2, effectStrength));
        triggerListCrit.AddRange(RuneInfo.GetTriggerEffect(modifiedDamageIntCrit, rune1, rune2, effectStrength));
        dInstanceCrit = new DamageInstance(damageCrit, statusListCrit, triggerListCrit);

        Instantiate(SummonEffect, transform.position, Quaternion.identity);

        lastPosition = transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        EnemyStats enemy = other.gameObject.GetComponentInParent<EnemyStats>();
        if (enemy != null && !cooldownList.Contains(enemy))
        {
            if (Random.Range(Mathf.Epsilon, 1f) <= PlayerStat.instance.critChance)
                CombatController.instance.CollisionEnter(enemy, dInstanceCrit);
            else
                CombatController.instance.CollisionEnter(enemy, dInstance);
            if (cooldownList.Count == 0) cooldown = 0;
            cooldownList.Add(enemy);

            var particle = Instantiate(hitParticle, other.ClosestPoint(transform.position), Quaternion.identity);
            particle.GetComponent<ProjectileParticle>().Initialize(r1, r2);
            AudioSource.PlayClipAtPoint(hitSound, transform.position, soundVolume);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        EnemyStats enemy = other.gameObject.GetComponentInParent<EnemyStats>();
        if (canStationaryDamage && enemy != null && !cooldownList.Contains(enemy))
        {
            if (Random.Range(Mathf.Epsilon, 1f) <= PlayerStat.instance.critChance)
                CombatController.instance.CollisionEnter(enemy, dInstanceCrit);
            else
                CombatController.instance.CollisionEnter(enemy, dInstance);
            cooldownList.Add(enemy);

            var particle = Instantiate(hitParticle, other.ClosestPoint(transform.position), Quaternion.identity);
            particle.GetComponent<ProjectileParticle>().Initialize(r1, r2);
            AudioSource.PlayClipAtPoint(hitSound, transform.position, soundVolume);
        }
    }


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        lifespan -= Time.deltaTime;
        if (cooldown < hitCooldown) cooldown += Time.deltaTime;
        else
        {
            cooldown = 0;
            cooldownList.Clear();
        }
        if (lifespan < 0)
        {
            Instantiate(vanishParticle, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }

        if (lastPosition != transform.position)
        {
            canStationaryDamage = true;
            lastPosition = transform.position;
        }
        else canStationaryDamage = false;
    }
}
