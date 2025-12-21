using System;
using System.Linq;
using Xunit;
using SaveState.Core.Services.Ai.Telemetry;
using SaveState.Core.Services.Ai.Testing;

namespace SaveState.Tests.Ai
{
    /// <summary>
    /// Tests for Telemetry and Testing layers - basic existence verification
    /// </summary>
    public class TelemetryTestingTests
    {
        [Fact]
        public void AiTelemetry_CanBeCreated()
        {
            var telemetry = new AiTelemetry();
            Assert.NotNull(telemetry);
        }

        [Fact]
        public void HallucinationDetector_CanBeCreated()
        {
            var detector = new HallucinationDetector();
            Assert.NotNull(detector);
        }

        [Fact]
        public void HallucinationContext_CanBeCreated()
        {
            var context = new HallucinationContext();
            Assert.NotNull(context);
        }

        [Fact]
        public void AiTestHarness_CanBeCreated()
        {
            var harness = new AiTestHarness();
            Assert.NotNull(harness);
        }

        [Fact]
        public void FakePlayerSimulator_CanBeCreated()
        {
            var simulator = new FakePlayerSimulator();
            Assert.NotNull(simulator);
        }

        [Fact]
        public void FakePlayerSimulator_HasPersonas()
        {
            var simulator = new FakePlayerSimulator();
            var personas = simulator.GetAvailablePersonas();
            Assert.NotEmpty(personas);
        }
    }
}
