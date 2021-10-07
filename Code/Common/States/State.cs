using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Com.States
{
	/** State
	 * 
	 * The State class stores the typical information of a particular state, most commonly the
	 * "value" the state is in. If you want a "ready" state, the new State will have the parameter
	 * of "ready".
	 * 
	 * States can also store functions to be run when the state has changed (controlled in StateMachine)
	 * These functions are: 
	 *	  "onExit()"  - to be ran when this state is no longer the current state
	 *	  "onEnter()" - to be ran when this state becomes the current state
	 * 
	 * States can adhere to a set of rules that can evaluate the current state.
	 * Example: if we are playing poker, and we want to see if the state is "deal",
	 * a state might also be able to "deal" if the state value is "ready", or "hand finished".
	 * For this to happen, the StateMachine.addStateRule() will accept a list of possible conditions
	 * for the state to be active. In this particular example, we would do this:
	 * 
	 * StateMachine.addStateRule( "deal", "ready", "hand finished" );
	 * 
	 * Then when we want to see if we can deal:
	 * StateMachine.can( "deal" )
	 * 
	 * If the current state value is "deal", or the state has the rules "ready", or "hand finished",
	 * then StateMachine.can( "deal" ) will return true
	 * 
	 * ...
	 * @author Bennett Yeates
	 * 
	 * */
	public class State
	{
		/** the actual name of the state */
		public string stateName;
		
		/** function to be called when this state is no longer the current state */
		public GenericDelegate onExit;
		
		/** function to be called when this state becomes the current state */
		public GenericDelegate onEnter;
		
		/** list of conditions that this state can be a part of, for examples see class comments */
		protected List<string> rules = new List<string>();

		// =============================
		// COMMON STATE CONSTANTS
		// =============================
		public const string READY = "ready";
		public const string IN_PROGRESS = "in_progress";
		public const string BUSY = "busy";
		public const string COMPLETE = "complete";		
		
		public State( string stateName )
		{
			this.stateName = stateName;
		}
		
		/** checkRules - checks the list of rules to see if the current state matches.
		 * this is for internal state use only (used by StateMachine)
		 * 
		 * @param name - the name of the rule (added with addRule())
		 * @return - returns true if the condition list has the state value
		 *  */
		internal bool checkRules( string condition )
		{
			return stateName == condition || evalRules( condition, rules );
		}
		
		/** enter - state machine calls this when this state is the current state */
		internal void enter()
		{
			if ( onEnter != null )
			{
				onEnter();
			}
		}
		
		/** exit - state machine calls this when this state was the current state */
		internal void exit()
		{
			if ( onExit != null )
			{
				onExit();
			}
		}
		
		/** addRule - add a condition to be met for a particular state 
		 * 
		 * @param conditions - a list of conditions that the state will need to match
		 * ie - if the state should be "run" or "ok", call addRule( "run", "ok" );
		 * */
		public void addRule( List<string> conditions )
		{
			if (conditions != null)
			{
				rules.AddRange(conditions); 
			}
		}
		
		/** removeRule - removes a condition from the rule list */
		public void removeRule( string condition )
		{
			if ( rules.Contains( condition ) )
			{
				rules.Remove( condition );
			}
		}
		
		/** evalRules - process the condition against the passed rules 
		 * 
		 * @param condition - the condition to check in the rules
		 * @param rules - the string of rules the condition applies to
		 * @return - Boolean returns true if the state complies with the rules
		 * */
		internal static bool evalRules( string condition, List<string> rules )
		{
			if ( rules == null ) { return false; }

			List<string> falseConditions = getFalseConditions(rules);
			return rules.Contains(condition) || falseConditions.Count > 0 && !falseConditions.Contains(condition);
		}

		internal static List<string> getFalseConditions(List<string> rules)
		{
			List<string> falseConditions = new List<string>();
			for (int i = 0; i < rules.Count; ++i)
			{
				if (rules[i].Contains("!"))
				{
					falseConditions.Add(rules[i]);
				}
			}

			return falseConditions;
		}
	}
}
