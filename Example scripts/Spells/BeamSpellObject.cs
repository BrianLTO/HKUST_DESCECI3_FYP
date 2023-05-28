using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;

public class BeamSpellObject : MonoBehaviour
{
    public GameObject hitParticle;

    readonly float damageModifier = .5f;
    readonly float effectStrength = .3f;
    readonly float effectApplyCooldown = 1f;
    readonly float handAngleThreshold = 90f;
    readonly float handDistanceThreshold = 0.4f;


    float lifespan = 10f;

    DamageInstance dInstance;
    List<StatusEffect> statusList = new List<StatusEffect>();
    List<TriggerEffect> triggerList = new List<TriggerEffect>();

    List<EnemyStats> cooldownList = new List<EnemyStats>();

    OVRSkeleton handSkeletonL;
    OVRSkeleton handSkeletonR;
    BeamSpellTemplate parent;

    Rune r1, r2;

    float cooldown;

    public void initializeSpell(Rune rune1, Rune rune2, OVRSkeleton handL, OVRSkeleton handR, BeamSpellTemplate template)
    {

        //set variables
        handSkeletonL = handL;
        handSkeletonR = handR;
        parent = template;
        r1 = rune1;
        r2 = rune2;

        //calculate modified damage from player stats
        float modifiedDamage = PlayerStat.instance.attack * damageModifier * RuneInfo.GetDamageValueModifier(rune1, rune2);
        int modifiedDamageInt = Mathf.RoundToInt(modifiedDamage);

        //Debug.Log("damage is " + modifiedDamageInt);

        Damage damage = new Damage("player_beam", modifiedDamageInt, rune1, rune2, true);
        statusList.AddRange(RuneInfo.GetStatusEffects(modifiedDamageInt, rune1, rune2, effectStrength));
        triggerList.AddRange(RuneInfo.GetTriggerEffect(modifiedDamageInt, rune1, rune2, effectStrength));
        dInstance = new DamageInstance(damage, statusList, triggerList);

        ContinousMovement.instance.canMove = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        EnemyStats enemy = other.gameObject.GetComponentInParent<EnemyStats>();
        if (enemy != null) CombatController.instance.CollisionEnter(enemy, dInstance);
    }

    private void OnTriggerStay(Collider other)
    {
        EnemyStats enemy = other.gameObject.GetComponentInParent<EnemyStats>();
        if (enemy != null && !cooldownList.Contains(enemy))
        {
            CombatController.instance.CollisionStay(enemy, dInstance);
            cooldownList.Add(enemy);

            var particle = Instantiate(hitParticle, other.ClosestPoint(transform.position), Quaternion.identity);
            particle.GetComponent<ProjectileParticle>().Initialize(r1, r2);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        EnemyStats enemy = other.gameObject.GetComponentInParent<EnemyStats>();
        if (enemy != null) CombatController.instance.CollisionExit(enemy, dInstance);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        lifespan -= Time.deltaTime;

        //calculate beam position and orientation
        transform.position = Vector3.Lerp(handSkeletonL.Bones[9].Transform.position, handSkeletonR.Bones[9].Transform.position, 0.5f);
        Quaternion handRightFlip = handSkeletonR.Bones[9].Transform.rotation;
        handRightFlip = Quaternion.Euler(handRightFlip.eulerAngles.x, handRightFlip.eulerAngles.y, handRightFlip.eulerAngles.z+180);
        transform.rotation = Quaternion.Lerp(handSkeletonL.Bones[9].Transform.rotation, handRightFlip, 0.5f);

        if (Quaternion.Angle(handSkeletonL.Bones[9].Transform.rotation, handRightFlip) > handAngleThreshold || Vector3.Distance(handSkeletonL.Bones[9].Transform.position, handSkeletonR.Bones[9].Transform.position) > handDistanceThreshold
            || !parent.beamHeld() || lifespan < 0)
        {
            ContinousMovement.instance.canMove = true;
            GetComponentInChildren<CapsuleCollider>().height = 0;
            GetComponentInChildren<CapsuleCollider>().radius = 0;
            Destroy(gameObject, 0.1f);
        }

        if (cooldown < effectApplyCooldown) cooldown += Time.deltaTime;
        else
        {
            cooldown = 0;
            cooldownList.Clear();
        }
    }
}
