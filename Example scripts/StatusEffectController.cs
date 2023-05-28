using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class StatusEffectController : MonoBehaviour
{
    public static StatusEffectController instance;
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

    private List<EnemyStats> enemyWatchList = new List<EnemyStats>();

    //for adding single status effect
    public void AddStatusEffect(EnemyStats enemy, StatusEffect statusEffect)
    {
        if (showDebug) Debug.Log("STATUS: AddStatusEffect of " + statusEffect.effectName + " to enemy");
        StatusEffect CopiedStatusEffect = CopyStatusEffect(statusEffect);

        //add the effect if it is StatusEffect_Damage or is not already on enemy
        if (CopiedStatusEffect.GetType() == typeof(StatusEffect_Damage) || !enemy.statusList.Any(x => x.effectName == CopiedStatusEffect.effectName))
            enemy.statusList.Add(CopiedStatusEffect);
        else
        {
            //refresh effect otherwise
            RemoveStatusEffect(enemy, enemy.statusList.Find(x => x.effectName == CopiedStatusEffect.effectName));
            enemy.statusList.Add(CopiedStatusEffect);
        }

        //add enemy into watch list if not already 
        if (!enemyWatchList.Contains(enemy))
        {
            enemyWatchList.Add(enemy);
        }

        //updates enemy attributes
        if (CopiedStatusEffect.GetType() == typeof(StatusEffect_Attribute))
            UpdateAttribute(enemy);
    }

    //for adding multiple (list of) status effect
    public void AddStatusEffect(EnemyStats enemy, List<StatusEffect> statusEffectList)
    {
        if (showDebug) Debug.Log("STATUS: AddStatusEffect of MULTIPLE (" + statusEffectList.Count + ") to enemy");

        bool containAttributeStatus = false;
        foreach (StatusEffect statusEffect in statusEffectList)
        {
            if (showDebug) Debug.Log("STATUS: Adding " + statusEffect.effectName);
            StatusEffect CopiedStatusEffect = CopyStatusEffect(statusEffect);

            //add the effect if it is StatusEffect_Damage or is not already on enemy
            if (CopiedStatusEffect.GetType() == typeof(StatusEffect_Damage) || !enemy.statusList.Any(x => x.effectName == CopiedStatusEffect.effectName))
                enemy.statusList.Add(CopiedStatusEffect);
            else
            {
                //refresh effect otherwise
                RemoveStatusEffect(enemy, enemy.statusList.Find(x => x.effectName == CopiedStatusEffect.effectName));
                enemy.statusList.Add(CopiedStatusEffect);
            }

            if (CopiedStatusEffect.GetType() == typeof(StatusEffect_Attribute))
                containAttributeStatus = true;
        }

        //add enemy into watch list if not already 
        if (!enemyWatchList.Contains(enemy))
        {
            enemyWatchList.Add(enemy);
        }

        //updates enemy attributes
        if (containAttributeStatus)
            UpdateAttribute(enemy);
    }

    //for removing status effect with instantiation
    public void RemoveStatusEffect(EnemyStats enemy, StatusEffect statusEffect)
    {
        if (showDebug) Debug.Log("STATUS: RemoveStatusEffect with instantiation " + statusEffect.effectName + " from enemy");

        //removes all status effect from enemy with the same effectName
        enemy.statusList.RemoveAll(x => x.effectName == statusEffect.effectName);
    }

    //for removing status effect with name
    public void RemoveStatusEffect(EnemyStats enemy, string statusEffectName)
    {
        if (showDebug) Debug.Log("STATUS: RemoveStatusEffect with name " + statusEffectName + " from enemy");

        //removes all status effect from enemy with the same effectName
        enemy.statusList.RemoveAll(x => x.effectName == statusEffectName);
    }

    //called when updating attribute
    void UpdateAttribute(EnemyStats enemy)
    {
        CopyAttribute(enemy.enemyStatsData_Current, enemy.enemyStatsData_Default); //perform deep copy

        //calculate new attribute with applied status
        List<StatusEffect_Attribute> listEffectAttribute = enemy.statusList.OfType<StatusEffect_Attribute>().ToList();
        int maxPriority = listEffectAttribute.Max(e => e.priority);
        for (int i = maxPriority; i >= 0; i--)
        {
            foreach (StatusEffect_Attribute SE_Attribute in listEffectAttribute.FindAll(e => e.priority == i))
            {
                enemy.enemyStatsData_Current = SE_Attribute.Modify(enemy.enemyStatsData_Current);
            }
        }
    }

    //helper function: deep copy attribute (def and att only)
    void CopyAttribute(EnemyStatsData target, EnemyStatsData source)
    {
        target.attackPower = source.attackPower.ToDictionary(e => e.Key, e => e.Value);
        target.defensePower = source.defensePower;
    }

    //helper function: deep copy status effect
    StatusEffect CopyStatusEffect(StatusEffect input)
    {
        if (input.GetType() == typeof(StatusEffect_Damage))
        {
            return new StatusEffect_Damage((StatusEffect_Damage)input);
        } 
        else if (input.GetType() == typeof(StatusEffect_Attribute))
        {
            return new StatusEffect_Attribute((StatusEffect_Attribute)input);
        }

        return null;
    }

    void Update()
    {
        foreach (EnemyStats enemy in enemyWatchList)
        {
            //deal damage to enemy for each damaging status effect
            foreach (StatusEffect_Damage SE_Damage in enemy.statusList.OfType<StatusEffect_Damage>())
            {
                CombatController.instance.DealDamageFlat(enemy, SE_Damage.GetDamage(Time.deltaTime));
            }

            //update expire time
            foreach (StatusEffect SE in enemy.statusList)
            {
                SE.expireTime -= Time.deltaTime;

                //remove status effect if it expires
                if (SE.expireTime <= 0)
                {
                    enemy.statusList.Remove(SE);

                    //update enemy attribute if status effect is attribute related
                    if (SE.GetType() == typeof(StatusEffect_Attribute))
                        UpdateAttribute(enemy);
                    

                    //remove enemy from watch list if there is no status effect left
                    if (enemy.statusList.Count == 0)
                        enemyWatchList.Remove(enemy);
                }
            }
        }


        foreach (EnemyStats enemy in enemyWatchList)
        {
            //remove from watch list if no status effect is attached
            if (enemy.statusList.Count <= 0) enemyWatchList.Remove(enemy);
        }
    }

}
