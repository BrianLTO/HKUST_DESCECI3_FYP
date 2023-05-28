using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;

public class ProjectileTrackSpellTemplate : SpellTemplate
{
    [Header("Variables")]
    public OVRSkeleton handSkeletonL;
    public OVRSkeleton handSkeletonR;
    public ActiveStateSelector handPoseL, handPoseR;
    public GameObject debugSphere;

    [Header("Settings")]
    public float raycastRadius = 1f;

    readonly static public float spellCastTime = 1;
    readonly static public int spellCastPriority = 0;
    readonly static public bool spellNeedRune = false;

    bool readyL, readyR;

    public ProjectileTrackSpellTemplate() : base(spellCastTime, spellCastPriority, spellNeedRune) { }

    public override bool IsCasting()
    {
        return readyL || readyR;
    }

    public override void CastSpell(Rune rune1, Rune rune2)
    {
        //calculate target location
        Transform eyeAnchor = ContinousMovement.rig.centerEyeAnchor;
        RaycastHit target;
        if (Physics.SphereCast(eyeAnchor.position, raycastRadius, eyeAnchor.forward, out target, 30f, LayerMask.GetMask("Enemy")))
        {
            //Instantiate(debugSphere, target.point, Quaternion.identity);
            SpellCastingController.instance.projectileSpell.ForEach(e => e.TrackLocation(target.collider.GetComponentInParent<EnemyStats>()));
        }
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
}
