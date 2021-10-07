using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


namespace Com.States
{
	/** 
	 * ---------------------------------------------------------------
	 * StateMachine - state machine pattern, some methods have default
	 * implementation, as they are pretty straight forward. this class is designed
	 * to manage different state changes of some algorithm. See the State.as class
	 * for more details.
	 * 
	 * Typical usage would be to add states using addState()
	 * i.e. - myStateMachine.addState( "ready" )
	 * 
	 * You can also add states with conditions that allow for a state to be satisfied
	 * i.e. - myStateMachine.addState( "ready", { conditions: [ "waiting for input", "running" ] } );
	 * This is saying that the state machine can also be in a "ready" state if it is "waiting for input"
	 * or "running"
	 * 
	 * you can also pass conditions that states cannot be in
	 * i.e. - myStateMachine.addState( "ready", { conditions: [ "!busy" ] };
	 * This is checking if the state is ready, under the condition that it is "not busy"
	 * 
	 * When you want to change a state use updateState();
	 * i.e. - myStateMachine.updateState( "busy" );
	 * 
	 * ---------------------------------------------------------------
	 * Adding a global state condition
	 * 
	 * Global state conditions are a state that the entire machine can be in if the conditions
	 * are satisfied.
	 * 
	 * For example, if you want a global state that is "can interact" which the state machine
	 * will be in whenever the state machine is "running", you would do:
	 * myStateMachine.addGlobalStateCondition( "can interact", "running" );
	 * 
	 * You can also add multiple rules that satisfy this:
	 * myStateMachine.addGlobalStateCondition( "can interact", "running", "ready" );
	 * 
	 * Global states can also handle conditions the state cannot be in:
	 * myStateMachine.addGlobalStateCondition( can interact", "running", "!busy" );
	 * 
	 * ...
	 * @author Bennett Yeates
	 */
	public class StateMachine
	{		
		/** global states holds a specific state string, and the conditions to check for */
		private Dictionary<string, List<string>> globalStates = new Dictionary<string, List<string>>();
		
		/** the current list of states */
		protected List<State> states = new List<State>();

		/** the name of the state machine */
		protected string name;

		// =============================
		// CONST
		// =============================
		public const string READY = "ready";
		public const string BUSY = "busy";
		
		public StateMachine( string name = "State Machine" )
		{
			this.name = name;
			states = new List<State>();
			globalStates = new Dictionary<string, List<string>>();
		}
		
		/** destroy - cleanup the state machine */
		public void destroy()
		{
			states 			= new List<State>();
			globalStates   	= null;
			_currentState	= null;
			_previousState 	= null;
		}
		
		/** addState - creates a new state to be tracked later 
		 * 
		 * @param value - the state value, ie - "ready", "done", "busy"
		 * @param conditions - must have conditions array if passing a rule name, see State.as 
		 * */
		public void addState( string state, StateOptions stateOptions = null )
		{
			State newState = getState( state );
			if ( stateOptions != null )
			{
				newState.addRule( stateOptions.rules );
			}
			
			addOptions( newState, stateOptions );
		}
		
		/** updateState - sets the state to the passed value 
		 * 
		 * @param to - the new state
		 * @param stateOptions - values to set inside the state object, this can be things like
		 * "onStateChange" which is a function that fires when the state is changed to something else
		 * */
		public void updateState( string to, StateOptions options = null )
		{
			state = getState( to );
			addOptions( state, options );
			Common.logVerbose( "{0} ----> Updated State: {1}", name, state.stateName );
		}
		
		/** revertState - return the previous state */
		public void revertState()
		{
			state = _previousState;
			Common.logVerbose( "{0} ----> Reverted State: {1}", name, state.stateName );
		}
		
		/** can - returns true if the current state matches the value or contains the condition 
		 * 
		 * @param condition - the value or condition the state is in
		 * @return Boolean
		 * */
		public bool can( string condition )
		{
			return _currentState != null && ( _currentState.checkRules( condition ) || checkGlobalRules( condition ) );
		}
		
		/** addStateRule - adds a specified rule with conditions to be met 
		 * 
		 * @param conditions - a single or list of condition(s) that the state value has to be in
		 * */
		public void addStateRule( params string[] conditions )
		{
			List<string> rules = new List<string>();
			rules.AddRange(conditions);
			_currentState.addRule( rules );
		}
		
		/** addGlobalStateRule - adds a condition to be checked against any current state value 
		 * 
		 * @param state - the global state we could be in
		 * @param conditions - conditions that satisfies the state
		 * */
		public void addGlobalStateCondition( string state, params string[] conditions )
		{
			List<string> rules = new List<string>();
			rules.AddRange(conditions);
			globalStates[ state ] = rules;
		}
		
		/** checkGlobalRules - checks if the condition is in the global rules list 
		 * 
		 * @param condition - string value of the state
		 * */
		internal bool checkGlobalRules( string condition )
		{
			if ( globalStates != null && globalStates.ContainsKey( condition ) )
			{
				return State.evalRules( currentState, globalStates[ condition ] );
			}
			return false;
		}
		
		/** addOptions - parse through any values we want to set in the state 
		 * 
		 * @param to - the state to add options to
		 * @param stateOptions - analogous values to fields in State.as
		 * */
		private void addOptions( State to, StateOptions stateOptions = null )
		{
			if ( stateOptions != null )
			{
				to.onExit = stateOptions.onExit;
				to.onEnter = stateOptions.onEnter;
			}
		}
		
		/** getState - returns a state with the expected value, checks the condition 
		 * against the state value and state rules
		 * 
		 * @param condition - the state value to retrieve
		 * */
		protected State getState( string condition )
		{
			int length = states.Count;
			for ( int i = 0; i < length; ++i ) 
			{
				if ( states[ i ].checkRules( condition ) )
				{
					return states[ i ];
				}
			}
			State newState = new State( condition );
			states.Add( newState );
			return newState;
		}

		// ====================================================================================
		// GETTERS
		// ====================================================================================		
		private State _currentState;
		/** currentState - returns the current state value */
		public string currentState
		{
			get
			{
				if ( _currentState != null )
				{
					return _currentState.stateName;
				}
				return "";
			}
		}

		private State _previousState;		
		/** previouState - returns the previous state value */
		public string previousState
		{
			get
			{
				if ( _previousState != null )
				{
					return _previousState.stateName;
				}
				return "";
			}
		}
		
		/** state - returns the current state object */
		protected State state
		{
			get
			{
				return _currentState;
			}
			set
			{
				// change the current state to the previous state
				_previousState = state;
			
				// call the exit, we are leaving this state. _previousState may be null at first
				if ( _previousState != null ) { _previousState.exit(); }
			
				// update to the current state
				_currentState = value;
			
				// call the state onEnter function since it is now focused
				if ( _currentState != null ) { _currentState.enter(); }
			}
		}
	}
}
