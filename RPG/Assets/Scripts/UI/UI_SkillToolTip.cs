using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UI_SkillToolTip : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI skillDescprtion;
    [SerializeField] private TextMeshProUGUI skillName;
    
    public void ShowToolTip(string _skillDescprtion, string _skillName)
    {
        skillName.text = _skillName;
        skillDescprtion.text = _skillDescprtion;
        gameObject.SetActive(true);
    }
    public void HideToolTip() => gameObject.SetActive(false);
}
