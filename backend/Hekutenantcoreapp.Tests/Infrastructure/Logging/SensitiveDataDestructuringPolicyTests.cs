using Hekutenantcoreapp.Infrastructure.Logging;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace Hekutenantcoreapp.Tests.Infrastructure.Logging;

public class SensitiveDataDestructuringPolicyTests
{
    private class FakePropertyValueFactory : ILogEventPropertyValueFactory
    {
        public LogEventPropertyValue CreatePropertyValue(object? value, bool destructureObjects = false) =>
            new ScalarValue(value);
    }

    private class LoginAttempt
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }

    private class PlainDto
    {
        public string Name { get; set; } = "";
    }

    [Fact]
    public void TryDestructure_MasksPasswordProperty_ButKeepsOthers()
    {
        var policy = new SensitiveDataDestructuringPolicy();
        var factory = new FakePropertyValueFactory();
        var value = new LoginAttempt { Email = "a@b.com", Password = "hunter2" };

        var handled = policy.TryDestructure(value, factory, out var result);

        Assert.True(handled);
        var structure = Assert.IsType<StructureValue>(result);

        var passwordProp = structure.Properties.Single(p => p.Name == "Password");
        Assert.Equal("***REDACTED***", Assert.IsType<ScalarValue>(passwordProp.Value).Value);

        var emailProp = structure.Properties.Single(p => p.Name == "Email");
        Assert.Equal("a@b.com", Assert.IsType<ScalarValue>(emailProp.Value).Value);
    }

    [Fact]
    public void TryDestructure_ReturnsFalse_WhenTypeHasNoSensitiveProperty()
    {
        var policy = new SensitiveDataDestructuringPolicy();
        var factory = new FakePropertyValueFactory();

        var handled = policy.TryDestructure(new PlainDto { Name = "x" }, factory, out _);

        Assert.False(handled);
    }
}
