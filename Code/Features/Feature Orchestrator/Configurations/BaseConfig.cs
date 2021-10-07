using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseConfig
{
	public string keyName { get; private set; }
	
	public string className { get; private set; }
	
	public Dictionary<string, object> properties { get; private set; }

	public JSON json { get; private set; }

	public BaseConfig(string keyName, string className, Dictionary<string, object> properties, JSON json)
	{
		this.keyName = keyName;
		this.className = className;
		this.properties = properties;
		this.json = json;
	}
}
