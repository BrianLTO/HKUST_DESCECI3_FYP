using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;

public class HealSpellTemplate : SpellTemplate
{
    [Header("Variables")]
    public OVRSkeleton handSkeletonL;
    public OVRSkeleton handSkeletonR;
    public ActiveStateSelector handPoseL, handPoseR;
    public AudioClip healSound;
    public GameObject healEffect;

    [Header("Settings")]
    public float minDistance = 0.05f;


    readonly static public float spellCastTime = 3;
    readonly static public int spellCastPriority = 0;
    readonly static public bool spellNeedRune = false;

    bool readyL, readyR;

    public HealSpellTemplate() : base(spellCastTime, spellCastPriority, spellNeedRune) { }

    public override bool IsCasting()
    {
        Vector3 distance = handSkeletonL.Bones[9].Transform.position - handSkeletonR.Bones[9].Transform.position;

        return readyL && readyR && distance.magnitude < minDistance;
    }

    public override void CastSpell(Rune rune1, Rune rune2)
    {
        PlayerStat.instance.ChangeHP(PlayerStat.instance.maxHP);
        var effectInstance = Instantiate(healEffect, ContinousMovement.instance.transform);
        StartCoroutine(EffectFade(effectInstance));
        
        SpellCastingController.instance.audioSource.PlayOneShot(healSound);

        tutorialCentre.tc.spell_casted = true;
    }

    // Start is called before the first frame update
    void Start()
    {
        handPoseL.WhenSelected += () => { readyL = true; };
        handPoseL.WhenUnselected += () => { readyL = false; };
        handPoseR.WhenSelected += () => { readyR = true; };
        handPoseR.WhenUnselected += () => { readyR = false; };
    }

    // Update is called once per frame
    void Update()
    {

    }

    IEnumerator EffectFade(GameObject effect)
    {
        yield return new WaitForSeconds(2);
        var pSystems = effect.GetComponentsInChildren<ParticleSystem>();
        foreach (var p in pSystems)
        {
            p.Stop();
        }
        Destroy(effect, 5);
    }
}
