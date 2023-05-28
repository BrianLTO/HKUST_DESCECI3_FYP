using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;

public class BeamSpellTemplate : SpellTemplate
{
    [Header("Variables")]
    public OVRSkeleton handSkeletonL;
    public OVRSkeleton handSkeletonR;
    public ActiveStateSelector handPoseL, handPoseR, heldPoseL, heldPoseR;
    public GameObject beamPrefab;

    readonly static public float spellCastTime = 0.75f;
    readonly static public int spellCastPriority = 0;
    readonly static public bool spellNeedRune = true;

    bool readyL, readyR, heldL, heldR;

    public BeamSpellTemplate() : base(spellCastTime, spellCastPriority, spellNeedRune) { }

    public override bool IsCasting()
    {
        return readyL && readyR;
    }

    public bool beamHeld()
    {
        return heldL && heldR;
    }

    public override void CastSpell(Rune rune1, Rune rune2)
    {
        //calculate beam origin
        Vector3 beamOrigin = (handSkeletonL.Bones[9].Transform.position + handSkeletonR.Bones[9].Transform.position) / 2;

        Debug.Log("casted beam");

        GameObject beamInstance = Instantiate(beamPrefab, beamOrigin, Quaternion.identity);
        beamInstance.GetComponent<BeamSpellObject>().initializeSpell(rune1, rune2, handSkeletonL, handSkeletonR, this);
        beamInstance.GetComponent<beamSpellShader>().rune1Type = rune1;
        beamInstance.GetComponent<beamSpellShader>().rune2Type = rune2;
        beamInstance.GetComponent<beamSpellShader>().setColor();

        //Material beamMaterial = beamInstance.GetComponentInChildren<MeshRenderer>().material;
        //beamMaterial.color = RuneInfo.GetCombinedColor(rune1, rune2);
    }

    // Start is called before the first frame update
    void Start()
    {
        handPoseL.WhenSelected += () => { readyL = true; };
        handPoseL.WhenUnselected += () => { readyL = false; };
        handPoseR.WhenSelected += () => { readyR = true; };
        handPoseR.WhenUnselected += () => { readyR = false; };
        heldPoseL.WhenSelected += () => { heldL = true; };
        heldPoseL.WhenUnselected += () => { heldL = false; };
        heldPoseR.WhenSelected += () => { heldR = true; };
        heldPoseR.WhenUnselected += () => { heldR = false; };
    }

    // Update is called once per frame
    void Update()
    {

    }
}
