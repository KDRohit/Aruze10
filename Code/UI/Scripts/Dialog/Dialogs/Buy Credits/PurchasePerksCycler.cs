using System.Collections.Generic;

public class PurchasePerksCycler
{
    public List<PurchasePerksPanel> panelsToCycle = new List<PurchasePerksPanel>();

    private List<int> cycleDelays;
    private int cycleCount = 0;
    private int currentIndex = 0;
    private GameTimerRange cycleTimer;

    private const int defaultDelay = 3;
    private bool pauseOnRestart = false;

    public PurchasePerksCycler(List<int> cycleDelays, int cycleCount)
    {
        this.cycleDelays = cycleDelays;
        this.cycleCount = cycleCount;
    }


    public void startCycling()
    {
        if (cycleCount > 1)
        {
            if (cycleTimer == null)
            {
                //Create a new timer if cycleTimer is null
                startNewCyclerTimer();
            }
            else
            {
                pauseOnRestart = false;
                //Unpause timer if its paused, or else start a new one if the current one is expired
                //Don't do anything if our timer is already running
                if (cycleTimer.startTimer.isPaused)
                {
                    cycleTimer.unPauseTimers();
                }
                else if (cycleTimer.isExpired)
                {
                    startNewCyclerTimer();
                }
            }
        }
    }

    public void pauseCycling()
    {
        if (cycleTimer != null)
        {
            if (cycleTimer.isExpired)
            {
                pauseOnRestart = true;
            }
            else
            {
                cycleTimer.pauseTimers();
            }
        }
    }

    private void moveToNextPanelType(Dict args, GameTimerRange caller)
    {
        if (pauseOnRestart)
        {
            //Tried to pause the timer as it expired. Don't cycle items
            return;
        }

        if (panelsToCycle != null && panelsToCycle.Count > 0)
        {
            int nextIndex = currentIndex + 1 >= cycleCount ? 0 : currentIndex + 1;
            for (int i=0; i<panelsToCycle.Count; i++)
            {
                PurchasePerksPanel panel = panelsToCycle[i];
                if (panel == null || panel.gameObject == null)
                {
                    return; //Return if the panel was destroyed prior to timer expiring
                }
                panel.swapPerks(currentIndex, nextIndex);
            }

            currentIndex = nextIndex;
            startNewCyclerTimer();    
        }   
    }

    private void startNewCyclerTimer()
    {
        int delay = cycleDelays != null && currentIndex < cycleDelays.Count
            ? cycleDelays[currentIndex]
            : defaultDelay;
        
        cycleTimer = GameTimerRange.createWithTimeRemaining(delay);
        cycleTimer.registerFunction(moveToNextPanelType);

        if (pauseOnRestart)
        {
            cycleTimer.pauseTimers();
        }
    }
    
}
