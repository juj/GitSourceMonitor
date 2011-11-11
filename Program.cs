using System;
using System.Web;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace GitSourceMonitor
{
    class Program
    {
        // Given time since Unix epoch time, returns a corresponding DateTime.
        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970,1,1,0,0,0,0);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: GitSourceMonitor <branchname> [outputfilename]");
                Console.WriteLine("   <branchname>: The name of the git branch to add to a SourceMonitor project.");
                Console.WriteLine("   [outputfilename]: The name of the output SourceMonitor .smproj file to use. "
                                        + "If not specified, the file \"GitSourceMonitorProject.smproj\" will be used.");
                return;
            }

            // Run 'git log' to get all the commits in the repository.
            // Assuming the tool is run with cwd inside the git repository.
            ProcessStartInfo ps = new ProcessStartInfo();
            ps.CreateNoWindow = true; // Bug: For some reason, when invoking git, it creates a window even if this is true.
            ps.FileName = "git";
            // Use a special '|||||' as a delimiter so that we can split the results easily below.
            ps.Arguments = "log " + args[0] + "  --format=\"%H|||||%s|||||%at\" --no-color --no-merges > gitsm_temp_file.txt";
            Process p = Process.Start(ps);
            p.WaitForExit();

            string outputProjectFile = args.Length >= 2 ? args[1] : "GitSourceMonitorProject.smproj";
            // Read the commits from the output file.
            List<string> commits = new List<string>();
            using (TextReader reader = File.OpenText("gitsm_temp_file.txt"))
            {
                string line = reader.ReadToEnd();
                commits.AddRange(line.Split('\n').ToList());
            }
            // Don't leave temp files lying around.
            File.Delete("gitsm_temp_file.txt");

            // Add checkpoints from oldest to newest. The log file had from newest to oldest.
            commits.Reverse();

            // Check if we have an old GitSourceMonitor-generated project in the folder.
            DateTime projectLastModifiedDate = new DateTime(1970,1,1,0,0,0,0);
            if (File.Exists(outputProjectFile))
                projectLastModifiedDate = new FileInfo(outputProjectFile).CreationTime;

            // Parse all commit lines to count exactly how many commits we have to process.
            List<string> parsedCommits = new List<string>();
            foreach (string s in commits)
            {
                List<string> items = Regex.Split(s, "\\|\\|\\|\\|\\|").ToList();
                if (items.Count != 3)
                    continue;
                parsedCommits.Add(s);
            }
            commits = parsedCommits;

            int n = 1;
            foreach (string s in commits)
            {
                List<string> items = Regex.Split(s, "\\|\\|\\|\\|\\|").ToList();
                if (items.Count != 3)
                    continue;

                string commitHash = items[0];
                string message = items[1].Replace("<", "&lt;").Replace(">", "&gt;");
                DateTime commitTime = UnixTimeStampToDateTime(double.Parse(items[2]));
                string commitTimeString = commitTime.ToString("yyyy-MM-ddTHH:mm:ss");

                if (commitTime < projectLastModifiedDate)
                {
                    Console.WriteLine("Skipping already added commit (" + (n++) + "/" + commits.Count + "): " + commitTimeString + ": " + message);
                    continue;
                }

                // Check out the commit so that SourceMonitor can process it.
                Console.WriteLine("Processing commit (" + (n++) + "/" + commits.Count + "): " + commitTimeString + ": " + message);
                ps = new ProcessStartInfo();
                ps.CreateNoWindow = true; // Bug: For some reason, when invoking git, it creates a window even if this is true.
                ps.FileName = "git";
                ps.Arguments = "checkout " + commitHash;
                p = Process.Start(ps);
                p.WaitForExit();

                // Create the command file for SourceMonitor to process. The new file will instruct SourceMonitor
                // to create a new checkpoint to the project.
                using (TextWriter writer = File.CreateText("gitsm_temp_checkpoint.xml"))
                {
                    writer.Write("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>\n\n" +
                             "<sourcemonitor_commands>\n" +
                             "\t<write_log>true</write_log>\n" +
                             "\t<command>\n" +
                             "\t\t<project_file>" + outputProjectFile + "</project_file>\n" +
                             "\t\t<checkpoint_name>" + message + "</checkpoint_name>\n" +                             
                             "\t\t<checkpoint_date>" + commitTimeString + "</checkpoint_date>\n" + 
                             "\t\t<project_language>C++</project_language>\n" +
                             "\t\t<modified_complexity>true</modified_complexity>\n" +
                             "\t\t<source_directory>.</source_directory>\n" +
                             "\t\t<parse_utf8_files>True</parse_utf8_files>\n" +
                             "\t\t<show_measured_max_block_depth>True</show_measured_max_block_depth>\n" +
                             "\t\t<file_extensions>*.h,*.cpp,*.inl</file_extensions>\n" +
                             "\t\t<include_subdirectories>true</include_subdirectories>\n" +
                             "\t\t<ignore_headers_footers>2 DOC only</ignore_headers_footers>\n" +
                             "\t\t<ignore_headers_footers>false</ignore_headers_footers>\n" + 
                             "\t</command>\n" +
                             "</sourcemonitor_commands>\n");
                    writer.Flush();
                    writer.Close();
                }

                // Run SourceMonitor.
                ps = new ProcessStartInfo();
                ps.CreateNoWindow = true;
                ps.FileName = "SourceMonitor";
                ps.Arguments = "/C gitsm_temp_checkpoint.xml";
                p = Process.Start(ps);
                p.WaitForExit();

                File.SetCreationTime(outputProjectFile, commitTime);
            }

            // Don't leave temp files lying around.
            File.Delete("gitsm_temp_checkpoint.xml");
        }
    }
}
