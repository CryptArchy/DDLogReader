using System;
using System.Linq;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;

namespace DDLogReader
{
    public class Program {
        static int Main(string[] args) {
            var app = new CommandLineApplication
            {
                Name = "Loggragator",
                Description = "An example utility for monitoring (and generating) simple log files.",
            };

            app.HelpOption(inherited: true);
            app.Command("read", cmd => {
                cmd.Description = "Runs the log reader and outputs alerts.";
                var filename = cmd.Argument("file", "name of file to monitor");
                var maxLogsPerSec = cmd.Option("-l|--lps <LPS>", "max logs per second before alerting", CommandOptionType.SingleValue);
                cmd.OnExecute(() => RunReader(filename.Value ?? "/var/log/access.log", int.Parse(maxLogsPerSec.Value() ?? "10")));
            });
            app.Command("write", cmd => {
                cmd.Description = "Runs the log generator writing to the given log file.";
                var filename = cmd.Argument("file", "name of file to monitor");
                var delay = cmd.Option("-d|--delay <DELAY>", "max wait time between writing logs", CommandOptionType.SingleValue);
                cmd.OnExecute(() => RunWriter(filename.Value ?? "/var/log/access.log", int.Parse(delay.Value() ?? "5000")));
            });
            app.Command("demo", cmd => {
                cmd.Description = "Runs the log generator and parser on seperate threads to demonstrate functionality.";
                var filename = cmd.Argument("file", "name of file to monitor");
                var delay = cmd.Option("-d|--delay <DELAY>", "max wait time between writing logs", CommandOptionType.SingleValue);
                var maxLogsPerSec = cmd.Option("-l|--lps <LPS>", "max logs per second before alerting", CommandOptionType.SingleValue);
                cmd.OnExecute(() => RunDemo(
                    filename.Value ?? "/var/log/access.log",
                    int.Parse(delay.Value() ?? "5000"),
                    int.Parse(maxLogsPerSec.Value() ?? "10")));
            });

            app.OnExecute(() => {
                Console.WriteLine("Specify a subcommand");
                app.ShowHelp();
                return 1;
            });

            return app.Execute(args);
        }

        private static void RunDemo(string filename, int delay, int maxLogsPerSec) {
            Console.WriteLine($"Demoing with {filename}");
            using(var g = new LogWriter(filename, delay))
            using(var p = new LogReader(filename)) {
                var aggregator = new LogAggregator(p, maxLogsPerSec);
                aggregator.AggregateProcessed += (sender, agg) => WriteStatus(agg);
                aggregator.AlertTriggered += (sender, agg) => WriteAlertTriggered(agg);
                aggregator.AlertCancelled += (sender, agg) => WriteAlertCancelled(agg);
                var cts = new System.Threading.CancellationTokenSource();
                var tr = p.ReadFile(cts.Token);
                var tw = g.WriteFile(cts.Token);
                Task.WaitAll(tw, tr);
            }
        }

        private static void RunWriter(string filename, int delay) {
            Console.WriteLine($"Writing to {filename}");
            using(var g = new LogWriter(filename, delay)) {
                var cts = new System.Threading.CancellationTokenSource();
                var t = g.WriteFile(cts.Token);
                t.Wait();
            }
        }

        private static void RunReader(string filename, int maxLogsPerSec) {
            Console.WriteLine($"Monitoring {filename}");
            using(var p = new LogReader(filename)) {
                var aggregator = new LogAggregator(p, maxLogsPerSec);
                aggregator.AggregateProcessed += (sender, agg) => WriteStatus(agg);
                aggregator.AlertTriggered += (sender, agg) => WriteAlertTriggered(agg);
                aggregator.AlertCancelled += (sender, agg) => WriteAlertCancelled(agg);

                var cts = new System.Threading.CancellationTokenSource();
                var t = p.ReadFile(cts.Token);
                t.Wait(cts.Token);
            }
        }

        private static void WriteStatus(LogAggregate agg) {
            Console.WriteLine($"Status Update - hits = {agg.RollingTotal} at {agg.Occurance}");
            foreach(var kvp in agg.Counts.OrderByDescending(k => k.Value).ThenBy(k => k.Key.Length)) {
                if (kvp.Value > 0) {
                    Console.WriteLine($"{kvp.Key} : {kvp.Value}");
                }
            }
            Console.WriteLine("--------------------------------------------------");
        }

        private static void WriteAlertTriggered(LogAggregate agg) {
            Console.WriteLine($"High traffic generated an alert - hits = {agg.RollingTotal}, triggered at {agg.Occurance}");
        }

        private static void WriteAlertCancelled(LogAggregate agg) {
            Console.WriteLine($"Traffic back to normal - hits = {agg.RollingTotal} at {agg.Occurance}");
        }
    }
}
