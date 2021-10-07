using System.Collections.Generic;

public class PersonalizedContentExperiment : EosExperiment
{
	private Dictionary<string, string> recommendedGames;
	private Dictionary<string, string> favoriteGames;

	private const int MAX_NUMBER_OF_RECOMMENDED_GAMES = 6;
	
	private const int MAX_NUMBER_OF_FAVORITE_GAMES = 6;
	
	public PersonalizedContentExperiment(string name) : base(name)
	{
		recommendedGames = new Dictionary<string, string>();
		favoriteGames = new Dictionary<string, string>();
	}

	protected override void init(JSON data)
	{
		recommendedGames.Clear();

		for (int i = 0; i < MAX_NUMBER_OF_RECOMMENDED_GAMES; i++)
		{
			string variableName = DoSomething.RECOMMENDED_GAME_PREFIX + "_" + (i + 1).ToString();
			string gameName = getEosVarWithDefault(data, variableName, "");
			recommendedGames[variableName] = gameName;
		}
		
		favoriteGames.Clear();
		for (int i = 0; i < MAX_NUMBER_OF_FAVORITE_GAMES; i++)
		{
			string variableName = DoSomething.FAVORITE_GAME_PREFIX + "_" + (i + 1).ToString();
			string gameName = getEosVarWithDefault(data, variableName, "");
			favoriteGames[variableName] = gameName;
		}
	}

	public override void reset()
	{
		base.reset();

		if (recommendedGames != null)
		{
			recommendedGames.Clear();
		}

		if (favoriteGames != null)
		{
			favoriteGames.Clear();
		}
	}

	public string getRecommendedGameName(string index)
	{
		string variableName = DoSomething.RECOMMENDED_GAME_PREFIX + "_" + index;
		string gameName = "";
		if (recommendedGames.TryGetValue(variableName, out gameName))
		{
			return gameName;
		}

		return "";
	}
	
	public string getFavoriateGameName(string index)
	{
		string variableName = DoSomething.FAVORITE_GAME_PREFIX + "_" + index;
		string gameName = "";
		if (favoriteGames.TryGetValue(variableName, out gameName))
		{
			return gameName;
		}

		return "";
	}
	
}
