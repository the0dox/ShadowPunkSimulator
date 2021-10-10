using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item
{
    protected private string name;
    protected private float weight;
    protected private float cost;
    protected private string availablity;
    protected private string description;
    protected private int stacks;
    protected private bool unique;
    public Item(ItemTemplate template)
    {
        this.name = template.name;
        this.weight = template.weight;
        this.cost = template.cost;
        this.availablity = template.availablity;
        this.description = template.description;
        this.stacks = 1;
        this.unique = template.unique;
    }

    public void AddStack()
    {
        stacks++;
    }
    public void SubtractStack()
    {
        stacks--;
    }
    public void SubtractStack(int value)
    {
        stacks -= value;
    }
    public void AddStack(int value)
    {
        stacks += value;
    }
    public void SetStack(int amount)
    {
        stacks = amount;
    }
    public int GetStacks()
    {
        if(unique)
        {
            return 1;
        }
        else
        {
            return stacks;
        }
    }
    public bool IsConsumed()
    {
        if(stacks < 1)
        {
            Debug.Log("im fully consumed");
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool Stackable()
    {
        return !unique;
    }
    public Item()
    {
    }
    public string GetName()
    {
        return name;
    }

    public float getWeight()
    {
        return weight;
    }
}

