using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TASKS
{
    class Program
    {
        static Dictionary<string, CancellationTokenSource> tasks = new Dictionary<string, CancellationTokenSource>()
        {
            { "ShowSplash", new CancellationTokenSource() },
            { "RequestLicense", new CancellationTokenSource() },
            { "SetupMenus", new CancellationTokenSource() },
            { "CheckForUpdate", new CancellationTokenSource() },
            { "DownloadUpdate", new CancellationTokenSource() },
            { "DisplayWelcomeScreen", new CancellationTokenSource() },
            { "HideSplash", new CancellationTokenSource() }
        };

        static void taskTemplate(string name, int id, bool cancellationPossible = false)
        {
            Console.WriteLine(id + ".1) " + name + " started");
            Thread.Sleep(2000);

            if (cancellationPossible)
                RandomCancel(name);

            if (tasks[name].Token.IsCancellationRequested)
            {
                Console.WriteLine("ERROR: Task " + name + " crashed!");
                tasks[name].Token.ThrowIfCancellationRequested();
            }

            Console.WriteLine(id + ".2) " + name + " ended");
        }

        static void RandomCancel(string name, int chancePercentage = 40)
        {
            if (new Random().Next(100) < chancePercentage)
            {
                tasks[name].Cancel();
                Console.WriteLine("==> DEBUG: token " + name + " is cancelled");
            }
        }

        static void Main(string[] args)
        {
            Task ShowSplash = new Task(() => taskTemplate("ShowSplash", 0));

            Task RequestLicense = ShowSplash.ContinueWith(ant => taskTemplate("RequestLicense", 1, true));
            Task SetupMenus = RequestLicense.ContinueWith(ant => taskTemplate("SetupMenus", 2),
                TaskContinuationOptions.OnlyOnRanToCompletion);

            Task CheckForUpdate = ShowSplash.ContinueWith(ant => taskTemplate("CheckForUpdate", 3, true));
            Task DownloadUpdate = CheckForUpdate.ContinueWith(ant => taskTemplate("DownloadUpdate", 4),
                TaskContinuationOptions.OnlyOnRanToCompletion);

            Task DisplayWelcomeScreen = Task.WhenAll(SetupMenus, DownloadUpdate)
                .ContinueWith(ant => taskTemplate("DisplayWelcomeScreen", 5), TaskContinuationOptions.OnlyOnRanToCompletion);
            Task HideSplash = DisplayWelcomeScreen.ContinueWith(ant => taskTemplate("HideSplash", 6));

            try
            {
                ShowSplash.Start();

                Thread.Sleep(200);

                HideSplash.Wait();
            }
            catch (AggregateException ae)
            {
                foreach (Exception e in ae.InnerExceptions)
                {
                    if (e is TaskCanceledException)
                        Console.WriteLine("Task cancelled");
                }
            }
            finally
            {
                foreach (var ct in tasks.Values)
                {
                    ct.Dispose();
                }
            }


            if (HideSplash.IsCompletedSuccessfully)
            {
                Console.BackgroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("ALL TASKS COMPLETED SUCCESSFULLY");
            }
            else
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine("TASKS COMPLETED UNSUCCESSFULLY");
            }

            Console.ResetColor();
            Console.WriteLine("MAIN END");

            Console.ReadLine();
        }
    }
}
