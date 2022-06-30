using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

// UIAction bar is a simple reference to a content filter used by other classes to print out buttons
public class UIActionbar : MonoBehaviour
{
    // Static reference to monobehavior
    public static UIActionbar current;
    void Awake()
    {
        current = this;
    }
}
