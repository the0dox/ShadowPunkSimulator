using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillScript : MonoBehaviour
{
    public Skill MySkill;

    public Toggle LevelOne;
    public Toggle LevelTwo;
    public Toggle LevelThree;
    //public Toggle LevelFour; 
    public GameObject ButtonText;
    public GameObject TurnOrder;

    public void UpdateValue(Skill input)
    {
        MySkill = input;
        ButtonText.GetComponent<Text>().text = MySkill.name + " (" + MySkill.characterisitc + ")";
        LevelOne.isOn = MySkill.levels > 0;
        LevelTwo.isOn = MySkill.levels > 1;
        LevelThree.isOn = MySkill.levels > 2;
    }

    public Skill GetSkill()
    {
        //updates level values
        MySkill.levels = (LevelOne.isOn ? 1 : 0) + (LevelTwo.isOn ? 1 : 0) + (LevelThree.isOn ? 1 : 0);
        return MySkill;
    }

    public void SkillCheck()
    {
        Debug.Log(MySkill.name);
        GameObject.FindGameObjectWithTag("GameController").GetComponent<TurnManager>().AbilityCheck(MySkill.name);
    }
}
