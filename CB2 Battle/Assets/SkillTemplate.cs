using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Skills", menuName = "ScriptableObjects/Skills")]
public class SkillTemplate : ScriptableObject
{
    //indicates where skill should be displayed/affects other properties
    [SerializeField] public bool basic;
    //indicates which characterisitc is referenced when this skill is attempted
    [SerializeField] public string characterisitc;
    //default true, false if you want this skill to be hidden for not true skill effects like parry
    [SerializeField] public bool visible;
    //Archetypes of skill, such as crafting
    [SerializeField] public List<string> Descriptor;
    //Text displayed When the Skill is Rolled
    [SerializeField] public string displayText;

}
