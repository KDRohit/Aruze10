using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(OverlayObject))]
public class OverlayObjectInspector : Editor
{
	public override void OnInspectorGUI()
	{
		OverlayObject _object = (OverlayObject)target;

		_object.index = EditorGUILayout.IntField("Index", _object.index);
		_object.sizingSprite = EditorGUILayout.ObjectField(
			"Sizing Sprite",
			_object.sizingSprite,
			typeof(UISprite),
			true) as UISprite;
		
		_object.leftMostObject = EditorGUILayout.ObjectField("Leftmost Point", _object.leftMostObject, typeof(GameObject), true) as GameObject;
		_object.rightMostObject = EditorGUILayout.ObjectField("Rightmost Point", _object.rightMostObject, typeof(GameObject), true) as GameObject;

		_object.width = EditorGUILayout.FloatField("width", _object.width);		
	}
}