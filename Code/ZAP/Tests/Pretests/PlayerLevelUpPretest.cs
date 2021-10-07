using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Runtime.Serialization;

/*
Class Name: RobustChallengesResetPretest.cs
Author: Michael Christensen-Calvin <mchristensencalvin@zynga.com>
Description: This class is used for resetting the RobustChallenges feature progress before starting testing.
Feature-flow: Add this to an AutomatableTestSetup to make sure that you reset challenge progress.
*/
namespace Zap.Automation
{
	#if UNITY_EDITOR && !ZYNGA_PRODUCTION
	[Serializable]
	public class PlayerLevelUpPretest : Pretest
	{
		[SerializeField] private int jumpToLevel = 0;
		private bool isWaitingOnLevelUpEvent = false;

		// need a default constructor to create objects
		public PlayerLevelUpPretest() {}

		// we need this because it is a requirement for ISerializable to deserialize the data
		public PlayerLevelUpPretest(SerializationInfo info, StreamingContext context)
		{
			DeserializeBaseData(info, context);
		}

		public override void DeserializeBaseData(SerializationInfo info, StreamingContext context)
		{
			base.DeserializeBaseData(info, context);
			jumpToLevel = (int)info.GetValue("jumpToLevel", typeof(int));
		}

		// serialize the data here
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("jumpToLevel", jumpToLevel);
		}

		public override void init()
		{
			base.init();
		}

		public override IEnumerator doTest()
		{
			// Wait for us to finish if we are loading something.
			while (Loading.isLoading)
			{
				yield return new WaitForSeconds(0.5f);
			}

			Server.registerEventDelegate("leveled_up", levelUpEvent, true);
			if (SlotsPlayer.instance.socialMember.experienceLevel < ExperienceLevelData.maxLevel)
			{
				// Only attempt to change level if we are below the cap.
				int targetLevel = 0;
				if (jumpToLevel <= 0)
				{
					// If we are trying to jump to zero, then just jump to the next level.
					targetLevel = SlotsPlayer.instance.socialMember.experienceLevel + 1;
				}
				else
				{
					targetLevel = Mathf.Max(SlotsPlayer.instance.socialMember.experienceLevel, ExperienceLevelData.maxLevel);
				}

				// Send up the event.
				isWaitingOnLevelUpEvent = true;
				PlayerAction.addLevels(targetLevel - SlotsPlayer.instance.socialMember.experienceLevel);
			}
			// Wait for callbacks.
			yield return RoutineRunner.instance.StartCoroutine(waitForCallbacks());
			testIsFinished();
		}

		private IEnumerator waitForCallbacks()
		{
			while (isWaitingOnLevelUpEvent)
			{
				yield return new WaitForSeconds(0.5f);
			}
			yield break;
		}

		private void levelUpEvent(JSON data)
		{
			isWaitingOnLevelUpEvent = false;
		}
	}
	#endif
}
