using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadingScreenBehavior : MonoBehaviour
{
    private static LoadingScreenBehavior instance;

    void Awake()
    {
        instance = this;
    }

    public static void FinishedLoading()
    {
        Destroy(instance.gameObject);
    }
}
