using System;
using System.IO;
using System.ServiceProcess;

namespace EspressoCommon
{
    public class Common
    {
        private readonly string ApplicationName = "Espresso";

        /// <summary>
        /// must use folder same fom service and commandline application (both running on different users!)
        /// Environment.SpecialFolder.CommonApplicationDat - The directory that serves as a common repository for application-specific data that is used by all users.
        /// </summary>
        private string AppDataPathForApplication => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), ApplicationName);

        private string LogFileName => $"{ApplicationName}.txt";
        private string SetupFileName => $"setup.json";

        public string LogFilePath => Path.Combine(AppDataPathForApplication, LogFileName);

        public string SetupFilePath => Path.Combine(AppDataPathForApplication, SetupFileName);

        public void WriteMsg(string message) 
        {
            Directory.CreateDirectory(AppDataPathForApplication);
            File.AppendAllLines(LogFilePath, new[] { $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}" });
        }

        public void WriteMsg(SessionChangeDescription changeDescription)
        {
            WriteMsg($"{GetUsername(changeDescription.SessionId)} {changeDescription.Reason}");
        }

        public string GetUsername(int sessionId) => GetUsernameBySession.Get(sessionId, true);


        public string ReadSetupFile()
        {
            return File.ReadAllText(SetupFilePath);
        }
    }
}
