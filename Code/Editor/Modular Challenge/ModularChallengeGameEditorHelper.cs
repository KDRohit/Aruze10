using UnityEngine;
using UnityEditor;
using System.Collections;

/**
 * Helper class for editor functions commonly used in ModularChallengeGame setup
 */
public class ModularChallengeGameEditorHelper : ScriptableObject
{

	// from provided target & autoInput, set values with reflection
	public static void autoSetFieldProperty(Object target, AutoAssignmentTargetInput input)
	{
		Component targetItem = (target as Component);

		object foundTarget = null;

		// Lookup game objects directly, handle component subclasses with dynamic lookup
		if (input.targetType == typeof(GameObject))
		{
			foundTarget = ModularChallengeGameEditorHelper.getGameObjectOfTargetChild(target, input.targetName, !input.useFirstFound);
		}
		else if (input.targetType.IsSubclassOf(typeof(Component)))
		{
			foundTarget = typeof(ModularChallengeGameEditorHelper)
				.GetMethod("getComponentOnTargetChildDynamic")
				.MakeGenericMethod(input.targetType)
				.Invoke(target, new object[] { target, input.targetName, !input.useFirstFound });
		}

		// Set the target field with the found value
		System.Reflection.FieldInfo targetField = targetItem.GetType().GetField(input.targetField);
		targetField.SetValue(targetItem, foundTarget);
	}

	// separate method for reflection calls
	public static T getComponentOnTargetChildDynamic<T>(Object inspectorTarget, string targetName, bool useDeepest = false) where T : Component
	{
		return getComponentOnTargetChild<T>(inspectorTarget, targetName, useDeepest);
	}

	/* Find a specific component on a child object from a single target with a specific targetName
	 * Returns first encountered by default, deepest component with flag. */
	public static T getComponentOnTargetChild<T>(Object inspectorTarget, string targetName, bool useDeepest = false) where T : Component
	{
		T foundComponent = default(T);

		// set the target
		Component targetItem = inspectorTarget as Component;

		// Assign Pick Buttons
		Transform[] childObjects = targetItem.gameObject.GetComponentsInChildren<Transform>(true);

		// search for the component
		foreach (Transform child in childObjects)
		{
			if (child.name == targetName)
			{
				foundComponent = child.GetComponent<T>();
				if (!useDeepest)
				{
					return foundComponent;
				}
			}
		}

		return foundComponent;
	}


	/* Find a specific component on a child object from an array of targets with a specific targetName
	 * Returns first encountered by default, deepest component with flag. */
	public static T getComponentOnTargetChild<T>(Object[] inspectorTargets, string targetName, bool useDeepest = false) where T : Component
	{
		T foundComponent = default(T);

		// loop through all selected objects
		foreach (Object currentTarget in inspectorTargets)
		{
			// set the target
			Component targetItem = currentTarget as Component;

			// Assign Pick Buttons
			Transform[] childObjects = targetItem.gameObject.GetComponentsInChildren<Transform>(true);

			// search for the component
			foreach (Transform child in childObjects)
			{
				if (child.name == targetName)
				{
					foundComponent = child.GetComponent<T>();
					if (!useDeepest)
					{
						return foundComponent;
					}
				}
			}
		}

		return foundComponent;
	}

	/* Find a specific game object by targetName on the list of children. */
	public static GameObject getGameObjectOfTargetChild(Object inspectorTarget, string targetName, bool useDeepest = false)
	{
		GameObject foundGameObject = null;

		// set the target
		Component targetItem = inspectorTarget as Component;

		// Assign Pick Buttons
		Transform[] childObjects = targetItem.gameObject.GetComponentsInChildren<Transform>(true);

		// search for the component
		foreach (Transform child in childObjects)
		{
			if (child.name == targetName)
			{
				foundGameObject = child.gameObject;
				if (!useDeepest)
				{
					return foundGameObject;
				}
			}
		}

		return foundGameObject;
	}

	/* Add component with reflection */
	public static void addComponentOnTargetChildDynamic(Object inspectorTarget, System.Type targetType, string targetName, bool useDeepest = false) 
	{
		typeof(ModularChallengeGameEditorHelper)
			.GetMethod("addComponentOnTargetChild")
			.MakeGenericMethod(targetType)
			.Invoke(inspectorTarget, new object[] { inspectorTarget, targetName, useDeepest });	
	}

	/* Add a specific component to target children matching string. */
	public static T addComponentOnTargetChild<T>(Object inspectorTarget, string targetName, bool useDeepest = false) where T : Component
	{
		T addedComponent = default(T);

		// set the target
		Component targetItem = inspectorTarget as Component;

		// Assign Pick Buttons
		Transform[] childObjects = targetItem.gameObject.GetComponentsInChildren<Transform>(true);

		Transform finalTarget = null;

		// search for the component
		foreach (Transform child in childObjects)
		{
			if (child.name == targetName)
			{
				finalTarget = child;
				if (!useDeepest)
				{
					break;
				}
			}
		}

		addedComponent = finalTarget.gameObject.AddComponent<T>();

		// size box colliders on children without renderers
		if(typeof(T) == typeof(BoxCollider))
		{
			autoSizeBoxCollider(finalTarget, (addedComponent as BoxCollider));
		}

		return addedComponent;
	}

	// auto-size a collider when added to an object without an attached renderer
	private static void autoSizeBoxCollider(Transform childTarget, BoxCollider attachedCollider)
	{
		//If the object that got a colider doesn't have a renderer Unity won't size it correctly
		if (childTarget.gameObject.GetComponent<Renderer>() == null)
		{
			//Fix the the box colliders center and size to match the pickButtons children	
			Bounds bounds = CommonGameObject.getObjectBounds(childTarget.gameObject);
			attachedCollider.size = childTarget.InverseTransformVector(bounds.size);
			attachedCollider.center = childTarget.InverseTransformPoint(bounds.center);
		}
	}


	// clear all components of a certain type on or below the current items
	public static void clearComponents<T>(Object[] inspectorTargets) where T : Component
	{
		foreach (Object currentTarget in inspectorTargets)
		{
			Component targetItem = (Component)currentTarget;
			T[] targetComponents = targetItem.gameObject.GetComponentsInChildren<T>(true);
			foreach (T targetObject in targetComponents)
			{
				DestroyImmediate(targetObject);
			}
		}
	}

}
