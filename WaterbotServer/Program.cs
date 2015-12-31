﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kappa;

namespace Waterbot.WaterbotServer
{
    internal class Program
    {
        private static TaskCompletionSource<bool> quitTask;

        private static List<string> Channels { get; set; }
        private static string OAuthKey { get; set; }
        private static string UserName { get; set; }

        private static void Main(string[] args)
        {
            var showHelp = false;

            var options = new Mono.Options.OptionSet();
            options.Add("user=", "The user name that Waterbot should use.",
                value => UserName = value);
            options.Add("key=", "The OAuth key to connect to Twitch chat with.",
                value => OAuthKey = value);
            options.Add("h|help|?", "Prints this message and exits.",
                value => showHelp = (value != null));

            Channels = options.Parse(args);
            if (Channels == null || Channels.Count == 0)
            {
                Channels = new List<string>();
                Channels.Add(UserName);
            }

            if (showHelp || UserName == null || OAuthKey == null)
            {
                Console.WriteLine("Runs the Waterbot server.");
                Console.WriteLine();
                Console.WriteLine("WaterbotServer --user=VALUE --key=VALUE [channel]");
                Console.WriteLine();
                options.WriteOptionDescriptions(Console.Out);
            }
            else
            {
                Task.WaitAll(MainAsync());
            }
        }

        private static async Task MainAsync()
        {
            Console.Title = "Waterbot server";

            // Prepare a task which completes when Ctrl+C is pressed
            quitTask = new TaskCompletionSource<bool>();
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                quitTask.SetResult(true);
            };

            using (var waterbot = new Waterbot())
            {
                waterbot.UserName = UserName;
                waterbot.OAuthKey = OAuthKey;
                waterbot.MessageReceived += Waterbot_MessageReceived;
                waterbot.MessageSent += Waterbot_MessageSent;

                await waterbot.StartAsync(Channels.ToArray());
                Console.WriteLine("Press Ctrl+C to stop Waterbot");

                // Wait until Ctrl+C is pressed, then exit gracefully
                await quitTask.Task;
                await waterbot.StopAsync();
            }
        }

        private static void Waterbot_MessageReceived(object sender, ChatMessageEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("[");
            Console.Write(e.Message.Channel);
            Console.Write("] ");

            if (e.Message.IsBroadcaster)
                Console.ForegroundColor = ConsoleColor.Red;
            else if (e.Message.IsAdmin)
                Console.ForegroundColor = ConsoleColor.Yellow;
            else if (e.Message.IsMod)
                Console.ForegroundColor = ConsoleColor.Green;
            else if (e.Message.IsSub)
                Console.ForegroundColor = ConsoleColor.Blue;
            else
                Console.ResetColor();

            Console.Write(e.Message.DisplayName);
            Console.Write(": ");

            Console.ResetColor();
            Console.WriteLine(e.Message.Contents);
        }

        private static void Waterbot_MessageSent(object sender, ChatMessageEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("[");
            Console.Write(e.Message.Channel);
            Console.Write("] ");

            Console.WriteLine(e.Message.Contents);
            Console.ResetColor();
        }
    }
}
