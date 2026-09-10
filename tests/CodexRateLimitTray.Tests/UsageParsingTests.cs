using CodexRateLimitTray.Core;

namespace CodexRateLimitTray.Tests;

public sealed class UsageParsingTests
{
    [Fact]
    public void Parses_primary_as_five_hour_and_secondary_as_week_window()
    {
        const string json = """
        {
          "rate_limit": {
            "primary_window": {
              "used_percent": 25.5,
              "reset_at": 1716094800,
              "limit_window_seconds": 18000
            },
            "secondary_window": {
              "used_percent": 80,
              "reset_at": 1716181200,
              "limit_window_seconds": 604800
            },
            "auxiliary_field": "ignored"
          },
          "auxiliary_top_level_field": true
        }
        """;

        var state = WhamUsageParser.Parse(json, TimeZoneInfo.Utc);

        Assert.False(state.HasError);
        Assert.Equal(25.5, state.FiveHour.UsedPercent);
        Assert.Equal(74.5, state.FiveHour.RemainingPercent);
        Assert.Equal(new DateTimeOffset(2024, 5, 19, 5, 0, 0, TimeSpan.Zero), state.FiveHour.ResetAt);

        Assert.NotNull(state.Week);
        Assert.Equal(80, state.Week!.UsedPercent);
        Assert.Equal(20, state.Week.RemainingPercent);
        Assert.Equal(new DateTimeOffset(2024, 5, 20, 5, 0, 0, TimeSpan.Zero), state.Week.ResetAt);
    }

    [Fact]
    public void Converts_reset_at_from_utc_to_tokyo_and_formats_week_text()
    {
        const string json = """
        {
          "rate_limit": {
            "primary_window": { "used_percent": 25.5, "reset_at": 1716094800 },
            "secondary_window": { "used_percent": 80, "reset_at": 1716094800 }
          }
        }
        """;
        var tokyo = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");

        var state = WhamUsageParser.Parse(json, tokyo);

        Assert.False(state.HasError);
        Assert.NotNull(state.Week);
        Assert.Equal(
            new DateTimeOffset(2024, 5, 19, 14, 0, 0, TimeSpan.FromHours(9)),
            state.FiveHour.ResetAt);
        Assert.Equal("05/19 14:00", state.Week!.WeekResetText);
    }

    [Theory]
    [InlineData(-10, 100)]
    [InlineData(0, 100)]
    [InlineData(42.4, 57.6)]
    [InlineData(125, 0)]
    public void Remaining_percent_is_clamped_to_zero_through_one_hundred(double used, double expectedRemaining)
    {
        var window = new UsageWindow(used, DateTimeOffset.UnixEpoch);

        Assert.Equal(expectedRemaining, window.RemainingPercent, precision: 6);
    }

    [Fact]
    public void Keeps_five_hour_window_when_secondary_window_is_null()
    {
        const string json = """
        {
          "rate_limit": {
            "primary_window": { "used_percent": 25.5, "reset_at": 1716094800 },
            "secondary_window": null
          }
        }
        """;

        var state = WhamUsageParser.Parse(json, TimeZoneInfo.Utc);

        Assert.False(state.HasError);
        Assert.Equal(25.5, state.FiveHour.UsedPercent);
        Assert.Null(state.Week);
    }

    [Fact]
    public void Keeps_five_hour_window_when_secondary_window_is_missing()
    {
        const string json = """
        {
          "rate_limit": {
            "primary_window": { "used_percent": 25.5, "reset_at": 1716094800 }
          }
        }
        """;

        var state = WhamUsageParser.Parse(json, TimeZoneInfo.Utc);

        Assert.False(state.HasError);
        Assert.Equal(25.5, state.FiveHour.UsedPercent);
        Assert.Null(state.Week);
    }

    [Fact]
    public void Error_state_has_dummy_five_hour_window_and_no_week_window()
    {
        var state = UsageState.Error(UsageErrorKind.InvalidResponse, "invalid");

        Assert.True(state.HasError);
        Assert.NotNull(state.FiveHour);
        Assert.Null(state.Week);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"rate_limit\":null}")]
    [InlineData("{\"rate_limit\":[]}")]
    [InlineData("{\"rate_limit\":\"invalid\"}")]
    [InlineData("{\"rate_limit\":{}}")]
    [InlineData("{\"rate_limit\":{\"primary_window\":null}}")]
    [InlineData("{\"rate_limit\":{\"primary_window\":[]}}")]
    [InlineData("{\"rate_limit\":{\"primary_window\":true}}")]
    [InlineData("{\"rate_limit\":{\"primary_window\":{\"reset_at\":1716094800}}}")]
    [InlineData("{\"rate_limit\":{\"primary_window\":{\"used_percent\":25.5}}}")]
    [InlineData("{\"rate_limit\":{\"primary_window\":{\"used_percent\":25.5,\"reset_at\":1716094800},\"secondary_window\":\"invalid\"}}")]
    [InlineData("{\"rate_limit\":{\"primary_window\":{\"used_percent\":25.5,\"reset_at\":1716094800},\"secondary_window\":[]}}")]
    [InlineData("{\"rate_limit\":{\"primary_window\":{\"used_percent\":25.5,\"reset_at\":1716094800},\"secondary_window\":{\"reset_at\":1716094800}}}")]
    [InlineData("{\"rate_limit\":{\"primary_window\":{\"used_percent\":25.5,\"reset_at\":1716094800},\"secondary_window\":{\"used_percent\":80}}}")]
    public void Invalid_response_shape_is_rejected(string json)
    {
        AssertInvalidResponse(json);
    }

    [Theory]
    [InlineData("{\"rate_limit\":{\"primary_window\":{\"used_percent\":\"25.5\",\"reset_at\":1716094800}}}")]
    [InlineData("{\"rate_limit\":{\"primary_window\":{\"used_percent\":true,\"reset_at\":1716094800}}}")]
    [InlineData("{\"rate_limit\":{\"primary_window\":{\"used_percent\":1e999,\"reset_at\":1716094800}}}")]
    [InlineData("{\"rate_limit\":{\"primary_window\":{\"used_percent\":25.5,\"reset_at\":1716094800},\"secondary_window\":{\"used_percent\":1e999,\"reset_at\":1716094800}}}")]
    public void Non_finite_or_wrong_type_used_percent_is_rejected(string json)
    {
        AssertInvalidResponse(json);
    }

    [Theory]
    [InlineData("{\"rate_limit\":{\"primary_window\":{\"used_percent\":25.5,\"reset_at\":1716094800.0}}}")]
    [InlineData("{\"rate_limit\":{\"primary_window\":{\"used_percent\":25.5,\"reset_at\":9223372036854775807}}}")]
    [InlineData("{\"rate_limit\":{\"primary_window\":{\"used_percent\":25.5,\"reset_at\":-9223372036854775808}}}")]
    [InlineData("{\"rate_limit\":{\"primary_window\":{\"used_percent\":25.5,\"reset_at\":9223372036854775808}}}")]
    [InlineData("{\"rate_limit\":{\"primary_window\":{\"used_percent\":25.5,\"reset_at\":1716094800},\"secondary_window\":{\"used_percent\":80,\"reset_at\":1716094800.0}}}")]
    public void Non_integer_or_out_of_range_reset_at_is_rejected(string json)
    {
        AssertInvalidResponse(json);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("not-json")]
    public void Malformed_json_is_rejected(string json)
    {
        AssertInvalidResponse(json);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("\"text\"")]
    [InlineData("null")]
    public void Non_object_top_level_json_is_rejected(string json)
    {
        AssertInvalidResponse(json);
    }

    private static void AssertInvalidResponse(string json)
    {
        var state = WhamUsageParser.Parse(json, TimeZoneInfo.Utc);

        Assert.True(state.HasError);
        Assert.Equal(UsageErrorKind.InvalidResponse, state.ErrorKind);
    }
}
