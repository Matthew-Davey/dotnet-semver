#! /bin/bash

dotnet nuget push $1 --source github --api-key $(pass GITHUB_ACCESS_TOKEN)
