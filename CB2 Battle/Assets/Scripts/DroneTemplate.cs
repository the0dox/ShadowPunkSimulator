using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "Drone", menuName = "ScriptableObjects/Drones")]
public class DroneTemplate : ItemTemplate
{
    [SerializeField] public int Handling;
    [SerializeField] public int Speed;
    [SerializeField] public int Structure;
    [SerializeField] public int Armor;
    [SerializeField] public int Sensor;
    [SerializeField] public int Piloting;
    [SerializeField] public string model = "Mastiff";

    [SerializeField] public WeaponTemplate Weapon;

}
