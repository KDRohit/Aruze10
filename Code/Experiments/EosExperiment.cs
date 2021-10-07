using System.CodeDom;
using System.Reflection;
using UnityEngine;
using Zynga.Core.Util;

public class EosExperiment
{	
	public string experimentName { get; private set; }

	public string variantName { get; private set; }
	
	public bool isEnabled { get; protected set; }
	private bool hasReadData = false;
	
	private int hashCode = 0;

	public EosExperiment(string name)
	{
		experimentName = name;
		reset();
	}

	// populate data for EOS experiment without checking data has been read
	private void populateHelper(JSON data)
	{
		if (data != null)
		{
			hashCode = data.ToString().GetHashCode();
			isEnabled = getEosVarWithDefault(data, "enabled", false);
			variantName = getEosVarWithDefault(data, "variant", "");
			initEosProperties(data);
			init(data);
			hasReadData = true;
		}
	}
	
	public void populateAll(JSON data, bool forceUpdate = false)
	{
		if (forceUpdate || !hasReadData)
		{
			populateHelper(data);
		}
		else 
		{
			Debug.LogError("Attempting to read data multiple times for eos experiment: " + experimentName);
		}
	}

	
	protected string getEosVar(JSON data, string name)
	{
		return data.getString(name, "");
	}

	protected static bool getEosVarWithDefault(JSON data, string name, bool defaultValue)
	{
		string value = data.getString(name, "");
		if (!string.IsNullOrEmpty(value))
		{
			if (value.Length > 0)
			{
				switch (value[0])
				{
					case '0':
					case 'f':
					case 'F':
						return false;

					case '1':
					case '2':
					case '3':
					case '4':
					case '5':
					case '6':
					case '7':
					case '8':
					case '9':
					case 't':
					case 'T':
						return true;
				}
			}
		}

		return defaultValue;
	}
		
	protected static float getEosVarWithDefault(JSON data, string name, float defaultValue)
	{
		string eosData = data.getString(name, "");
		if (!string.IsNullOrEmpty(eosData) && float.TryParse(eosData, out float value))
		{
			return value;
		}
		
		return defaultValue;
	}

	protected static int getEosVarWithDefault(JSON data, string name, int defaultValue)
	{
		string value = data.getString(name, "");
		if (!string.IsNullOrEmpty(value))
		{
			if (value.Contains('.'))
			{
				// If there is a decimal point, only use the part before it,
				// since int.TryParse() fails on decimal strings.
				value = value.Substring(0, value.IndexOf('.'));
			}
			
			if (int.TryParse(value, out int integerValue))
			{
				return integerValue;
			}
		}
		return defaultValue;
	}

	protected static long getEosVarWithDefault(JSON data, string name, long defaultValue)
	{
		string value = data.getString(name, "");
		if (!string.IsNullOrEmpty(value))
		{
			if (value.Contains('.'))
			{
				// If there is a decimal point, only use the part before it,
				// since int.TryParse() fails on decimal strings.
				value = value.Substring(0, value.IndexOf('.'));
			}
			
			if (long.TryParse(value, out long longValue))
			{
				return longValue;
			}
		}
		return defaultValue;
	}		

	protected static string getEosVarWithDefault(JSON data, string name, string defaultValue)
	{
		if (data == null)
		{
			return defaultValue;
		}
		
		string value = data.getString(name, "");
		if (!string.IsNullOrEmpty(value))
		{
			return value;
		}

		return defaultValue;
	}

	protected virtual void init(JSON data)
	{

	}

	//Uses reflection -- only use in dev menu
	public int getVariableCount()
	{
		System.Reflection.PropertyInfo[] props = this.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
		if (props == null)
		{
			return 0;
		}

		return props.Length;
	}

	//Uses reflection -- only use in dev menu
	public string getVariableName(int index)
	{
		System.Reflection.PropertyInfo[] props = this.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
		if (props == null || props.Length <= index)
		{
			return "";
		}
		else if (props[index] == null)
		{
			return "";
		}
		return props[index].Name;
	}

	//Uses reflection, only use in dev menu
	public string getVariableValue(int index)
	{
		System.Reflection.PropertyInfo[] props = this.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
		if (props == null || props.Length <= index)
		{
			return "";
		}
		else if (props[index] == null)
		{
			return "";
		}
		object value = props[index].GetValue(this, null);
		return value == null ? "" : value.ToString();
	}
	
	protected int getHashCode()
	{
		return hashCode;
	}

	public virtual void reset()
	{
		isEnabled = false;
		variantName = "";
		hasReadData = false;
		resetEosProperties();
	}

	public virtual bool isInExperiment
	{
		get
		{
			return hasReadData && isEnabled;
		}
	}

	private void initEosProperties(JSON data)
	{
		PropertyInfo[] props = this.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

		for (int i = 0; i < props.Length; ++i)
		{
			EosAttribute attribute = System.Attribute.GetCustomAttribute(props[i], typeof(EosAttribute), true) as EosAttribute;
			if (attribute != null)
			{
				switch (props[i].PropertyType.Name.ToLower())
				{
					case "int32":
						props[i].SetValue(this, getEosVarWithDefault(data, attribute.name, (int)attribute.defaultValue));
						break;
						
					case "bool":
						props[i].SetValue(this, getEosVarWithDefault(data, attribute.name, (bool)attribute.defaultValue));
						break;
					
					case "float":
						props[i].SetValue(this, getEosVarWithDefault(data, attribute.name, (float)attribute.defaultValue));
						break;
					
					case "int64":
						props[i].SetValue(this, getEosVarWithDefault(data, attribute.name, (long)attribute.defaultValue));
						break;
					
					case "string":
						props[i].SetValue(this, getEosVarWithDefault(data, attribute.name, (string)attribute.defaultValue));
						break;
					
					default:
						Debug.LogError("Unsupported eos property type: " + props[i].PropertyType.Name);
						break;
				}
				
			}
		}
	}

	private void resetEosProperties()
	{
		PropertyInfo[] props = this.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

		for (int i = 0; i < props.Length; ++i)
		{
			EosAttribute attribute = System.Attribute.GetCustomAttribute(props[i], typeof(EosAttribute), true) as EosAttribute;
			if (attribute != null)
			{
				props[i].SetValue(this, attribute.defaultValue);
			}
		}
	}
	
}
