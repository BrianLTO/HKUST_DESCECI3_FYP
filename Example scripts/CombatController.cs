using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatController : MonoBehaviour
{
    public static CombatController instance;
    public bool showDebug = false;

    void Awake()
    {
        //singleton script
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    //called when hit boxes collide to deal damage and apply status (once per collision, player to enemy)
    public void CollisionEnter(EnemyStats enemy, DamageInstance damageInstance)
    {
        //check if the damage instance is continuous
        if(showDebug) Debug.Log("COMBAT: CollisionEnter " + damageInstance.damage.damageName);
        if (damageInstance.damage.isContinuous)
        {
            DealDamageOverTime(enemy, damageInstance);
        }
        else
        {
            DealDamageFlat(enemy, damageInstance);
        }
    }

    //called when hitboxes collide to apply damage to player (once per collision, enemy to player)
    public void CollisionEnter(DamageInstance damageInstance)
    {
        float actualDamage = damageInstance.damage.damageValue * (1 - GetDamageReductionPercentage(PlayerStat.instance.defence)) * (1 - PlayerStat.instance.reduction/100);
        int actualDamageInt = Mathf.CeilToInt(actualDamage);

        PlayerStat.instance.ChangeHP(-actualDamageInt);
        if (showDebug) Debug.Log("COMBAT: " + actualDamageInt + "\tis dealt to player.");
    }

    //called when hitboxes are intersected (used by continuous damage to apply status/trigger, enemy to player)
    public void CollisionStay(EnemyStats enemy, DamageInstance damageInstance)
    {
        if (showDebug) Debug.Log("COMBAT: CollisionStay " + damageInstance.damage.damageName);
        if (damageInstance.damage.isContinuous)
        {
            //apply status effects
            StatusEffectController.instance.AddStatusEffect(enemy, damageInstance.statusEffects);

            //trigger effects
            foreach (TriggerEffect tEffect in damageInstance.triggerEffects)
            {
                tEffect.trigger();
            }
        }
    }

    //called when hit boxes exit each other (once per exit, player to enemy)
    public void CollisionExit(EnemyStats enemy, DamageInstance damageInstance)
    {
        if (showDebug) Debug.Log("COMBAT: CollisionExit " + damageInstance.damage.damageName);
        if (damageInstance.damage.isContinuous)
        {
            StopDamageOverTime(enemy, damageInstance);
        }
    }

    //called for non-continuous damage calculation
    void DealDamageFlat(EnemyStats enemy, DamageInstance damageInstance)
    {
        //apply status effects
        StatusEffectController.instance.AddStatusEffect(enemy, damageInstance.statusEffects);

        //trigger effects
        foreach (TriggerEffect tEffect in damageInstance.triggerEffects)
        {
            tEffect.trigger();
        }

        //get damage modifers for element and defense
        float damageModifier = GetDamageModifier(enemy.enemyStatsData_Current, damageInstance.damage.rune1, damageInstance.damage.rune2);

        //apply calculated damage to enemy
        ApplyActualDamage(enemy, damageInstance.damage.damageValue * damageModifier);
        if (showDebug) Debug.Log("COMBAT: DealDamageFlat " + damageInstance.damage.damageValue * damageModifier);

    }

    //this function is only for calculating damage from damaging status
    public void DealDamageFlat(EnemyStats enemy, Damage damage)
    {
        //get damage modifers for element and defense
        float damageModifier = GetDamageModifier(enemy.enemyStatsData_Current, damage.rune1, damage.rune2);

        //apply calculated damage to enemy
        ApplyActualDamage(enemy, damage.damageValue * damageModifier);
    }

    //called when continuous damage is applied
    void DealDamageOverTime(EnemyStats enemy, DamageInstance damageInstance)
    {
        Damage damage = damageInstance.damage;

        //apply status effects
        StatusEffectController.instance.AddStatusEffect(enemy, damageInstance.statusEffects);

        //create continuous damage status
        StatusEffect_Damage continuousDamage = new StatusEffect_Damage(damage.damageName, 10f, damage.damageValue, damage.rune1, damage.rune2);

        //apply continuous damage status
        StatusEffectController.instance.AddStatusEffect(enemy, continuousDamage);
    }

    //called when continuous damage is stopped
    void StopDamageOverTime(EnemyStats enemy, DamageInstance damageInstance)
    {
        string continuousDamageName = damageInstance.damage.damageName;

        //apply status effects
        StatusEffectController.instance.AddStatusEffect(enemy, damageInstance.statusEffects);

        //remove continuous damage status
        StatusEffectController.instance.RemoveStatusEffect(enemy, continuousDamageName);
    }

    //helper function: actually damage the enemy
    void ApplyActualDamage(EnemyStats enemy, float damage)
    {
        if (showDebug) Debug.Log("COMBAT: ApplyActualDamage " + damage);
        enemy.damagePool += damage;

        if (enemy.damagePool >= 1)
        {
            int damageAmount = Mathf.FloorToInt(enemy.damagePool);
            enemy.damagePool -= damageAmount;
            enemy.changeHP(-damageAmount);
        }
    }

    //helper function: get total damage modifers for element and defense
    float GetDamageModifier(EnemyStatsData enemyStat, Rune e1, Rune e2)
    {
        float elementModifier = 1, defenseModifier = 1;

        elementModifier += RuneInfo.GetElementModifier(enemyStat.element, e1);
        elementModifier += RuneInfo.GetElementModifier(enemyStat.element, e2);

        defenseModifier -= GetDamageReductionPercentage(enemyStat.defensePower);

        return elementModifier * defenseModifier;
    }

    //helper function: get damage reduction percentage
    float GetDamageReductionPercentage (int defense)
    {
        if (defense >= 0) return defense / (50f + defense);
        else return defense / 50f;
    }


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

public class Enemy_Dummy
{
    public List<StatusEffect> statusList = new List<StatusEffect>();
    public float damagePool;
    public StatusInfo statusInfo;
    readonly public StatusInfo statusInfo_default;
    string elementType;
}
