using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if ZYNGA_TRAMP
public class TRAMPLogFiles
{	
	
#if ZYNGA_SKU_HIR
	public static readonly string SKU_NAME = "HIR";
#else
	public static readonly string SKU_NAME = "UNKNOWN_SKU"
#endif

	// Specifies what SKU and what type of file is being saved.
	public static readonly string JSON_FILE_NAME = SKU_NAME + "_{0}.json";
	public static readonly string TXT_FILE_NAME = SKU_NAME + "_{0}.txt";
	
	private static string _instances_directory = null;

	// Directory where TRAMP instances are stored, to reduce clutter.
	public static string INSTANCES_DIRECTORY
	{
		get
		{
			if (_instances_directory == null)
			{
				_instances_directory = combineWithTRAMPDirectory("Instance_" + AutomatedPlayer.instanceIndex + "/");

				// Create directory if it doesn't exist.
				if (!System.IO.Directory.Exists(_instances_directory))
				{				
					System.IO.Directory.CreateDirectory(_instances_directory);
				}
			}

			return _instances_directory;
		}
	}

	private static string _tramp_directory = null;
	public static string TRAMP_DIRECTORY
	{

		get 
		{
			if (string.IsNullOrEmpty(_tramp_directory))
			{
				_tramp_directory = Application.dataPath + "/../../TRAMP";
			}

			return _tramp_directory;
		}

		set
		{
			if (!System.IO.Directory.Exists(value))
			{
				System.IO.Directory.CreateDirectory(value);
			}
			
			_tramp_directory = value;
		}
	}

	// JSON files for saving/loading test status.
	private static string _ladi_file = null;
	public static string LADI_FILE
	{
		get
		{
			if (_ladi_file == null)
			{
				_ladi_file = getTRAMPFileName("LADI", JSON_FILE_NAME);
			}
			return _ladi_file;
		}
	}
	public static string _current_test_plan_file = null;
	public static string CURRENT_TEST_PLAN_FILE
	{
		get 
		{
			if (_current_test_plan_file == null)
			{
				_current_test_plan_file = getTRAMPFileName("CurrentTestPlan", JSON_FILE_NAME);
			}
			return _current_test_plan_file;
		}
	}

	private static string _complete_test_plan_file = null;
	public static string COMPLETE_TEST_PLAN_FILE
	{
		get 
		{
			if (_complete_test_plan_file == null)
			{
				_complete_test_plan_file = getTRAMPFileName("CompleteTestPlan", JSON_FILE_NAME);
			}
			return _complete_test_plan_file;
		}
	}

	// REGEX files for setting log sorting options.
	public static readonly string LADI_REGEX_FILE_DEFAULT = "LadiRegexList.json";
	private static string _ladi_regex_directory = null;
	public static string LADI_REGEX_DIRECTORY
	{
		get 
		{
			if (_ladi_regex_directory == null)
			{
				_ladi_regex_directory = combineWithTRAMPDirectory("LadiRegexList");
			}
			return _ladi_regex_directory;
		}
	}

	// Summary and detailed results files.
	private static string _summary_file = null;
	public static string SUMMARY_FILE
	{
		get 
		{
			if (_summary_file == null)
			{
				_summary_file = getTRAMPFileName("TestSummary", TXT_FILE_NAME);
			}
			return _summary_file;
		}
	}
	private static string _test_results_file = null;
	public static string TEST_RESULTS_FILE
	{
		get
		{
			if (_test_results_file == null)
			{
				_test_results_file = getTRAMPFileName("Results", TXT_FILE_NAME);
			}
			return _test_results_file;
		}
	}
	private static string _test_other_file = null; 
	public static string TEST_OTHER_FILE
	{
		get 
		{
			if (_test_other_file == null)
			{
				_test_other_file = getTRAMPFileName("OtherLogs", TXT_FILE_NAME);
			}
			return _test_other_file;
		}
	}

	// Color tags for debug messages.
	public static readonly string DEBUG_COLOR_START_TAG = string.Format("<color={0}>", AutomatedPlayer.TRAMP_DEBUG_COLOR);
	public static readonly string DEBUG_COLOR_END_TAG = "</color>";
	public static readonly string LADI_DEBUG_COLOR_START_TAG = string.Format("<color={0}>", AutomatedPlayerCompanion.LADI_DEBUG_COLOR);

	// Makes it easier to get file names for TRAMP files, given a name and a formatted extension.
	public static string getTRAMPFileName(string fileName, string extensionFormat)
	{

		// If there are multiple instances, put the file in the instances directory.
		if (AutomatedPlayer.areMultipleInstances)
		{
			return INSTANCES_DIRECTORY + string.Format(extensionFormat, fileName + "_" + AutomatedPlayer.instanceIndex);
		}
		else
		{
			return combineWithTRAMPDirectory(string.Format(extensionFormat, fileName));
		}
	}

	// This is used to capture messages that occur before, between and
	// after game tests.
	public static void logToOther(string message, params object[] args)
	{
		logToOther(string.Format(message, args));
	}

	// This is used to capture messages that occur before, between and
	// after game tests.
	public static void logToOther(string message)
	{
		// This removes the <color> clutter from message TRAMP submits.
		string cleaned = message.Replace(DEBUG_COLOR_START_TAG, "").Replace(DEBUG_COLOR_END_TAG, "");

		appendTextToFile(string.Format("{0:HH:mm:ss.fff} {1}\n", System.DateTime.Now, cleaned), TEST_OTHER_FILE);
	}

	// This removes the <color> clutter from message TRAMP submits.
	public static string cleanMessage(string message)
	{
		return message.Replace(DEBUG_COLOR_START_TAG, "").Replace(DEBUG_COLOR_END_TAG, "");
	}

	public static void appendTextToFile(string text, string fileName)
	{
		try
		{
			System.IO.File.AppendAllText(fileName, text);
		}
		catch (System.Exception e)
		{
			Debug.LogErrorFormat("<color={0}>TRAMP> Can't append test to {1} because {2}</color>", 
				AutomatedPlayer.TRAMP_DEBUG_COLOR, fileName, e);
		}
	}

	// Gets all the folders in the TRAMP directory, which should be test archives.
	public static string[] getArchivedTrampRunDirectories()
	{
		
		return System.IO.Directory.GetDirectories(getTRAMPDirectory());

	}

	// Loads a TRAMP run given a specific directory to load.
	public static void loadTRAMPRun(string directory)
	{

		// Loads the specific LADI and test run files.
		string ladiFilePath = directory + "/" + System.IO.Path.GetFileName(LADI_FILE);
		string testPlanPath = directory + "/" + System.IO.Path.GetFileName(CURRENT_TEST_PLAN_FILE);

		// Load TRAMP run by passing in specific files.
		if (System.IO.File.Exists(ladiFilePath) && System.IO.File.Exists(testPlanPath))
		{
			loadTRAMPRun(ladiFilePath, testPlanPath);
		}
		else
		{
			Debug.LogErrorFormat("Could not find ladi file or test plan in directory: {0}. Paths: {1} and {2}", directory, ladiFilePath, testPlanPath);
		}
	}

	// Given LADI and test plan files, load them as JSON files and load the TRAMP run.
	public static void loadTRAMPRun(string runDataPath, string testPlanDataPath)
	{

		loadTRAMPRun(new JSON(System.IO.File.ReadAllText(runDataPath)), new JSON(System.IO.File.ReadAllText(testPlanDataPath)));

	}

	// Given JSON data, load a previous TRAMP run.
	public static void loadTRAMPRun(JSON ladiData, JSON testPlanData)
	{
		AutomatedPlayerCompanion.instance.loadPastTest(ladiData, testPlanData);
	}

	// Loads the current active test plan and populates games to test queue.
	public static bool loadCurrentTestPlan()
	{
		try
		{
			if (System.IO.File.Exists(TRAMPLogFiles.CURRENT_TEST_PLAN_FILE) &&
				AutomatedPlayerCompanion.instance != null)
			{
				JSON json = new JSON(System.IO.File.ReadAllText(TRAMPLogFiles.CURRENT_TEST_PLAN_FILE));

				AutomatedPlayerCompanion.instance.loadTestPlanFromJSON(json);

				return true;
			}
		}
		catch (System.Exception e)
		{
			Debug.LogErrorFormat("<color={0}>TRAMP> Can't read Current Test Plan from {1} because {2}</color>", 
				AutomatedPlayer.TRAMP_DEBUG_COLOR, TRAMPLogFiles.CURRENT_TEST_PLAN_FILE, e);
		}

		return false;
	}

	// Saves the current test plan to file.
	public static bool saveCurrentTestPlan()
	{

		try
		{
			if (AutomatedPlayer.instance != null && AutomatedPlayer.instance.companion != null)
			{
				if (AutomatedPlayerCompanion.instance != null && AutomatedPlayerCompanion.instance.isCurrentTest)
				{
					System.IO.File.WriteAllText(CURRENT_TEST_PLAN_FILE, AutomatedPlayer.instance.companion.testPlanToJSON());
				}

				return true;
			}
		}
		catch (System.Exception e)
		{
			Debug.LogErrorFormat("<color={0}>TRAMP> Can't save Current Test Plan to {1} because {2}</color>", 
				AutomatedPlayer.TRAMP_DEBUG_COLOR, CURRENT_TEST_PLAN_FILE, e);
		}

		return false;
	}

	// Saves the companion data to file.
	public static void saveCompanionToFile()
	{
		// Don't save if there's no companion or if there's past data loaded
		if (AutomatedPlayerCompanion.instance != null && AutomatedPlayerCompanion.instance.isCurrentTest)
		{
			try
			{
				string companionAsString = AutomatedPlayerCompanion.instance.ToJSON();
				System.IO.File.WriteAllText(LADI_FILE, companionAsString);
			}
			catch (System.Exception e)
			{
				Debug.LogErrorFormat(AutomatedPlayerCompanion.FAILED_TO_SAVE, AutomatedPlayerCompanion.LADI_DEBUG_COLOR, LADI_FILE, e);
			}
		}
	}

	// Loads the companion from file.
	public static void loadCompanionFromFile()
	{
		if (AutomatedPlayerCompanion.instance != null)
		{
			// First, check and make sure that the file actually exists or is created
			if (!System.IO.File.Exists(TRAMPLogFiles.LADI_FILE))
			{
				System.IO.File.Create(TRAMPLogFiles.LADI_FILE).Close();
			}

			// Then, try to read from the file.
			try
			{
				string loadedFile = System.IO.File.ReadAllText(LADI_FILE);

				// If we were able to read from a valid file, load the JSON data from that file.
				if (!string.IsNullOrEmpty(loadedFile))
				{
					JSON json = new JSON(loadedFile);
					AutomatedPlayerCompanion.instance.loadJSON(json);
				}
			}
			catch (System.Exception e)
			{
				Debug.LogErrorFormat(AutomatedPlayerCompanion.FAILED_TO_LOAD, AutomatedPlayerCompanion.LADI_DEBUG_COLOR, LADI_FILE, e);
			}
		}
	}

	// Saves all relevant files - called whenever we want to ensure no data is lost.
	public static void saveAllFiles()
	{
		// Only write to these files if the current test is an active test, not an archived test.
		if (AutomatedPlayerCompanion.instance != null && AutomatedPlayerCompanion.instance.isCurrentTest)
		{
			saveCurrentTestPlan();
			saveCompanionToFile();
			saveTestSummary();
		}
	}

	// Finishes the test plan. Called whenever testing is done or when the test is force ended.
	// This will archive all results and place them in a separate folder with timestamp.
	public static void completeTestPlan()
	{
		try
		{
			if (AutomatedPlayer.instance != null && AutomatedPlayerCompanion.instance != null && AutomatedPlayerCompanion.instance.isCurrentTest)
			{
				AutomatedPlayerCompanion.instance.endActiveGameLog(AutomatedPlayer.instance.getGameMode());

				// Write the completed test plan (Games tested).
				System.IO.File.WriteAllText(COMPLETE_TEST_PLAN_FILE, AutomatedPlayerCompanion.instance.testPlanToJSON());
			}
		}
		catch (System.Exception e)
		{
			Debug.LogErrorFormat("<color={0}>TRAMP> Can't save Current Test Plan to {1} because {2}</color>", 
				AutomatedPlayer.TRAMP_DEBUG_COLOR, COMPLETE_TEST_PLAN_FILE, e);
		}


		if (AutomatedPlayerCompanion.instance != null && AutomatedPlayerCompanion.instance.isCurrentTest)
		{

			// Archives all files into a folder.
			archiveResults();
		}
	}

	// Saves all relevant files into a timestamped folder, which can be used later to resume testing or view results.
	public static void archiveResults()
	{
		// Get the name of the archive to save.
		string directoryName = getArchiveDirectory();
		string archiveDirectory = combineWithTRAMPDirectory(directoryName);

		if (AutomatedPlayer.areMultipleInstances)
		{
			archiveDirectory += "_" + AutomatedPlayer.instanceIndex;
		}

		System.IO.Directory.CreateDirectory(archiveDirectory);

		if (!AutomatedPlayer.areMultipleInstances)
		{
			// Move all the test files into the archive directory.
			moveFile(TEST_RESULTS_FILE, System.IO.Path.Combine(archiveDirectory, System.IO.Path.GetFileName(TEST_RESULTS_FILE)));
			moveFile(LADI_FILE, System.IO.Path.Combine(archiveDirectory, System.IO.Path.GetFileName(LADI_FILE)));
			moveFile(TEST_OTHER_FILE, System.IO.Path.Combine(archiveDirectory, System.IO.Path.GetFileName(TEST_OTHER_FILE)));
			moveFile(SUMMARY_FILE, System.IO.Path.Combine(archiveDirectory, System.IO.Path.GetFileName(SUMMARY_FILE)));
			moveFile(COMPLETE_TEST_PLAN_FILE, System.IO.Path.Combine(archiveDirectory,  System.IO.Path.GetFileName(COMPLETE_TEST_PLAN_FILE)));
			moveFile(CURRENT_TEST_PLAN_FILE, System.IO.Path.Combine(archiveDirectory,  System.IO.Path.GetFileName(CURRENT_TEST_PLAN_FILE)));
		}
		else
		{
			string newLocation = System.IO.Path.Combine(archiveDirectory, "Instance_" + AutomatedPlayer.instance);
			System.IO.Directory.Move(INSTANCES_DIRECTORY, newLocation);
		}
	}

	// Gets the current directory that test results should go into once archived.
	private static string getArchiveDirectory()
	{
		// Get the git branch.
		string branchName = AutomatedPlayerProcesses.getBranchName();

		// Slashes are used in paths and we don't want them in file names. Can cause issues when parsing.
		branchName = branchName.Replace('/', '-');

		// Format the current timestamp and save the file.
		return string.Format("{1:MM-dd-yy_hh-mmtt}_{0}", branchName, System.DateTime.Now);
	}

	// Given a directory, get the actual name of the archive (reformat it).
	public static string getArchiveNameFromDirectory(string directoryName)
	{
		// By default, we use underscores to separate the different parts of a test directory name.
		string[] pathParts = directoryName.Split('_');

		// Unrecognized or can't be properly parsed. Just return the name.
		if (pathParts.Length < 3)
		{
			Debug.LogWarningFormat("Unrecognized test name format {0}. Just using the directory name.", directoryName);
			return directoryName;
		}

		// Break the directory path into pieces,
		string date = pathParts[0];
		string time = pathParts[1];
		string branch = pathParts[2];

		// Branch name may contain underscores, so just rebuild the full branch name.
		for (int i = 3; i < pathParts.Length; i++)
		{
			branch += "_" + pathParts[i];
		}

		// Make the directory name readable from it's file version.
		date = date.Replace('-', '/'); // Dates should have slashes. MM/DD/YYYY
		time = time.Replace('-', ':'); // Times should have colons. hh:mmtt
		branch = branch.Replace('-', '/'); // Branches should use slashes. tramp/example_branch

		// Create the final name to show.
		// i.e. MM/DD/YY hh:mmtt tramp/example_branch
		directoryName = date + " " + time + " " + branch;
		return directoryName;
	}

	// Given an archive name, convert it to something more directory friendly.
	public static string getArchiveDirectoryFromName(string archiveName)
	{ 
		// Lots of characters can't be directory names, just make them dashes. 
		archiveName = archiveName.Replace('/', '-');
		archiveName = archiveName.Replace(':', '-');
		return archiveName;
	}

	// Deletes all the active files.
	public static void resetResults(bool shouldArchiveFirst)
	{

		if (shouldArchiveFirst)
		{
			archiveResults();
		}

		deleteFile(TEST_RESULTS_FILE);
		deleteFile(TEST_OTHER_FILE);
		deleteFile(SUMMARY_FILE);
		deleteFile(CURRENT_TEST_PLAN_FILE);
		deleteFile(LADI_FILE);
		deleteFile(CURRENT_TEST_PLAN_FILE);
	}

	// Deletes a specific file.
	private static void deleteFile(string fileName)
	{
		try
		{
			System.IO.File.Delete(fileName);
		}
		catch (System.Exception e)
		{
			Debug.LogErrorFormat("<color={0}>TRAMP> Can't delete {1} because {2}</color>", 
				AutomatedPlayer.TRAMP_DEBUG_COLOR, fileName, e);
		}
	}

	// Copies a file to a new location.
	private static void copyFile(string sourceFileName, string destinationFileName)
	{
		try
		{
			System.IO.File.Copy(sourceFileName, destinationFileName, true);
		}
		catch (System.Exception e)
		{
			Debug.LogErrorFormat("<color={0}>TRAMP> Can't copy {1} to {2} because {3}</color>", 
				AutomatedPlayer.TRAMP_DEBUG_COLOR, sourceFileName, destinationFileName, e);
		}
	}

	// Moves a file to a new location.
	private static void moveFile(string sourceFileName, string destinationFileName)
	{
		if (System.IO.File.Exists(sourceFileName))
		{
			try
			{
				System.IO.File.Move(sourceFileName, destinationFileName);
			}
			catch (System.Exception e)
			{
				Debug.LogErrorFormat("<color={0}>TRAMP> Can't move {1} to {2} because {3}</color>", 
					AutomatedPlayer.TRAMP_DEBUG_COLOR, sourceFileName, destinationFileName, e);
			}
		}
	}

	public static void prefixToFile(string prefix, string fileName)
	{
		try
		{
			string originalText = "\n\n--- REPORT MISSING ---";
			if (System.IO.File.Exists(fileName))
			{
				originalText = System.IO.File.ReadAllText(fileName);

				System.IO.File.Delete(fileName);
			}

			System.IO.File.WriteAllText(fileName, string.Format("{0}{1}", prefix, originalText));
		}
		catch (System.Exception e)
		{
			Debug.LogErrorFormat("<color={0}>TRAMP> Can't prefix text to {1} because {2}</color>",
				AutomatedPlayer.TRAMP_DEBUG_COLOR, fileName, e);
		}
	}

	// Save a text summary of this TRAMP run.
	public static void saveTestSummary()
	{
		if (AutomatedPlayerCompanion.instance != null)
		{
			try
			{

				// Save a string of the companion class.
				string testSummary = AutomatedPlayerCompanion.instance.ToString();
				System.IO.File.WriteAllText(SUMMARY_FILE, testSummary);
			}

			catch(System.Exception e)
			{
				Debug.LogError("Failed to save test summary: " + e.ToString());
			}
		}
	}

	public static string getTRAMPDirectory()
	{
		//string directory = System.IO.Path.Combine(Application.persistentDataPath, "TRAMP");
		string directory = TRAMP_DIRECTORY;
		try
		{
			if (!System.IO.Directory.Exists(directory))
			{
				System.IO.Directory.CreateDirectory(directory);
			}
		}
		catch (System.Exception e)
		{
			Debug.LogErrorFormat("<color={0}>TRAMP> Can't read create directory {1} because {2}</color>", 
				AutomatedPlayer.TRAMP_DEBUG_COLOR, directory, e);
		}

		return directory;
	}

	// Combines the specified file with the TRAMP directory path.
	private static string combineWithTRAMPDirectory(string filename)
	{
		return System.IO.Path.Combine(getTRAMPDirectory(), filename);
	}
}
#endif