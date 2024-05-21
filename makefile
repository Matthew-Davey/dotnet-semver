.DEFAULT_GOAL := build

restore:
	dotnet tool restore

format:
	dotnet fantomas ./dotnet-semver/Main.fs

build: restore format
	dotnet semver build --nologo --artifacts-path ./artifacts --configuration Release

pack: build
	dotnet semver pack --nologo --no-build --artifacts-path ./artifacts --configuration Release

push:
	dotnet nuget push ./artifacts/package/release/$(package) --source github --api-key $$(pass GITHUB_ACCESS_TOKEN) --skip-duplicate
	dotnet nuget push ./artifacts/package/release/$(package) --source nuget.org --api-key $$(pass NUGET_API_KEY) --skip-duplicate

