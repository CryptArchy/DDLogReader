using System;
using System.Text.RegularExpressions;

namespace DDLogReader {
    public class LogLine {
        private static readonly Regex LogLineRegex = new Regex(
            @"^(?<client>\S+)\s\S+\s(?<userid>\S+)\s\[(?<datetime>[^\]]+)\]\s\""(?<method>[A-Z]+)\s(?<request>[^\s\""]+)?\sHTTP/[0-9.]+\""\s(?<status>[0-9]{3})\s(?<size>[0-9]+|-)");
        public string Client {get;set;}
        public string User {get;set;}
        public DateTimeOffset Occurance {get;set;}
        public string Method {get;set;}
        public string Path {get;set;}
        public int StatusCode {get;set;}
        public long Size {get;set;}

        public LogLine(string line) {
            var matches = LogLineRegex.Match(line);
            var groups = matches.Groups;
            this.Client = groups["client"].Value;
            this.User = groups["userid"].Value;
            this.Occurance = DateTimeOffset.ParseExact(groups["datetime"].Value, "dd/MMM/yyyy:HH:mm:ss zzz", null);
            this.Method = groups["method"].Value;
            this.Path = groups["request"].Value;
            this.StatusCode = Int32.Parse(groups["status"].Value);
            this.Size = Int64.Parse(groups["size"].Value);
        }
    }
}
