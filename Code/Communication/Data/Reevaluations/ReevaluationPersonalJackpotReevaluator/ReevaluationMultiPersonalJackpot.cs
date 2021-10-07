//
// Reevaluation to deserialize and store "multi_personal_jackpot" reevaluation
// This is used by PersonalJackpotFromSymbolModule to extract data to be used in
// increasing, winning, and resetting jackpots based on JackpotEvents
//
// Author : Nick Saito <nsaito@zynga.com>
// Date : Nov 5th 2020
//
// games : orig010
//

using System;
using System.Collections.Generic;
using System.Linq;

public class ReevaluationMultiPersonalJackpot : ReevaluationBase
{
	public List<Jackpot> jackpots;

	private List<JackpotEvent> jackpotEvents;

	public ReevaluationMultiPersonalJackpot(JSON reevalJSON) : base(reevalJSON)
	{
		jackpots = new List<Jackpot>();

		JSON[] jackpotsJSON = reevalJSON.getJsonArray("jackpots");

		if (jackpotsJSON != null)
		{
			foreach (JSON jackpotJSON in jackpotsJSON)
			{
				jackpots.Add(new Jackpot(jackpotJSON));
			}
		}
	}

	// creates an ordered list of jackpot events
	public List<JackpotEvent> getJackpotEventsOrderedByFirstReelIncreaseWinResetPosition()
	{
		jackpotEvents = getJackpotEvents();
		jackpotEvents.Sort(new FirstReelIncreaseWinResetPositionComparer());
		return jackpotEvents;
	}

	// creates a list of unordered jackpot events and caches the result so this
	// operation is only done once.
	private List<JackpotEvent> getJackpotEvents()
	{
		if (jackpotEvents == null)
		{
			jackpotEvents = new List<JackpotEvent>();

			foreach (Jackpot jackpot in jackpots)
			{
				jackpotEvents.AddRange(jackpot.jackpotEvents);
			}
		}

		return jackpotEvents;
	}

	public class Jackpot
	{
		public string jackpotKey;
		public List<JackpotEvent> jackpotEvents;

		public Jackpot(JSON jackpotJSON)
		{
			jackpotEvents = new List<JackpotEvent>();
			jackpotKey = jackpotJSON.getString("jackpot_key", "");

			JSON[] jackpotEventsJSON = jackpotJSON.getJsonArray("jackpot_events");

			if (jackpotEventsJSON != null)
			{
				foreach (JSON jackpotEventJSON in jackpotEventsJSON)
				{
					jackpotEvents.Add(new JackpotEvent(jackpotEventJSON, jackpotKey));
				}
			}
		}
	}

	private class FirstReelIncreaseWinResetPositionComparer : IComparer<JackpotEvent>
	{
		public int Compare(JackpotEvent x, JackpotEvent y)
		{
			if (x == null && y == null)
			{
				return 0;
			}

			if (x == null)
			{
				return 1;
			}

			if (y == null)
			{
				return -1;
			}

			// sort events per reel
			if (x.triggerSymbols[0].reelIndex != y.triggerSymbols[0].reelIndex)
			{
				return x.triggerSymbols[0].reelIndex.CompareTo(y.triggerSymbols[0].reelIndex);
			}

			// they are on the same reel and are the same event, sort by positions
			// we check y first because position visually starts from the bottom of
			// the reel and we want to award from top to bottom.
			if (x.eventType == y.eventType)
			{
				return y.triggerSymbols[0].pos.CompareTo(x.triggerSymbols[0].pos);
			}

			// they are not the same event but they are on the same reel, so sort by event type
			if (x.eventType == JackpotEvent.EVENT_TYPE.INCREASE)
			{
				return -1;
			}

			if (x.eventType == JackpotEvent.EVENT_TYPE.WIN && y.eventType == JackpotEvent.EVENT_TYPE.RESET)
			{
				return -1;
			}

			return 1;
		}
	}

	public class JackpotEvent
	{
		public enum EVENT_TYPE
		{
			INCREASE,
			WIN,
			RESET
		};

		public string jackpotKey;
		public EVENT_TYPE eventType;
		public long addCredits;
		public List<TriggerSymbol> triggerSymbols = new List<TriggerSymbol>();

		public JackpotEvent(JSON jackpotEventJSON, string myJackpotKey)
		{
			jackpotKey = myJackpotKey;

			// convert the jackpot event type into an enum.
			string type = jackpotEventJSON.getString("type", "");
			eventType = (EVENT_TYPE) Enum.Parse(typeof(EVENT_TYPE), type, true);

			addCredits = jackpotEventJSON.getLong("add_credits", 0);
			JSON[] triggerSymbolsJSON = jackpotEventJSON.getJsonArray("trigger_symbols");

			foreach (JSON triggerSymbolJSON in triggerSymbolsJSON)
			{
				triggerSymbols.Add(new TriggerSymbol(triggerSymbolJSON));
			}
		}
	}

	public class TriggerSymbol
	{
		public int reelIndex;
		public int pos;

		public TriggerSymbol(JSON triggerSymbolJSON)
		{
			reelIndex = triggerSymbolJSON.getInt("reel_index", 0);
			pos = triggerSymbolJSON.getInt("pos", 0);
		}
	}
}