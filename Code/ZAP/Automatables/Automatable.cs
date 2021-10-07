using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.Serialization;
using System.IO;

namespace Zap.Automation
{
#if UNITY_EDITOR && !ZYNGA_PRODUCTION
	using Newtonsoft.Json;

	public abstract class Automatable : ScriptableObject, ISerializable
	{
		public int testIndex = 0;
		public List<Test> tests = new List<Test>();
		public string key = "replace_this";
		public Test activeTest = null;
		public abstract IEnumerator startTests();
		public abstract void onTestsFinished();
		public abstract Test getNextTest();

		private AutomatableResult _automatableResult;
		public AutomatableResult automatableResult
		{
			get { return _automatableResult; }
			set { _automatableResult = value; }
		}

		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			throw new System.NotImplementedException();
		}

		public virtual void saveResult()
		{
			//Serialize Automatable result to file
			string jsonTest = JsonConvert.SerializeObject(_automatableResult, Formatting.Indented, new JsonSerializerSettings
			{
				TypeNameHandling = TypeNameHandling.All
			});

			string filename = key + ".json";
			string filePath = Path.Combine(
				Path.Combine(TestPlanResults.directoryPath, "Automatables"),
				Path.Combine(key, filename));

			Directory.CreateDirectory(TestPlanResults.directoryPath + "Automatables/");
			using (StreamWriter sw = new StreamWriter(filePath))
			{
				sw.Write(jsonTest);
			}
			testIndex = 0;
		}
	}
#endif
}
