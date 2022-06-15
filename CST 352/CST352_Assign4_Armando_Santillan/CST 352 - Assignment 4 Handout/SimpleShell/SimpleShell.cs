// Assignment 4
// Pete Myers
// OIT, Spring 2018
// Handout

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimpleFileSystem;

namespace SimpleShell
{
    public class SimpleShell : Shell
    {
        private abstract class Cmd
        {
            private string name;
            private SimpleShell shell;

            public Cmd(string name, SimpleShell shell) { this.name = name; this.shell = shell; }

            public string Name => name;
            public SimpleShell Shell => shell;
            public Session Session => shell.session;
            public Terminal Terminal => shell.session.Terminal;
            public FileSystem FileSystem => shell.session.FileSystem;
            public SecuritySystem SecuritySystem => shell.session.SecuritySystem;

            abstract public void Execute(string[] args);
            virtual public string HelpText { get { return ""; } }
            virtual public void PrintUsage() { Terminal.WriteLine("Help not available for this command"); }
        }

        private Session session;
        private Directory cwd;
        private Dictionary<string, Cmd> cmds;   // name -> Cmd
        private bool running;

        public SimpleShell(Session session)
        {
            this.session = session;
            cwd = null;
            cmds = new Dictionary<string, Cmd>();
            running = false;

            AddCmd(new ExitCmd(this));
            AddCmd(new PwdCmd(this));
            AddCmd(new CdCmd(this));
            AddCmd(new LsCmd(this));
            AddCmd(new DateDiffCmd(this));
            AddCmd(new TouchCmd(this));
        }

        private void AddCmd(Cmd c) { cmds[c.Name] = c; }

        public void Run(Terminal terminal)
        {
            // NOTE: takes over the current thread, returns only when shell exits
            // expects terminal to already be connected

            // set the initial current working directory
            cwd = session.HomeDirectory;
            // main loop...
            running = true;
            while (running)
            {
                // print command prompt
                terminal.Write($"{cwd.FullPathName}>");

                // get command line
                string cmdline = terminal.ReadLine();

                if (!string.IsNullOrWhiteSpace(cmdline))
                {
                    // identify and execute command
                    string[] args = cmdline.Split(' ');
                    string cmdName = args[0];
                    if (cmds.ContainsKey(cmdName))
                    {
                        Cmd cmd = cmds[cmdName];
                        cmd.Execute(args);
                    }
                    else
                    {
                        terminal.WriteLine("Error: Command not found!");
                    }
                }
            }

        }

        #region commands

        // example command: exit
        private class ExitCmd : Cmd
        {
            public ExitCmd(SimpleShell shell) : base("exit", shell) { }

            public override void Execute(string[] args)
            {
                Terminal.WriteLine("Bye!");
                Shell.running = false;
            }

            override public string HelpText { get { return "Exits shell"; } }

            override public void PrintUsage()
            {
                Terminal.WriteLine("usage: exit");
            }
        }
        //print working directory 
        private class PwdCmd:Cmd
        {
            public PwdCmd(SimpleShell shell) : base("pwd", shell) { }

            public override void Execute(string[] args)
            {
                //print the current working directory
                Terminal.WriteLine(Shell.cwd.FullPathName);
            }
            override public string HelpText { get { return "Prints the current working directoyr"; } }
            public override void PrintUsage()
            {
                Terminal.WriteLine("usage: pwd");
            }
        }

        //change directory command
        private class CdCmd:Cmd
        {
            public CdCmd(SimpleShell shell) : base("cd", shell) { }

            public override void Execute(string[] args)
            {
                //change the current working directory
                //<path> is args 1

                //verify path argument exists and is a directory
                try
                {
                    //verify there is an argument
                    if (args.Length != 2)
                        throw new Exception("Arguments invalid!");
                    

                    FSEntry pathEntry = FileSystem.Find(args[1]);
                    if (pathEntry == null)
                        throw new Exception("Directory not found!");
                    if (pathEntry.IsFile) 
                        throw new Exception("Path must be a directory!");

                    //change the current working directory
                    Shell.cwd = (Directory)pathEntry;
                }
                catch (Exception ex)
                {
                    Terminal.WriteLine($"Errer: {ex.Message}");
                    PrintUsage();
                }
            }
            public override string HelpText { get { return "Change the current working directory"; } }
            public override void PrintUsage()
            {
                Terminal.WriteLine("usage: cd <path>");
                Terminal.WriteLine("   <path> - new curent working directory");
            }
        }
        //list the contents of the working directory command
        private class LsCmd : Cmd
        {
            public LsCmd(SimpleShell shell) : base("ls", shell) { }

            public override void Execute(string[] args)
            {
                //print list of contents of the current working directory
                foreach (Directory subDir in Shell.cwd.GetSubDirectories())
                {
                    Terminal.WriteLine($"   Dir:{subDir.Name}/");
                }

                //files
                foreach (File file in Shell.cwd.GetFiles())
                {
                    Terminal.WriteLine($"   File:{file.Name}");
                }
            }
            public override string HelpText { get { return "Change the current working directory"; } }
            public override void PrintUsage()
            {
                Terminal.WriteLine("usage: ls");
            }
        }
        //calculates the difference between two dates
        private class DateDiffCmd : Cmd
        {
            public DateDiffCmd(SimpleShell shell) : base("datediff", shell) { }

            public override void Execute(string[] args)
            {
                //calculates and prints the difference between two dates
                //<date> is args 1

                //verify path argument exists and is a directory
                try
                {
                    //verify there is an argument
                    if (args.Length != 2)
                        throw new Exception("Arguments invalid!");

                    DateTime date2 = DateTime.Parse(args[1]);
                    
                    if (date2 == null)
                        throw new Exception("Invalid date!");

                    //calculate now
                    DateTime date1 = DateTime.Now;
                    TimeSpan diff = date2 - date1;
                    if (date1 > date2)
                        diff = date1 - date2;

                    Terminal.WriteLine($"Days between {date1.ToShortDateString()} and {date2.ToShortDateString()}: {Math.Ceiling(diff.TotalDays)}");
                }
                catch (Exception ex)
                {
                    Terminal.WriteLine($"Errer: {ex.Message}");
                    PrintUsage();
                }
            }
            public override string HelpText { get { return "Prints the number of days between today and given date"; } }
            public override void PrintUsage()
            {
                Terminal.WriteLine("usage: cd <date> ");
                Terminal.WriteLine("   <date> - calculate difference between today and this date");
            }
        }

        private class TouchCmd : Cmd
        {
            public TouchCmd(SimpleShell shell) : base("touch", shell) { }
            

            public override void Execute(string[] args)
            {
                //create a file name with the give name in the current working directory
                //<fname> is args 1

                //verify path argument exists and is a directory
                try
                {
                    //verify fname argument doesn't already exist in the current working directory
                    if (args.Length != 2)
                        throw new Exception("Arguments invalid!");

                    string fileName = args[1];
                    string fullPathName = JoinPaths(Shell.cwd.FullPathName, fileName);
                    if (FileSystem.Find(fullPathName) != null)
                        throw new Exception("Cannot create file that already exists");

                    //change the current working directory
                    Shell.cwd.CreateFile(fileName);
                }
                catch (Exception ex)
                {
                    Terminal.WriteLine($"Errer: {ex.Message}");
                    PrintUsage();
                }
            }
            public override string HelpText { get { return "Creates a file with the given name"; } }
            public override void PrintUsage()
            {
                Terminal.WriteLine("usage: touch <fname>");
                Terminal.WriteLine("   <fname> - name of new file");

            }
            #region Helpers
            private string JoinPaths(string path1, string path2)
            {
                string path = path1;
                if (path.EndsWith("/"))
                    path = path.Remove(path.Length - 1, 1);
                if (!path2.StartsWith("/"))
                    path += "/";
                path += path2;
                return path;
            }
            #endregion
        }
        #endregion

    }
}
