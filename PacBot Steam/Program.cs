//Simple Steam Bot
//(using SteamKit 2)
//by "Pacnet Netty"

//Supports SteamGuard

//This is a command line basic steam bot that you can modify to whatever you need it to do on Steam.
//It was originally built to log chat messages from Steam.
//It uses SteamKit 2 so you can add in whatever functionality you want from it.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

using SteamKit2;

namespace PacBot_Steam
{
    class Program
    {
        private static SteamClient steamClient = new SteamClient(System.Net.Sockets.ProtocolType.Tcp);
        private static CallbackManager callbackManager = new CallbackManager(steamClient);

        private static SteamUser steamUser = steamClient.GetHandler<SteamUser>();
        private static SteamFriends steamFriends = steamClient.GetHandler<SteamFriends>();

        private static bool isRunning = false; //if the bot should be running or not

        private static bool isAFK = true; //if the owner of the bot is AFK and the bot should do special actions because of it

        private static string steamGuardSentryFile = "sentry.bin";

        static string authCode, twoFactorAuth;


        private static string steamUsername = "YOURUSERNAME"; //if you want to fork this bot or compile it for yourself, please enter your username and password in these fields. No, it is not stolen by SteamKit or anything like that.
        private static string steamPassword = "YOURPASSWORD";

        static void Main(string[] args)
        {
             isRunning = true;

             steamClient.Connect(); //attempts to create the connection to steam


             new Callback<SteamFriends.FriendMsgCallback>(onMessageReceived, callbackManager); //the main callback i'll use for the bot: it's when you receive a message on steam

             new Callback<SteamClient.ConnectedCallback>(onConnected, callbackManager); //callback for when we get connected to Steam
             new Callback<SteamClient.DisconnectedCallback>(onDisconnected, callbackManager); //callback for when we get disconnected from Steam

             new Callback<SteamUser.LoggedOnCallback>(onSignedIn, callbackManager); //callback for when we log on to steam
             new Callback<SteamUser.LoggedOffCallback>(onSignedOut, callbackManager); //callback for when we log off of steam

             new Callback<SteamUser.AccountInfoCallback>(onAccountInfo, callbackManager);

             new Callback<SteamFriends.FriendsListCallback>(onFriendsList, callbackManager);
             new Callback<SteamFriends.ChatInviteCallback>(onChatInvite, callbackManager);
             new Callback<SteamFriends.ChatMsgCallback>(onGroupMessage, callbackManager);


             // this callback is triggered when the steam servers wish for the client to store the sentry file
             new Callback<SteamUser.UpdateMachineAuthCallback>(OnMachineAuth, callbackManager);

             while (isRunning)
             {
                 //You can change the time here to check however fast/slow you want
                 callbackManager.RunWaitCallbacks(TimeSpan.FromMilliseconds(500)); //how fast the manager checks for new steam callbacks
             } 

                
        }

        //the actual functions for each of the callbacks I want to use
        static void onMessageReceived(SteamFriends.FriendMsgCallback callback)
        {
            //Check if it's an actual chat message
            if (callback.EntryType == EChatEntryType.ChatMsg)
            {


                //Print the SteamID of the user who messaged you
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("[" + callback.Sender.Render() + "] ");
               
                //Print the nickname
                Console.Write(steamFriends.GetFriendPersonaName(callback.Sender) + ": ");
               
                //Print the message
                Console.ForegroundColor = ConsoleColor.Gray;
                log(callback.Message);
                Console.ForegroundColor = ConsoleColor.White;

                if (callback.Message.ToLower() == "hello")
                {
                    //Sends "Hello {name}!" when it gets hello as a message.
                    steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "Hello " + steamFriends.GetFriendPersonaName(callback.Sender) + "!");
                }

                if(isAFK)
                {
                    steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "[PacBot] My owner is not online right now but will read your chat messages later.");
                }

            }
        }

        static void onConnected(SteamClient.ConnectedCallback callback)
        {
            
            if (callback.Result != EResult.OK) //if we didn't connect successfully, stop the bot
            {
                isRunning = false;
                Console.ReadLine();
                return;
            }

            byte[] sentryHash = null;
            if (File.Exists(steamGuardSentryFile))
            {
                // if we have a saved sentry file, read and sha-1 hash it
                byte[] sentryFile = File.ReadAllBytes(steamGuardSentryFile);
                sentryHash = CryptoHelper.SHAHash(sentryFile);
            }


            steamUser.LogOn(new SteamUser.LogOnDetails  
            {                                                                               
                Username = steamUsername, 
                Password = steamPassword, 

                AuthCode = authCode,

                TwoFactorCode = twoFactorAuth,

                SentryFileHash = sentryHash,
            });
        }

        static void onDisconnected(SteamClient.DisconnectedCallback callback)
        {
            log("#DISCONNECTED FROM STEAM, RETRYING CONNECTION#");
            //disconnected from steam


            log("Disconnected from Steam, reconnecting in 5...");

            Thread.Sleep(TimeSpan.FromSeconds(5));

            steamClient.Connect();
        }

        static void onSignedIn(SteamUser.LoggedOnCallback callback)
        {
            bool isSteamGuard = callback.Result == EResult.AccountLogonDenied;
            bool is2FA = callback.Result == EResult.AccountLogonDeniedNeedTwoFactorCode;

            if (isSteamGuard || is2FA)
            {
                log("This account is SteamGuard protected!");

                if (is2FA)
                {
                    Console.Write("Please enter your 2 factor auth code from your authenticator app: ");
                    twoFactorAuth = Console.ReadLine();
             
                }
                else
                {
                    Console.Write("Please enter the auth code sent to the email at {0}: ", callback.EmailDomain);
                    authCode = Console.ReadLine();
                }

                return;
            }

            if (callback.Result != EResult.OK)
            {
                log("#ERROR SIGNING IN TO STEAM, STOPPING BOT#");
                Console.ReadLine();
                isRunning = false;
            }

            log("#SIGNED INTO STEAM#");

        }

        static void onSignedOut(SteamUser.LoggedOffCallback callback)
        {
            log("#SIGNED OUT OF STEAM, STOPPING BOT#");
            Console.ReadLine();
            isRunning = false;
        }

        static void onAccountInfo(SteamUser.AccountInfoCallback callback)
        {
            log("#SETTING STATUS TO ONLINE#");
            steamFriends.SetPersonaState(EPersonaState.Online);
        } 

        static void onFriendsList(SteamFriends.FriendsListCallback callback)
        {

        }

        static void onChatInvite(SteamFriends.ChatInviteCallback callback)
        {

        }

        static void onGroupMessage(SteamFriends.ChatMsgCallback callback)
        {

        }

        static void OnMachineAuth(SteamUser.UpdateMachineAuthCallback callback)
        {
            log("Updating sentryfile...");

            byte[] sentryHash = CryptoHelper.SHAHash(callback.Data);

            // write out our sentry file
            File.WriteAllBytes("sentry.bin", callback.Data);

            // inform the steam servers that we're accepting this sentry file
            steamUser.SendMachineAuthResponse(new SteamUser.MachineAuthDetails
            {
                JobID = callback.JobID,

                FileName = callback.FileName,

                BytesWritten = callback.BytesToWrite,
                FileSize = callback.Data.Length,
                Offset = callback.Offset,

                Result = EResult.OK,
                LastError = 0,

                OneTimePassword = callback.OneTimePassword,

                SentryFileHash = sentryHash,
            });

            log("#UPDATED SENTRY FILE AND SENT AUTHENTICATION RESPONSE#");
        }

        static void log(string msg)
        {
            Console.WriteLine(msg);

            System.IO.File.WriteAllText(@"C:\Program Files (x86)\Steam\SSBLog.txt", Environment.NewLine + msg);
        }
    }
}
