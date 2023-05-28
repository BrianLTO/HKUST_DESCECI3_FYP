using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStat : MonoBehaviour
{
    public static PlayerStat instance;
    public bool showDebug = false;

    [Header("Variables")]
    public Player_HealthBar leftBar;
    public Player_HealthBar rightBar;
    public AudioClip levelUpSound;
    public GameObject levelUpEffect;


    bool useCustomStatL;
    [Header("Settings")]
    public float soundVolume = 0.5f;
    [Header("Cheats")]
    public bool cheatDeath = false;
    [Header("")]
    public bool useCustomStat = false;
    public int custom_maxHP = 500;
    public int custom_HP = 500;
    public int custom_attack = 200;
    public float custom_critChance = 0.2f;
    public float custom_critDamage = 1.5f;
    public int custom_defence = 50;
    public float custom_reduction = 15;
    [Header("")]
    public bool addEXP = false;
    public int addEXPAmount = 1000;
    [Header("")]
    public bool changeHP = false;
    public int changeHPAmount = -200;

    [Tooltip("1 = start, 2 = killed red bull, 3 = took prison quest. 4 = killed warden, 5 = talked with phillip")]
    public int permissionLevel = 1;

    readonly static int BASE_MAXHP = 500;
    readonly static int BASE_ATTACK = 200;
    readonly static float BASE_CRITCHANCE = 0f;
    readonly static float BASE_CRITDAMAGE = 1.5f;
    readonly static int BASE_DEFENCE = 0;
    readonly static int BASE_REDUCTION = 0;

    public int level { private set; get; }
    public int exp { private set; get; }
    public int maxHP { private set; get; }
    public int HP { private set; get; }
    public int attack { private set; get; }
    public float critChance { private set; get; } //value is between 0 and 1
    public float critDamage { private set; get; } //value starts from 1.5
    public int defence { private set; get; }
    public float reduction { private set; get; } //value is 0 or 15

    public float totalReduction { get { return Mathf.Floor((1 - (1 - (defence / (50f + defence))) * (1 - reduction / 100))*100); } }

    void Awake()
    {
        //singleton script
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(gameObject);
        DontDestroyOnLoad(gameObject);

        useCustomStatL = useCustomStat;

        level = 1;
        exp = 0;
        ResetStat();
    }

    void Update()
    {
        if (useCustomStatL != useCustomStat) UpdateStat();
        useCustomStatL = useCustomStat;

        if (addEXP)
        {
            addEXP = false;
            AddExp(addEXPAmount);
        }
        
        if (changeHP)
        {
            changeHP = false;
            ChangeHP(changeHPAmount);
        }
    }

    void ResetStat()
    {
        maxHP = BASE_MAXHP;
        HP = BASE_MAXHP;
        attack = BASE_ATTACK;
        critChance = BASE_CRITCHANCE;
        critDamage = BASE_CRITDAMAGE;
        defence = BASE_DEFENCE;
        reduction = BASE_REDUCTION;
    }

    //called once when player skills are modified
    //this functions retrieves and processes bonus attributes from skills
    // 0 = atk, 1 = def, 2 = hp, 3 = crit chance(%), 4 = crit multi(%), 5 = damage reduction(%)
    public void UpdateStat()
    {
        if (useCustomStat)
        {
            maxHP = custom_maxHP;
            HP = custom_HP;
            attack = custom_attack;
            critChance = custom_critChance;
            critDamage = custom_critDamage;
            defence = custom_defence;
            reduction = custom_reduction;
        }
        else
        {
            ResetStat();

            if (PassiveTreeNew.passiveTree == null)
            {
                return;
            }
            int[] stats = PassiveTreeNew.passiveTree.returnNormalStat();
            int missingHP = maxHP - HP;


            maxHP += stats[2];
            HP = maxHP - missingHP;
            if (HP <= 0) HP = 1;
            attack += stats[0];
            critChance += (float)(stats[3])/100;
            critDamage += (float)(stats[4])/100;
            defence += stats[1];
            reduction += stats[5];
        }

    }

    //called when HP is changed (heal or damage)
    public void ChangeHP(int amount)
    {
        HP += amount;
        if (HP > maxHP) HP = maxHP;
        if (showDebug && amount <= 0) Debug.Log("PLAYER: Takes " + -amount + " Damage");
        if (showDebug && amount > 0) Debug.Log("PLAYER: Heals for " + amount);

        if (HP <= 0)
        {
            //player dies
            if(showDebug) Debug.Log("PLAYER: DIES");
            if (cheatDeath) HP = maxHP;
            else OnDeath();
        }

        leftBar.ChangeSlider((float)HP / maxHP);
        rightBar.ChangeSlider((float)HP / maxHP);
    }

    void OnDeath()
    {
        
        menuButton.menuB.setRoomActive(true);
        menuButton.menuB.setScene(true);
        menuButton.menuB.changeText("You are dead");
        StartCoroutine(goToWaitRoom());
    }

    public IEnumerator goToWaitRoom()
    {
        yield return new WaitForSeconds(0.5f);
        GameState.gameState.player.position = menuButton.menuB.spawnPt.position;
        ResetHP();
    }

    public void ResetHP()
    {
        HP = maxHP;
        leftBar.ChangeSlider((float)HP / maxHP);
        rightBar.ChangeSlider((float)HP / maxHP);
    }

    //called when exp is added
    public void AddExp(int amount)
    {
        bool leveled = false;

        if (amount <= 0) return;
        exp += amount;
        if (showDebug) Debug.Log("PLAYER: Gains " + amount + " experience");

        while (exp >= level * 100 && level < 30)
        {
            //level up
            leveled = true;
            exp -= level * 100;
            level++;

            if (showDebug) Debug.Log("PLAYER: Levels up to Lv" + level);
            if (PassiveTreeNew.passiveTree != null) PassiveTreeNew.passiveTree.levelUp();
        }

        if (leveled)
        {
            Instantiate(levelUpEffect, ContinousMovement.instance.transform);
            SpellCastingController.instance.audioSource.PlayOneShot(levelUpSound, soundVolume);
        }
    }

    //returns critical damage modifier for critical damage (for continuous damage)
    public float GetCritAvgModifier()
    {
        return 1 - critChance + critChance * critDamage;
    }

}
