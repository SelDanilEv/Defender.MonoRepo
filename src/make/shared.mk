build : restore
	dotnet build --no-restore --configuration Release $(sln)
restore :
	dotnet restore --locked-mode $(sln)
restore-update:
	dotnet restore --force-evaluate $(sln)
test : build
	dotnet test --no-build --no-restore --configuration Release --logger trx --results-directory ../TestResults $(sln)
analyse : restore
	dotnet build --no-incremental --configuration Release $(sln)
	dotnet test --no-build --no-restore --logger junit --logger trx --configuration Release /p:CollectCoverage=true /p:CoverletOutputFormat=opencover $(sln)
clean :
	dotnet clean $(sln)
