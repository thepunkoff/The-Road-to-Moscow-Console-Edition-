using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Console = Colorful.Console;

namespace ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            dynamic current = AllObjects().Where(x => x.Properties.Id == "0x0100000000000277").FirstOrDefault();

            string firstText = current.Properties.Text;
            (string Speaker, Color Color) firstSpeaker = GetSpeaker(current);

            Console.BackgroundColor = InvertColor(firstSpeaker.Color);
            Console.Write($"{firstSpeaker.Speaker}:", Color.Black);
            Console.BackgroundColor = Color.Black;
            Console.WriteLine($" {firstText}\n", firstSpeaker.Color);

            double firstSeconds = (firstText.Length * 0.06) > 1 ? (firstText.Length * 0.06) : 1;

            Wait((float)firstSeconds);

            while (true)
            {
                List<dynamic> candidates = GetCandidates(current);

                if (candidates.Count() == 1)
                {
                    var n = candidates[0];
                    var m = n.Properties;
                    current = AllObjects().Where(x => x.Properties.Id == candidates.ToList()[0].Properties.Id).FirstOrDefault();
                }
                else
                {
                    current = Choice(candidates);
                }

                (string Speaker, Color Color) speaker = GetSpeaker(current);
                
                string text = current.Properties.Text;

                Console.BackgroundColor = InvertColor(speaker.Color);
                Console.Write($"{speaker.Speaker}:", Color.Black);
                Console.BackgroundColor = Color.Black;
                Console.WriteLine($" {text}\n", speaker.Color);

                double seconds = (text.Length * 0.06) > 1 ? (text.Length * 0.06) : 1;

                Wait((float)seconds);
            }
        }

        private static Color InvertColor(Color color)
        {
            var r = byte.MaxValue - color.R + 30;
            var g = byte.MaxValue - color.G + 30;
            var b = byte.MaxValue - color.B + 30;

            return Color.FromArgb(r, g, b);
        }

        private static void Wait(float seconds)
        {
            var cts = new CancellationTokenSource();
            var waitTask = Task.Run(() => WaitTask(seconds, cts));
            var readKeyTask = Task.Run(() => ReadKeyTask(seconds, cts));

            var start = DateTime.Now;

            var dotsCount = 0;
            double passedSeconds = 0;
            while (!cts.IsCancellationRequested)
            {
                passedSeconds = DateTime.Now.Subtract(start).TotalSeconds;

                if (passedSeconds % 1 == 0)
                {
                    if (dotsCount == 3)
                    {
                        Console.SetCursorPosition(0, Console.CursorTop);
                        Console.Write(new string(' ', Console.WindowWidth));
                        Console.SetCursorPosition(0, Console.CursorTop);
                        dotsCount = 0;
                    }
                    else
                    {
                        Console.Write(".");
                        dotsCount++;
                    }
                }
            }

            Console.SetCursorPosition(0, Console.CursorTop);
        }

        private static void ReadKeyTask(float seconds, CancellationTokenSource cts)
        {
            // problem here
            Console.ReadKey();
            cts.Cancel();
        }

        private static async Task WaitTask(float seconds, CancellationTokenSource cts)
        {
            await Task.Delay((int)(seconds * 1000));
            cts.Cancel();
        }

        private static (string Speaker, Color Color) GetSpeaker(dynamic current)
        {
            var speakerId = current.Properties.Speaker;

            var speaker = AllObjects().Where(x => x.Properties.Id == speakerId).FirstOrDefault();

            var r = (int)((double)speaker.Properties.Color.r * byte.MaxValue);
            var g = (int)((double)speaker.Properties.Color.g * byte.MaxValue);
            var b = (int)((double)speaker.Properties.Color.b * byte.MaxValue);

            return ((string)speaker.Properties.DisplayName, Color.FromArgb(r, g, b));
        }

        private static dynamic Choice(List<dynamic> candidates)
        {
            var choiceDic = new Dictionary<int, string>();

            for (int i = 1; i <= candidates.Count(); i++)
            {
                var candidate = candidates[i - 1];

                var menuText = candidate.Properties.MenuText;

                var text = $"[{i}] {menuText}\n";

                choiceDic.Add(i, text);

                Console.WriteLine(text, Color.Orange);
            }

            var key = Console.ReadKey(true);
            var answerInt = 0;
            if (char.IsDigit(key.KeyChar))
            {
                answerInt = int.Parse(key.KeyChar.ToString());
            }
            else
            {
                answerInt = -1;
            }

            Console.SetCursorPosition(Console.CursorLeft, Console.CursorTop - 2);
            Console.BackgroundColor = Color.SaddleBrown;
            var replacementText = choiceDic[answerInt];
            Console.WriteLine(replacementText, Color.Orange);
            Console.BackgroundColor = Color.Black;

            return candidates[answerInt - 1];
        }

        private static List<dynamic> GetCandidates(dynamic obj)
        {
            var type = obj.Type;
            var connections = (IEnumerable<dynamic>)obj.Properties.OutputPins[0].Connections;

            var candidates = connections.SelectMany(c => AllObjects().Where(o => o.Properties.Id == c.Target));

            return candidates.ToList();

        }

        private static IEnumerable<dynamic> AllObjects()
        {
            var path = $"{Environment.CurrentDirectory}/trtm.json";

            var json = File.ReadAllText(path);
            dynamic data = JsonConvert.DeserializeObject(json);
            return (IEnumerable<dynamic>)data.Packages[0].Models;
        }
    }
}
