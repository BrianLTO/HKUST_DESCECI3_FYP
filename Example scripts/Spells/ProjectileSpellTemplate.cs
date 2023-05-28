using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;

public class ProjectileSpellTemplate : SpellTemplate
{
    [Header("Variables")]
    public OVRSkeleton handSkeleton;
    public ActiveStateSelector handPose;
    public GameObject projectilePrefab;
    public bool isLeftHand = false;

    readonly static public float spellCastTime = 0.5f;
    readonly static public int spellCastPriority = 0;
    readonly static public bool spellNeedRune = true;

    bool canCast;

    public ProjectileSpellTemplate() : base(spellCastTime, spellCastPriority, spellNeedRune) { }

    public override bool IsCasting()
    {
        return canCast;
    }

    public override void CastSpell(Rune rune1, Rune rune2)
    {
        //calculate orb spawn location
        Transform middleFingerTransform = handSkeleton.Bones[9].Transform;
        Vector3 spawnLocaton;

        if (isLeftHand) spawnLocaton = middleFingerTransform.position + middleFingerTransform.TransformDirection(Vector3.up) * 0.1f;
        else spawnLocaton = middleFingerTransform.position - middleFingerTransform.TransformDirection(Vector3.up) * 0.1f;

        GameObject projectileInstance = Instantiate(projectilePrefab, spawnLocaton, Quaternion.identity);
        projectileInstance.GetComponent<ProjectileSpellObject>().initializeSpell(rune1, rune2);
        SpellCastingController.instance.projectileSpell.Add(projectileInstance.GetComponent<ProjectileSpellObject>());

        projectileInstance.GetComponent<sphereSpellShader>().rune1Type = rune1;
        projectileInstance.GetComponent<sphereSpellShader>().rune2Type = rune2;
        projectileInstance.GetComponent<sphereSpellShader>().setColor();
    }

    // Start is called before the first frame update
    void Start()
    {
        handPose.WhenSelected += () => { canCast = true; };
        handPose.WhenUnselected += () => { canCast = false; };
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
