FROM microsoft/dotnet:2.1-sdk-alpine AS build
WORKDIR /app

# copy csproj and restore as distinct layers
COPY DDLogReader/*.csproj ./DDLogReader/
WORKDIR /app/DDLogReader
RUN dotnet restore

# copy and publish app and libraries
WORKDIR /app/
COPY DDLogReader/. ./DDLogReader/
WORKDIR /app/DDLogReader
RUN dotnet publish -c Release -o out


# test application -- see: dotnet-docker-unit-testing.md
FROM build AS testrunner
WORKDIR /app/Tests
COPY Tests/. .
ENTRYPOINT ["dotnet", "test", "--logger:trx"]


FROM microsoft/dotnet:2.1-runtime-alpine AS runtime
RUN touch /var/log/access.log
WORKDIR /app
COPY --from=build /app/DDLogReader/out ./
ENTRYPOINT ["dotnet", "DDLogReader.dll"]
