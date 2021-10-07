using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AdjustObjectColorsByFactor))]

public class AdjustObjectColorsByFactorInspector : Editor
{
    private AdjustObjectColorsByFactor handler;

    public override void OnInspectorGUI()
    {
        handler = target as AdjustObjectColorsByFactor;

        if (GUILayout.Button("Relink"))
        {
            setupLinks();
        }
        
        if (GUILayout.Button("Multiply"))
        {
            handler.multiplyColors();
        }
        
        if (GUILayout.Button("Restore"))
        {
            handler.restoreColors();
        }
        
        DrawDefaultInspector();

    }

    private void setupLinks()
    {
        handler.sprites = handler.GetComponentsInChildren<UISprite>(true);
        handler.textures = handler.GetComponentsInChildren<UITexture>(true);
        handler.tmpros = handler.GetComponentsInChildren<TextMeshPro>(true);
    }
}
