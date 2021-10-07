using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;

public class UILabelToWrapper : ScriptableWizard 
{
	public TextAsset scriptToModify;
	
	private const string PROPERTY_SEARCH = "public UILabel ";
	private const string PROPERTY_SEARCH_ARRAY = "public UILabel[] ";
	private const string PROPERTY_SEARCH_LIST = "public List<UILabel> ";
	private const string PROPERTY_SEARCH_PRIVATE = "private UILabel ";
	private const string PROPERTY_SEARCH_PRIVATE_ARRAY = "private UILabel[] ";
	private const string PROPERTY_SEARCH_PRIVATE_LIST = "private List<UILabel> ";
	private const string SUFFIX_TEMP = "_suffix" + "Temp";	// Split it so when we do a global search and replace, this isn't replaced.
	
	private const string TEMPLATE = @"
	public LabelWrapper {0}Wrapper
	{{
		get
		{{
			if (_{0}Wrapper == null)
			{{
				if ({0}WrapperComponent != null)
				{{
					_{0}Wrapper = {0}WrapperComponent.labelWrapper;
				}}
				else
				{{
					_{0}Wrapper = new LabelWrapper({0}" + SUFFIX_TEMP + @");
				}}
			}}
			return _{0}Wrapper;
		}}
	}}
	private LabelWrapper _{0}Wrapper = null;
	";

	private const string TEMPLATE_ARRAY = @"
	public List<LabelWrapper> {0}Wrapper
	{{
		get
		{{
			if (_{0}Wrapper == null)
			{{
				_{0}Wrapper = new List<LabelWrapper>();

				if ({0}WrapperComponent != null && {0}WrapperComponent.Length > 0)
				{{
					foreach (LabelWrapperComponent wrapperComponent in {0}WrapperComponent)
					{{
						_{0}Wrapper.Add(wrapperComponent.labelWrapper);
					}}
				}}
				else
				{{
					foreach (UILabel label in {0}" + SUFFIX_TEMP + @")
					{{
						_{0}Wrapper.Add(new LabelWrapper(label));
					}}
				}}
			}}
			return _{0}Wrapper;
		}}
	}}
	private List<LabelWrapper> _{0}Wrapper = null;	
	";


	[MenuItem("Zynga/Editor Tools/UILabel to Wrapper")]
	public static void openDialogEditor()
	{
		ScriptableWizard.DisplayWizard<UILabelToWrapper>("UILabel to Wrapper", "Close", "Modify Script");
	}
		
	public void OnWizardOtherButton()
	{
		if (scriptToModify == null)
		{
			return;
		}
		
		StringBuilder newScript = new StringBuilder();
		string[] lines = scriptToModify.text.Split('\n');
		
		foreach (string line in lines)
		{
			string template = "";
			string labelName = line;
			if (labelName.Contains("[SerializeField]"))
			{
				labelName = labelName.Replace("[SerializeField]", "");
			}
			labelName = labelName.Trim();
			
			if (labelName.Contains(PROPERTY_SEARCH))
			{
				template = TEMPLATE;
				labelName = labelName.Substring(PROPERTY_SEARCH.Length);
			}
			else if (line.Contains(PROPERTY_SEARCH_PRIVATE))
			{
				template = TEMPLATE;
				labelName = labelName.Substring(PROPERTY_SEARCH_PRIVATE.Length);
			}
			else if (line.Contains(PROPERTY_SEARCH_ARRAY))
			{
				template = TEMPLATE_ARRAY;
				labelName = labelName.Substring(PROPERTY_SEARCH_ARRAY.Length);
			}
			else if (line.Contains(PROPERTY_SEARCH_PRIVATE_ARRAY))
			{
				template = TEMPLATE_ARRAY;
				labelName = labelName.Substring(PROPERTY_SEARCH_PRIVATE_ARRAY.Length);
			}
			else if (line.Contains(PROPERTY_SEARCH_LIST))
			{
				template = TEMPLATE_ARRAY;
				labelName = labelName.Substring(PROPERTY_SEARCH_LIST.Length);
			}
			else if (line.Contains(PROPERTY_SEARCH_PRIVATE_LIST))
			{
				template = TEMPLATE_ARRAY;
				labelName = labelName.Substring(PROPERTY_SEARCH_PRIVATE_LIST.Length);
			}
			
			if (template != "")
			{
				labelName = labelName.Replace(" ", "");
			
				if (labelName.Contains('='))
				{
					labelName = labelName.Substring(0, labelName.IndexOf('='));
				}
				else if (labelName.Contains(';'))
				{
					labelName = labelName.Substring(0, labelName.IndexOf(';'));
				}
							
				string oldLine = line;
				bool hasComment = oldLine.Contains("//");
				oldLine += (hasComment ? " - " : "	//") + " To be removed when prefabs are updated.";
				oldLine = oldLine.Replace(labelName, labelName + SUFFIX_TEMP);	// Temporary add a suffix to force compiler errors to help us fine uses of the old variable.
				newScript.AppendLine(oldLine);

				string newLine = line.Replace(labelName, labelName + "WrapperComponent");
				newLine = newLine.Replace("UILabel", "LabelWrapperComponent");
				
				newScript.AppendLine(newLine);
				newScript.AppendLine(string.Format(template, labelName));
			}
			else
			{
				newScript.AppendLine(line);
			}
		}
		
		string scriptPath = Application.dataPath + "/../" + AssetDatabase.GetAssetPath(scriptToModify);
		
		System.IO.File.WriteAllText(scriptPath, newScript.ToString());
		
		AssetDatabase.Refresh();
	}
}
