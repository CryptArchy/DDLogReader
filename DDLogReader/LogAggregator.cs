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
        private const int ROLLING_WINDOW = 120000;
        private LogReader Reader {get;set;}
        private Timer FlushTimer {get;set;}
        private LogAggregate Current {get;set;}
        private List<LogAggregate> Aggregates {get;set;}
        private int MaxLogsPerSec {get;set;}
        private int PostingDelay {get;set;}
        private int RollingWindow {get;set;}
        private bool IsAlertTriggered {get;set;}

        public event EventHandler<LogAggregate> AggregateProcessed;
        public event EventHandler<LogAggregate> AlertTriggered;
        public event EventHandler<LogAggregate> AlertCancelled;

        protected void OnAggregateProcessed(LogAggregate agg) => AggregateProcessed?.Invoke(this, agg);
        protected void OnAlertTriggered(LogAggregate agg) => AlertTriggered?.Invoke(this, agg);
        protected void OnAlertCancelled(LogAggregate agg) => AlertCancelled?.Invoke(this, agg);

        public LogAggregator(LogReader reader, int maxLogsPerSec, int postingDelay = POSTING_DELAY, int rollingWindow = ROLLING_WINDOW) {
            this.MaxLogsPerSec = maxLogsPerSec;
            this.PostingDelay = postingDelay;
            this.RollingWindow = rollingWindow;
            this.Reader = reader;
            this.Current = new LogAggregate();
            this.Aggregates = new List<LogAggregate>();
            this.FlushTimer = new Timer(this.PostingDelay);
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

            // With defaults of RollingWindow at 120k ms and PostingDelay at 10k ms, this gives a lookback of 12 for the required 2 minute window
            var lookback = this.RollingWindow / this.PostingDelay;

            // Timestamp this moment for reference
            prevAgg.Occurance = DateTimeOffset.UtcNow;
            // Once the reader has caught up, taking the last `lookback` aggregates gives the rolling window
            // Admittedly, not the cleanest way to do it but quick and functional for now.
            prevAgg.RollingTotal = this.Aggregates.TakeLast(lookback).Sum(ag => ag.Counts["/"]);
            // Likewise, divide the running total by the window in seconds (120) to get the running average logs per second,
            // which is then used for alert checking
            prevAgg.RollingAverageLPS = prevAgg.RollingTotal / (this.RollingWindow / 1000);

            if (prevAgg.RollingAverageLPS > this.MaxLogsPerSec) {
                this.IsAlertTriggered = true;
                OnAlertTriggered(prevAgg);
            }
            else if (this.IsAlertTriggered){
                this.IsAlertTriggered = false;
                OnAlertCancelled(prevAgg);
            }

            OnAggregateProcessed(prevAgg);
        }
    }
}