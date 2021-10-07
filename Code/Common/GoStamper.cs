using UnityEngine;

// TODO:UNITY2018:nestedprefab
// Unity 2018.3 nested prefabs should replace this.

/*
Game Object Stamper

If you have a lot of copies of the same thing in a lot of different places
(like pickems in a picking game), when the pickems change (and they will change),
you either have to:

	(1) make the same changes over and over again in each pick
or	(2) delete the old picks and copy/paste the new pick over and over again.

But now you can use Game Object Stampers and game object stamps.
GO stamps are a way to implement subprefabs.

	Set-Up

For example, the ghostbusters01 Picking Game has lots of trap pickems.
Create a parent folder and attach a GO Stamper.

	Hierarchy
	{
		Trap Pickems (GO Stamper)
	}

Create pickem anchors (empty game objects) and attach go stamps
(make sure each anchor is in the right position for each pickem).

	Hierarchy
	{
		Trap Pickems (GO Stamper)
	
			Trap Anchor 1 (Go Stamp)
				...
			Trap Anchor 12 (Go Stamp)
	}

Assign the GO stamper prefab tunable to the trap pickem stamp in the project,

	Inspector
	{
		Trap Pickems
		
		GO Stamper
		{
			prefab   ghostbusters01 Trap Pickem Stamp
		}
	}

Press the Assign Prefab button to stamp all the anchors.

	Apply Changes

When you have to change the trap pickems, just change one of them,
then press the Apply Changes button on the anchor go stamp
to change all the pickems!

	Naming Convention

Please name stamp prefabs with the suffix "Stamp".

	Rock Pickem Stamp
	Scissors Pickem Stamp
	Paper Pickem Stamp

It names instances by replacing "Stamp" with "Object".

	Rock Pickem Object
	Scissors Pickem Object
	Paper Pickem Object

Thank you.
*/

public class GoStamper : TICoroutineMonoBehaviour
{
	public GameObject prefab = null; // This is the stamp prefab.
	public GoStamp[] goStamps;       // You can explicitly list the go stamps (but you don't have to).

	void Awake()
	{
		if (Application.isPlaying)
		{
			enabled = false;
		}
	}

	public void assignPrefab()
	{
		if (prefab != null)
		{
			GoStamp[] goStamps = this.goStamps;
			
			if (goStamps == null || goStamps.Length == 0)
			{
				goStamps = GetComponentsInChildren<GoStamp>();
			}
			
			GoStamp goStamp = null;
			
			for (int iGoStamp = 0; iGoStamp < goStamps.Length; iGoStamp++)
			{
				goStamp = goStamps[iGoStamp];
				goStamp.assignPrefab(prefab);
			}				
		}

		stampAllObjects();
	}

	public void stampAllObjects()
	{
		GoStamp[] goStamps = this.goStamps;
		
		if (goStamps == null || goStamps.Length == 0)
		{
			goStamps = GetComponentsInChildren<GoStamp>();
		}
		
		GoStamp goStamp = null;
		
		for (int iGoStamp = 0; iGoStamp < goStamps.Length; iGoStamp++)
		{
			goStamp = goStamps[iGoStamp];
			goStamp.stampObject();
		}
	}

	public void deleteAllObjects()
	{
		GoStamp[] goStamps = this.goStamps;
		
		if (goStamps == null || goStamps.Length == 0)
		{
			goStamps = GetComponentsInChildren<GoStamp>();
		}
		
		GoStamp goStamp = null;
		
		for (int iGoStamp = 0; iGoStamp < goStamps.Length; iGoStamp++)
		{
			goStamp = goStamps[iGoStamp];
			goStamp.deleteObject();
		}
	}
}
