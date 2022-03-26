using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class UIActionbar : MonoBehaviour
{
    // Static reference to monobehavior
    public static UIActionbar current;
    private static List<GameObject> Buttons = new List<GameObject>();

    void Awake()
    {
        current = this;
    }
}
