using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

// Wrapping in UNITY_EDITOR as we shouldn't ever need to use this outside of Editor related scripts
#if UNITY_EDITOR
/**
 * Common functionality having to do with code reflection.
 *
 * Original Author: Scott Lepthien
 * Creation Date: 12/10/2019
 */
public static class CommonReflection
{
	// Get all serialized fields from the passed in type.  That means
	// any public fields not marked [System.NonSerialized] and any non-public
	// fields marked [SerializeField].
	public static List<string> getNamesOfAllSerializedFieldsForType(System.Type typeToSearch, bool isIncludingHideInInspectorFields = false)
	{
		List<string> allSerializedFields = new List<string>();

		FieldInfo[] publicTargetObjectFields = typeToSearch.GetFields(BindingFlags.Instance | BindingFlags.Public);
		foreach (FieldInfo info in publicTargetObjectFields)
		{
			bool isNonSerializedField = (System.Attribute.GetCustomAttribute(info, typeof(System.NonSerializedAttribute)) as System.NonSerializedAttribute) != null;
			bool isHideInInspectorField = (System.Attribute.GetCustomAttribute(info, typeof(UnityEngine.HideInInspector)) as UnityEngine.HideInInspector) != null;

			if (!isNonSerializedField && (isIncludingHideInInspectorFields || !isHideInInspectorField))
			{
				// This is a public variable not marked as [System.NonSerialized] so we need to add it
				allSerializedFields.Add(info.Name);
			}
		}
		
		FieldInfo[] nonPublicTargetObjectFields = typeToSearch.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
		foreach (FieldInfo info in nonPublicTargetObjectFields)
		{
			bool isSerializedField = (System.Attribute.GetCustomAttribute(info, typeof(SerializeField)) as SerializeField) != null;
			bool isHideInInspectorField = (System.Attribute.GetCustomAttribute(info, typeof(UnityEngine.HideInInspector)) as UnityEngine.HideInInspector) != null;
			
			if (isSerializedField && (isIncludingHideInInspectorFields || !isHideInInspectorField))
			{
				// This is a non-public variable that was marked [SerializeField] so we need to add it
				allSerializedFields.Add(info.Name);
			}
		}

		return allSerializedFields;
	}

	// Tells if the passed type is an array
	public static bool isTypeArray(System.Type typeToCheck)
	{
		return typeToCheck.IsArray;
	}

	// Tells if the passed type is a List
	public static bool isTypeList(System.Type typeToCheck)
	{
		return (typeToCheck.IsGenericType && typeToCheck.GetGenericTypeDefinition() == typeof(List<>));
	}

	// Tells if a Type is an Array or List
	public static bool isTypeArrayOrList(System.Type typeToCheck)
	{
		return (isTypeArray(typeToCheck) || isTypeList(typeToCheck));
	}

	// If Type is an Array or a List the element type will be extracted and returned.
	// If it isn't an Array or a List this function will return NULL.
	public static System.Type getElementTypeFromArrayOrList(System.Type typeToExtractFrom)
	{
		if (isTypeArrayOrList(typeToExtractFrom))
		{
			if (typeToExtractFrom.IsArray)
			{
				// This is an array of elements
				return typeToExtractFrom.GetElementType();
			}
			else if (typeToExtractFrom.IsGenericType && typeToExtractFrom.GetGenericTypeDefinition() == typeof(List<>))
			{
				// This is a generic list of elements
				return typeToExtractFrom.GetGenericArguments()[0];
			}
		}

		return null;
	}
	
	// Gets the Type for an individual element.  If this is an Array/List it
	// will be the Type the Array/List contains.  If it is already an individual
	// type it will just return the passed in type.
	// NOTE : This function would need to be updated if we want to handle other Generic
	// container types in the future, like say a Dictionary.
	public static System.Type getIndividualElementType(System.Type passedInType)
	{
		System.Type outputType = passedInType;
		
		if (CommonReflection.isTypeArrayOrList(passedInType))
		{
			outputType = CommonReflection.getElementTypeFromArrayOrList(passedInType);
			if (outputType == null)
			{
				Debug.LogError("CommonReflection.getIndividualElementType() - getElementTypeFromArrayOrList was unable to extract a type from passedInType.Name = " + passedInType.Name);
			}
		}

		return outputType;
	}
}
#endif
