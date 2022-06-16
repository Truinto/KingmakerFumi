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


                string path_changelog = args[0];
                string path_assembly = args[1];

                string version = null;

                if (path_changelog == "--increment")
                {
                    var rxAssembly = new Regex("AssemblyVersion.*?([\\d\\.\\*]+)");
                    string org = File.ReadAllText(path_assembly);
                    Match match = rxAssembly.Match(org);
                    version = match.Groups[1].Value;

                    int index = version.LastIndexOf('.') + 1;
                    if (index > 0 && int.TryParse(version[index..], out int rev))
                        version = version[..index] + (rev + 1);
                    else
                        Console.WriteLine("Unable to increment version");
                }
                else
                {
                    var changelog = File.ReadAllLines(path_changelog);
                    for (int i = 0; i < changelog.Length; i++)
                    {
                        changelog[i] = changelog[i].TrimEnd();
                        if (changelog[i].StartsWith("## [", StringComparison.Ordinal) && changelog[i].Length >= 5)
                        {
                            version = changelog[i][4..^1];
                            break;
                        }
                    }
                }

                if (version == null)
                {
                    Console.WriteLine("Unable to parse version from changelog");
                    return 0;
                }

                var rxVersion = new Regex("(?<!Manager)(Version.*\\\"|\\/v)([\\d\\.\\*]{5,})(\\\"|\\/)");
                for (int i = 1; i < args.Length; i++)
                {
                    string org = File.ReadAllText(args[i]);
                    string repl = rxVersion.Replace(org, "${1}" + version + "${3}");
                    if (org != repl)
                        File.WriteAllText(args[i], repl);
                }

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
