using System;
using System.Collections.Generic;
using Microsoft.Azure.Services.AppAuthentication;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace ClickSync
{
    class DBHelper{
        private string _sqlConnString;
        private SqlConnection _conn;

        public DBHelper(string sqlConnString){
            this._sqlConnString = sqlConnString;
        }

        public async Task Connect(){
            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            string accessToken = await azureServiceTokenProvider.GetAccessTokenAsync("https://database.windows.net/");
            try
            {
                this._conn = new SqlConnection(_sqlConnString);
                _conn.AccessToken = accessToken;
                _conn.Open();
            }
            catch (Exception ex)
            {
                Program.WriteLog("e",$"Error connecting to SQL, connection string: {_sqlConnString}\n error: {ex.Message}");
                await GraphHelper.SendMail($"Error connecting to SQL, connection string: {_sqlConnString}\n error: {ex.Message}", "There was an error in Click synchronization process");
                Environment.Exit(-1);
            }
        }

        public void Disconnect(){
            _conn.Close();
        }

        public async Task<List<ClickUser>> GetUsersFromDB(int numberOfUsersToGet)
        {
            try
            {
                List<ClickUser> clickUsers = new List<ClickUser>();
                var sqlCommand = $"SELECT TOP {numberOfUsersToGet} * FROM pratim_pp WHERE ClickSynced=0";
                SqlCommand cmd = new SqlCommand(sqlCommand, _conn);
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                    clickUsers.Add(new ClickUser(reader));
                reader.Close();
                await reader.DisposeAsync();
                return clickUsers;
            }
            catch (Exception ex)
            {
                Program.WriteLog("e",$"Error getting users from SQL!\n error: {ex.Message}");
                await GraphHelper.SendMail($"Error getting users from SQL!\n error: {ex.Message}", "There was an error in Click synchronization process");
                Environment.Exit(-1);
                return new List<ClickUser>();
            }
        }

        public async Task UpdateClickObjectID(string ClickObjectID, string TZ)
        {
            var sqlCommand = $"UPDATE Pratim_pp SET ClickObjectID='{ClickObjectID}', ClickSynced=1 WHERE TZ='{TZ}'";

            try
            {
                SqlCommand cmd = new SqlCommand(sqlCommand, _conn);
                cmd.ExecuteNonQuery();
                cmd.Dispose();
            }
            catch (System.Exception ex)
            {
                string message = $"Error updating record in SQL command: {sqlCommand} error: {ex.Message}";
                Program.WriteLog("e", message);
                WriteLog("ERROR", message);
                await GraphHelper.SendMail($"Error updating record in SQL command: {sqlCommand} error: {ex.Message}", "There was an error in Click synchronization process");
                Environment.Exit(-1);
            }
        }

        public void WriteLog(string type, string description)
        {
            var sqlCommand = $"INSERT INTO Sync_log (date,type,description) VALUES(GETDATE(),'{type}','{description}')";
            try
            {
                SqlCommand cmd = new SqlCommand(sqlCommand, _conn);
                cmd.ExecuteNonQuery();
                cmd.Dispose();
            }
            catch (System.Exception ex)
            {

                Program.WriteLog("e",$"Error writing to SQL {sqlCommand} error: {ex.Message}");
            }
        }

    }
}