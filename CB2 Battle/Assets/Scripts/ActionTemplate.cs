using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Action", menuName = "ScriptableObjects/Actions")]
// Templates for selectable button on the ui actionbar
public class ActionTemplate : ScriptableObject
{
    // Downloaded onto an items tooltips
    [TextArea(5,10)]
    [SerializeField] public string description;  
    // Display image 
    [SerializeField] public Sprite icon; 
    [SerializeField] public string code;
    
}
