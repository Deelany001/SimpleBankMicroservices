FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY TransactionService/TransactionService.csproj TransactionService/
RUN dotnet restore TransactionService/TransactionService.csproj
COPY TransactionService/ TransactionService/
RUN dotnet publish TransactionService/TransactionService.csproj -c Release -o /out --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "TransactionService.dll"]
