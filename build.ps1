Param([string] $rid = 'win10-x64')
dotnet publish Raznor -c Release -r $rid --self-contained true -p:PublishTrimmed=true -p:PublishSingleFile=true -o dist


