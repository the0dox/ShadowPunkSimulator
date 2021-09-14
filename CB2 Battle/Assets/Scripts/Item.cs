using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item
{
    private string name;
    private int weight;
    private int cost;
    private string availablity;
    public Item(ItemTemplate template)
    {
        this.name = template.name;
        this.weight = template.weight;
        this.cost = template.cost;
        this.availablity = template.availablity;
    }
}

