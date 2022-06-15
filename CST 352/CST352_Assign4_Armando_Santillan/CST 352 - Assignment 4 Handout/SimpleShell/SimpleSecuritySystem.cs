// Assignment 4
// Pete Myers
// OIT, Spring 2022
// Handout
//

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using SimpleFileSystem;
using System.Text.RegularExpressions;

namespace SimpleShell
{
    public class SimpleSecurity : SecuritySystem
    {
        private class User
        {
            public int userID;
            public string userName;
            public string password;
            public string homeDirectory;
            public string shell;
        }

        private int nextUserID;
        private Dictionary<int, User> usersById;        // userID -> User

        private FileSystem filesystem;
        private string passwordFileName;
        
        public SimpleSecurity()
        {
            nextUserID = 1;
            usersById = new Dictionary<int, User>();
        }

        public SimpleSecurity(FileSystem filesystem, string passwordFileName)
        {
            nextUserID = 1;
            usersById = new Dictionary<int, User>();
            this.filesystem = filesystem;
            this.passwordFileName = passwordFileName;

            LoadPasswordFile();
        }

        private void LoadPasswordFile()
        {
            // Read all users from the password file
            // Each user on their own line in this format: userID;username;password;homedir;shell
            
            //open the pwdFile
            File pwdFile = (File)filesystem.Find(passwordFileName);
            if (pwdFile == null)
                throw new Exception("Password file not found!");
            
            //open the file 
            FileStream fs = pwdFile.Open();

            //read the contents of the file
            byte[] contents = fs.Read(0, pwdFile.Length);
            fs.Close();
            string contentString = Encoding.ASCII.GetString(contents);

            
            //process each line as user
            nextUserID = 0;
            string[] userStrings = contentString.Split('\n');
            foreach (string us in userStrings.Where(s=> !string.IsNullOrWhiteSpace(s)))
            {
                //
                string[] usParts = us.Split(';');
                if (usParts.Length == 5)
                {
                    User u = new User()
                    {
                        userID = int.Parse(usParts[0]),
                        userName = usParts[1],
                        password = usParts[2],
                        homeDirectory = usParts[3],
                        shell = usParts[4]
                    };

                    //add in user to the list of users
                    usersById.Add(u.userID, u);

                    //keep track of the largest user id
                    if (u.userID > nextUserID)
                        nextUserID = u.userID;
                }
            }

            //increment the next user id to the next valid id
            nextUserID++;

        }

        private void SavePasswordFile()
        {
            // Save all users to the password file
            // Each user on their own line in this format: userID;username;password;homedir;shell

            //open the pwdFile
            File pwdFile = (File)filesystem.Find(passwordFileName);
            if (pwdFile == null)
                throw new Exception("Password file not found!");

            //open the file 
            FileStream fs = pwdFile.Open();

            //create the password file contents
            string contents = "";
            foreach (User u in usersById.Values)
            {
                //add user on its own line
                contents +=$"{u.userID};{u.userName};{u.password};{u.homeDirectory};{u.shell}\n" ;
            }

            //save to the file and close it
            byte[] byteContents = Encoding.ASCII.GetBytes(contents);
            fs.Write(0, byteContents);
            fs.Close();
        }

        private User UserByName(string username)
        {
            return usersById.Values.FirstOrDefault(u => u.userName == username);
        }

        public int AddUser(string username)
        {
            //make suer user doesnt already exist
            if (UserByName(username) != null)
                throw new Exception("User with that username already exists");

            // create a new user with default home directory and shell
            // initially empty password
            User u = new User()
            {
                userID = nextUserID++,
                userName = username,
                password = "",
                homeDirectory = "/users/"+username,
                shell = "pshell"
            };
            usersById.Add(u.userID, u);

            
            if (filesystem != null)
            {
                //create user's home directory if needed
                if (filesystem.Find(u.homeDirectory) == null)
                {
                    Directory usersDir = (Directory)filesystem.Find("/users");
                    usersDir.CreateDirectory(username);
                }
                
                //save the user to the password file
                SavePasswordFile();
            }   
            
            //return user id
            return u.userID;
        }

        public int UserID(string username)
        {
            // lookup user by username and return user id
            User user = usersById.Values.Where(u => u.userName == username).FirstOrDefault();

            //if the user is not found, return -1 as an invalid user id
            if (user == null)
                throw new Exception("User not found");

            return user.userID;
        }

        public bool NeedsPassword(string username)
        {
            // return true if user needs a password set
            int id = UserID(username);
            return string.IsNullOrWhiteSpace(usersById[id].password);
        }

        public void SetPassword(string username, string password)
        {
            // find user by username
            int id = UserID(username);

            //validate it meets any rules
            //only valid characters
            Regex regex = new Regex("^[a-zA-Z0-9]*$");
            if (!regex.IsMatch(password))
                throw new Exception("Password must contain letters and digts only!");
            //at least 6 characters
            if (password.Length < 6 || password.Length > 22)
                throw new Exception("Password must be between 6 and 22 characters long");

            //set users password
            usersById[id].password = password;

            //save to the password file if the filesystem exists
            if (filesystem != null)
                SavePasswordFile();
        }

        public int Authenticate(string username, string password)
        {
            // authenticate user by username/password
            // fine user by username
            int id = UserID(username);

            //check passwords match
            if (usersById[id].password != password)
                throw new Exception("Authentication failed!");

            //return user id
            return id;
        }

        public string UserName(int userID)
        {
            // lookup user by user id and return username
            if (!usersById.ContainsKey(userID))
                throw new Exception("User not found!");
            return usersById[userID].userName;
        }

        public string UserHomeDirectory(int userID)
        {
            // lookup user by user id and return home directory
            if (!usersById.ContainsKey(userID))
                throw new Exception("User not found!");

            return usersById[userID].homeDirectory;
        }

        public string UserPreferredShell(int userID)
        {
            // lookup user by user id and return shell name
            if (!usersById.ContainsKey(userID))
                throw new Exception("User not found!");

            return usersById[userID].shell;
        }
    }
}
