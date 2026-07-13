$ErrorActionPreference = 'Stop'

dotnet restore FlowPainter.sln
dotnet build FlowPainter.sln -c Release --no-restore
dotnet test FlowPainter.sln -c Release --no-build --logger 'console;verbosity=normal'
