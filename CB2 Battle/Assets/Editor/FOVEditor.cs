using System.Collections;
using UnityEngine;
using UnityEditor;

[CustomEditor (typeof (ThreatRange))]
public class FOVEditor : Editor
{
    void OnScreenGUI()
    {
        ThreatRange fow = (ThreatRange)target;
        Handles.color = Color.white;
        Handles.DrawWireArc (fow.transform.position, Vector3.up, Vector3.forward, 360, fow.viewRadius);
    }
}
