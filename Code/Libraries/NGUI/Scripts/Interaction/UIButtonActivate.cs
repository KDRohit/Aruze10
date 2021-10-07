//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright � 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;

/// <summary>
/// Very basic script that will activate or deactivate an object (and all of its children) when clicked.
/// </summary>

[AddComponentMenu("NGUI/Interaction/Button Activate")]
public class UIButtonActivate : TICoroutineMonoBehaviour
{
    public GameObject target;
    public bool state = true;

    void OnClick () { if (target != null) NGUITools.SetActive(target, state); }
}