using System;
using Application.TelegramBot;

namespace Application
{
    class Program
    {
        private static string SecretTokenVar = "TelegramBotSecret";
        private static string CredentialsEnvVar = "GoogleApiCredentials";
        private static string FirebaseEnvVar = "FirebaseSecret";

        static void Main(string[] args)
        {
            var credentialDirPath = Environment.GetEnvironmentVariable(CredentialsEnvVar);
            var credentialPath = credentialDirPath + "\\client_secrets.json";
            var secretToken = Environment.GetEnvironmentVariable(SecretTokenVar);

            var basePath = "https://schedule-generator-5f50e.firebaseio.com/";
            var firebaseSecret = Environment.GetEnvironmentVariable(FirebaseEnvVar);
            Console.WriteLine("Starting...");

            var bot = new TBot(secretToken, credentialPath, firebaseSecret, basePath);
            bot.Start();
            Console.WriteLine("Press Enter to stop the Bot");
            Console.ReadLine();
            bot.Stop();
        }
    }
}
