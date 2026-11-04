using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace PepperDash.Essentials.Plugins.ContemporaryResearch.Tuner.Tests;

/// <summary>
/// Mobile Control picks its messengers from the interfaces a device implements (MessengerFactoryRegistry), so
/// dropping one of these silently removes a messenger. The plugin assembly is inspected, not executed.
/// </summary>
public class ConformanceTests : IDisposable
{
    private readonly MetadataLoadContext _context;
    private readonly Assembly _plugin;

    public ConformanceTests()
    {
        var baseDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
        var configuration = baseDir.Split(Path.DirectorySeparatorChar)[^2];
        var outputDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "bin", configuration, "net8.0"));
        var dllPath = Path.Combine(outputDir, "epi-contemporaryresearch-tuner.dll");
        if (!File.Exists(dllPath))
            throw new FileNotFoundException("Plugin DLL not found. Build the plugin first.", dllPath);

        var paths = new List<string>(Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll"));
        paths.AddRange(Directory.GetFiles(outputDir, "*.dll"));
        paths.AddRange(ResolveNugetAssemblies(Path.GetFullPath(Path.Combine(outputDir, "..", "..", "..", "obj", "project.assets.json"))));
        _context = new MetadataLoadContext(new PathAssemblyResolver(paths));
        _plugin = _context.LoadFromAssemblyPath(dllPath);
    }

    // The plugin references PepperDash.Essentials.Core and the Crestron SDK as NuGet packages that aren't copied next to it,
    // so find their compile assemblies through the restore output.
    private static IEnumerable<string> ResolveNugetAssemblies(string assetsPath)
    {
        if (!File.Exists(assetsPath)) yield break;

        using var doc = JsonDocument.Parse(File.ReadAllText(assetsPath));
        var root = doc.RootElement;
        var folder = root.GetProperty("packageFolders").EnumerateObject().First().Name;
        var target = root.GetProperty("targets").EnumerateObject().First().Value;

        foreach (var package in target.EnumerateObject())
        {
            if (!package.Value.TryGetProperty("compile", out var compile)) continue;
            var parts = package.Name.Split('/');
            var packageDir = Path.Combine(folder, parts[0].ToLowerInvariant(), parts[1]);
            foreach (var asset in compile.EnumerateObject())
            {
                if (!asset.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) continue;
                var full = Path.Combine(packageDir, asset.Name.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(full)) yield return full;
            }
        }
    }

    public void Dispose() => _context.Dispose();

    private Type Device => _plugin.GetType("PepperDash.Essentials.Plugins.ContemporaryResearch.Tuner.ContemporaryResearchDevice", true)!;

    [Theory]
    [InlineData("ISetTopBoxControls")]
    [InlineData("IChannel")]
    [InlineData("IDPad")]
    [InlineData("INumericKeypad")]
    [InlineData("ITransport")]
    [InlineData("IColor")]
    [InlineData("IHasPowerControlWithFeedback")]
    [InlineData("IBasicVolumeWithFeedbackAdvanced")]
    [InlineData("IBasicVolumeControls")]
    [InlineData("ITvPresetsProvider")]
    [InlineData("IDeviceInfoProvider")]
    [InlineData("ICommunicationMonitor")]
    [InlineData("IRoutingSource")]
    public void Device_Implements_The_Interface_Mobile_Control_Keys_On(string name)
    {
        Device.GetInterfaces().Select(i => i.Name).Should().Contain(name);
    }

    [Fact]
    public void Device_Factory_Registers_The_Plugin_Type()
    {
        var factory = _plugin.GetType("PepperDash.Essentials.Plugins.ContemporaryResearch.Tuner.ContemporaryResearchFactory", true)!;
        factory.Should().NotBeNull();
        factory.GetConstructor(Type.EmptyTypes).Should().NotBeNull();
    }

    [Fact]
    public void Mock_Factory_Exists_With_A_Parameterless_Constructor()
    {
        var factory = _plugin.GetType("PepperDash.Essentials.Plugins.ContemporaryResearch.Tuner.ContemporaryResearchMockFactory", true)!;
        factory.GetConstructor(Type.EmptyTypes).Should().NotBeNull();
    }

    [Fact]
    public void Simulated_Comms_Is_A_Basic_Communication()
    {
        var comms = _plugin.GetType("PepperDash.Essentials.Plugins.ContemporaryResearch.Tuner.TunerSimulatorComms", true)!;
        comms.GetInterfaces().Select(i => i.Name).Should().Contain("IBasicCommunication");
    }
}
