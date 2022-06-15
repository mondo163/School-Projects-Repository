// Assignment 4
// Pete Myers
// OIT, Spring 2018
// Handout
//Armando Santillan - Edited 6/8/2022

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleFileSystem;

namespace SimpleShell
{
    public class SimpleSessionManager : SessionManager
    {
        private class SimpleSession : Session
        {
            private int userID;
            private SecuritySystem security;
            private FileSystem filesystem;
            private ShellFactory shells;
            private Directory homeDir;
            private Shell shell;
            private Terminal terminal;

            public SimpleSession(SecuritySystem security, FileSystem filesystem, ShellFactory shells, Terminal terminal, int userID)
            {
                this.security = security;
                this.filesystem = filesystem;
                this.shells = shells;
                this.terminal = terminal;
                this.userID = userID;


                // get user's home directory
                homeDir = (Directory)filesystem.Find(security.UserHomeDirectory(userID));
                // identify user's shell
                shell = shells.CreateShell(security.UserPreferredShell(userID), this);
            }

            public int UserID => userID;
            public string Username => security.UserName(userID);
            public Terminal Terminal => terminal;
            public Shell Shell => shell;
            public Directory HomeDirectory => homeDir;
            public FileSystem FileSystem => filesystem;
            public SecuritySystem SecuritySystem => security;

            public void Run()
            {
                shell.Run(terminal);
            }

            public void Logout()
            {
                //remove the user specific info
                userID = -1;
                homeDir = null;
                shell = null;

            }
        }

        private SecuritySystem security;
        private FileSystem filesystem;
        private ShellFactory shells;

        public SimpleSessionManager(SecuritySystem security, FileSystem filesystem, ShellFactory shells)
        {
            this.security = security;
            this.filesystem = filesystem;
            this.shells = shells;
        }

        public Session NewSession(Terminal terminal)
        {
            // ask the user to login
            terminal.Echo = true;

            // give them 3 tries
            const int maxAttempts = 3;
            int attempts = 0;
            while (attempts< maxAttempts)
            {
                // prompt for user name
                terminal.Write("username: ");
                string username = terminal.ReadLine();
                try
                {
                    //determin if the user needs to set their password
                    if (security.NeedsPassword(username))
                    {
                        //prompt for new password
                        terminal.WriteLine("You need to set you password");
                        terminal.Write("New password: ");
                        terminal.Echo = false;
                        string password = terminal.ReadLine();
                        terminal.Echo = true;
                        terminal.WriteLine("");

                        //save their password choice
                        security.SetPassword(username, password);

                        //return them to the login prompt
                    }
                    else
                    {
                        //prompt for password
                        terminal.Write("password:");
                        terminal.Echo = false;
                        string password = terminal.ReadLine();
                        terminal.Echo = true;
                        terminal.WriteLine("");

                        //authenticate user
                        try
                        {
                            int userId = security.Authenticate(username, password);

                            //return new session if makes it this far
                            return new SimpleSession(security, filesystem, shells, terminal, userId);
                        }
                        catch (Exception ex)
                        {
                            terminal.WriteLine($"Error! {ex.Message}");
                            attempts++;                        }
                    }
                }
                catch (Exception ex)
                {
                    terminal.WriteLine($"Error! {ex.Message}");
                }
            }

            //failed to login
            terminal.WriteLine("You failed too many times!");
            return null;
        }
    }
}
