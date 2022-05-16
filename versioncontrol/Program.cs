using System;
using System.IO;
using System.Text.RegularExpressions;

namespace versioncontrol
{
    // reads changelog and updates AssemblyInfo and info.json with new version
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                if (args.Length < 2)
                {
                    Console.WriteLine("not enough arguments");
                    return 2;
                }

                var rxVersion = new Regex("(?<!Manager)(Version.*\\\"|\\/v)([\\d\\.\\*]{5,})(\\\"|\\/)");

                string path_changelog = args[0];
                string path_assembly = args[1];

                var changelog = File.ReadAllLines(path_changelog);
                var assembly = File.ReadAllLines(path_assembly);

                string version = null;

                for (int i = 0; i < changelog.Length; i++)
                {
                    changelog[i] = changelog[i].TrimEnd();
                    if (changelog[i].StartsWith("## [", StringComparison.Ordinal) && changelog[i].Length >= 5)
                    {
                        version = changelog[i][4..^1];
                        break;
                    }
                }

                if (version == null)
                {
                    Console.WriteLine("Unable to parse version from changelog");
                    return 0;
                }

                for (int i = 1; i < args.Length; i++)
                {
                    string org = File.ReadAllText(args[i]);
                    string repl = rxVersion.Replace(org, "${1}" + version + "${3}");
                    File.WriteAllText(args[i], repl);
                }

                //for (int i = 0; i < assembly.Length; i++)
                //{
                //    if (assembly[i].StartsWith("[assembly: AssemblyVersion(\"", StringComparison.Ordinal))
                //    {
                //        assembly[i] = "[assembly: AssemblyVersion(\"" + version + "\")]";
                //    }
                //    else if (assembly[i].StartsWith("[assembly: AssemblyFileVersion(\"", StringComparison.Ordinal))
                //    {
                //        assembly[i] = "[assembly: AssemblyFileVersion(\"" + version + "\")]";
                //    }
                //}
                //File.WriteAllLines(path_assembly, assembly);

                //for (int j = 2; j < args.Length; j++)
                //{
                //    var info = File.ReadAllLines(args[j]);
                //
                //    for (int i = 0; i < info.Length; i++)
                //    {
                //        if (info[i].Contains("\"Version\"", StringComparison.Ordinal))
                //        {
                //            int spaces; for (spaces = 0; spaces < info[i].Length && info[i][spaces] == ' '; spaces++) ;
                //
                //            int index1 = info[i].LastIndexOf('"');
                //            if (index1 < 1) continue;
                //
                //            int index2 = info[i].LastIndexOf('"', index1 - 1);
                //
                //            if (index2 >= 0 && index2 < index1)
                //            {
                //                info[i] = info[i].Substring(0, index2 + 1) + version + info[i].Substring(index1);
                //            }
                //        }
                //    }
                //
                //    File.WriteAllLines(args[j], info);
                //}

                Console.WriteLine("Updated version numbers.");
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return 1;
            }
        }
    }
}
