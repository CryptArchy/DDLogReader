using System;
using System.IO;
using System.Threading.Tasks;
using System.Timers;

namespace DDLogReader
{
    public class LogReader : IDisposable {
        private FileStream File {get;set;}
        private TextReader Reader {get;set;}
        private LogAggregate Current {get;set;}
        private Timer FlushTimer {get;set;}
        private bool IsDisposed {get;set;}

        public event EventHandler<LogLine> LogParsed;

        protected void OnLogParsed(LogLine line) => LogParsed?.Invoke(this, line);

        /// <summary>
        /// Constructor for a LogReader that will process a given file.
        /// </summary>
        /// <param name="filename">Full path to the log file for processing. Will be held open in Read mode to monitor new lines. Must be in Common Logging format.</param>
        public LogReader(string filename) {
            try {
                this.File = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, true);
            }
            catch (ArgumentException) {
                Console.Write($"ERROR! Bad file '{filename}'");
            }
            catch (FileNotFoundException) {
                Console.Write($"ERROR! Bad file '{filename}'");
            }
            catch (Exception ex) {
                Console.Write($"ERROR! Unknown exception {ex.GetType().Name}");
            }
            this.Reader = new StreamReader(this.File);
        }

        /// <summary>
        /// Constructor that allows use of a custom reader, mainly for testing.
        /// </summary>
        /// <param name="reader">A TextReader object that should provide lines of text in Common Logging format.</param>
        public LogReader(TextReader reader) {
            this.Reader = reader;
        }

        public async Task ReadFile(System.Threading.CancellationToken canceller) {
            if (this.IsDisposed) {
                throw new ObjectDisposedException(nameof(LogReader));
            }
            while (true) {
                if(canceller.IsCancellationRequested) {
                    break;
                }

                var line = await this.Reader.ReadLineAsync();
                if (!string.IsNullOrWhiteSpace(line)) {
                    // The LogLine object parses each textual line and exposes the various fields
                    var ll = new LogLine(line);
                    OnLogParsed(ll);
                }
            }
        }

        public void Dispose() {
            if (this.IsDisposed) {
                throw new ObjectDisposedException(nameof(LogReader));
            }

            if (this.Reader != null) {
                this.Reader.Dispose();
                this.Reader = null;
            }

            if (this.File != null) {
                this.File.Dispose();
                this.File = null;
            }

            this.IsDisposed = true;
        }
    }
}