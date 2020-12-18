using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using static Bullseye.Targets;
using static SimpleExec.Command;

namespace Xenial.Delicious.Beer.Recipes
{
    /// <summary>
    /// Class IISRecipe.
    /// </summary>
    public static class IISRecipe
    {
        /// <summary>
        /// Builds the and deploys IIS projects using SimpleExec and Bullseye.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="postfix">The postfix.</param>
        public static void BuildAndDeployIISProject(IISDeployOptions options, string postfix = "")
        {
            _ = options ?? throw new ArgumentNullException(nameof(options));

            if (!string.IsNullOrEmpty(postfix))
            {
                if (!postfix.StartsWith(":"))
                {
                    postfix = $":{postfix}";
                }
            }

            var version = new Lazy<Task<string>>(async () => (await ReadToolAsync(() => ReadAsync("dotnet", "minver -v e", noEcho: true))).Trim());
            var branch = new Lazy<Task<string>>(async () => (await ReadAsync("git", "rev-parse --abbrev-ref HEAD", noEcho: true)).Trim());
            var lastUpdate = new Lazy<Task<string>>(async () => $"{UnixTimeStampToDateTime(await ReadAsync("git", "log -1 --format=%ct", noEcho: true)):yyyy-MM-dd}");
            var hash = new Lazy<Task<string>>(async () => (await ReadAsync("git", "rev-parse HEAD", noEcho: true)).Trim());

            async Task<string> assemblyProperties() => $"/property:LastUpdate={await lastUpdate.Value} /property:GitBranch={await branch.Value} /property:GitHash={await hash.Value} {options.AssemblyProperties}";

            Target($"prepare{postfix}", async () => await options.PrepareTask());

            if (options.DotnetCore)
            {
                Target($"publish{postfix}", DependsOn($"prepare{postfix}"),
                    async () => await RunAsync("dotnet", $"build {options.PathToCsproj} /p:Configuration={options.Configuration} /p:RuntimeIdentifier={options.RuntimeIdentifier} /p:SelfContained={options.SelfContained} /p:PackageAsSingleFile={options.PackageAsSingleFile} /p:DeployOnBuild=true /p:WebPublishMethod=package /p:PublishProfile=Package /v:minimal /p:DesktopBuildPackageLocation={options.Artifact} /p:DeployIisAppPath={options.PackageName} {await assemblyProperties()}")
                );
            }
            else
            {
                Target($"publish{postfix}", DependsOn($"prepare{postfix}"),
                    async () => await RunAsync("dotnet", $"msbuild {options.PathToCsproj} /t:Restore;Build /p:Configuration={options.Configuration} /p:RuntimeIdentifier={options.RuntimeIdentifier} /p:SelfContained={options.SelfContained} /p:PackageAsSingleFile={options.PackageAsSingleFile} /p:DeployOnBuild=true /p:WebPublishMethod=package /p:PublishProfile=Package /v:minimal /p:DesktopBuildPackageLocation={options.Artifact} /p:DeployIisAppPath={options.PackageName} {await assemblyProperties()}")
                );
            }

            Target($"deploy{postfix}", DependsOn($"publish{postfix}"),
                async () => await RunAsync("cmd.exe", $"/C {options.ProjectName}.deploy.cmd /Y /M:{await options.GetWebdeployIP()} /U:{await options.GetWebdeployUser()} /P:{await options.GetWebdeployPass()} -allowUntrusted -enableRule:AppOffline", workingDirectory: options.WebdeployArtifactsLocation)
            );

            static async Task<string> ReadToolAsync(Func<Task<string>> action)
            {
                try
                {
                    return await action();
                }
                catch (SimpleExec.NonZeroExitCodeException)
                {
                    Console.WriteLine("Tool seams missing. Try to restore");
                    await RunAsync("dotnet", "tool restore");
                    return await action();
                }
            }

            static DateTime UnixTimeStampToDateTime(string unixTimeStamp)
            {
                var time = double.Parse(unixTimeStamp.Trim());
                var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                dtDateTime = dtDateTime.AddSeconds(time).ToLocalTime();
                return dtDateTime;
            }
        }
    }

    /// <summary>
    /// Class IISDeployOptions.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public record IISDeployOptions(string ProjectName, string PackageName)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
        /// <summary>
        /// Gets or sets the path to csproj.
        /// Defaults to $"src/{ProjectName}/{ProjectName}.csproj"
        /// </summary>
        /// <value>The path to csproj.</value>
        public string PathToCsproj { get; set; } = $"src/{ProjectName}/{ProjectName}.csproj";

        /// <summary>
        /// Gets the artifacts location.
        /// Defaults to Path.GetFullPath($"./artifacts")
        /// </summary>
        /// <value>The artifacts location.</value>
        public string ArtifactsLocation { get; set; } = Path.GetFullPath($"./artifacts");

        /// <summary>
        /// Gets the webdeploy artifacts location.
        /// Defaults to Path.GetFullPath($"{ArtifactsLocation}/webdeploy");
        /// </summary>
        /// <value>The webdeploy artifacts location.</value>
        public string WebdeployArtifactsLocation => Path.GetFullPath($"{ArtifactsLocation}/webdeploy");

        /// <summary>
        /// Gets the artifact.
        /// Value is Path.GetFullPath($"{WebdeployArtifactsLocation}/{ProjectName}.zip")
        /// </summary>
        /// <value>The artifact.</value>
        public string Artifact => Path.GetFullPath($"{WebdeployArtifactsLocation}/{ProjectName}.zip");

        /// <summary>
        /// Gets or sets the configuration.
        /// Defaults to Release
        /// </summary>
        /// <value>The configuration.</value>
        public string Configuration { get; set; } = "Release";

        /// <summary>
        /// Gets or sets a value indicating whether [self contained].
        /// </summary>
        /// <value><c>true</c> if [self contained]; otherwise, <c>false</c>.</value>
        public bool SelfContained { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [package as single file].
        /// </summary>
        /// <value><c>true</c> if [package as single file]; otherwise, <c>false</c>.</value>
        public bool PackageAsSingleFile { get; set; }

        /// <summary>
        /// Gets or sets the dotnet core.
        /// </summary>
        /// <value>The dotnet core.</value>
        /// <autogeneratedoc />
        public bool DotnetCore { get; set; }

        /// <summary>
        /// Gets or sets the runtime identifier.
        /// Defaults to "win-x64"
        /// </summary>
        /// <value>The runtime identifier.</value>
        public string RuntimeIdentifier { get; set; } = "win-x64";

        /// <summary>
        /// Gets or sets the additional assembly build properties.
        /// </summary>
        /// <value>The assembly properties.</value>
        public string AssemblyProperties { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the get webdeploy ip.
        /// Defaults to Environment.GetEnvironmentVariable("WEBDEPLOY_IP")
        /// </summary>
        /// <value>The get webdeploy ip.</value>
        public Func<Task<string?>> GetWebdeployIP { get; set; } = () => Task.FromResult<string?>(Environment.GetEnvironmentVariable("WEBDEPLOY_IP"));

        /// <summary>
        /// Gets or sets the get webdeploy user.
        /// Defaults to Environment.GetEnvironmentVariable("WEBDEPLOY_USER")
        /// </summary>
        /// <value>The get webdeploy user.</value>
        public Func<Task<string?>> GetWebdeployUser { get; set; } = () => Task.FromResult<string?>(Environment.GetEnvironmentVariable("WEBDEPLOY_USER"));

        /// <summary>
        /// Gets or sets the get webdeploy pass.
        /// Defaults to Environment.GetEnvironmentVariable("WEBDEPLOY_PASS")
        /// </summary>
        /// <value>The get webdeploy pass.</value>
        public Func<Task<string?>> GetWebdeployPass { get; set; } = () => Task.FromResult<string?>(Environment.GetEnvironmentVariable("WEBDEPLOY_PASS"));

        /// <summary>
        /// Gets or sets the prepare task.
        /// </summary>
        /// <value>The prepare task.</value>
        public Func<Task> PrepareTask { get; set; } = () => Task.CompletedTask;
    }
}
