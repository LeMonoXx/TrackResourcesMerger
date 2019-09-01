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

            var firstSourceFilePath = @"E:\Downloads\TrackResources_merged.lua"; //args[0];//
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

            var secondSourceFilePath = @"E:\Downloads\TrackResources_merged.lua"; //args[1];//@"E:\Applications\TrackResourcesMerger\TrackResourcesMerger\bin\Debug\TrackResources_target.lua";
            if (!File.Exists(secondSourceFilePath))
            {
                Console.WriteLine($"Source file '{secondSourceFilePath}' does not exists! Execution stopped.");
                Console.ReadLine();
                return;
            }

            var mergeResultFileName = "TrackResources_merged.lua";
            // var createBackup = false; // bool.Parse(args[2]);

           //  var targetFileName = new FileInfo(secondSourceFilePath);

           // if (DirectoryHasPermission(targetFileInfo.Directory.FullName, FileSystemRights.CreateFiles))
           // {
                // interpret file with LUA
            var secondSourceLua = lua.DoFile(secondSourceFilePath);
            var secondSourceTable = lua.GetTable("TrackResourcesCfg.db");

            if (secondSourceTable == null)
            {
                Console.WriteLine($"Target database not found in file '{secondSourceFilePath}'. Wrong file?");
                Console.ReadLine();
                return;
            }

            //if (createBackup)
            //{ 
            //    var backupFileName = $"TrackResources_PreMerge_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.bak";
            //    File.Copy(secondSourceFilePath, Path.Combine(targetFileName.Directory.FullName, backupFileName));
            //    Console.WriteLine($"Created backup file '{backupFileName}' in directory '{targetFileName.Directory.FullName}'");
            //}

            var mergedTable = JoinTable(firstSourceTable, secondSourceTable);

            WriteLuaTableFile(mergedTable, mergeResultFileName);

            Console.WriteLine("\n Merge Finished. Press Enter to close.");
            Console.ReadLine();
          //  } 
         //   else
          //  {
          //      Console.WriteLine("Missing write permission to create merged TrackResources file.");
          //      Console.ReadLine();
         //       return;
         //   }
        }

        public static LuaTable JoinTable(LuaTable sourceTable, LuaTable targetTable)
        {
            // merge source file into target file
            foreach (DictionaryEntry sourceUiMapDic in sourceTable)
            {
                Console.WriteLine($"Checking uiMap: {sourceUiMapDic.Key}");

                // if there is no uiMap, there is no data at all for that area. so we can grap
                // the whole table and add it with all nodes in there.
                if (targetTable[sourceUiMapDic.Key] is LuaTable targetUiMapT)
                {
                    foreach (DictionaryEntry sourceGatherItem in (LuaTable)sourceUiMapDic.Value)
                    {
                        Console.WriteLine($"    Checking gather item: {sourceGatherItem.Key}");

                        if (targetUiMapT[sourceGatherItem.Key] is LuaTable targetItemT)
                        {
                            foreach (DictionaryEntry sourceCordsT in (LuaTable)sourceGatherItem.Value)
                            {
                                Console.WriteLine($"            Checking cords: {sourceCordsT.Key}");

                                if (targetItemT[sourceCordsT.Key] is LuaTable targetCordsT)
                                {
                                    // item entry exists in target -> nothing to do here.
                                }
                                else
                                {
                                    // no entry for the current cords in that area for that item -> add it.
                                    ((LuaTable)((LuaTable)targetTable[sourceUiMapDic.Key])[sourceGatherItem.Key])[sourceCordsT.Key] = sourceCordsT.Value;
                                    Console.WriteLine($"            Added for uiMap '{sourceUiMapDic.Key}', gather item '{sourceGatherItem.Key}' on cords '{sourceCordsT.Key}'.");
                                }
                            }
                        }
                        else
                        {
                            // if there is no gather-item entry for the current item in the current uiMap, add it and take alle subnodes with it.
                            ((LuaTable)targetTable[sourceUiMapDic.Key])[sourceGatherItem.Key] = sourceGatherItem.Value;
                            Console.WriteLine($"    Added for uiMap '{sourceUiMapDic.Key}', complete list of gather item '{sourceGatherItem.Key}'.");
                        }
                    }
                }
                else
                {
                    // complete area is not in the target file. Add everything we got for tha area.
                    targetTable[sourceUiMapDic.Key] = sourceUiMapDic.Value;
                    Console.WriteLine($"Added all nodes for uiMap '{sourceUiMapDic.Key}' with all including items.");
                }
            }

            return targetTable;
        }

        public static void WriteLuaTableFile(LuaTable table, string filePath)
        {
            var fileBegin = "\nTrackResourcesCfg = {\n"
                   + "	[\"db\"] = {" + "\n";

            var body = "";

            foreach (DictionaryEntry UiMapDic in table)
            {
                body += $"		[{UiMapDic.Key}] = {{" + "\n";
                foreach(DictionaryEntry gatherItem in (LuaTable)UiMapDic.Value)
                {
                    body += $"			[{gatherItem.Key}] = {{" + "\n";
                    foreach(DictionaryEntry cordsItem in (LuaTable)gatherItem.Value)
                    {
                        body += $"				[\"{cordsItem.Key}\"] = {{" + "\n";

                        //foreach(DictionaryEntry values in (LuaTable)cordsItem.Value)
                        //{
                        //    body += $"					{values.Value},\n";
                        //}

                        for (int i = 1; i < ((LuaTable)cordsItem.Value).Keys.Count + 1; i++)
                        {
                            var curValue = ((LuaTable)cordsItem.Value)[i];
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
                    + "		[\"sizeMinimap\"] = 12,\n"
                    + "		[\"showZonemap\"] = true,\n"
                    + "		[\"sizeZonemap\"] = 12,\n"
                + "	},\n"
               + "	[\"version\"] = 1,\n"
            + "}\n";

            File.WriteAllText(filePath, fileBegin + body + fileEnd);
        }

        public static bool DirectoryHasPermission(string DirectoryPath, FileSystemRights AccessRight)
        {
            if (string.IsNullOrEmpty(DirectoryPath)) return false;

            try
            {
                var rules = Directory.GetAccessControl(DirectoryPath).GetAccessRules(true, true, typeof(SecurityIdentifier));
                var identity = WindowsIdentity.GetCurrent();

                foreach (FileSystemAccessRule rule in rules)
                {
                    if (identity.Groups.Contains(rule.IdentityReference))
                    {
                        if ((AccessRight & rule.FileSystemRights) == AccessRight)
                        {
                            if (rule.AccessControlType == AccessControlType.Allow)
                            {
                                return true;
                            }     
                        }
                    }
                }
            }
            catch { }
            return false;
        }
    }
}
