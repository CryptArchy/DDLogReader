using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DDLogReader
{
    public class LogWriter : IDisposable {
        private FileStream File {get;set;}
        private TextWriter Writer {get;set;}
        private Random Rng { get; } = new Random();
        private int Delay {get;set;}

        public LogWriter(string filename, int delay) {
            this.Delay = delay;
            try {
                this.File = new FileStream(filename, FileMode.Append, FileAccess.Write, FileShare.Read, 4096, true);
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
            this.Writer = new StreamWriter(this.File);
        }

        public async Task WriteFile(System.Threading.CancellationToken canceller){
            while (true) {
                if (canceller.IsCancellationRequested) {
                    break;
                }

                await Task.Delay(Rng.Next(this.Delay), canceller)
                        .ContinueWith(async (_) => {
                            await this.Writer.WriteLineAsync(RandomLogLine(this.Rng));
                            await this.Writer.FlushAsync();
                            // Console.Write("$");
                        });
            }
        }

        private static readonly List<string> NAMES = new List<string> {
            "james", "jill", "frank", "chris", "mary", "avery", "bailey", "carson", "drew", "kelsey", "lane", "marley"
        };
        private static readonly List<string> VERBS = new List<string> {
            "GET", "POST", "PUT", "PATCH", "DELETE"
        };
        private static readonly List<string> PATHS = new List<string> {
            "/api/user", "/report", "/api/post", "/api/comment", "/admin/portal", "/admin/user", "/admin/moderate"
        };
        private static readonly List<string> RESPONSES = new List<string> {
            "200", "404", "501"
        };

        public static string RandomLogLine(Random r) {
            var name = NAMES[r.Next(NAMES.Count)];
            var verb = VERBS[r.Next(VERBS.Count)];
            var path = PATHS[r.Next(PATHS.Count)];
            var response = RESPONSES[r.Next(RESPONSES.Count)];
            var dt = DateTimeOffset.UtcNow.ToString("dd/MMM/yyyy:HH:mm:ss zzz");
            return $"127.0.0.1 - {name} [{dt}] \"{verb} {path} HTTP/1.0\" {response} {r.Next()}";
        }

        public void Dispose() {
            if (this.Writer != null) {
                this.Writer.Dispose();
                this.Writer = null;
            }

            if (this.File != null) {
                this.File.Dispose();
                this.File = null;
            }
        }
    }
}
