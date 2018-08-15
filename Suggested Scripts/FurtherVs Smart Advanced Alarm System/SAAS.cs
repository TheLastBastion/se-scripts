//Setup Guide:
//1. Create a group of blocks named [SAAS]. The group can contain all types of blocks but only lights, turrets, timers with a specific suffix and antennas (laser antennas too!) will be used.
//1.1. Timers to be triggered on alarm start need to contain the word START in caps.
//1.2. Timers to be triggered on alarm end need to contain the word STOP in caps.
//2. Load this script into a programable block.
//2.1 Maybe edit the values in the configuration section to your needs.
//3. Compile script
//4. Youre finished. Enjoy your alarm system!

//Configuration Section
string GROUP_NAME = "[SAAS]";       //Name of the block group that is going to be used.
int activeTime = 30;                //Time after the alarm stops if no new threat is detected.
Boolean enableAllTurrets = true;    //Should all turrets in the block group be turned on when the alarm starts?
Boolean silentTransmit = true;      //Should all antennas in the block group send a silent message to all allied and owned receivers when the alarm starts?
Boolean useLights = true;           //Should all lights in the block group be turned on and painted red when the alarm starts?
string toSend = "ENEMYATGATE";      //Message to be send by the silent transmit

//No Tochy Zone
string SCRIPT_TAG = "[SAAS]";

int blockCount = 0;
int runCount = 0;
int runCountOnTrigger = -1;

double lastRun = 2;

int messageSend = 0;
bool onAlert = false;
bool findBlocksError = false;
bool debug = false;

IMyBlockGroup blockGroup;
List<IMyLargeConveyorTurretBase> turretList;
List<IMyLightingBlock> lightList;
List<IMyTimerBlock> timerStartList;
List<IMyTimerBlock> timerStopList;
List<IMyRadioAntenna> radioAntennaList;
List<IMyLaserAntenna> laserAntennaList;

public Program()
{
    if (!Me.CustomName.Contains(SCRIPT_TAG))
    {
        Me.CustomName += " " + SCRIPT_TAG;
    }
    findBlocks();

    Runtime.UpdateFrequency = UpdateFrequency.Update10;
}

public void Save()
{

}

public void Main(string argument)
{
    if (findBlocksError)
    {
        return;
    }


    //High Priority Antenna Check
    if (messageSend > 0)
    {
        /*if (messageSend == 1)
        {
            messageSend++;
            foreach (var radioAntenna in radioAntennaList)
            {
                if (radioAntenna == null) continue;
                //debug = radioAntenna.TransmitMessage(toSend, MyTransmitTarget.Default);
            }
           // return;
        }*/
        if (messageSend <= 4)
        {
            messageSend++;
            return;
        }
        foreach (var radioAntenna in radioAntennaList)
        {
            if (radioAntenna == null) continue;
            //radioAntenna.Enabled = false;
            //radioAntenna.EnableBroadcasting = false;
            radioAntenna?.TransmitMessage(toSend, MyTransmitTarget.Default);
            radioAntenna.Radius = 1;
        }
        messageSend = 0;
        findBlocks();
        Runtime.UpdateFrequency = UpdateFrequency.Update10;
        return;
    }

    //Cooldown Check
    if (Runtime.TimeSinceLastRun.TotalSeconds + lastRun < 1)
    {
        lastRun += Runtime.TimeSinceLastRun.TotalSeconds;
        Echo($"Cooldown: {lastRun}\nAlarm: {onAlert}\nDebug: {debug}");
        return;
    }

    //Reset Last Run
    lastRun = 0;

    //Run every tenth iteration. (Every 30 seconds)
    if (runCount % 30 == 0 && !onAlert)
    {
        //Get new Blocks
        findBlocks();
    }

    bool newAlert = false;
    newAlert = checkTurrets();
    if (!newAlert)
    {
        newAlert = getBlockCount() < blockCount;
    }

    //Check if a new Alarm is triggered
    if (newAlert)
    {
        //Start Alarm
        onAlert = true;
        startAlarm();
    } else
    {
        //If Alarm was triggered before
        if (onAlert)
        {
            //Cooldown is over
            if(runCountOnTrigger + activeTime < runCount)
            {
                //Stop Alarm
                onAlert = false;
                stopAlarm();
            }
        }
    }

    //Increase Runcount.
    if (runCount != int.MaxValue)
    {
        runCount++;
    } else
    {
        runCount = 0;
    }
}

void startAlarm()
{
    runCountOnTrigger = runCount;
    if (enableAllTurrets)
    {
        foreach (var turret in turretList)
        {
            //if (turret == null) continue;
            turret?.ApplyAction("OnOff_On");
        }
    }
    if (silentTransmit)
    {
        foreach (var radioAntenna in radioAntennaList)
        {
            if (radioAntenna == null) continue;
            //radioAntenna.Enabled = true;
            //radioAntenna.EnableBroadcasting = true;
            radioAntenna.Radius = 50000;
            messageSend = 1;
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }
        foreach (var laserAntenna in laserAntennaList)
        {
            if (laserAntenna == null) continue;
            //laserAntenna.Enabled = true;
            laserAntenna?.TransmitMessage(toSend);
        }
    }
    if (useLights)
    {
        foreach (var light in lightList)
        {
            if (light == null) continue;
            Color color = new Color(255, 0, 0);
            light.Color = color;
            light.Enabled = true;
        }
    }
    foreach (var timer in timerStartList)
    {
        if (timer == null) continue;
        timer?.Trigger();
    }
}

void stopAlarm()
{
    foreach (var radioAntenna in radioAntennaList)
    {
        if (radioAntenna == null) continue;
        //radioAntenna.EnableBroadcasting = false;
        //radioAntenna.Enabled = false;
        radioAntenna.Radius = 1;
    }
    if (useLights)
    {
        foreach (var light in lightList)
        {
            if (light == null) continue;
            Color color = new Color(0, 255, 0);
            light.Color = color;
            light.Enabled = true;
        }
    }
    foreach (var timer in timerStopList)
    {
        timer?.Trigger();
    }
    findBlocks();
}

Boolean checkTurrets()
{
    foreach (var turret in turretList)
    {
        if (turret == null) continue;
        if (!turret.IsFunctional) return true;
        MyDetectedEntityInfo myDetectedEntityInfo = turret.GetTargetedEntity();
        if (myDetectedEntityInfo.IsEmpty()) continue;
        if (myDetectedEntityInfo.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies) return true;
    }
    return false;
}

int getBlockCount()
{
    List<IMyTerminalBlock> list = new List<IMyTerminalBlock>();
    GridTerminalSystem.GetBlocks(list);
    return list.Count;
}

void findBlocks()
{
    blockGroup = GridTerminalSystem.GetBlockGroupWithName(GROUP_NAME);
    if (blockGroup == null)
    {
        Echo("Error! Block Group not found!\nPlease Recompile!");
        findBlocksError = true;
        return;
    }

    //Create Lists
    turretList = new List<IMyLargeConveyorTurretBase>();
    lightList = new List<IMyLightingBlock>();
    timerStartList = new List<IMyTimerBlock>();
    timerStopList = new List<IMyTimerBlock>();
    radioAntennaList = new List<IMyRadioAntenna>();
    laserAntennaList = new List<IMyLaserAntenna>();

    //Fill Lists
    blockGroup.GetBlocksOfType<IMyLargeConveyorTurretBase>(turretList, x => x.IsFunctional);
    blockGroup.GetBlocksOfType<IMyLightingBlock>(lightList, x => x.IsFunctional);
    blockGroup.GetBlocksOfType<IMyTimerBlock>(timerStartList, x => x.IsFunctional && x.CustomName.Contains("START"));
    blockGroup.GetBlocksOfType<IMyTimerBlock>(timerStopList, x => x.IsFunctional && x.CustomName.Contains("STOP"));
    blockGroup.GetBlocksOfType<IMyRadioAntenna>(radioAntennaList, x => x.IsFunctional);
    blockGroup.GetBlocksOfType<IMyLaserAntenna>(laserAntennaList, x => x.IsFunctional);
    blockCount = getBlockCount();
    findBlocksError = false;
}
