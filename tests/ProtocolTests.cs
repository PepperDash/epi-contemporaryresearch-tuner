using FluentAssertions;
using Xunit;
using static PepperDash.Essentials.Plugins.ContemporaryResearch.Tuner.ContemporaryResearchProtocol;

namespace PepperDash.Essentials.Plugins.ContemporaryResearch.Tuner.Tests;

public class ProtocolTests
{
    [Fact]
    public void Commands_Match_The_Manual()
    {
        BuildCommand("1", CmdPwrOn).Should().Be(">1P1\r");
        BuildCommand("1", CmdPwrOff).Should().Be(">1P0\r");
        BuildCommand("3", CmdChanUp).Should().Be(">3TU\r");
        BuildCommand("1", CmdKeyEmu, ParamDigit0).Should().Be(">1KK=10\r");
        BuildCommand("1", CmdKeyEmu, ParamDash).Should().Be(">1KK=99\r");
        BuildCommand("1", CmdKeyEmu, ParamDpEnter).Should().Be(">1KK=110\r");
        BuildCommand("1", CmdVolLevel, "=40").Should().Be(">1VH=40\r");
        BuildCommand("1", CmdPoll).Should().Be(">1ST\r");
        BuildCommand("1", CmdPollAv).Should().Be(">1SV\r");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Missing_UnitId_Defaults_To_One(string? unit)
    {
        // the tuner replies <1T... so an empty unit id must not produce "<T" parse keys
        NormalizeUnitId(unit).Should().Be("1");
        BuildCommand(unit, CmdPoll).Should().Be(">1ST\r");
        ResponsePrefix(unit, "T").Should().Be("<1T");
    }

    [Theory]
    [InlineData("<1TU032U0A0002x0", true, "32-2")]
    [InlineData("<1TM032U0A0002x0", false, "32-2")]
    [InlineData("<1TU005U0A0000x0", true, "5")]
    public void Status_Parses_Power_And_Channel(string line, bool power, string channel)
    {
        TryParseStatus(line, out var on, out var ch).Should().BeTrue();
        on.Should().Be(power);
        ch.Should().Be(channel);
    }

    [Fact]
    public void Status_Rejects_Short_Lines()
    {
        TryParseStatus("<1T", out _, out _).Should().BeFalse();
        TryParseStatus(null, out _, out _).Should().BeFalse();
    }

    [Theory]
    [InlineData("<1VU63Ux100", 100, false)]
    [InlineData("<1VU32Mx050", 50, true)]
    [InlineData("<1VM00Ux000", 0, false)]
    public void Audio_Parses_Level_And_Mute(string line, int level, bool muted)
    {
        TryParseAudio(line, out var l, out var m).Should().BeTrue();
        l.Should().Be(level);
        m.Should().Be(muted);
    }

    [Fact]
    public void Audio_Rejects_Short_Lines()
    {
        TryParseAudio("<1VU63U", out _, out _).Should().BeFalse();
    }

    [Theory]
    [InlineData("$MAC=0014C8090001", null, "0014C8090001")]
    [InlineData("$IP=192.168.001.231S IG=000.000.000.000 IM=255.255.255.000 IY=1", "192.168.1.231", null)]
    [InlineData("<1IP$IP=010.000.000.005D IG=010.000.000.001", "10.0.0.5", null)]
    public void Network_Parses_The_Manuals_Examples(string line, string? ip, string? mac)
    {
        TryParseNetwork(line, out var i, out var m).Should().BeTrue();
        i.Should().Be(ip);
        m.Should().Be(mac);
    }

    [Fact]
    public void Network_Rejects_Unrelated_Lines()
    {
        TryParseNetwork("<1TU032U0A0002x0", out _, out _).Should().BeFalse();
    }

    [Fact]
    public void Info_Parses_Model_And_Firmware()
    {
        TryParseInfo("<1ID 232-ATSC4 1.05", "1", out var model, out var fw).Should().BeTrue();
        model.Should().Be("232-ATSC4");
        fw.Should().Be("1.05");

        TryParseInfo("<2ID 232-ATSC4 1.05", "1", out _, out _).Should().BeFalse();
    }

    [Fact]
    public void Volume_Round_Trips_For_Every_Percent()
    {
        // the feedback value, sent back as a level, must not creep down
        for (var percent = 0; percent <= 100; percent++)
            EssentialsToPercent(PercentToEssentials(percent)).Should().Be(percent);
    }

    [Fact]
    public void Volume_Scaling_Hits_The_Ends()
    {
        PercentToEssentials(0).Should().Be(0);
        PercentToEssentials(100).Should().Be(65535);
        EssentialsToPercent(65535).Should().Be(100);
        EssentialsToPercent(0).Should().Be(0);
    }
}
