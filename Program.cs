using System;
using System.Text.Json;
using static System.Console;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using static System.Environment;
using System.Text;
using System.Collections.Generic;

namespace LifeSpace
{
    class Program
    {
        static (string Name, Func<(Activity Activity, string Value), Activity> Handler)[] EditHandlers = {
            ("name", (args) => args.Activity with {Name = args.Value}),
            ("importance", (args) => args.Activity with {Importance = new (Int32.Parse(args.Value))}),
            ("effort", (args) => args.Activity with {Effort = new (Int32.Parse(args.Value))}),
            ("pleasure", (args) => args.Activity with {Pleasure = new (Int32.Parse(args.Value))}),
            ("urgency", (args) => args.Activity with {Urgency = args.Activity.Urgency with {Urgency = args.Value.Split("->") is var (start, end, _) ? new(new(Int32.Parse(start)), new(Int32.Parse(end))): throw new Exception()}}),
            ("urgency-dates", (args) => args.Activity with {Urgency = args.Activity.Urgency with {Interval = args.Value.Split("->") is var (start, end, _) ? new(DateTime.Parse(start), DateTime.Parse(end)): throw new Exception()}}),
        };

        static T Collect<T>(string prompt, Func<string, T> processor)
        {
            Write($"{prompt}: ");
            var input = ReadLine();
            if (input == null)
            {
                throw new InvalidOperationException();
            }

            try
            {
                return processor(input);
            }
            catch
            {
                return Collect(prompt, processor);
            }
        }

        static Activity AddNewActivity()
        {
            var name = Collect("Activity name", x => x);
            var importance = Collect<Importance>("Importance 0 - 100", x => new(Int32.Parse(x)));
            var effort = Collect<Effort>($"Effort 0 - {Effort.MaxValue}", x => new(Int32.Parse(x)));
            var pleasure = Collect<Pleasure>("Pleasure -100 - 100", x => new(Int32.Parse(x)));

            var dateChange = Collect<Delta<DateTime>>("Urgency dates (\"start->due\")", x =>
            {
                var split = x.Split("->");
                return new(DateTime.Parse(split[0]), DateTime.Parse(split[1]));
            });
            var urgencyChange = Collect<Delta<Urgency>>("Urgency change (\"initial->final\")", x =>
            {
                var split = x.Split("->");
                return new(new(Int32.Parse(split[0])), new(Int32.Parse(split[1])));
            });

            return new(
                name,
                new(dateChange, urgencyChange),
                importance,
                effort,
                pleasure
            );
        }

        static void WriteLineColor(string value, ConsoleColor? color = null)
        {
            var oldColor = ForegroundColor;
            ForegroundColor = color ?? oldColor;
            Console.WriteLine(value);
            ForegroundColor = oldColor;
        }

        static ConsoleColor? MarginColor(double margin) => margin > 4 ? ConsoleColor.Green : margin > 3 ? ConsoleColor.DarkYellow : ConsoleColor.Red;
        static ConsoleColor? PriorityColor(double priority) => priority > 3 ? ConsoleColor.Red : priority > 2 ? ConsoleColor.DarkYellow : null;
        static ConsoleColor? ValueColor(double valueRatio) => valueRatio > 20 ? ConsoleColor.Green : valueRatio > 10 ? ConsoleColor.Blue : null;

        static void RenderActivity(Activity activity)
        {
            WriteLine(activity.Name);
            WriteLine($"  Importance: {activity.Importance}");
            WriteLine($"  Effort: {activity.Effort}");
            WriteLine($"  Pleasure: {activity.Pleasure}");
            WriteLine($"  Urgency: {activity.Urgency.CurrentUrgency}");
            WriteLine($"    {activity.Urgency.Urgency.Start} -> {activity.Urgency.Urgency.End}");
            WriteLine($"    {activity.Urgency.Interval.Start.ToIsoDate()} -> {activity.Urgency.Interval.End.ToIsoDate()}");
            WriteLineColor($"  Margin: {activity.Margin}", MarginColor(activity.Margin));
            WriteLineColor($"  Priority Summary: {activity.PrioritySummary}", PriorityColor(activity.PrioritySummary));
        }

        static Activity[] HandleCommand(Activity[] activities, (string command, IList<string?> args) input)
        {
            var (command, args) = input;
            if (command == "clear")
            {
                Clear();
                return activities;
            }

            if (command == "add")
            {
                var newActivity = AddNewActivity();
                return activities.Append(newActivity).ToArray();
            }

            if (command == "view"
                && args.SingleOrDefault()?.ToLowerInvariant() is string viewName
                && activities.SingleOrDefault(x => x.Name.ToLowerInvariant().StartsWith(viewName)) is Activity vActivity)
            {
                RenderActivity(vActivity);
                return activities;
            }

            if (command == "list")
            {
                var variant = args.FirstOrDefault();
                if (variant == null || variant == "priority")
                {
                    WriteLine("Priority listing:");
                    foreach (var a in activities.OrderByDescending(x => x.PrioritySummary))
                    {
                        WriteLineColor($"  {a.Name}: {a.PrioritySummary}", PriorityColor(a.PrioritySummary));
                    }
                }

                if (variant == "value")
                {
                    WriteLine("Low-hanging-fruit listing:");
                    foreach (var a in activities.OrderByDescending(x => x.ValueForEffort))
                    {
                        WriteLineColor($"  {a.Name}: {a.ValueForEffort}", ValueColor(a.ValueForEffort));
                    }
                }


                if (variant == "margin")
                {
                    WriteLine("Time-margin listing:");
                    foreach (var a in activities.OrderBy(x => x.Margin))
                    {
                        WriteLineColor($"  {a.Name}: {a.Margin}", MarginColor(a.Margin));
                    }
                }

                if (variant == "fun")
                {
                    WriteLine("Fun-for-effort listing:");
                    foreach (var a in activities.OrderByDescending(x => (int)x.PleasureForEffort))
                    {
                        WriteLineColor($"  {a.Name}: {a.PleasureForEffort}");
                    }
                }

                return activities;
            }

            if (command == "edit" && !args.Any())
            {
                foreach (var (Name, _) in EditHandlers)
                {
                    WriteLine($"  {Name}");
                    return activities;
                }
            }

            if (
                command == "edit" && args.FirstOrDefault()?.ToLowerInvariant() is string editName
                && activities.SingleOrDefault(x => x.Name.ToLowerInvariant().StartsWith(editName)) is Activity eActivity
                && args.ElementAtOrDefault(1) is string field
                && EditHandlers.SingleOrDefault((h) => h.Name == field) is var handler
                && args.ElementAtOrDefault(2) is string value
            )
            {
                var edited = handler.Handler((eActivity, value));
                RenderActivity(edited);
                return activities.Where(x => x != eActivity).Append(edited).ToArray();
            }

            return activities;
        }

        static string InitializeConfig()
        {
            var configDir = Path.Combine(GetFolderPath(SpecialFolder.UserProfile), ".life-space");
            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }

            var configPath = Path.Combine(configDir, "activities.json");
            if (!File.Exists(configPath))
            {

                using var file = File.Create(configPath);
                file.Write(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new Activity[0])));
            }
            return configPath;
        }

        static async Task Main(string[] args)
        {
            var legalCommands = new string[] { "v", "view", "a", "add", "d", "delete", "e", "edit", "l", "list", "c", "clear" };
            var configPath = InitializeConfig();
            var activities = JsonSerializer.Deserialize<Activity[]>(await File.ReadAllTextAsync(configPath))!;

            Console.Clear();
            while (true)
            {
                var command = Collect<(string, IList<string?>)>("", x =>
                {
                    var (c, args) = x.Split(" ");
                    var command = c.ToLowerInvariant();
                    if (!legalCommands.Contains(command))
                    {
                        throw new ArgumentException();
                    }
                    return (legalCommands.Single(x => x.Length > 1 && x.StartsWith(command[0])), args);
                });

                activities = HandleCommand(activities, command);
                await File.WriteAllTextAsync(configPath, JsonSerializer.Serialize(activities));
            }
        }
    }

}
