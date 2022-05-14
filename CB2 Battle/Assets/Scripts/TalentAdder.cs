using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TalentAdder : MonoBehaviour
{
    [SerializeField] private List<TalentInputField> talentInputFields;
    private static TalentInputField[] talentSlots;  
    void Awake()
    {
        int index = 0;
        talentSlots = new TalentInputField[20];
        foreach(TalentInputField iif in talentInputFields)
        {
            talentSlots[index] = iif;
            index++;
        }
    }

    public void OnValueChanged()
    {
        for(int i = 0; i < talentSlots.Length;i++)
        {
            talentSlots[i].UpdateDisplay();
        }
    }

    public void DownloadOwner(CharacterSaveData newowner)
    {
        int index = 0;
        Dictionary<TalentKey, Talent> allTalents = TalentReference.getLibraries();
        foreach(KeyValuePair<TalentKey,Talent> kvp in allTalents)
        {
            talentSlots[index].DownloadCharacter(newowner, kvp.Key);
            index++;
        }
        OnValueChanged();
    }
}
