# Datadog Loggragator

A simple example application that monitors a log file and reports basic stats. Can also generate fake log files for demonstration.

## Usage

While not common in .NET development, the use of a Makefile and Docker makes working with this application straight forward.
Use the following `make` commands from the solution root directory:

+ `make build` to assemble the code inside a docker container
+ `make run` to execute the application in demo mode
+ `make reader` to execute the application in reader/monitor mode
+ `make writer` to execute the application in writer/generator mode
+ `make test` to build and run the unit tests inside a container
+ `make clean` to remove output directories and clean out some docker images

## Notes and Future Enhancements

These are my thoughts on the design and how it might be improved in the future (were this to be a real production application).

The CLI library I used was very convenient but I didn't have time to learn it's features in depth. I'm sure there must be a better way to handle default values than what I did, but I didn't want to spend extra time figuring it out.

There are some subtleties to how time works in this application that I'm not entirely happy with. The reader parses the entire log file first, potentially emitting events, before settling into a monitoring mode that's essentially real-time. This completely ignores the time stamps on the logs, which should probably be used to drive the emission of aggregates and alerts.

The way concurrency is used could be better. I think using something closer to an Actor model (shared queues of events for processing) would be cleaner and safer. Microsoft has a library for this with Tasks called TPL Dataflow but I haven't used it before and wasn't sure it was compatible with .NET CORE. For ease of use and simplicity, I went with events and basic use of Tasks.

I only exposed the maximum LPS for alerting as specified in the requirements. In a real app, I'd expose some additional configuration parameters, like the length of the rolling window and the aggregate time box.

Interfaces could be used more heavily. In particular it would be easy to expose the public functionality of the LogReader and LogAggregator classes as interfaces, which would aid composability. But there's no real gain from that enhancement with an app this simple. Interfaces would be useful if we wanted to swap out different readers (network logs or windows event logs) or different emitters (a graphical UI instead of console messages).

The way aggregates are calculated is hard coded and pretty basic. I know there are ways to use array buckets to generate histograms and some other data structures that could be used for calculating more interesting metrics, but I think this simple method works well for a prototype/example.

## Acknowledgements

Special thanks to the community fork of [CommandLineUtils](https://github.com/natemcmaster/CommandLineUtils) for making CLI parsing a breeze.

The Dockerfile and related techniques for containerizing dotnetcore code came from [the Microsoft dotnet-docker samples](https://github.com/dotnet/dotnet-docker).

And a very special thanks to you, code reviewer at Datadog, for taking the time to read this and look at my code!