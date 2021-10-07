using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SlotBasicProperty))]
public class SlotBasicPropertyEditor: Editor
{
    // Editor Script for SlotBasicProperty
    //
    // Author : Xueer Zhu <xzhu@zynga.com>
    // Date : Jan 25th, 2021
    //
    
    GUILayoutOption[] buttonOption = { GUILayout.MinHeight(20.0f), GUILayout.MaxWidth(150.0f), GUILayout.MinWidth(100.0f) };
    
    public override void OnInspectorGUI()
    {
        SlotBasicProperty slotBasicPropertyScript = (SlotBasicProperty)target;
        GUILayout.Space(10);
        if (Application.isPlaying)
        {
            if (GUILayout.Button("Update", buttonOption))
            {
                slotBasicPropertyScript.forceUpdate();
            }
        }
        GUILayout.Space(10);
        DrawDefaultInspector();
    }
}
