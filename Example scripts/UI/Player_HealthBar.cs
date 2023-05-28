using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_HealthBar : MonoBehaviour
{
    UnityEngine.UI.Slider slider;

    // Start is called before the first frame update
    void Start()
    {
        slider = GetComponentInChildren<UnityEngine.UI.Slider>();
    }

    public void ChangeSlider(float ratio)
    {
        slider.value = ratio;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
