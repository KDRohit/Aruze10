using UnityEngine;
using System.Collections;

// this class will fade in out between two groups of game objects
public class FadeInAnOut : MonoBehaviour {

	public GameObject[] group1;
	public GameObject[] group2;
	public float  holdTime = 1.0f;    				// after the fade process is complete, how long to hold before starting over
	public float  fadeTime = 1.0f;	  				// length of fade process
	public bool	  makeGroupMembersActive = true;
	public bool	  initOnStart = true;
	public bool	  isOneShot = false;                // if true just fade to 2nd group and stop
	public bool   shouldBeReplayable = false;
	private float	curTime;
	private bool	paused;
	private GameObject[] currentFadeOutGroup;
	private GameObject[] currentFadeInGroup;
	private GameObject[] stopGroup;


	// Use this for initialization
	void Start () 
	{
		if (initOnStart)
		{
			init();
		}
	}

	public void init()
	{
		curTime = holdTime;
		currentFadeOutGroup = group1;
		currentFadeInGroup = group2;
		stopGroup = null;
		paused = false;

		// set initial states
		StartCoroutine(CommonGameObject.fadeGameObjectsTo(currentFadeOutGroup, 1.0f, 1.0f, 0, false));
		StartCoroutine(CommonGameObject.fadeGameObjectsTo(currentFadeInGroup, 0.0f, 0.0f, 0, false));

		if (makeGroupMembersActive)
		{
			activateGroup(group1, true);
			activateGroup(group2, true);
		}		
	}

	public void deactiveAllGroups()
	{
		activateGroup(group1, false);		
		activateGroup(group2, false);		
	}

	private void activateGroup(GameObject[] group, bool activeState)
	{
		foreach (GameObject go in group)
		{
			go.SetActive(activeState);
		}
	}

	public void stopAtStartGroup()
	{
		stopGroup = group1;
	}
	
	// Update is called once per frame
	void Update () 
	{
		if (paused || currentFadeOutGroup == null)
		{
			return;
		}

		curTime -= Time.deltaTime;

		if (curTime <= 0)
		{

			if (stopGroup == currentFadeOutGroup || (isOneShot && currentFadeOutGroup == group2))
			{
				paused = !shouldBeReplayable;
				activateGroup(currentFadeInGroup, false);
				return;
			}

			curTime = holdTime + fadeTime;

			if (currentFadeOutGroup != null)
			{
				StartCoroutine(CommonGameObject.fadeGameObjectsTo(currentFadeOutGroup, 1.0f, 0.0f, fadeTime, false));
			}
			if (currentFadeInGroup != null)
			{
				StartCoroutine(CommonGameObject.fadeGameObjectsTo(currentFadeInGroup, 0.0f, 1.0f, fadeTime, false));
			}

			// swap the groups
			GameObject[] tempGroup = currentFadeOutGroup;
			currentFadeOutGroup = currentFadeInGroup;
			currentFadeInGroup = tempGroup;
		}
	}
}
