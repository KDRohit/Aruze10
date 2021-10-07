using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(TIUILineSprite))]
public class TIUILineSpriteInspector : UISpriteInspector
{
    #region Members
    private Vector2 m_newPoint;
    #endregion Members

    #region Properties
    private Vector2 NewPoint
    {
        get { return this.m_newPoint; }
        set { this.m_newPoint = value; }
    }
    #endregion Properties

    #region Inspector Overrides
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        NGUIEditorTools.DrawSeparator();
		EditorGUIUtility.fieldWidth = 120f;
		EditorGUIUtility.labelWidth = 120f;
        TIUILineSprite sprite = target as TIUILineSprite;

        //Width
        bool changed = false;
        float width;
        width = EditorGUILayout.FloatField("Width", sprite.Width);
        if (width != sprite.Width)
        {
            sprite.Width = width;
            changed = true;
        }
        width = EditorGUILayout.Slider("AA Width", sprite.AaWidth, 0, sprite.Width/2);
        if (width != sprite.AaWidth)
        {
            sprite.AaWidth = width;
            changed = true;
        }

        //Corner Types
        TIUILineSprite.CornerTypes cornerType = (TIUILineSprite.CornerTypes)EditorGUILayout.EnumPopup("Corner Type", sprite.CornerType);
        if (cornerType != sprite.CornerType)
        {
            sprite.CornerType = cornerType;
            changed = true;
        }


        //Points
        bool pointsChanged = false;
        List<Vector2> points = new List<Vector2>(sprite.Points);
        while (points.Count < 2)
        {
            points.Add(new Vector2());
        }

        EditorGUILayout.LabelField("Points");
        Vector2 p;
        Vector2 point;
        for (int i = 0 ; i < points.Count ; i++)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(50);
            point = points[i];
            p = EditorGUILayout.Vector2Field("Point " + i, point);
            if (point != p)
            {
                points[i] = p;
                changed = true;
                pointsChanged = true;
            }
            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                points.RemoveAt(i);
                changed = true;
                pointsChanged = true;
                i--;
            }
            EditorGUILayout.EndHorizontal();
        }

        //New Point
        EditorGUILayout.BeginHorizontal();
        this.NewPoint = EditorGUILayout.Vector2Field("New Point", this.NewPoint);
        if (GUILayout.Button("Add", GUILayout.Width(60)))
        {
            points.Add(this.NewPoint);
            this.NewPoint = new Vector2();
            changed = true;
            pointsChanged = true;
        }
        EditorGUILayout.EndHorizontal();

        if (pointsChanged)
        {
            sprite.Points = points.ToArray();
        }

        if (changed)
        {
            EditorUtility.SetDirty(sprite);
        }
    }
    #endregion Inspector Implementation
}
