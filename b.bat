@pushd %~dp0
@dotnet run --project ".\build\build.csproj" --no-launch-profile -- %*
@popd