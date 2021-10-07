using UnityEngine;

public class DoSomethingInvite : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		MFSDialog.inviteFacebookNonAppFriends();
	}
}
