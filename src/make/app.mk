DOCKER_OPTS ?=

name:
	echo "$(app)" \
	| tr '[:upper:]' '[:lower:]' \
	| tr '.' '-' \
	| sed 's/^defender-//' \
	> app.name

publish:
	dotnet publish --no-restore --configuration Release -o ./publish "$(app)/$(app).csproj"

image:
	docker build \
		-f ../Dockerfile \
		--build-arg SERVICE=$(system) \
		--build-arg PROJECT=$(app) \\
		-t $(system):latest \
		.. $(DOCKER_OPTS)
