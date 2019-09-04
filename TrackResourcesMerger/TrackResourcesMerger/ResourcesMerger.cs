using System;
using System.Collections;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using LuaInterface;

namespace TrackResourcesMerger
{
    public static class ResourcesMerger
    {
        public static void Main(string[] args)
        {
            var lua = new Lua();
            
            //if(args == null || args.Length < 3)
            //{
            //    Console.WriteLine("Missing inputs 1: Path to a 'TrackResources.lua' that represents the source!");
            //    Console.WriteLine("Missing inputs 2: Path to a 'TrackResources.lua' that represents the target!");
            //    Console.ReadLine();
            //    return;
            //}

            var firstSourceFilePath = @"C:\Repositories\TrackResourcesMerger\TrackResourcesMerger\lib\TrackResources_wowhead.lua"; //args[0];//
            if (!File.Exists(firstSourceFilePath))
            {
                Console.WriteLine($"Source file '{firstSourceFilePath}' does not exists! Execution stopped.");
                Console.ReadLine();
                return;
            }

            // interpret file with LUA
            var firstSourceLua = lua.DoFile(firstSourceFilePath);
            var firstSourceTable = lua.GetTable("TrackResourcesCfg.db");

            if (firstSourceTable == null) {
                Console.WriteLine($"Source database not found in file '{firstSourceFilePath}'. Wrong file?");
                Console.ReadLine();
                return;
            }

            var secondSourceFilePath = @"C:\Repositories\TrackResourcesMerger\TrackResourcesMerger\lib\TrackResources_wowhead.lua"; //args[1];//@"E:\Applications\TrackResourcesMerger\TrackResourcesMerger\bin\Debug\TrackResources_target.lua";
            if (!File.Exists(secondSourceFilePath))
            {
                Console.WriteLine($"Source file '{secondSourceFilePath}' does not exists! Execution stopped.");
                Console.ReadLine();
                return;
            }

            var mergeResultFileName = "TrackResources_merged.lua";

            // interpret file with LUA
            var secondSourceLua = lua.DoFile(secondSourceFilePath);
            var secondSourceTable = lua.GetTable("TrackResourcesCfg.db");

            if (secondSourceTable == null)
            {
                Console.WriteLine($"Target database not found in file '{secondSourceFilePath}'. Wrong file?");
                Console.ReadLine();
                return;
            }

            var mergedTable = JoinTable(firstSourceTable, secondSourceTable);

            WriteLuaTableFile(mergedTable, mergeResultFileName);

            Console.WriteLine("\n Merge Finished. Press Enter to close.");
            Console.ReadLine();
        }

        public static LuaTable JoinTable(LuaTable sourceTable, LuaTable targetTable)
        {
            // merge source file and target file
            foreach (var entry in Consts.UiMapDic)
            {
                object sourceUiMapDic = sourceTable[int.Parse(entry.Key.ToString())];

                if(sourceUiMapDic == null)
                {
                    // there is no information for that ui map in the source file, lets skip it.
                    continue;
                }

                Console.WriteLine($"Checking uiMap: { entry.Value }");

                // if there is no uiMap, there is no data at all for that area. so we can grap
                // the whole table and add it with all nodes in there.
                if (targetTable[entry.Key] is LuaTable targetUiMapT)
                {
                    foreach (DictionaryEntry sourceGatherItem in (LuaTable)sourceUiMapDic)
                    {
                        Console.WriteLine($"    Checking gather item: {sourceGatherItem.Key}");

                        if (targetUiMapT[sourceGatherItem.Key] is LuaTable targetItemT)
                        {
                            foreach (DictionaryEntry sourceCordsT in (LuaTable)sourceGatherItem.Value)
                            {
                                // Console.WriteLine($"            Checking cords: {sourceCordsT.Key}");

                                if (targetItemT[sourceCordsT.Key] is LuaTable targetCordsT)
                                {
                                    // item entry exists in target -> nothing to do here.
                                }
                                else
                                {
                                    // no entry for the current cords in that area for that item -> add it.
                                    ((LuaTable)((LuaTable)targetTable[entry.Key])[sourceGatherItem.Key])[sourceCordsT.Key] = sourceCordsT.Value;
                                    WriteToConsole($"            Added coordinations '{sourceCordsT.Key}'.", ConsoleColor.Green);
                                }
                            }
                        }
                        else
                        {
                            // if there is no gather-item entry for the current item in the current uiMap, add it and take alle subnodes with it.
                            ((LuaTable)targetTable[entry.Key])[sourceGatherItem.Key] = sourceGatherItem.Value;
                            WriteToConsole($"    Added all coordinations for gather item '{sourceGatherItem.Key}'.", ConsoleColor.Green);
                        }
                    }
                }
                else
                {
                    // complete area is not in the target file. Add everything we got for tha area.
                    targetTable[entry.Key] = sourceUiMapDic;
                    WriteToConsole($"Added all nodes for uiMap '{ Consts.UiMapDic[entry.Key] }' with all including items.\n", ConsoleColor.Green);
                }
            }

            return targetTable;
        }

        public static void WriteLuaTableFile(LuaTable table, string filePath)
        {

            Console.Write("\nStart building up merged file... ");

            var fileBegin = "\nTrackResourcesCfg = {\n"
                   + "	[\"db\"] = {" + "\n";

            var body = "";

            foreach (var entry in Consts.UiMapDic)
            {
                object UiMapDic = table[entry.Key];

                if (UiMapDic == null)
                {
                    // there is no information for that ui map in the source file, lets skip it.
                    continue;
                }

                body += $"		[{entry.Key}] = {{" + "\n";

                //foreach(DictionaryEntry gatherItem in (LuaTable)UiMapDic)
                foreach (var gItemEntry in Consts.ResourceIconDic)
                {
                    object gItemDic = ((LuaTable)UiMapDic)[gItemEntry.Key];

                    if (gItemDic == null)
                    {
                        // there is no information for that gather item in the source file, lets skip it.
                        continue;
                    }

                    body += $"			[{gItemEntry.Key}] = {{" + "\n";
                    foreach(DictionaryEntry cordsItem in (LuaTable)gItemDic)
                    {
                        body += $"				[\"{cordsItem.Key}\"] = {{" + "\n";

                        for (int i = 1; i < ((LuaTable)cordsItem.Value).Keys.Count + 1; i++)
                        {
                            var curValue = ((LuaTable)cordsItem.Value)[i];

                            // make sure that double numbers are displayed in the american format.
                            curValue = curValue.ToString().Replace(",", ".");

                            body += $"					{curValue}, -- [{ i }]\n";
                        }

                        body += "				},\n";
                    }
                    body += "			},\n";
                }
                body += "		},\n";
            }

            //ToDo: extract config.
                var fileEnd = "	},\n"
                 + "	[\"settings\"] = {\n"
                    + "		[\"alwaysShow\"] = false,\n"
                    + "		[\"showMinimap\"] = true,\n"
                    + "		[\"sizeMinimap\"] = 8,\n"
                    + "		[\"showZonemap\"] = true,\n"
                    + "		[\"sizeZonemap\"] = 8,\n"
                + "	},\n"
               + "	[\"version\"] = 1,\n"
            + "}\n";

            WriteToConsole("Done!\n", ConsoleColor.Green);
            Console.Write("Start writing to file... ");

            File.WriteAllText(filePath, fileBegin + body + fileEnd);

            WriteToConsole("Done!\n", ConsoleColor.Green);
        }

        private static void WriteToConsole(string text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ResetColor();
        }
    }
}
