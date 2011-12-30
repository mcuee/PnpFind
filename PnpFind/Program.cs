using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace PnpFind
{
    internal class Program
    {
        private static bool DeleteMode = false;
        private static String MatchText = string.Empty;
        private static List<Dictionary<string, List<string>>> oemFileInfoSelectedList = new List<Dictionary<string, List<string>>>();

        private static void Main(string[] args)
        {
            StreamWriter sw = null;
            foreach (string arg in args)
            {
                if (arg.ToLower().StartsWith("/out:"))
                {
                    sw = File.CreateText(arg.Substring(5));
                    Console.SetOut(sw);
                }
                else if (arg.ToLower() == "/delete")
                {
                    DeleteMode = true;
                }
                else if (arg.ToLower() == "/?" || 
                    (arg.ToLower() == "--help"))
                {
                    Console.WriteLine("PnpFind - Driverstore Destroyer");
                    Console.WriteLine("Copyright (C) 2011 Travis Robinson");
                    Console.WriteLine("");
                    Console.WriteLine("Usage: PnpFind [/out:file.ext] [/delete] [matchtext]");
                    return;
                }
                else
                {
                    MatchText = arg;
                }
            }

            List<FileInfo> oemFileList = DriverStore.GetOemInfFileList();
            foreach (FileInfo oemFileInfo in oemFileList)
            {
                bool matched = MatchText == String.Empty ? true : oemFileInfo.Name.ToLower().Contains(MatchText);

                Dictionary<string, List<string>> infEntities;
                DriverStore.GetInfSection(oemFileInfo.FullName, "Version", out infEntities);
                infEntities.Add("Inf", new List<string>(new String[] { oemFileInfo.Name}));
                infEntities.Remove("Signature");
                infEntities.Remove("signature");
                foreach (KeyValuePair<string, List<string>> infEntity in infEntities)
                {
                    foreach (string  value in infEntity.Value)
                    {
                        if (MatchText == String.Empty)
                            matched = true;
                        else if (value.ToLower().Contains(MatchText.ToLower()))
                            matched = true;

                        if (matched) break;
                     
                    }
                    if (matched) break;
                }
                if (matched)
                {
                    oemFileInfoSelectedList.Add(infEntities);
                }
            }

            foreach (Dictionary<string, List<string>> infEntities in oemFileInfoSelectedList)
            {
                string infFile = String.Empty;
                foreach (KeyValuePair<string, List<string>> infEntity in infEntities)
                {
                    String valueDisplayText = String.Empty;
                    foreach (string infValue in infEntity.Value)
                        valueDisplayText += infValue + ", ";

                    if (valueDisplayText.Length >=2)
                        valueDisplayText = valueDisplayText.Substring(0, valueDisplayText.Length - 2);

                    Console.WriteLine("{0,-15} : {1}", infEntity.Key, valueDisplayText);

                    if (infEntity.Key.ToLower()=="inf")
                    {
                        infFile = valueDisplayText;
                    }
                }
                Console.WriteLine();

                if (DeleteMode && infFile != String.Empty)
                {
                    if (MatchText==string.Empty)
                    {
                        throw new Exception("aborting because empty match string would result in total driverstore annihilation.");
                    }
                    int result = DriverStore.RemoveOemInf(infFile);
                    switch (result)
                    {
                        case 0:
                            Console.WriteLine("{0} deleted successfully.", infFile);
                            break;
                        case 2: // does not exist.
                            try
                            {
                                DriverStore.GetOemInfFullPath(infFile).Delete();
                            }
                            catch (Exception)
                            {
                            }
                            break;
                        default:
                            Console.WriteLine("Failed deleting {0} result={1}", infFile, result);
                            break;
                    }
                }
            }

            if (!ReferenceEquals(null,sw))
            {
                sw.Flush();
                sw.Close();
            }
        }
    }
}
