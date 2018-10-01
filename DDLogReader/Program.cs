using System;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;

namespace DDLogReader
{
    public class Program {
        static int Main(string[] args) {
            var app = new CommandLineApplication
            {
                Name = "Datadog Loggragator",
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
                var agg = new LogAggregator(p, maxLogsPerSec);
                var tw = g.WriteFile();
                var tr = p.ReadFile();
                Task.WaitAll(tw, tr);
            }
        }

        private static void RunWriter(string filename, int delay) {
            Console.WriteLine($"Writing to {filename}");
            using(var g = new LogWriter(filename, delay)) {
                var t = g.WriteFile();
                t.Wait();
            }
        }

        private static void RunReader(string filename, int maxLogsPerSec) {
            Console.WriteLine($"Monitoring {filename}");
            using(var p = new LogReader(filename)) {
                var agg = new LogAggregator(p, maxLogsPerSec);
                var t = p.ReadFile();
                t.Wait();
            }
        }
    }
}

/*

Display stats every 10s about the traffic during those 10s: the sections of the web site with the most hits, as well as interesting summary statistics on the traffic as a whole. A section is defined as being what's before the second '/' in the path. For example, the section for "http://my.site.com/pages/create” is "http://my.site.com/pages".

Make sure a user can keep the app running and monitor the log file continuously

Whenever total traffic for the past 2 minutes exceeds a certain number on average, add a message saying that “High traffic generated an alert - hits = {value}, triggered at {time}”. The default threshold should be 10 requests per second and should be overridable.

Whenever the total traffic drops again below that value on average for the past 2 minutes, print or displays another message detailing when the alert recovered.

Write a test for the alerting logic.

Explain how you’d improve on this application design.

If you have access to a linux docker environment, we'd love to be able to docker build and run your project! If you don't though, don't sweat it. As an example:



FROM python:3
RUN touch /var/log/access.log # since the program will read this by default
WORKDIR /usr/src
ADD . /usr/src
ENTRYPOINT ["python", "main.py"]
 */