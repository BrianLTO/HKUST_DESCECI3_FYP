using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_CastBar : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float progress = SpellCastingController.instance.GetCastingProgress();
        if (progress < Mathf.Epsilon) transform.localScale = new Vector3(0f, 0f, 0f);
        else transform.localScale = new Vector3(1f, progress, 1f);
    }
}
