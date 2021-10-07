using System;
using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class TextMeshProMasker : MonoBehaviour
{
	public enum Orientation { VERTICAL, HORIZONTAL };
	public Orientation currentOrientation = Orientation.VERTICAL;
	public GameObject centerPoint;
	public TextMeshPro[] objectsToManipulate;

	public int totalHeight = 0;
	public int totalWidth = 0;
	public GameObject animationSizer;

	public int verticalMaskTopOffset = 0;
	public int verticalMaskBottomOffset = 0;
	public int horizantalMaskLeftOffset = 0;
	public int horizantalMaskRightOffset = 0;

	private List<TextMeshPro> objectsToManipulateList = new List<TextMeshPro>();
	private List<TextMeshPro> objectsToAdd = new List<TextMeshPro>();
	private List<Material> cachedMaterials = new List<Material>();

	private Vector3 reletivePointFromCenter;
	private Vector4 workingVector;
	private Vector3 workingCenterPointLocation;

	private float maskWidth = 0;
	private float maskHeight = 0;
	private float difference = 0;
	private float adjustedWidth = 0;
	private float adjustedHeight = 0;

	// using uipanel and camera bounds
	public Camera renderingCamera;
	public UIPanel panel;
	private Vector4 bounds = new Vector4();

	public void Start()
	{
		addObjectArrayToList(objectsToManipulate);
	}

	public void clearList()
	{
		objectsToManipulateList = new List<TextMeshPro>();
	}

	public void addObjectToList(TextMeshPro objectToAdd)
	{
		if (objectsToManipulateList == null)
		{
			objectsToManipulateList = new List<TextMeshPro>();
		}

		if (!objectsToManipulateList.Contains(objectToAdd))
		{
			objectsToAdd.Add(objectToAdd);
		}

		// If we had a TON of objects we could sort by X or Y or whatever and then when we checked the clipping rect, we could
		// check and see what direction we just moved everything in and if the object to the left or right of the checked
		// object has a collapsed rect, we could skip checking it
		//objectsToManipulateList.Sort((a, b) => a.transform.RealPosition().x.CompareTo(b.transform.RealPosition().x));
	}

	public void addObjectArrayToList(TextMeshPro[] array)
	{
		objectsToAdd.AddRange(array);
	}

	public void LateUpdate()
	{
		// This is in case the content is made on the fly.
		if (objectsToManipulateList != null && objectsToManipulateList.Count > 0)
		{
			updateWithList();
		}

		// Add when we're done.
		if (objectsToAdd != null && objectsToAdd.Count > 0)
		{

			for (int i = 0; i < objectsToAdd.Count; i++)
			{
				if (objectsToAdd[i] != null)
				{
					cachedMaterials.Add(objectsToAdd[i].fontMaterial);
					objectsToManipulateList.Add(objectsToAdd[i]);
				}
			}

			objectsToAdd.Clear();
			// sort each time?
			//objectsToManipulateList.Sort((a, b) => a.transform.RealPosition().x.CompareTo(b.transform.RealPosition().x));
		}
	}


	private Vector3 getPositionRelevantToGameObject(GameObject objectToPosition, GameObject objectRelativeTo)
	{
		Vector3 relevantVector;

		relevantVector = objectRelativeTo.transform.InverseTransformPoint(objectToPosition.transform.position);

		return relevantVector;
	}

	private void updateWithList()
	{
		if (panel != null && renderingCamera != null)
		{
			updateBasedOnShaderMask();
			return;
		}
		
		if (centerPoint != null)
		{
			// In reverse to prevent problems with removing
			for (int i = objectsToManipulateList.Count - 1; i >= 0; i--)
			{
				if (objectsToManipulateList[i] == null)
				{
					objectsToManipulateList.RemoveAt(i);
					cachedMaterials.RemoveAt(i);
					continue;
				}
				else if (!objectsToManipulateList[i].IsActive())
				{
					continue;
				}

				if (cachedMaterials.Count < objectsToManipulateList.Count)
				{
					Debug.LogError("Mismatch list lengths. Re-building");
					return;
				}

				// We do this each frame in case the size of the text box changes.
				maskWidth = objectsToManipulateList[i].rectTransform.rect.xMax;
				maskHeight = objectsToManipulateList[i].rectTransform.rect.yMax;
				reletivePointFromCenter = getPositionRelevantToGameObject(objectsToManipulateList[i].gameObject, centerPoint);
				workingVector = cachedMaterials[i].GetVector("_ClipRect");
				workingCenterPointLocation = centerPoint.transform.localPosition;
				difference = 0;

				adjustedWidth = totalWidth / 2;
				adjustedHeight = totalHeight / 2;

				switch (currentOrientation)
				{
					case Orientation.VERTICAL:
						if (animationSizer != null)
						{
							adjustedHeight *= animationSizer.transform.localScale.y;
						}

						// If we're too high up or too far down...
						if (reletivePointFromCenter.y + maskHeight - verticalMaskTopOffset > workingCenterPointLocation.y + adjustedHeight)
						{
							difference = (reletivePointFromCenter.y + maskHeight - verticalMaskTopOffset) - (workingCenterPointLocation.y + adjustedHeight);
							workingVector.Set(maskWidth * -1, maskHeight * -1, maskWidth, maskHeight - difference);

							cachedMaterials[i].SetVector("_ClipRect", workingVector);
						}
						else if (reletivePointFromCenter.y - maskHeight + verticalMaskBottomOffset < workingCenterPointLocation.y - adjustedHeight)
						{
							difference = (reletivePointFromCenter.y - maskHeight + verticalMaskBottomOffset) - (workingCenterPointLocation.y - adjustedHeight);
							workingVector.Set(maskWidth * -1, (maskHeight + difference) * -1, maskWidth, maskHeight);

							cachedMaterials[i].SetVector("_ClipRect", workingVector);
						}
						else
						{
							// Set back to normal.
							workingVector.Set(maskWidth * -1, maskHeight * -1, maskWidth, maskHeight);
							cachedMaterials[i].SetVector("_ClipRect", workingVector);
						}

						break;

					case Orientation.HORIZONTAL:
						
						if (animationSizer != null)
						{
							adjustedWidth *= animationSizer.transform.localScale.x;
						}

						// If we're too far in a given direction... left then right
						if (reletivePointFromCenter.x - maskWidth + horizantalMaskLeftOffset < workingCenterPointLocation.x - adjustedWidth)
						{
							difference = ((reletivePointFromCenter.x - maskWidth + horizantalMaskLeftOffset) - (workingCenterPointLocation.x - adjustedWidth));
							workingVector.Set((maskWidth + (difference)) * -1, maskHeight * -1, maskWidth, maskHeight);

							cachedMaterials[i].SetVector("_ClipRect", workingVector);
						}
						else if (reletivePointFromCenter.x + maskWidth - horizantalMaskRightOffset > workingCenterPointLocation.x + adjustedWidth)
						{
							difference = (reletivePointFromCenter.x + maskWidth - horizantalMaskRightOffset) - (workingCenterPointLocation.x + adjustedWidth);
							workingVector.Set(maskWidth * -1, maskHeight * -1, maskWidth - (difference), maskHeight);
							cachedMaterials[i].SetVector("_ClipRect", workingVector);
						}
						else
						{
							// Set back to normal.
							workingVector.Set(maskWidth * -1, maskHeight * -1, maskWidth, maskHeight);
							cachedMaterials[i].SetVector("_ClipRect", workingVector);
						}	
					break;
				}
			}
		}
	}

	public void updateBasedOnShaderMask()
	{
		if (cachedMaterials != null && objectsToManipulateList != null)
		{
			Transform t = panel.transform;
			Vector3 panelPos = new Vector3(-panel.clipRange.z/2, panel.clipRange.w/2, 0);
			Vector3 panelMax = new Vector3(panelPos.x + panel.clipRange.z, panelPos.y - panel.clipRange.w, 0);
			panelMax = t.TransformPoint(panelMax);
			panelMax = renderingCamera.WorldToScreenPoint(panelMax);

			panelPos = t.TransformPoint(panelPos);
			panelPos = renderingCamera.WorldToScreenPoint(panelPos);

			bounds.x = panelPos.x;
			bounds.y = panelPos.y;
			bounds.z = panelMax.x;
			bounds.w = panelMax.y;

			for (int i = objectsToManipulateList.Count - 1; i >= 0; i--)
			{
				if (objectsToManipulateList[i] == null)
				{
					objectsToManipulateList.RemoveAt(i);
					cachedMaterials.RemoveAt(i);
					continue;
				}
				else if (!objectsToManipulateList[i].IsActive())
				{
					continue;
				}

				if (cachedMaterials.Count < objectsToManipulateList.Count)
				{
					Debug.LogError("Mismatch list lengths. Re-building");
					return;
				}
				
				Material cachedMaterial = cachedMaterials[i];
				cachedMaterial.SetVector("_MaskAreaRect", bounds);
			}
		}
	}
}



