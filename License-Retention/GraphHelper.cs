using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Graph;
using System.Net.Http.Headers;
using Microsoft.Azure.Services.AppAuthentication;

namespace ClickSync
{
    class GraphHelper{
        public static async Task<GraphServiceClient> GetGraphApiClient()
        {
            try{
                var azureServiceTokenProvider = new AzureServiceTokenProvider();
                string accessToken = await azureServiceTokenProvider
                    .GetAccessTokenAsync("https://graph.microsoft.com/");

                var graphServiceClient = new GraphServiceClient(
                    new DelegateAuthenticationProvider((requestMessage) =>
                {
                    requestMessage
                            .Headers
                            .Authorization = new AuthenticationHeaderValue("bearer", accessToken);

                    return Task.CompletedTask;
                }));

                return graphServiceClient;
            }catch(Exception ex){
                Program.WriteLog("e",$"Error connecting Microsoft Graph.\n error: {ex.Message}");
                Environment.Exit(-1);
                return null;
            }  
        }

        public static async Task HandleUser(ClickUser clickUser, DBHelper db)
        {
            if (clickUser.clickObjectID == "" || clickUser.clickObjectID == null)
                await CreateUserInGraph(clickUser, db);
            else
                await UpdateUserInGraph(clickUser, db);
        }

        public static async Task UpdateUserInGraph(ClickUser clickUser, DBHelper db)
        {
            User user = new User();
            user.GivenName = clickUser.firstName;
            user.Surname = clickUser.lastName;
            user.MobilePhone = clickUser.mobilePhone;
            user.DisplayName = $"{clickUser.firstName} {clickUser.lastName}";
            user.State = "ClickSync";
            
            if(Program.disableUsers)
                user.AccountEnabled = clickUser.isActive;

            try
            {
                Program.WriteLog("d",$"updating user {clickUser.tz}{Program.userPrincipalNameSuffix}");
                var result =  await Program.graphServiceClient.Users[$"{clickUser.tz}{Program.userPrincipalNameSuffix}"].Request().UpdateAsync(user);
                string graphObjectId = clickUser.clickObjectID;
                if (graphObjectId == "")
                {
                    user = await Program.graphServiceClient.Users[$"{clickUser.tz}{Program.userPrincipalNameSuffix}"].Request().GetAsync();
                    graphObjectId = user.Id;
                }
                await db.UpdateClickObjectID(graphObjectId, clickUser.tz);
                Program.WriteLog("d",$"updated user {clickUser.tz}{Program.userPrincipalNameSuffix}");
            }
            catch (Exception ex)
            {
                if (((ServiceException)ex).StatusCode == System.Net.HttpStatusCode.NotFound)
                    await CreateUserInGraph(clickUser, db);
                else
                {
                    string str = $"error updating user {clickUser.tz}{Program.userPrincipalNameSuffix} Error: {ex.Message}";
                    Program.WriteLog("e", str);
                    db.WriteLog("ERROR", str);
                }
            }
        }

        public static async Task CreateUserInGraph(ClickUser clickUser, DBHelper db)
        {
            string password = GeneratePassword(4, 4, 4);
            var user = new User
            {
                GivenName = clickUser.firstName,
                Surname = clickUser.lastName,
                MobilePhone = clickUser.mobilePhone,
                ShowInAddressList = true,
                DisplayName = $"{clickUser.firstName} {clickUser.lastName}",
                MailNickname = clickUser.tz,
                UserPrincipalName = $"{clickUser.tz}{Program.userPrincipalNameSuffix}",
                State = "ClickSync",
                PasswordProfile = new PasswordProfile
                {
                    ForceChangePasswordNextSignIn = true,
                    Password = password
                }
            };

            if(Program.disableUsers)
                user.AccountEnabled = clickUser.isActive;
            else
                user.AccountEnabled = true;

            try
            {
                Program.WriteLog("d",$"creating user {clickUser.tz}{Program.userPrincipalNameSuffix}");
                var newUser = await Program.graphServiceClient.Users.Request().AddAsync(user);
                await db.UpdateClickObjectID(newUser.Id, clickUser.tz);
                Program.WriteLog("d",$"created user {clickUser.tz}{Program.userPrincipalNameSuffix}");
            }
            catch (Exception ex)
            {
                if (((ServiceException)ex).Error.Message == "Another object with the same value for property userPrincipalName already exists.")
                {
                    //TODO: update the object in azure and db
                    Program.WriteLog("d",$"warning user {clickUser.tz}{Program.userPrincipalNameSuffix} already exists will update now.");
                    await UpdateUserInGraph(clickUser, db);
                }
                else
                {
                    //TODO: write error to log
                    string str = $"Error creating user {clickUser.tz}{Program.userPrincipalNameSuffix} error: {ex.Message}";
                    db.WriteLog("ERROR", str);
                    Program.WriteLog("e",str);
                }
            }
        }

        public static async Task SendMail(string body, string subject)
        {
            bool sendMailNotification;
            string strSendMailNotification = Program.GetValue("sendMailNotification");
            bool.TryParse(strSendMailNotification, out sendMailNotification);
            if(!sendMailNotification)
                return;
                
            string to = Program.GetValue("mailNotificationTo");
            string from = Program.GetValue("mailNotificationFrom");

            var message = new Message
            {
                Subject = subject,
                Body = new ItemBody
                {
                    ContentType = BodyType.Text,
                    Content = body
                },
                ToRecipients = new List<Recipient>()
                {
                    new Recipient
                    {
                        EmailAddress = new EmailAddress
                        {
                            Address = to
                        }
                    }
                }
            };

            var saveToSentItems = true;
            try{
                await Program.graphServiceClient.Users[from]
                .SendMail(message, saveToSentItems)
                .Request()
                .PostAsync();
            }catch(Exception ex){
                string str = $"Error cannot send email, error: {ex.Message}";
                Program.WriteLog("e",str);
            }
            
        }

        public static string GeneratePassword(int lowercase, int uppercase, int numerics)
        {
            string lowers = "abcdefghijklmnopqrstuvwxyz";
            string uppers = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string number = "0123456789";

            Random random = new Random();

            string generated = string.Empty;
            for (int i = 1; i <= lowercase; i++)
                generated = generated.Insert(
                    random.Next(generated.Length),
                    lowers[random.Next(lowers.Length - 1)].ToString()
                );

            for (int i = 1; i <= uppercase; i++)
                generated = generated.Insert(
                    random.Next(generated.Length),
                    uppers[random.Next(uppers.Length - 1)].ToString()
                );

            for (int i = 1; i <= numerics; i++)
                generated = generated.Insert(
                    random.Next(generated.Length),
                    number[random.Next(number.Length - 1)].ToString()
                );

            return generated;
        }
    }
}