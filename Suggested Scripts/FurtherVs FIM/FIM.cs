/*
 * FurtherVs friendly inventory manager
 * 
 * This script either sorts all items into their respective containers [CURRENTLY NOT INCLUDED] OR counts all ingots and prints them onto an LCD
 *
 * SETUP:
 * 1. Stick this script on a programmable block
 * 2. Put LCDs and Blocks with an Inventory that should be searched in a Block Group called [FIM].
 * 3. Run the script using a terminal block (like a sensor, a timer or a button panel) or manually using one of the following arguments. BTW All other methods won't work.
 * 4. Enter the subtype of the items you want displayed in the customData of the LCD. Example: one LCD containing ore in the customData will only display Ores.
 * 
 * ARGUMENTS:
 * sort :   Sorts Items [CURRENTLY NOT INCLUDED]
 * count:   Counts Items.
 */
//Configuration Section
String GENERAL_TAG = "[FIM]";


//Do not touchy section

List<IMyTextPanel> outputLCDList = new List<IMyTextPanel>();
List<IMyTerminalBlock> cargoBlocks = new List<IMyTerminalBlock>();

double lastRun = 3;

public Program()
{
    if (!Me.CustomName.EndsWith(GENERAL_TAG))
    {
        Me.CustomName += " " + GENERAL_TAG;
    }
    Echo("This Script can only be run manually");
    findBlocks();
}

public void Main(string argument, UpdateType updateSource)
{
    if((Runtime.TimeSinceLastRun.TotalSeconds + lastRun) < 2)
    {
        Echo("Script on cooldown...");
        lastRun += Runtime.TimeSinceLastRun.TotalSeconds;
        Echo("" + lastRun);
        return;
    }
    lastRun = 0;
    if ((updateSource & (UpdateType.Trigger | UpdateType.Terminal)) != 0)
    {
        if (outputLCDList.Count == 0 || cargoBlocks.Count == 0)
        {
            findBlocks();   
        }
        if (argument.ToLowerInvariant().Contains("sort"))
        {
            //Do Sorting
            //sort();
            cooldown();
            Echo("");
        }
        if (argument.ToLowerInvariant().Contains("count"))
        {
            //Do Counting
            foreach (var lcd in outputLCDList)
            {
                if(lcd==null || !lcd.IsFunctional)
                {
                    continue;
                }
                String type = "ingot";
                if (!String.IsNullOrEmpty(lcd.CustomData))
                {
                    if(lcd.CustomData.Contains("ore") || lcd.CustomData.Contains("ingot") || lcd.CustomData.Contains("component") || lcd.CustomData.Contains("ammomagazine") || lcd.CustomData.Contains("physicalgunobject"))
                    {
                        type = lcd.CustomData;
                    }
                }
                count(type, lcd);
            }
            cooldown();
            Echo("Counted Items.");
        }
        return;
    }
}

void count(String type, IMyTextPanel lcd)
{
    Dictionary<String, double> itemDictionary = new Dictionary<string, double>();

    //Loop through all tagged blocks
    foreach (var block in cargoBlocks)
    {
        if(block != null && block.IsWorking)
        {
            IMyInventory inventory = block.GetInventory();
            List<IMyInventoryItem> items = inventory.GetItems();
            foreach (var item in items)
            {
                if (item.Content.ToString().ToLowerInvariant().Contains(type.ToLowerInvariant()))
                {
                    if (!itemDictionary.ContainsKey(item.Content.SubtypeName))
                    {
                        itemDictionary[item.Content.SubtypeName] = (double)item.Amount;
                    } else
                    {
                        itemDictionary[item.Content.SubtypeName] += (double)item.Amount;
                    }
                }
            }
        }
    }

    int largestKey = 10;
    foreach (var key in itemDictionary.Keys)
    {
        if (key.Length > largestKey)
        {
            largestKey = key.Length + 1;
        }
    }

    //Print Results to LCD
    String output = "";
    output += centerText($"<--- {type.ToUpperInvariant()} --->") + "\n\n";
    foreach (var key in itemDictionary.Keys)
    {
        string toAdd = $"{key}";
        while (toAdd.Length < largestKey)
        {
            toAdd += " ";
        }
        output += $"{toAdd}: {Math.Round(itemDictionary[key],2)}\n";

    }
    lcd.Enabled = true;
    lcd.ShowPublicTextOnScreen();
    lcd.WritePublicText(output);
    lcd.Font = "Monospace";
    lcd.FontSize = (float)1;
}

string centerText(String s)
{
    int maxSize = 27;
    if (s.Length > maxSize) return s;
    string blankText = "";
    for (int i = 0; i < (maxSize - s.Length) / 2; i++)
    {
        blankText = blankText + " ";
    }
    string printLine = blankText + s;
    return printLine;
}

void findBlocks()
{
    IMyBlockGroup group = GridTerminalSystem.GetBlockGroupWithName(GENERAL_TAG);
    if (group != null)
    {
        outputLCDList.Clear();
        cargoBlocks.Clear();

        group.GetBlocksOfType<IMyTextPanel>(outputLCDList, (IMyTextPanel x) => x.IsFunctional);
        group.GetBlocksOfType<IMyTerminalBlock>(cargoBlocks, (IMyTerminalBlock x) => x.HasInventory);

    } else
    {
        Echo("ERROR: Block Group not found!");
        return;
    }
}

void log(String s)
{
    Me.CustomData += s + "\n";
}
