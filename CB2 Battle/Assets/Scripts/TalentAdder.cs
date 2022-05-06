using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TalentAdder : MonoBehaviour
{
    [SerializeField] private List<TalentInputField> talentInputFields;
    private static TalentInputField[] talentSlots;  
    private static CharacterSaveData owner;
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

    public static void OnValueChanged()
    {
        for(int i = 0; i < talentSlots.Length;i++)
        {
            talentSlots[i].UpdateDisplay();
        }
    }

    public static void DownloadOwner(CharacterSaveData newowner)
    {
        int index = 0;
        owner = newowner;
        Dictionary<TalentKey, Talent> allTalents = TalentReference.getLibraries();
        foreach(KeyValuePair<TalentKey,Talent> kvp in allTalents)
        {
            talentSlots[index].DownloadCharacter(owner, kvp.Key);
            index++;
        }
        OnValueChanged();
    }
}
