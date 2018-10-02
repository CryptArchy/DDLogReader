using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace DDLogReader
{
    /// <summary>
    /// Watches a LogReader and then aggregates the log entries over a configurable time window.
    /// </summary>
    public class LogAggregator {
        // Time to wait in milliseconds between reports to the console
        private const int POSTING_DELAY = 10000;
        // Time to use in milliseconds for calculating rolling totals and averages
        private const int ROLLING_WINDOW = 120000;

        private LogReader Reader {get;set;}
        private Timer FlushTimer {get;set;}
        private LogAggregate Current {get;set;}
        private List<LogAggregate> Aggregates {get;set;}
        private int MaxLogsPerSec {get;set;}
        private int PostingDelay {get;set;}
        private int RollingWindow {get;set;}
        private bool IsAlertTriggered {get;set;}

        /// <summary>
        /// Emits periodically based on the PostingDelay value with aggregated info from all logs recieved in that period.
        /// </summary>
        public event EventHandler<LogAggregate> AggregateProcessed;
        /// <summary>
        /// Emits if the Logs per Second is higher than the configured maximum during the period defined by the rolling window.
        /// </summary>
        public event EventHandler<LogAggregate> AlertTriggered;
        /// <summary>
        /// Emits when the Logs per Second has dropped back below the configured maximum.
        /// </summary>
        public event EventHandler<LogAggregate> AlertCancelled;

        protected void OnAggregateProcessed(LogAggregate agg) => AggregateProcessed?.Invoke(this, agg);
        protected void OnAlertTriggered(LogAggregate agg) => AlertTriggered?.Invoke(this, agg);
        protected void OnAlertCancelled(LogAggregate agg) => AlertCancelled?.Invoke(this, agg);

        /// <summary>
        /// Constructs a new LogAggregator with the provided LogReader and configuration options.
        /// </summary>
        /// <param name="reader">A prepared LogReader instance that will provide the necessary log events</param>
        /// <param name="maxLogsPerSec">The maximum average LPS beyond which alerts should be triggered</param>
        /// <param name="postingDelay">Duration in milliseconds between emitting aggregates, aka how big the time box should be</param>
        /// <param name="rollingWindow">Duration in milliseconds for the rolling window used for averaging LPS, must be longer than the posting delay</param>
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
                // Trigger an alert state if the average LPS over the window is above the configured maximum
                this.IsAlertTriggered = true;
                OnAlertTriggered(prevAgg);
            }
            else if (this.IsAlertTriggered){
                // Return from the alert state and trigger a return-to-normal when the average LPS drops back down
                this.IsAlertTriggered = false;
                OnAlertCancelled(prevAgg);
            }

            // Regardless of alert status, emit the processed event after every time box
            OnAggregateProcessed(prevAgg);
        }
    }
}