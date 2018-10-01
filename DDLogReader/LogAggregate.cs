using System;
using System.Collections.Generic;

namespace DDLogReader
{
    public class LogAggregate {
        public DateTimeOffset Occurance {get;set;}
        public Dictionary<string, int> Counts {get;set;} = new Dictionary<string, int>{{"/", 0}};
        public int RollingTotal {get;set;}
        public double RollingAverageLPS {get;set;}
    }
}
