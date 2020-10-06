using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Graph;
using System.Net.Http.Headers;
using System.Data.SqlClient;
using Microsoft.Azure.Services.AppAuthentication;

namespace ClickSync
{
    class Program
    {
        public static GraphServiceClient graphServiceClient;
        public static string userPrincipalNameSuffix;
        public static bool disableUsers = false;
        public static bool debug = false;

        static async Task Main(string[] args)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            #region Collect values
            string sqlConnString = GetValue("sqldb_connection");
            userPrincipalNameSuffix = GetValue("userPrincipalNameSuffix");
            string strRowsPerCycle = GetValue("rowsPerCycle");

            string strDebug = GetValue("debug");
            bool.TryParse(strDebug, out debug);

            string strDisableUsers = GetValue("disableUsers");
            bool.TryParse(strDisableUsers, out disableUsers);

            int rowsPerCycle;
            bool parsed = int.TryParse(strRowsPerCycle, out rowsPerCycle);
            if(!parsed)
                rowsPerCycle = 100;
            #endregion
            
            graphServiceClient = await GraphHelper.GetGraphApiClient();



            DBHelper db = new DBHelper(sqlConnString);
            await db.Connect();
            db.WriteLog("INFORMATION", $"Starting synchronization");
            int counter = 0;
            List<ClickUser> clickUsers = new List<ClickUser>();
            while (true)
            {
                clickUsers = await db.GetUsersFromDB(rowsPerCycle);
                counter += clickUsers.Count;
                if (clickUsers.Count == 0)
                    break;

                foreach (ClickUser clickUser in clickUsers)
                    await GraphHelper.HandleUser(clickUser, db);
            }


            watch.Stop();
            string message = $"Synchronization finished, synchronized {counter} objects in {watch.ElapsedMilliseconds * 0.001} seconds";
            db.WriteLog("INFORMATION", message);
            db.Disconnect();
            WriteLog("i",message);
            //await GraphHelper.SendMail(message, "Click synchronization finished");
        }

        public static string GetValue(string valueName){
            return Environment.GetEnvironmentVariable(valueName) != null ? Environment.GetEnvironmentVariable(valueName) : System.AppContext.GetData(valueName) as string; 
        }

        public static void WriteLog(string level, string str){
            switch (level)
            {
                case "i":
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("INFORMATUON: ");
                    Console.ResetColor();
                    Console.Write(str);
                    Console.WriteLine();
                    break;
                case "w":
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("WARNING: ");
                    Console.ResetColor();
                    Console.Write(str);
                    Console.WriteLine();
                    break;
                case "e":
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("ERROR: ");
                    Console.ResetColor();
                    Console.Write(str);
                    Console.WriteLine();
                    break;
                case "d":
                    if(debug){
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.Write("DEBUG: ");
                        Console.ResetColor();
                        Console.Write(str);
                        Console.WriteLine();
                    } 
                    break;
            }
        }
    }
}
