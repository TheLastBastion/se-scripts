//Setup Guide:
//1. Create a group of blocks named [REC]. The group can contain all types of blocks but only timers will be used.
//2. Load this script into a programable block.
//2.1 Maybe edit the values in the configuration section to your needs.
//3. Compile script
//4. Youre finished. Enjoy your basic receiver system.

//Configuration Section
String[] filter = { "ALARM", "CaKePaRtY" };     //Enter messages to be filtered. Messages are not case sensitive!
                                                //To disable filter, remove all entries.
String GROUP_TAG = "[REC]";						//Group Tag. Basically Name of the Blockgroup that is going to be used.


//No Touchy zone
String SCRIPT_TAG = "[RECEIVER]";
List<IMyTimerBlock> timerList = new List<IMyTimerBlock>();
Boolean findblocksError = false;

public Program()
{
    if (!Me.CustomName.Contains(SCRIPT_TAG))
    {
        Me.CustomName += " " + SCRIPT_TAG;
    }

}

public void Save()
{

}

public void Main(string argument, UpdateType updateSource)
{
    if (findblocksError)
    {
        Echo("Error. Please Recompile with working group TAG!");
        return;
    }
    if(String.IsNullOrEmpty(argument) || String.IsNullOrWhiteSpace(argument))
    {
        return;
    }
    if (isStringInFilter(argument))
    {
        foreach (var timer in timerList)
        {
            timer?.Trigger();
        }
    }
}

void findBlocks()
{
    IMyBlockGroup blockGroup = GridTerminalSystem.GetBlockGroupWithName(GROUP_TAG);
    if (blockGroup == null)
    {
        findblocksError = true;
        return;
    }
    blockGroup.GetBlocksOfType<IMyTimerBlock>(timerList, x => x.IsFunctional);
}

Boolean isStringInFilter(String s)
{
    if (filter.Count() == 0)
    {
        return true;
    }
    String upperCase = s.ToUpperInvariant();
    foreach (var entry in filter)
    {
        if (entry.ToUpperInvariant().Equals(upperCase))
        {
            return true;
        }
    }
    return false;
}
