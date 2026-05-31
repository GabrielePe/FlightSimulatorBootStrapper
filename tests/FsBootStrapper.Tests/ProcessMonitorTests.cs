using FsBootStrapper.Core.Models;
using FsBootStrapper.Core.Services.Monitor;
using Microsoft.Extensions.Logging;
using NSubstitute;
using FluentAssertions;

namespace FsBootStrapper.Tests;

public sealed class ProcessMonitorTests
{
    private readonly ILogger<ProcessMonitor> _logger =
        Substitute.For<ILogger<ProcessMonitor>>();

    [Fact]
    public void GetCurrentStates_WhenEmpty_ReturnsEmptyDictionary()
    {
        var sut = new ProcessMonitor(_logger);
        var states = sut.GetCurrentStates();
        states.Should().BeEmpty();
    }

    [Fact]
    public void UntrackAll_ClearsTrackedProcesses()
    {
        var sut = new ProcessMonitor(_logger);
        sut.UntrackAll();
        sut.GetCurrentStates().Should().BeEmpty();
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var sut = new ProcessMonitor(_logger);
        var act = sut.Dispose;
        act.Should().NotThrow();
    }
}
