using System.Collections.Generic;
using UnityEngine;

namespace QuestForTheChest
{
	public class QFCAction : ServerAction
	{
		/** Action Names **/
		private const string CLAIM_REWARD = "qfc_claim_reward";
		private const string GET_RACE_INFO = "qfc_get_race_info";
		private const string COMPLETE_RACE = "qfc_race_complete";

		/****** Action Variables *****/
		private int raceId = 0;
		private int contestId = 0;
		private string eventId = "";

		/***** Property Names *****/
		private const string CONTEST_ID = "contest_id";
		private const string RACE_INDEX = "race_index";
		private const string EVENT_ID = "event";



		/** Constructor */
		private QFCAction(ActionPriority priority, string type) : base(priority, type) {}

		public static void getCurrentRaceInformation(int contest, int race)
		{
			//response handler is in feature base
			QFCAction action = new QFCAction(ActionPriority.IMMEDIATE, GET_RACE_INFO);
			action.contestId = contest;
			action.raceId = race;
			processPendingActions(true);
		}

		public static void claimReward(string _eventId)
		{
			QFCAction action = new QFCAction(ActionPriority.IMMEDIATE, CLAIM_REWARD);
			action.eventId = _eventId;
			processPendingActions();
		}

		// Consumes the current race complete event and returns a qfc_race_info event
		public static void completeRace(string _eventId)
		{
			QFCAction action = new QFCAction(ActionPriority.IMMEDIATE, COMPLETE_RACE);
			action.eventId = _eventId;
			processPendingActions();
		}

		public static Dictionary<string, string[]> propertiesLookup
		{
			get
			{
				if (_propertiesLookup == null)
				{
					_propertiesLookup = new Dictionary<string, string[]>();
					_propertiesLookup.Add(CLAIM_REWARD, new string[] {EVENT_ID});
					_propertiesLookup.Add(GET_RACE_INFO, new string[] {CONTEST_ID, RACE_INDEX});
					_propertiesLookup.Add(COMPLETE_RACE, new string[] {EVENT_ID});

				}
				return _propertiesLookup;
			}
		}

		/// Implements IResetGame hook, clears out properties as well as clearing based on superclass's view
		new public static void resetStaticClassData()
		{
			_propertiesLookup = null;
		}

		private static Dictionary<string, string[]> _propertiesLookup = null;

		/// Appends all the specific action properties to json
		public override void appendSpecificJSON(System.Text.StringBuilder builder)
		{
			if (!propertiesLookup.ContainsKey(type))
			{
				Debug.LogError("No properties defined for action: " + type);
				return;
			}

			foreach (string property in propertiesLookup[type])
			{
				switch (property)
				{
					case CONTEST_ID:
						appendPropertyJSON(builder, property, contestId);
						break;
					case RACE_INDEX:
						appendPropertyJSON(builder, property, raceId);
						break;
					case EVENT_ID:
						appendPropertyJSON(builder, property, eventId);
						break;
					default:
						Debug.LogWarning("Unknown property for action: " + type + ", " + property);
						break;
				}
			}

			//Debug.LogError(builder.ToString());
		}

	}
}

