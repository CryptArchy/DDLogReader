using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace DDLogReader
{
    public class LogAggregator {
        // Time to wait in milliseconds between reports to the console
        private const int POSTING_DELAY = 10000;
        private LogReader Reader {get;set;}
        private Timer FlushTimer {get;set;}
        private LogAggregate Current {get;set;}
        private List<LogAggregate> Aggregates {get;set;}
        private int MaxLogsPerSec {get;set;}
        private bool IsAlertTriggered {get;set;}

        public LogAggregator(LogReader reader, int maxLogsPerSec) {
            this.MaxLogsPerSec = maxLogsPerSec;
            this.Reader = reader;
            this.Current = new LogAggregate();
            this.Aggregates = new List<LogAggregate>();
            this.FlushTimer = new Timer(POSTING_DELAY);
            // There's probably a cleaner way to use Task.Delay or the TPL Dataflow library to do this
            // But the timer event method works well and is conceptually simple
            this.FlushTimer.Elapsed += async ( sender, e ) => await Task.Run(() => ProcessAggregate());
            this.Reader.LogParsed += (sender, line) => ProcessLog(line);
            this.FlushTimer.Start();
        }

        private void ProcessLog(LogLine line) {
            // Console.Write("*");
            // Chop the path backwards at slashes to get counts for subsections
            var path = line.Path;
            // The root path is always available and always incremented
            this.Current.Counts["/"]++;
            while (!string.IsNullOrWhiteSpace(path)) {
                if (!this.Current.Counts.ContainsKey(path)) {
                    this.Current.Counts[path] = 1;
                }
                else {
                    this.Current.Counts[path]++;
                }
                path = path.Remove(path.LastIndexOf('/'));
            }
        }

        private void ProcessAggregate() {
            // Swap out the current aggregate so the processing thread can keep appending to it
            // Yeah, this probably needs an atomic swap to prevent race conditions and the aggregates list might need
            // to be replaced with a concurrent-safe collection, but it all works fine for a first pass.
            var prevAgg = this.Current;
            this.Current = new LogAggregate();
            this.Aggregates.Add(prevAgg);

            // Timestamp this moment for reference
            prevAgg.Occurance = DateTimeOffset.UtcNow;
            // Once the reader has caught up, taking the last 12 aggregates (each at 10 second intervals) gives the required 2 minute window
            // Admittedly, not the cleanest way to do it but quick and functional for now.
            prevAgg.RollingTotal = this.Aggregates.TakeLast(12).Sum(ag => ag.Counts["/"]);
            // Likewise, divide the running total seconds (120) to get the running average logs per second,
            // which is then used for alert checking
            prevAgg.RollingAverageLPS = prevAgg.RollingTotal / 120;

            if (prevAgg.RollingAverageLPS > this.MaxLogsPerSec) {
                this.IsAlertTriggered = true;
                Console.WriteLine($"High traffic generated an alert - hits = {prevAgg.RollingTotal}, triggered at {prevAgg.Occurance}");
            }
            else if (this.IsAlertTriggered){
                this.IsAlertTriggered = false;
                Console.WriteLine($"Traffic back to normal - hits = {prevAgg.RollingTotal} at {prevAgg.Occurance}");
            }
            else {
                Console.WriteLine($"Status ok - hits = {prevAgg.RollingTotal} at {prevAgg.Occurance}");
            }

            foreach(var kvp in prevAgg.Counts.OrderByDescending(k => k.Value).ThenBy(k => k.Key.Length)) {
                if (kvp.Value > 0) {
                    Console.WriteLine($"{kvp.Key} : {kvp.Value}");
                }
            }
            Console.WriteLine("--------------------------------------------------");
        }
    }
}