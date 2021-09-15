using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item
{
    protected private string name;
    protected private float weight;
    protected private int cost;
    protected private string availablity;
    protected private string description;
    public Item(ItemTemplate template)
    {
        this.name = template.name;
        this.weight = template.weight;
        this.cost = template.cost;
        this.availablity = template.availablity;
        this.description = template.description;
    }
    public Item()
    {
    }
    public string GetName()
    {
        return name;
    }
}

