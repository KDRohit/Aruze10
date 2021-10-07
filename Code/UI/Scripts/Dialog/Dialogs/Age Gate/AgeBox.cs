using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/**
Attached to the parent dialog object so the buttons can be linked and processed for clicks.
**/

public class AgeBox : TICoroutineMonoBehaviour
{
	public TextMeshPro ageLabel;
	public GameObject crossHairs;
	private UICenterOnChild uiCenterOnChild = null;

	new public Collider collider
	{
		get
		{
			if (!checkedCollider)
			{
				_collider = GetComponent<Collider>();
			}
			return _collider;
		}
	}
	private Collider _collider = null;
	private bool checkedCollider = false;

	public int age
	{
		set
		{
			_age = value;
			string text = "";
			
			if (value != 0)
			{
				text = value.ToString();
				
				if (value == AgeGateDialog.MAX_AGE)
				{
					text = text + "+";
				}
			}
			else
			{
				collider.enabled = false;
			}
			
			ageLabel.text = text;
		}
		
		get
		{
			return (_age);
		}
	}
	private int _age = 0;
	
	public void init(int initAge)
	{
		age = initAge;
		select(false);
	}
	
	public void select(bool shouldSelect)
	{
		if (!shouldSelect)
		{
			crossHairs.SetActive(shouldSelect);
		}
		
		if (shouldSelect)
		{
			center();
		}
	}
	
	public void center()
	{
		if (uiCenterOnChild == null)
		{
			uiCenterOnChild = gameObject.AddComponent<UICenterOnChild>();
		}
		else
		{
			uiCenterOnChild.Recenter();
		}
		
		uiCenterOnChild.enabled = false;
	}
}
