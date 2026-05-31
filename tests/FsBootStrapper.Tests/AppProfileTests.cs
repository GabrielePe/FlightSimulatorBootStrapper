using FsBootStrapper.Core.Models;
using FluentAssertions;

namespace FsBootStrapper.Tests;

public sealed class AppProfileTests
{
    [Fact]
    public void GetItemsForPhase_ReturnsOnlyItemsMatchingPhase()
    {
        var profile = new AppProfile
        {
            Items =
            [
                new LaunchItem { Phase = LaunchPhase.BeforeMsfs, SortOrder = 0, DisplayName = "Before1" },
                new LaunchItem { Phase = LaunchPhase.AfterMsfs,  SortOrder = 0, DisplayName = "After1"  },
                new LaunchItem { Phase = LaunchPhase.BeforeMsfs, SortOrder = 1, DisplayName = "Before2" }
            ]
        };

        var before = profile.GetItemsForPhase(LaunchPhase.BeforeMsfs).ToList();
        var after = profile.GetItemsForPhase(LaunchPhase.AfterMsfs).ToList();

        before.Should().HaveCount(2);
        after.Should().HaveCount(1);
    }

    [Fact]
    public void GetItemsForPhase_ReturnsSortedBySortOrder()
    {
        var profile = new AppProfile
        {
            Items =
            [
                new LaunchItem { Phase = LaunchPhase.AfterMsfs, SortOrder = 2, DisplayName = "C" },
                new LaunchItem { Phase = LaunchPhase.AfterMsfs, SortOrder = 0, DisplayName = "A" },
                new LaunchItem { Phase = LaunchPhase.AfterMsfs, SortOrder = 1, DisplayName = "B" }
            ]
        };

        var after = profile.GetItemsForPhase(LaunchPhase.AfterMsfs).ToList();

        after[0].DisplayName.Should().Be("A");
        after[1].DisplayName.Should().Be("B");
        after[2].DisplayName.Should().Be("C");
    }

    [Fact]
    public void NewProfile_HasNewGuid()
    {
        var p1 = new AppProfile();
        var p2 = new AppProfile();

        p1.Id.Should().NotBe(p2.Id);
    }
}
