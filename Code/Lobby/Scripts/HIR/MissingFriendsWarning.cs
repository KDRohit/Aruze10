using Com.Scheduler;
using UnityEngine;

/**
Missing Friends Warning

This script pops up a warning dialog if the user has removed Facebook permissions so that our app can
no longer view a list of friends.
*/

internal static class MissingFriendsWarning
{
	private const int NUMBER_OF_TIMES_TO_SKIP_WARNING = 5;
	private static bool DISPLAY_WARNING = false; // set to true to enable the dialog again
	
	public static void displayIfNecessary()
	{
		// Never display if we're not Facebook connected:
		if (!DISPLAY_WARNING || !SlotsPlayer.isFacebookUser)
		{
			return;
		} 

		//TODO: After Updating Zynga Facebook APIs, create an in app method to enable permissions for friends list.
		//Check if we have any friends listed. You can also just set this to "if (true)" to test the function
		if (SocialMember.allFriends.Count == 0 || SocialMember.friendsNonPlayers.Count == 0)
		{
			// TODO: Have server tell us if we have permission instead of making a calculated guess.
			// Ignoring two edge cases: 1, they have no friends, or 2, all friends play the game.
			// We assume the majority case, they have disabled permissions to friends list.
			// Info box tells how to enable permissions to friends list.
			int skips = PlayerPrefsCache.GetInt(Prefs.WARNINGS_SKIPPED_WHEN_FRIENDS_MISSING, NUMBER_OF_TIMES_TO_SKIP_WARNING);
			if (skips >= NUMBER_OF_TIMES_TO_SKIP_WARNING)
			{
				GenericDialog.showDialog(
					Dict.create(
						D.TITLE, Localize.text("missing_friends_title"),
						D.MESSAGE, Localize.text("missing_friends_body_{0}", Glb.FACEBOOK_LINK_APPLICATIONS),
						D.OPTION1, Localize.textUpper("remind_me_later"),
						D.OPTION2, Localize.textUpper("visit_now"),
						D.REASON, "missing-friends-warning",
						D.CALLBACK, new DialogBase.AnswerDelegate(quitDialogCallback)
					),
					SchedulerPriority.PriorityType.IMMEDIATE
				);
			}
			else
			{
				// Skip the warning for now, but get one step closer to warning again.
				PlayerPrefsCache.SetInt(Prefs.WARNINGS_SKIPPED_WHEN_FRIENDS_MISSING, skips + 1);
				PlayerPrefsCache.Save();
			}
		}
	}

	// Callback when missing friends dialog is closed.
	private static void quitDialogCallback(Dict args)
	{
		if ((string)args.getWithDefault(D.ANSWER, "") == "1")
		{
			// Player asked us to warn later.
			PlayerPrefsCache.SetInt(Prefs.WARNINGS_SKIPPED_WHEN_FRIENDS_MISSING, 0);
			PlayerPrefsCache.Save();
		}

		if ((string)args.getWithDefault(D.ANSWER, "") == "2")
		{
			Application.OpenURL(Glb.FACEBOOK_LINK_APPLICATIONS);
		}
	}
}
