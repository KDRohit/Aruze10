using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForcedFlow
{

	public delegate void onCompleteStep(Dict args = null);

	// These are the functions we'll fire off. They take a dict in case we need to pass info between steps
	private List<onCompleteStep> delegateList = new List<onCompleteStep>();
	private Dict flowArguments = null;

	public void addStep(onCompleteStep step, Dict overwriteDict = null)
	{
		delegateList.Add(step);

		flowArguments = overwriteDict;
	}

	public void addObjectOrOverwriteInDict(D key, Object value)
	{
		flowArguments[key] = value;
	}

	// The list removes things as we go, but in case we need to avoid one step we can.
	public void removeStep(onCompleteStep step)
	{
		delegateList.Remove(step);
	}

	public void clearSteps()
	{
		if (delegateList != null)
		{
			delegateList.Clear();
		}
	}

	public void completeCurrentStep(Dict args = null)
	{
		if (delegateList.Count > 0)
		{
			delegateList[0](args);
			delegateList.RemoveAt(0);
		}
	}
}