using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;

public class SpellCastingController : MonoBehaviour
{
    public static SpellCastingController instance;
    public static bool showDebug = false;
    void Awake()
    {
        //singleton script
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    [Header("Fixed variable")]
    public OVRSkeleton skeleton;
    public GameObject handRendererObjectLeft;
    public GameObject handRendererObjectRight;
    public AudioSource audioSource;
    public AudioClip castSound;
    public AudioClip recognizedSound;
    public AudioClip notRecognizedSound;
    public AudioSource audioSourceLeft, audioSourceRight;

    [Header("Spells")]
    public SpellTemplate[] spells;

    [Header("Settings")]
    public float soundVolumeCast = 0.5f;
    public float soundVolumeR = 0.5f;
    public float soundVolumeNR = 1f;
    public int dummy;

    private bool hasRune { get { return runeLeft != Rune.Empty || runeRight != Rune.Empty; } }

    private Rune runeLeft = Rune.Empty;
    private Rune runeRight = Rune.Empty;
    private List<SpellTemplate> castingSpell = new List<SpellTemplate>();

    private SkinnedMeshRenderer handRendererLeft, handRendererRight;
    private Material handRendererMaterialLeft, handRendererMaterialRight;
    private Color defHandColor;

    private OVRCameraRig rig;

    private float castProgress = 0f;

    //for projectile tracking spell
    public List<ProjectileObject> projectileSpell = new List<ProjectileObject>();

    // Start is called before the first frame update
    void Start()
    {
        //get the hand renderer and material
        handRendererLeft = handRendererObjectLeft.GetComponent<SkinnedMeshRenderer>();
        handRendererMaterialLeft = handRendererObjectLeft.GetComponent<SkinnedMeshRenderer>().material;
        handRendererRight = handRendererObjectLeft.GetComponent<SkinnedMeshRenderer>();
        handRendererMaterialRight = handRendererObjectRight.GetComponent<SkinnedMeshRenderer>().material;

        //get default hand color
        defHandColor = handRendererMaterialLeft.GetColor("_ColorTop");

        rig = GetComponentInParent<OVRCameraRig>();
    }

    void Update()
    {
        foreach (SpellTemplate spell in spells)
        {
            if (spell.IsCasting() && !castingSpell.Contains(spell) && (hasRune || !spell.needRune)) 
                castingSpell.Add(spell);
        }

        if (castingSpell.Count > 0)
        {
            //update held time on each casting spell
            var toBeRemoved = new List<SpellTemplate>();
            foreach (SpellTemplate spell in castingSpell)
            {
                if (!spell.IsCasting())
                {
                    spell.heldTime = 0;
                    toBeRemoved.Add(spell);
                }
                else
                {
                    spell.heldTime += Time.deltaTime;
                }
            }

            //remove non-casting spell from list
            foreach (SpellTemplate spell in toBeRemoved)
            {
                castingSpell.Remove(spell);
            }

            //find highest-priority spell that is ready to be cast
            int maxPriority = findHighestPriority();
            SpellTemplate toBeCasted = null;
            foreach (SpellTemplate spell in castingSpell)
            {
                if (spell.finishedCasting && spell.castPriority == maxPriority) toBeCasted = spell;
                if (spell.castPriority == maxPriority) castProgress = spell.heldTime / spell.castTime;
            }
            if (toBeCasted != null)
            {
                var runeL = runeLeft;
                var runeR = runeRight;
                if (toBeCasted.needRune)
                {
                    resetRunes();
                    audioSource.PlayOneShot(castSound, soundVolumeCast);
                }
                resetCastingSpells();
                toBeCasted.CastSpell(runeL, runeR);
            }
        }
        else
        {
            castProgress = 0f;
        }


        /*
        old spell casting code
        */
        //if (hasRune)
        //{
        //    if (castingSpell != null)
        //    {
        //        if (castingSpell.isCasting())
        //        {
        //            heldTime += Time.deltaTime;
        //            if (heldTime > castingSpell.castTime)
        //            {
        //                castingSpell.castSpell(runeLeft, runeRight);
        //                resetRunes();
        //            }
        //        }
        //        else
        //        {
        //            heldTime = 0;
        //            castingSpell = null;
        //        }
        //    }
        //    else
        //    {
        //        foreach (SpellTemplate spell in spells)
        //        {
        //            if (spell.isCasting()) castingSpell = spell;
        //        }
        //    }
        //}
        //else
        //{
        //    heldTime = 0;
        //}
    }

    public void SetRuneLeft (Rune input)
    {
        Debug.Log("Left set to " + RuneInfo.GetString(input));
        if (input == Rune.Empty)
        {
            audioSourceRight.PlayOneShot(notRecognizedSound, soundVolumeNR);
            return;
        } else audioSourceRight.PlayOneShot(recognizedSound, soundVolumeR);

        if (runeLeft == Rune.Empty || runeRight != Rune.Empty)
        {
            runeLeft = input;
            handRendererMaterialLeft.SetColor("_ColorTop", RuneInfo.GetColor(runeLeft));
        }
        else if (runeRight == Rune.Empty)
        {
            runeRight = input;
            handRendererMaterialRight.SetColor("_ColorTop", RuneInfo.GetColor(runeRight));
        }
    }

    public void SetRuneRight(Rune input)
    {
        Debug.Log("Right set to " + RuneInfo.GetString(input));
        if (input == Rune.Empty)
        {
            audioSourceLeft.PlayOneShot(notRecognizedSound, soundVolumeNR);
            return;
        }
        else audioSourceLeft.PlayOneShot(recognizedSound, soundVolumeR);

        if (runeRight == Rune.Empty || runeLeft != Rune.Empty)
        {
            runeRight = input;
            handRendererMaterialRight.SetColor("_ColorTop", RuneInfo.GetColor(runeRight));
        }
        else if (runeLeft == Rune.Empty)
        {
            runeLeft = input;
            handRendererMaterialLeft.SetColor("_ColorTop", RuneInfo.GetColor(runeLeft));
        }
    }

    public void resetRunes()
    {
        Debug.Log("resetting all runes");
        runeLeft = Rune.Empty;
        handRendererMaterialLeft.SetColor("_ColorTop", defHandColor);
        runeRight = Rune.Empty;
        handRendererMaterialRight.SetColor("_ColorTop", defHandColor);
    }

    public float GetCastingProgress()
    {
        return castProgress;
    }

    private void resetCastingSpells()
    {
        foreach (SpellTemplate spell in castingSpell)
        {
            spell.heldTime = 0;
        }
        castingSpell.Clear();
    }

    private int findHighestPriority()
    {
        int maxPriority = 0;
        foreach(SpellTemplate spell in castingSpell)
        {
            if (spell.castPriority > maxPriority) maxPriority = spell.castPriority;
        }
        return maxPriority;
    }
}
