using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;

public class SwordSpellTemplate : SpellTemplate
{
    [Header("Variables")]
    public OVRSkeleton handSkeletonL;
    public OVRSkeleton handSkeletonR;
    public ActiveStateSelector handPoseL, handPoseR;
    public GameObject swordPrefab;

    readonly static public float spellCastTime = 1;
    readonly static public int spellCastPriority = 0;
    readonly static public bool spellNeedRune = true;

    bool readyL, readyR;

    public SwordSpellTemplate() : base(spellCastTime, spellCastPriority, spellNeedRune) { }

    public override bool IsCasting()
    {
        Vector3 distance = handSkeletonL.Bones[9].Transform.position - handSkeletonR.Bones[9].Transform.position;
        distance.y = 0;

        return readyL && readyR && distance.magnitude < 0.1f;
    }

    public override void CastSpell(Rune rune1, Rune rune2)
    {
        //calculate sword spawn position
        Vector3 spawnPosition = (handSkeletonL.Bones[9].Transform.position + handSkeletonR.Bones[9].Transform.position) / 2;

        GameObject swordInstance = Instantiate(swordPrefab, spawnPosition, Quaternion.identity);
        swordInstance.GetComponent<SwordSpellObject>().initializeSpell(rune1, rune2);

        swordInstance.GetComponent<swordSpellShader>().rune1Type = rune1;
        swordInstance.GetComponent<swordSpellShader>().rune2Type = rune2;
        swordInstance.GetComponent<swordSpellShader>().setColor();
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
}
