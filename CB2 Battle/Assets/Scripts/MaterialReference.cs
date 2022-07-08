using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A static library that contains all conditions TEMPLATES so that scripts can easily create add/edit conditions
public class MaterialReference : MonoBehaviour
{
    // IMPORTANT: you must add all scriptable objects to this list in order for them to be intialized in the game scene
    [SerializeField] public List<Material> MaterialInitalizer = new List<Material>();
    // static reference of conditionsinitializer for other scripts
    private static Material[] Library;
    // creates Library so that it can be referenced statically
    [SerializeField] private static Material defaultMaterial;

    public void Init()
    {
        Library = MaterialInitalizer.ToArray();
        defaultMaterial = Library[0];
    }

    // name: the name of the scriptable object that needs to be copied
    // creates and returns a regular condition object out of the template that shares a name with input
    public static Material GetMaterial(int index)
    {
        if(Library.Length -1 < index || index < 0)
        {
            Debug.LogWarning("Error invalid index (" + index + ")");
            return null;
        }
        return Library[index];
    }

    // returns the library
    public static Material[] Materials()
    {
        return Library;
    }

    public static Material GetDefaultMaterial()
    {
        return defaultMaterial;
    }

    public static int GetMaterialIndex(Material mat)
    {
        for(int i = 0; i < Library.Length; i++)
        {
            if(Library[i].Equals(mat))
            {
                return i;
            }
        }
        Debug.LogWarning("Error material index not found");
        return 0;
    }
}
