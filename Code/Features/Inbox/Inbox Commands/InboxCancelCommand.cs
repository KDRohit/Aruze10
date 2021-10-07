using UnityEngine;
using System.Collections;

public class InboxCancelCommand : InboxCommand
{
	public const string CANCEL = "cancel";

	/// <inheritdoc/>
	public override string actionName
	{
		get { return CANCEL; }
	}
}