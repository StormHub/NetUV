// Usings
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

// Arguments
var target = Argument<string>("target", "Default");
var source = Argument<string>("source", null);
var apiKey = Argument<string>("apikey", null);
var releaseNote = ParseReleaseNotes("./RELEASE_NOTES.md");
var buildNumber = EnvironmentVariable("BUILD_NUMBER") ?? "0";
var version = Argument<string>("targetversion", $"{releaseNote.Version}.{buildNumber}-beta");
var skipClean = Argument<bool>("skipclean", false);
var skipTests = Argument<bool>("skiptests", false);
var nogit = Argument<bool>("nogit", false);

// Variables
var configuration = IsRunningOnWindows() ? "Release" : "MonoRelease";
var projectJsonFiles = GetFiles("./src/**/project.json");

// Directories
var nuget = Directory(".nuget");
var output = Directory("build");
var outputBinaries = output + Directory("binaries");
var outputBinariesNet46 = outputBinaries + Directory("net46");
var outputBinariesNetstandard = outputBinaries + Directory("netstandard1.3");
var outputPackages = output + Directory("packages");
var outputNuGet = output + Directory("nuget");
var outputPerfResults = Directory("perfResults");

///////////////////////////////////////////////////////////////

Task("Clean")
  .Does(() =>
{
  // Clean artifact directories.
  CleanDirectories(new DirectoryPath[] {
    output, outputBinaries, outputPackages, outputNuGet,
    outputBinariesNet46, outputBinariesNetstandard
  });

  if(!skipClean) {
    // Clean output directories.
    CleanDirectories("./**/bin/" + configuration);
    CleanDirectories("./**/obj/" + configuration);
  }
});

Task("Restore-NuGet-Packages")
  .Description("Restores dependencies")
  .Does(() =>
{
  var settings = new DotNetCoreRestoreSettings
  {
    Verbose = true,
    Verbosity = DotNetCoreRestoreVerbosity.Warning
  };

  DotNetCoreRestore("./", settings);
});

Task("Compile")
  .Description("Builds the solution")
  .IsDependentOn("Clean")
  .IsDependentOn("Restore-NuGet-Packages")
  .Does(() =>
{
  DotNetCoreBuild("./**/project.json", new DotNetCoreBuildSettings {
      Configuration = configuration,
      Verbose = true
    });
});

Task("Test")
  .Description("Executes xUnit tests")
  .WithCriteria(!skipTests)
  .IsDependentOn("Compile")
  .Does(() =>
{
  var projects = GetFiles("./test/**/*.xproj")
    - GetFiles("./test/**/*.Performance.xproj");

  foreach(var project in projects)
  {
      DotNetCoreTest(project.GetDirectory().FullPath, new DotNetCoreTestSettings
        {
          Configuration = configuration,
          Verbose = true
        });
  }
});

Task("Package-NuGet")
  .Description("Generates NuGet packages for each project that contains a nuspec")
  .Does(() =>
{
  var projects = GetFiles("./src/**/*.xproj");
  foreach(var project in projects)
  {
    var settings = new DotNetCorePackSettings {
      Configuration = configuration,
      OutputDirectory = outputNuGet
    };

    DotNetCorePack(project.GetDirectory().FullPath, settings);
  }
});

Task("Publish-NuGet")
  .Description("Pushes the nuget packages in the nuget folder to a NuGet source. Also publishes the packages into the feeds.")
  .Does(() =>
{
  // Make sure we have an API key.
  if(string.IsNullOrWhiteSpace(apiKey)){
    throw new CakeException("No NuGet API key provided.");
  }

  // Upload every package to the provided NuGet source (defaults to nuget.org).
  var packages = GetFiles(outputNuGet.Path.FullPath + "/*" + version + ".nupkg");
  foreach(var package in packages)
  {
    NuGetPush(package, new NuGetPushSettings {
      Source = source,
      ApiKey = apiKey
    });
  }
});

///////////////////////////////////////////////////////////////

Task("Benchmark")
  .Description("Runs benchmarks")
  .IsDependentOn("Compile")
  .Does(() =>
{
  var libraries = GetFiles("./test/**/bin/" + configuration + "/netcoreapp1.1/*.Performance.dll");
  CreateDirectory(outputPerfResults);

  foreach (var lib in libraries)
  {
    Information("Benchmark: {0}", lib);
    int result = StartProcess(" dotnet " + lib, new ProcessSettings { WorkingDirectory = lib.GetDirectory() } );
    if (result != 0)
    {
      throw new CakeException($"Benchmark failed. {lib}");
    }
  }
});

///////////////////////////////////////////////////////////////

Task("Tag")
  .Description("Tags the current release.")
  .Does(() =>
{
  StartProcess("git", new ProcessSettings {
    Arguments = string.Format("tag \"v{0}\"", version)
  });
});

Task("Prepare-Release")
  .Does(() =>
{
  // Update version.
  UpdateProjectJsonVersion(version, projectJsonFiles);

    // Add
    foreach (var file in projectJsonFiles)
    {
      if (nogit)
      {
        Information("git " + string.Format("add {0}", file.FullPath));
      }
      else
      {
        StartProcess("git", new ProcessSettings {
          Arguments = string.Format("add {0}", file.FullPath)
        });
      }
    }

    // Commit
    if (nogit)
    {
      Information("git " + string.Format("commit -m \"Updated version to {0}\"", version));
    }
    else
    {
      StartProcess("git", new ProcessSettings {
        Arguments = string.Format("commit -m \"Updated version to {0}\"", version)
      });
    }

    // Tag
    if (nogit)
    {
      Information("git " + string.Format("tag \"v{0}\"", version));
    }
    else
    {
      StartProcess("git", new ProcessSettings {
        Arguments = string.Format("tag \"v{0}\"", version)
      });
    }

    //Push
    if (nogit)
    {
      Information("git push origin master");
      Information("git push --tags");
    }
    else
    {
      StartProcess("git", new ProcessSettings {
        Arguments = "push origin master"
      });

      StartProcess("git", new ProcessSettings {
        Arguments = "push --tags"
      });
    }
});

Task("Update-Version")
  .Does(() =>
{
  if(string.IsNullOrWhiteSpace(version)) {
    throw new CakeException("No version specified!");
  }

  UpdateProjectJsonVersion(version, projectJsonFiles);
});

///////////////////////////////////////////////////////////////

public void UpdateProjectJsonVersion(string version, FilePathCollection filePaths)
{
  Verbose(logAction => logAction("Setting version to {0}", version));
  foreach (var file in filePaths)
  {
    var project = System.IO.File.ReadAllText(file.FullPath, Encoding.UTF8);

    project = System.Text.RegularExpressions.Regex.Replace(project, "(\"version\":\\s*)\".+\"", "$1\"" + version + "\"");

    System.IO.File.WriteAllText(file.FullPath, project, Encoding.UTF8);
  }
}


Task("Default")
  .IsDependentOn("Test")
  //.IsDependentOn("Update-Version")
  .IsDependentOn("Package-NuGet");

Task("Mono")
  .IsDependentOn("Test");

Task("PR")
  .IsDependentOn("Test")
  .IsDependentOn("Benchmark")
  .IsDependentOn("Package-NuGet");

Task("Nightly")
  .IsDependentOn("Update-Version")
  .IsDependentOn("Package-NuGet");
  

///////////////////////////////////////////////////////////////

RunTarget(target);