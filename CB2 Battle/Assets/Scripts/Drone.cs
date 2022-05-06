using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Drone : Item
{
    new public DroneTemplate Template;

    public bool deployed = false;
    public PlayerStats droneMinion;

    public Drone(DroneTemplate template)
    {
        this.Template = template;
        this.name = template.name;
        this.weight = template.weight;
        this.cost = template.cost;
        this.unique = template.unique;
        this.stacks = 1;
        this.availablity = template.availablity;
        this.description = template.description;

        upgrades = new Dictionary<ItemTemplate, bool>();
        foreach(ItemTemplate ug in template.upgrades)
        {
            this.upgrades.Add(ug, false);
        }
        UpdateTooltip();
    }

    public void DeployDrone(PlayerStats owner, Vector3 location)
    {
        deployed = true;
        droneMinion = PlayerSpawner.CreatePlayer(owner, this, location).GetComponent<PlayerStats>();
    }

    public override Sprite GetSprite()
    {
        if(Template.icon != null)
        {
            return Template.icon;
        }
        return Resources.Load<Sprite>("Assets/Resources/Materials/Icons/Items/Drone.png");
    }

    public override void UpdateTooltip()
    {
        tooltip = "Rating " + rating + " drone";
        tooltip += "\nPiloting: " + Template.Piloting;
        tooltip += "\nHandling: " + Template.Handling;
        tooltip += "\nSpeed: " + Template.Speed;
        tooltip += "\nStructure: " + Template.Structure;
        tooltip += "\nArmor: " + Template.Armor;
        tooltip += "\nSensor: " + Template.Sensor;
        tooltip += "\nWeapon: " + Template.Weapon.name;
        tooltip += "\n\nupgrades:";
        string upgradedesc = " ";
        foreach(ItemTemplate ug in upgrades.Keys)
        {
            if(upgrades[ug])
            {
                upgradedesc += ug.name + ",";
            } 
            upgradedesc = upgradedesc.TrimEnd(upgradedesc[upgradedesc.Length - 2]);
        }
        tooltip += upgradedesc;
        tooltip += "\nweight: " + weight;
        tooltip += "\nvalue: " + cost;
        tooltip += "\n\n\"" + description + "\"";
    }
}
