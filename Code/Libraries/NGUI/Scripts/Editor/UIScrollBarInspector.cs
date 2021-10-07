//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using UnityEditor;
using System;
using System.ComponentModel;

[CustomEditor(typeof(UIScrollBar))]
public class UIScrollBarInspector : TICoroutineMonoBehaviourEditor
{
	public override void OnInspectorGUI ()
	{
		///NGUI Warning Fix Start
		//EditorGUIUtility.LookLikeControls(80f);
		EditorGUIUtility.fieldWidth = 80f;
		EditorGUIUtility.labelWidth = 80f;
		///NGUI Warning Fix End
		UIScrollBar sb = target as UIScrollBar;

		NGUIEditorTools.DrawSeparator();

		float val = EditorGUILayout.Slider("Scroll Value", sb.scrollValue, 0f, 1f);
		float size = EditorGUILayout.Slider("Thumb Size", sb.barSize, 0f, 1f);
		float defaultAlpha = EditorGUILayout.Slider("Default Alpha", sb.defaultAlpha, 0f, 1f);
		float hoverAlpha = EditorGUILayout.Slider("Hover Alpha", sb.hoverAlpha, 0f, 1f);

		NGUIEditorTools.DrawSeparator();

		Color overrideColor = EditorGUILayout.ColorField("Color Override", sb.overrideColor);
		
		UISprite bg = (UISprite)EditorGUILayout.ObjectField("Background", sb.background, typeof(UISprite), true);
		UISprite fg = (UISprite)EditorGUILayout.ObjectField("Foreground", sb.foreground, typeof(UISprite), true);
		UISprite a0 = (UISprite)EditorGUILayout.ObjectField("Arrow 0", sb.arrow0, typeof(UISprite), true);
		UISprite a1 = (UISprite)EditorGUILayout.ObjectField("Arrow 1", sb.arrow1, typeof(UISprite), true);
		UIScrollBar.Direction dir = (UIScrollBar.Direction)EditorGUILayout.EnumPopup("Direction", sb.direction);
		bool inv = EditorGUILayout.Toggle("Inverted", sb.inverted);
		bool centered = EditorGUILayout.Toggle("Centered", sb.centerAlign);

		bool shouldControlPivot = EditorGUILayout.Toggle("Controls Pivot?", sb.shouldControlPivot);
		bool showArrows = EditorGUILayout.Toggle("Always Show Arrows?", sb.alwaysShowArrows);
		bool showScrollbar = EditorGUILayout.Toggle("Always Show?", sb.alwaysShowScrollbar);

		if (!Mathf.Approximately(sb.scrollValue, val) ||
			!Mathf.Approximately(sb.barSize, size) ||
			sb.background != bg ||
			sb.foreground != fg ||
			sb.direction != dir ||
			sb.arrow0 != a0 ||
			sb.arrow1 != a1 ||
			sb.inverted != inv ||
			sb.centerAlign != centered ||
			!Mathf.Approximately(sb.defaultAlpha, defaultAlpha) ||
			!Mathf.Approximately(sb.hoverAlpha, hoverAlpha) || 
			sb.overrideColor != overrideColor || 
			sb.shouldControlPivot != shouldControlPivot ||
			sb.alwaysShowArrows != showArrows ||
			sb.alwaysShowScrollbar != showScrollbar)
		{
			NGUIEditorTools.RegisterUndo("Scroll Bar Change", sb);
			sb.scrollValue = val;
			sb.barSize = size;
			sb.inverted = inv;
			sb.centerAlign = centered;
			sb.background = bg;
			sb.foreground = fg;
			sb.arrow0 = a0;
			sb.arrow1 = a1;
			sb.direction = dir;
			sb.defaultAlpha = defaultAlpha;
			sb.hoverAlpha = hoverAlpha;
			overrideColor = new Color(overrideColor.r, overrideColor.g, overrideColor.b, defaultAlpha);
			sb.overrideColor = overrideColor;
			sb.shouldControlPivot = shouldControlPivot;
			sb.alwaysShowArrows = showArrows;
			sb.alwaysShowScrollbar = showScrollbar;
			UnityEditor.EditorUtility.SetDirty(sb);
		}
		
		//base.OnInspectorGUI();
	}
}
