using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;

// Editor class for SetReelStopOrder module
// populates a visual inspector interface that represents the different reel set ups in various kinds of slot games
// this script works for regular reels, independent reels, layered reels. and hybrid reels
// 
// Author : Xueer Zhu xzhu@zynga.com
// Date : Nov 10th 2020
//
[CustomEditor(typeof(SetReelStopOrder))]
public class SetReelStopOrderEditor : CustomEditorBase<SetReelStopOrder>
{
    GUILayoutOption[] stopIndexArrayLayoutOptions = { GUILayout.MaxWidth(50.0f), GUILayout.MinWidth(30.0f) };
    
    private bool isHybridGame;
    private int stopOrderSize;
    private int cols;  
    private int rows; 
    private int layers; 
    private bool isIndependentReelGame;
    private bool isLayeredGame;

    private int hybridStopOrderListSize;
    private SerializedProperty stopIndexArray;
    private SerializedProperty hybridStopArray;
    private SerializedProperty hybridStopJaggedArraySize;

    private void OnEnable()
    {
        isHybridGame = serializedObject.FindProperty("isHybridGame").boolValue;
        stopOrderSize = serializedObject.FindProperty("stopOrderSize").intValue;
        cols = serializedObject.FindProperty("cols").intValue;
        rows = serializedObject.FindProperty("rows").intValue;
        layers = serializedObject.FindProperty("layers").intValue;
        isIndependentReelGame = serializedObject.FindProperty("isIndependentReelGame").boolValue;
        isLayeredGame = serializedObject.FindProperty("isLayeredGame").boolValue;
        
        hybridStopOrderListSize = serializedObject.FindProperty("hybridStopOrderListSize").intValue;
        stopIndexArray = serializedObject.FindProperty("stopIndexArray");
        hybridStopArray = serializedObject.FindProperty("hybridStopArray");
        hybridStopJaggedArraySize = serializedObject.FindProperty("hybridStopJaggedArraySize");
    }

    protected override void drawGUIGuts()
    {
        drawReelTypeInfo();
        drawReelLayout();
    }

    private void drawReelTypeInfo()
    {
        drawHeader("Reel Settings");
        GUILayout.Space(10);
        drawBool("isHybridGame", "Is Hybrid Game");
        if (!isHybridGame)
        {
            drawNonHybridTypeReelInfo();
        }
        else
        {
            drawHybridTypeReelInfo();
        }
    }

    private void drawReelLayout()
    {
        drawHeader("Reel Layout");
        if (!isHybridGame)
        {
            drawNonHybridTypeReelLayout();
        }
        else
        {
            drawHybridTypeReelLayout();
        }
    }

    private void drawHybridTypeReelInfo()
    {
        GUILayout.Space(10);
        drawInt("hybridStopOrderListSize", "Hybrid Reel Stop List Size");
    }

    private void drawNonHybridTypeReelInfo()
    {
        GUILayout.Space(10);
        drawBool("isIndependentReelGame", "Is Independent Game");
        drawBool("isLayeredGame", "Is Layered Game");
        drawHelp("Stop order Size should equal to the largest stopOrder.");
        drawInt("stopOrderSize", "Stop Order Size");
        drawInt("cols", "Reels Size");

        if (isIndependentReelGame)
        {
            drawInt("rows", "Rows Size");
        }
        else
        {
            rows = 1;
        }

        if (isLayeredGame)
        {
            drawInt("layers", "Layers Size");
        }
        else
        {
            layers = 1; // non layered game default to 1
        }
    }
    
    private void drawHybridTypeReelLayout()
    {
        GUILayout.Space(10);

        int stopIndex = 0;
        for (int k = 0; k < hybridStopOrderListSize; k++)
        {
            drawArrayElementAtIndex("hybridStopJaggedArraySize", "List " + k + " Size", k);

            GUILayout.Space(10);
            for (int m = 0; m < hybridStopJaggedArraySize.GetArrayElementAtIndex(k).intValue; m++)
            {
                drawArrayElementAtIndex("hybridStopArray", "StopOrder = " + m, stopIndex);
                stopIndex++;
            }
            GUILayout.Space(10);
        }
    }
    
    private void drawNonHybridTypeReelLayout()
    {
        drawHelp("Stop order Size should be the same as the largest stopOrder.");
        
        int index = 0;
        for (int layer = 0; layer < layers; layer++)
        {
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            
            if (isLayeredGame)
            {
                GUILayout.Label("Layer " + layer);
            }

            EditorGUILayout.BeginVertical();
            GUILayout.Label("ReelID");
            GUILayout.Label("StopOrder");
            EditorGUILayout.EndVertical();
            
            // draw a rows * cols grid of that layer
            for (int col = 0; col < cols; col++)
            {
                EditorGUILayout.BeginVertical();
                GUILayout.Label(col.ToString());
                for (int row = 0; row < rows; row++)
                {
                    if (index <= cols * rows * layers)
                    {
                        drawArrayElementAtIndex("stopIndexArray", "" , index, stopIndexArrayLayoutOptions);
                        index++;
                    }
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
            
            if (!isLayeredGame)
            {
                layer++;  // not layered game only needs to draw reel info once
            }
        }
    }
}
