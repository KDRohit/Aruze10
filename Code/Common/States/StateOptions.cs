using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Com.States
{
	public class StateOptions
	{
		public GenericDelegate onExit;
		public GenericDelegate onEnter;
		
		public List<string> rules = new List<string>();
		
		public StateOptions(List<string> rules = null, GenericDelegate onExit = null, GenericDelegate onEnter = null)
		{
			this.onExit = onExit;
			this.onEnter = onEnter;
			this.rules = rules;
		}
	}
}
