
using System.Collections.Generic;

// Mission that has a start time, and end time that are used instead of the challenge campaigns start and end time
public class SeasonMission : Mission
{
    public int startTime { get; private set; }
    public int endTime { get; private set; }
    public int id { get; private set; }
    
    public SeasonMission(JSON data) : base(data)
    {
        
    }
    
    public bool isActive
    {
        get
        {
            return GameTimer.currentTime >= startTime && (endTime < 0 || endTime >= GameTimer.currentTime);    
        }
    }

    private int missionId;
    
    
    public override void init(JSON data)
    {
        objectives = new List<Objective>();
        gameObjectives = new Dictionary<string, List<Objective>>();
        rewards = null;
        dialogStateData = null;
        
        if (data != null)
        {
            startTime = data.getInt("start_time", System.Int32.MaxValue);
            endTime = data.getInt("end_time", -1);
            id = data.getInt("id", 0);
            parseObjectives(data.getJsonArray("challenges"));
        }
    }
}
