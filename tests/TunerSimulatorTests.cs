using FluentAssertions;
using Xunit;
using static PepperDash.Essentials.Plugins.ContemporaryResearch.Tuner.ContemporaryResearchProtocol;

namespace PepperDash.Essentials.Plugins.ContemporaryResearch.Tuner.Tests;

/// <summary>
/// The simulator is written from the manual's response formats. Feeding its output to the parsers checks the plugin
/// against the documented protocol rather than against itself.
/// </summary>
public class TunerSimulatorTests
{
    private static string Cmd(string command, string param = null) => param == null ? BuildCommand("1", command) : BuildCommand("1", command, param);

    [Fact]
    public void Status_Matches_The_Manuals_Layout()
    {
        var sim = new TunerSimulator("1", new[] { "32-2" });
        sim.Process(Cmd(CmdPwrOn));
        sim.Status().Should().Be("<1TU032U0A0002x0");
        sim.Audio().Should().Be("<1VU32Ux050");
    }

    [Fact]
    public void Power_Commands_Round_Trip_Through_The_Parser()
    {
        var sim = new TunerSimulator("1");

        var on = sim.Process(Cmd(CmdPwrOn)).Single();
        TryParseStatus(on, out var p1, out _).Should().BeTrue();
        p1.Should().BeTrue();

        var off = sim.Process(Cmd(CmdPwrOff)).Single();
        TryParseStatus(off, out var p2, out _).Should().BeTrue();
        p2.Should().BeFalse();

        TryParseStatus(sim.Process(Cmd(CmdPwrToggle)).Single(), out var p3, out _);
        p3.Should().BeTrue();
    }

    [Fact]
    public void Channel_Up_Down_Last_And_Direct_Tune()
    {
        var sim = new TunerSimulator("1", new[] { "2-1", "4-1", "5-1" });
        sim.Process(Cmd(CmdChanUp));
        sim.Channel.Should().Be("4-1");
        sim.Process(Cmd(CmdChanDn));
        sim.Channel.Should().Be("2-1");
        sim.Process(Cmd(CmdChanDn));
        sim.Channel.Should().Be("5-1", "the list wraps");
        sim.Process(Cmd(CmdChanLast));
        sim.Channel.Should().Be("2-1");
        sim.Process(Cmd(CmdTuneChannel, "=28:1"));
        sim.Channel.Should().Be("28-1");
        sim.Process(Cmd(CmdTuneChannel, "=32"));
        sim.Channel.Should().Be("32-1", "a bare major channel tunes minor 1 per the manual");
    }

    [Fact]
    public void Keypad_Digits_Dash_And_Enter_Tune_A_Channel()
    {
        var sim = new TunerSimulator("1");
        foreach (var key in new[] { ParamDigit2, ParamDigit8, ParamDash, ParamDigit1 })
            sim.Process(Cmd(CmdKeyEmu, key));
        sim.Channel.Should().NotBe("28-1", "nothing is tuned until enter");

        var response = sim.Process(Cmd(CmdKeyEmu, ParamKpEnter)).Single();
        sim.Channel.Should().Be("28-1");
        TryParseStatus(response, out _, out var channel).Should().BeTrue();
        channel.Should().Be("28-1");
    }

    [Fact]
    public void Volume_Mute_And_Level_Round_Trip_Through_The_Parser()
    {
        var sim = new TunerSimulator("1");
        sim.Process(Cmd(CmdPwrOn));

        var level = sim.Process(Cmd(CmdVolLevel, "=70")).Single();
        TryParseAudio(level, out var pct, out var muted).Should().BeTrue();
        pct.Should().Be(70);
        muted.Should().BeFalse();

        TryParseAudio(sim.Process(Cmd(CmdMuteOn)).Single(), out _, out muted);
        muted.Should().BeTrue();
        TryParseAudio(sim.Process(Cmd(CmdMuteOff)).Single(), out _, out muted);
        muted.Should().BeFalse();
        TryParseAudio(sim.Process(Cmd(CmdMuteToggle)).Single(), out _, out muted);
        muted.Should().BeTrue();
    }

    [Fact]
    public void Volume_Ramps_Until_Stopped()
    {
        var sim = new TunerSimulator("1");
        sim.Process(Cmd(CmdPwrOn));
        sim.Process(Cmd(CmdVolUp));
        sim.Tick();
        sim.Volume.Should().Be(55);
        sim.Process(Cmd(CmdVolStop));
        sim.Tick().Should().BeNull();
        sim.Volume.Should().Be(55);

        sim.Process(Cmd(CmdVolDn));
        sim.Tick();
        sim.Volume.Should().Be(50);
    }

    [Fact]
    public void Network_Query_Uses_The_Manuals_Lines_And_The_Parser_Accepts_Them()
    {
        var sim = new TunerSimulator("1");
        var lines = sim.Process(Cmd(CmdPollIp));
        lines.Should().HaveCount(2);
        lines[0].Should().StartWith("$MAC=");
        lines[1].Should().StartWith("$IP=");

        TryParseNetwork(lines[0], out _, out var mac).Should().BeTrue();
        TryParseNetwork(lines[1], out var ip, out _).Should().BeTrue();
        mac.Should().Be("0014C8090001");
        ip.Should().Be("192.168.1.231");
    }

    [Fact]
    public void Info_Round_Trips_Through_The_Parser()
    {
        var sim = new TunerSimulator("1");
        TryParseInfo(sim.Process(Cmd(CmdPollInfo)).Single(), "1", out var model, out var fw).Should().BeTrue();
        model.Should().Be("232-ATSC4");
        fw.Should().Be("1.00");
    }

    [Fact]
    public void Commands_For_Other_Units_And_Garbage_Are_Ignored()
    {
        var sim = new TunerSimulator("1");
        sim.Process(BuildCommand("2", CmdPwrOn)).Should().BeEmpty();
        sim.PowerOn.Should().BeFalse();
        sim.Process("garbage").Should().BeEmpty();
        sim.Process(">").Should().BeEmpty();
        sim.Process(null).Should().BeEmpty();
    }

    [Fact]
    public void Unit_Zero_Addresses_All_Units()
    {
        var sim = new TunerSimulator("3");
        sim.Process(BuildCommand("0", CmdPwrOn)).Should().HaveCount(1);
        sim.PowerOn.Should().BeTrue();
    }

    [Fact]
    public void Default_Unit_Responds_As_Unit_One()
    {
        var sim = new TunerSimulator(null);
        sim.Process(BuildCommand(null, CmdPoll)).Single().Should().StartWith("<1T");
    }
}
