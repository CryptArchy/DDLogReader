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

## Acknowledgements

Special thanks to the community fork of [CommandLineUtils](https://github.com/natemcmaster/CommandLineUtils) for making CLI parsing a breeze.

The Dockerfile and related techniques for containerizing dotnetcore code came from [the Microsoft dotnet-docker samples](https://github.com/dotnet/dotnet-docker).