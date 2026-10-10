using OpenAdr.Vtn.Core.Logic;

namespace OpenAdr.Vtn.Core.Tests;

public class TargetMatcherTests
{
    [Fact]
    public void No_query_targets_matches_everything()
    {
        Assert.True(TargetMatcher.Matches(["PROGRAM_NAME:HomeForecast"], null));
        Assert.True(TargetMatcher.Matches([], []));
        Assert.True(TargetMatcher.Matches(null, []));
    }

    [Fact]
    public void Object_with_no_targets_never_matches_a_non_empty_query()
    {
        Assert.False(TargetMatcher.Matches(null, ["PROGRAM_NAME:HomeForecast"]));
        Assert.False(TargetMatcher.Matches([], ["PROGRAM_NAME:HomeForecast"]));
    }

    [Fact]
    public void Matches_when_object_targets_are_a_superset_of_the_query()
    {
        string[] objectTargets = ["VEN_NAME:kitchen", "PROGRAM_NAME:HomeForecast"];

        Assert.True(TargetMatcher.Matches(objectTargets, ["PROGRAM_NAME:HomeForecast"]));
        Assert.True(TargetMatcher.Matches(objectTargets, objectTargets));
    }

    [Fact]
    public void Filtering_is_additive_every_query_target_must_be_present()
    {
        string[] objectTargets = ["PROGRAM_NAME:HomeForecast"];

        Assert.False(TargetMatcher.Matches(objectTargets, ["PROGRAM_NAME:HomeForecast", "VEN_NAME:kitchen"]));
    }

    [Fact]
    public void Matching_is_case_sensitive_and_exact()
    {
        Assert.False(TargetMatcher.Matches(["program_name:HomeForecast"], ["PROGRAM_NAME:HomeForecast"]));
    }
}
