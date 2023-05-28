using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SpellTemplate : MonoBehaviour
{
    public float heldTime { set; get; }
    public float castTime { private set; get; }
    public int castPriority { private set; get; }
    public bool needRune { private set; get; }
    public bool finishedCasting { get { return heldTime >= castTime; } }

    abstract public bool IsCasting();
    abstract public void CastSpell(Rune rune1, Rune rune2);

    public SpellTemplate (float spellCastTime, int spellCastPriority, bool spellNeedRune)
    {
        castTime = spellCastTime;
        castPriority = spellCastPriority;
        needRune = spellNeedRune;
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
