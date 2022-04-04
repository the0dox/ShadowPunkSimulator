using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "Weapons", menuName = "ScriptableObjects/Weapons")]
public class WeaponTemplate : ItemTemplate
{
    [SerializeField] public List<string> Attributes = new List<string>();
    //Flat damage bonus for ranged weapons, multiplier for SB for melee weapons
    [SerializeField] public int damageBonus;
    [SerializeField] public RangeWeaponClass rangeClass;
    //Negates AP of target
    [SerializeField] public int pen;
    // If a weapon can fire in single shot mode
    [SerializeField] public bool SingleShot;
    // If a weapon can fire in semi auto mode
    [SerializeField] public bool SemiAuto;
    // If a weapon can fire in burst fire mode
    [SerializeField] public bool BurstFire;
    // If a weapon can fire in full auto mode
    [SerializeField] public bool FullAuto;
    //Skill used to fire the weapon
    [SerializeField] public SkillTemplate WeaponSkill;
    //subtype of weapon that can be specialized in
    [SerializeField] public int WeaponSpecialization;
    //limit for hits with this weapon
    [SerializeField] public int accuracy;
    //clip capacity
    [SerializeField] public int clipMax;
    //damage type for determining critical table
    [SerializeField] public string damageType;
    [SerializeField] public int blast;
    [SerializeField] public ItemTemplate AmmoSource;
    [SerializeField] public reloadingMethod clipType; 
    [SerializeField] public WeaponClass weaponClass;
    [SerializeField] public bool Lethal;
    [SerializeField] public bool debugAddAllUpgrades = false;
    [SerializeField] public BlastClass blastClass = BlastClass.none;

}

public enum BlastClass
{
    // used for most weapons
    none,
    // grenades and rockets
    sphere,
    // fire cones and overwatch 
    cone
}

public enum reloadingMethod
{
    // uses detachable clip easiest to reload
    clip,
    // complex action to reload 2 shots
    breakaction,
    // uses detachable clip complex to attach/detach
    beltfed,
    // complex to reload agility score bullets
    internalmagazine,
    // complex to single muzzle loaded round
    muzzleloader,
    // complex to reload agility score bullets
    cylinder,
    // uses detachable clip complex to attach/detach
    drum,
    // simple action to knock an arrow 
    bow
}

public enum WeaponClass
{
    melee,
    ranged,
    thrown,
    shield
}