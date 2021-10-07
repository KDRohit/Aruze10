//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using UnityEditor;
using System;

/// <summary>
/// Inspector class used to edit UILabels.
/// </summary>

[CustomEditor(typeof(UILabel))]
public class UILabelInspector : UIWidgetInspector
{
	UILabel mLabel;

	/// <summary>
	/// Register an Undo command with the Unity editor.
	/// </summary>

	void RegisterUndo () { NGUIEditorTools.RegisterUndo("Label Change", mLabel); }

	/// <summary>
	/// Font selection callback.
	/// </summary>

	void OnSelectFont (MonoBehaviour obj)
	{
		if (mLabel != null)
		{
			NGUIEditorTools.RegisterUndo("Font Selection", mLabel);
			bool resize = (mLabel.font == null);
			mLabel.font = obj as UIFont;
			if (resize) mLabel.MakePixelPerfect();
		}
	}

	/// Added by Zynga - Mike Wood (Telos) - adds the handle drawing code for labels to show their max size as well as drawn size.
	public override void OnSceneGUI ()
	{
		base.OnSceneGUI();

		Color c = Handles.color;
		Handles.color = Color.magenta;
		Vector3[] corners = NGUIMath.CalculateWidgetCorners(mWidget, (mWidget as UILabel).maxSize);
		Handles.DrawLine(corners[0], corners[1]);
		Handles.DrawLine(corners[1], corners[2]);
		Handles.DrawLine(corners[2], corners[3]);
		Handles.DrawLine(corners[0], corners[3]);
		Handles.color = c;
	}

	protected override bool DrawProperties ()
	{
		mLabel = mWidget as UILabel;
		ComponentSelector.Draw<UIFont>(mLabel.font, OnSelectFont);

		if (mLabel.font != null)
		{
			GUI.skin.textArea.wordWrap = true;
			string text = string.IsNullOrEmpty(mLabel.text) ? "" : mLabel.text;
			text = EditorGUILayout.TextArea(mLabel.text, GUI.skin.textArea, GUILayout.Height(100f));
			if (!text.Equals(mLabel.text)) { RegisterUndo(); mLabel.text = text; }

			GUILayout.BeginHorizontal();
			int len = EditorGUILayout.IntField("Max Width", mLabel.lineWidth, GUILayout.Width(120f));
			GUILayout.Label("pixels");
			GUILayout.EndHorizontal();
			if (len != mLabel.lineWidth && len >= 0f) { RegisterUndo(); mLabel.lineWidth = len; }

			GUILayout.BeginHorizontal();
			len = EditorGUILayout.IntField("Max Height", mLabel.lineHeight, GUILayout.Width(120f));
			GUILayout.Label("pixels");
			GUILayout.EndHorizontal();
			if (len != mLabel.lineHeight && len >= 0f) { RegisterUndo(); mLabel.lineHeight = len; }

			int count = EditorGUILayout.IntField("Max Lines", mLabel.maxLineCount, GUILayout.Width(100f));
			if (count != mLabel.maxLineCount) { RegisterUndo(); mLabel.maxLineCount = count; }

			GUILayout.BeginHorizontal();
			bool shrinkToFit = EditorGUILayout.Toggle("Shrink to Fit", mLabel.shrinkToFit, GUILayout.Width(100f));
			GUILayout.Label("- adjust scale to fit");
			GUILayout.EndHorizontal();
			
			if (shrinkToFit != mLabel.shrinkToFit)
			{
				RegisterUndo();
				mLabel.shrinkToFit = shrinkToFit;
				if (!shrinkToFit) mLabel.MakePixelPerfect();
			}

			// Only input fields need this setting exposed, and they have their own "is password" setting, so hiding it here.
			//GUILayout.BeginHorizontal();
			//bool password = EditorGUILayout.Toggle("Password", mLabel.password, GUILayout.Width(100f));
			//GUILayout.Label("- hide characters");
			//GUILayout.EndHorizontal();
			//if (password != mLabel.password) { RegisterUndo(); mLabel.password = password; }

			GUILayout.BeginHorizontal();
			bool encoding = EditorGUILayout.Toggle("Encoding", mLabel.supportEncoding, GUILayout.Width(100f));
			GUILayout.Label("- use emoticons and colors");
			GUILayout.EndHorizontal();
			if (encoding != mLabel.supportEncoding) { RegisterUndo(); mLabel.supportEncoding = encoding; }

			//GUILayout.EndHorizontal();

			if (encoding && mLabel.font.hasSymbols)
			{
				UIFont.SymbolStyle sym = (UIFont.SymbolStyle)EditorGUILayout.EnumPopup("Symbols", mLabel.symbolStyle, GUILayout.Width(170f));
				if (sym != mLabel.symbolStyle) { RegisterUndo(); mLabel.symbolStyle = sym; }
			}

			GUILayout.BeginHorizontal();
			{
				UILabel.Effect effect = (UILabel.Effect)EditorGUILayout.EnumPopup("Effect", mLabel.effectStyle, GUILayout.Width(170f));
				if (effect != mLabel.effectStyle) { RegisterUndo(); mLabel.effectStyle = effect; }

				if (effect != UILabel.Effect.None)
				{
					Color c = EditorGUILayout.ColorField(mLabel.effectColor);
					if (mLabel.effectColor != c) { RegisterUndo(); mLabel.effectColor = c; }
				}
			}
			GUILayout.EndHorizontal();

			if (mLabel.effectStyle != UILabel.Effect.None)
			{
				GUILayout.Label("Distance", GUILayout.Width(70f));
				GUILayout.BeginHorizontal();
				Vector2 offset = EditorGUILayout.Vector2Field("", mLabel.effectDistance);
				GUILayout.Space(20f);

				if (offset != mLabel.effectDistance)
				{
					RegisterUndo();
					mLabel.effectDistance = offset;
				}
				GUILayout.EndHorizontal();
			}
			
			GUILayout.BeginHorizontal();
			UILabel.ColorMode colorMode = (UILabel.ColorMode)EditorGUILayout.EnumPopup( "Color Mode", mLabel.colorMode, GUILayout.Width( 170f ) );
			
			if( colorMode != mLabel.colorMode ) { RegisterUndo(); mLabel.colorMode = colorMode; }
			if( colorMode == UILabel.ColorMode.Gradient )
			{
				Color e = EditorGUILayout.ColorField( mLabel.endGradientColor );
				if( e != mLabel.endGradientColor ) { RegisterUndo(); mLabel.endGradientColor = e; }
			}
			GUILayout.EndHorizontal();
			
			if( colorMode == UILabel.ColorMode.Gradient )
			{
				int numSteps = mLabel.gradientSteps.Count;
				int removeIndex = -1;
				
				//GUILayout.BeginHorizontal();
				//float gScale = EditorGUILayout.FloatField( "Scale", mLabel.gradientScale );
				//if( gScale != mLabel.gradientScale ) { RegisterUndo(); mLabel.gradientScale = gScale; }
				//
				//float gOffset = EditorGUILayout.FloatField( "Offset", mLabel.gradientOffset );
				//if( gOffset != mLabel.gradientOffset ) { RegisterUndo(); mLabel.gradientOffset = gOffset; }
				//GUILayout.EndHorizontal();
				
				GUILayout.BeginVertical();
				bool addStep = GUILayout.Button( "+" );
				if( addStep )
				{
					float location = 0.5f;
					if( numSteps > 0 )
					{
						float stepLoc = mLabel.gradientSteps[ numSteps - 1 ].location;
						location = stepLoc + ( 1.0f - stepLoc ) * 0.5f;
					}
					RegisterUndo();
					mLabel.gradientSteps.Add( new GradientStep( location, Color.white ) );
					mLabel.MarkAsChanged();
				}
				
				GradientStep step;
				for( int i = 0; i < numSteps; i++ )
				{
					bool changed = false;
					step = mLabel.gradientSteps[ i ];
					
					float min = 0;
					float max = 1;
					if( i > 0 )
					{
						min = mLabel.gradientSteps[ i - 1 ].location;
					}
					if( i < numSteps - 1 )
					{
						max = mLabel.gradientSteps[ i + 1 ].location;
					}
					
					GUILayout.BeginHorizontal();
					bool removeStep = GUILayout.Button( "-" );
					float location = EditorGUILayout.Slider( step.location, min, max );
					Color color = EditorGUILayout.ColorField( step.color );
					GUILayout.EndHorizontal();
					
					if( removeStep && removeIndex == -1 )
					{
						removeIndex = i;
					}
					
					if( location != step.location )
					{
						step.location = location;
						changed = true;
					}
					if( color != step.color )
					{
						step.color = color;
						changed = true;
					}
					
					if( changed )
					{
						mLabel.gradientSteps[ i ] = step;
						mLabel.MarkAsChanged();
					}
				}
				GUILayout.EndVertical();
				
				if( removeIndex != -1 )
				{
					RegisterUndo();
					mLabel.gradientSteps.RemoveAt( removeIndex );
					mLabel.MarkAsChanged();
					removeIndex = -1;
				}
			}
			
			GUILayout.BeginHorizontal();
			int spacing = EditorGUILayout.IntField("Line Spacing", mLabel.lineSpacing, GUILayout.Width(120f));
			GUILayout.Label("pixels");
			GUILayout.EndHorizontal();
			if (spacing != mLabel.lineSpacing) { RegisterUndo(); mLabel.lineSpacing = spacing; }

			return true;
		}
		EditorGUILayout.Space();
		return false;
	}
}
