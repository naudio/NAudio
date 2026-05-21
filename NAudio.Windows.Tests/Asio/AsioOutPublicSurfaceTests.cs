using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NAudio.Wave;
using NUnit.Framework;

namespace NAudioTests.Asio
{
    /// <summary>
    /// Pins the NAudio 2.x public surface of <see cref="AsioOut"/>. The class was rebuilt as a facade over
    /// <see cref="AsioDevice"/> in NAudio 3 and must not break any binary or source consumer of the legacy API.
    /// If you intentionally add a new public member, append to the expected lists; if a test fails on a member
    /// that already existed, you've changed the back-compat surface — fix the implementation, not the test.
    /// </summary>
    [TestFixture]
    [Category("UnitTest")]
    public class AsioOutPublicSurfaceTests
    {
        [Test]
        public void AsioOut_ImplementsIWavePlayer()
        {
            Assert.That(typeof(IWavePlayer).IsAssignableFrom(typeof(AsioOut)),
                "AsioOut must continue to implement IWavePlayer.");
        }

        [Test]
        public void AsioOut_IsPublicAndNonSealed()
        {
            Assert.Multiple(() =>
            {
                Assert.That(typeof(AsioOut).IsPublic, "AsioOut must be public.");
                Assert.That(typeof(AsioOut).IsSealed, Is.False, "AsioOut was non-sealed in NAudio 2.x.");
            });
        }

        [Test]
        public void Constructors_MatchNAudio2Surface()
        {
            var actual = typeof(AsioOut)
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .Select(c => string.Join(",", c.GetParameters().Select(p => p.ParameterType.FullName)))
                .OrderBy(s => s)
                .ToList();

            var expected = new[]
            {
                "",                          // AsioOut()
                "System.Int32",              // AsioOut(int driverIndex)
                "System.String",             // AsioOut(string driverName)
            }.OrderBy(s => s).ToList();

            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCaseSource(nameof(ExpectedInstanceMethods))]
        public void InstanceMethod_ExistsWithExpectedSignature(string name, Type returnType, Type[] parameterTypes)
        {
            var method = typeof(AsioOut).GetMethod(name,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                binder: null, types: parameterTypes, modifiers: null);

            Assert.That(method, Is.Not.Null,
                $"Missing public instance method: {returnType.Name} {name}({string.Join(", ", parameterTypes.Select(p => p.Name))})");
            Assert.That(method!.ReturnType, Is.EqualTo(returnType),
                $"{name} return type must be {returnType.FullName}.");
        }

        [TestCaseSource(nameof(ExpectedStaticMethods))]
        public void StaticMethod_ExistsWithExpectedSignature(string name, Type returnType, Type[] parameterTypes)
        {
            var method = typeof(AsioOut).GetMethod(name,
                BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly,
                binder: null, types: parameterTypes, modifiers: null);

            Assert.That(method, Is.Not.Null,
                $"Missing public static method: {returnType.Name} {name}({string.Join(", ", parameterTypes.Select(p => p.Name))})");
            Assert.That(method!.ReturnType, Is.EqualTo(returnType));
        }

        [TestCaseSource(nameof(ExpectedProperties))]
        public void Property_ExistsWithExpectedShape(string name, Type type, bool hasGetter, bool hasSetter)
        {
            var prop = typeof(AsioOut).GetProperty(name, BindingFlags.Public | BindingFlags.Instance);

            Assert.That(prop, Is.Not.Null, $"Missing public property: {type.Name} {name}");
            Assert.That(prop!.PropertyType, Is.EqualTo(type), $"{name} type must be {type.FullName}.");
            Assert.That(prop.GetMethod is not null, Is.EqualTo(hasGetter),
                $"{name} expected getter={hasGetter} but found {prop.GetMethod is not null}.");
            Assert.That(prop.SetMethod is not null && prop.SetMethod.IsPublic, Is.EqualTo(hasSetter),
                $"{name} expected public setter={hasSetter} but found {prop.SetMethod is not null && prop.SetMethod.IsPublic}.");
        }

        [TestCaseSource(nameof(ExpectedEvents))]
        public void Event_ExistsWithExpectedHandlerType(string name, Type handlerType)
        {
            var ev = typeof(AsioOut).GetEvent(name, BindingFlags.Public | BindingFlags.Instance);

            Assert.That(ev, Is.Not.Null, $"Missing public event: {name}");
            Assert.That(ev!.EventHandlerType, Is.EqualTo(handlerType));
        }

        [Test]
        public void Volume_IsMarkedObsolete()
        {
            // Old NAudio 2.x marked Volume with [Obsolete]; the attribute itself is part of the API contract.
            var prop = typeof(AsioOut).GetProperty("Volume")!;
            Assert.That(prop.GetCustomAttribute<ObsoleteAttribute>(), Is.Not.Null,
                "AsioOut.Volume must remain marked [Obsolete] for back-compat.");
        }

        [Test]
        public void NoUnexpectedPublicMembers()
        {
            // Defense in depth: any new public method/property/event we ship must be added to the expected lists,
            // forcing a deliberate API decision.
            var actualMethodNames = typeof(AsioOut)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName) // exclude property/event accessors
                .Select(m => m.Name)
                .Distinct()
                .ToList();

            var expectedMethodNames = ExpectedInstanceMethods()
                .Concat(ExpectedStaticMethods())
                .Select(c => (string)c.Arguments[0]!)
                .Concat(new[] { "Dispose" }) // IDisposable.Dispose
                .Distinct()
                .ToList();

            var unexpected = actualMethodNames.Except(expectedMethodNames).ToList();
            Assert.That(unexpected, Is.Empty,
                "Unexpected public methods on AsioOut: " + string.Join(", ", unexpected));
        }

        // --- expected-surface fixtures ----------------------------------------------------------------

        private static IEnumerable<TestCaseData> ExpectedInstanceMethods() => new[]
        {
            Method(nameof(AsioOut.Dispose),                  typeof(void)),
            Method(nameof(AsioOut.IsSampleRateSupported),    typeof(bool),   typeof(int)),
            Method(nameof(AsioOut.ShowControlPanel),         typeof(void)),
            Method(nameof(AsioOut.Play),                     typeof(void)),
            Method(nameof(AsioOut.Stop),                     typeof(void)),
            Method(nameof(AsioOut.Pause),                    typeof(void)),
            Method(nameof(AsioOut.Init),                     typeof(void),   typeof(IWaveProvider)),
            Method(nameof(AsioOut.InitRecordAndPlayback),    typeof(void),   typeof(IWaveProvider), typeof(int), typeof(int)),
            Method(nameof(AsioOut.AsioInputChannelName),     typeof(string), typeof(int)),
            Method(nameof(AsioOut.AsioOutputChannelName),    typeof(string), typeof(int)),
        };

        private static IEnumerable<TestCaseData> ExpectedStaticMethods() => new[]
        {
            Method(nameof(AsioOut.GetDriverNames),            typeof(string[])),
            Method("isSupported",                             typeof(bool)),
        };

        private static IEnumerable<TestCaseData> ExpectedProperties() => new[]
        {
            Property(nameof(AsioOut.PlaybackLatency),          typeof(int),          hasGetter: true, hasSetter: false),
            Property(nameof(AsioOut.AutoStop),                 typeof(bool),         hasGetter: true, hasSetter: true),
            Property(nameof(AsioOut.HasReachedEnd),            typeof(bool),         hasGetter: true, hasSetter: false),
            Property(nameof(AsioOut.PlaybackState),            typeof(PlaybackState), hasGetter: true, hasSetter: false),
            Property(nameof(AsioOut.DriverName),               typeof(string),       hasGetter: true, hasSetter: false),
            Property(nameof(AsioOut.NumberOfOutputChannels),   typeof(int),          hasGetter: true, hasSetter: false),
            Property(nameof(AsioOut.NumberOfInputChannels),    typeof(int),          hasGetter: true, hasSetter: false),
            Property(nameof(AsioOut.DriverInputChannelCount),  typeof(int),          hasGetter: true, hasSetter: false),
            Property(nameof(AsioOut.DriverOutputChannelCount), typeof(int),          hasGetter: true, hasSetter: false),
            Property(nameof(AsioOut.FramesPerBuffer),          typeof(int),          hasGetter: true, hasSetter: false),
            Property(nameof(AsioOut.ChannelOffset),            typeof(int),          hasGetter: true, hasSetter: true),
            Property(nameof(AsioOut.InputChannelOffset),       typeof(int),          hasGetter: true, hasSetter: true),
            Property("Volume",                                 typeof(float),        hasGetter: true, hasSetter: true),
            Property(nameof(AsioOut.OutputWaveFormat),         typeof(WaveFormat),   hasGetter: true, hasSetter: false),
        };

        private static IEnumerable<TestCaseData> ExpectedEvents() => new[]
        {
            Event(nameof(AsioOut.PlaybackStopped),    typeof(EventHandler<StoppedEventArgs>)),
            Event(nameof(AsioOut.AudioAvailable),     typeof(EventHandler<AsioAudioAvailableEventArgs>)),
            Event(nameof(AsioOut.DriverResetRequest), typeof(EventHandler)),
        };

        private static TestCaseData Method(string name, Type returnType, params Type[] parameterTypes)
            => new TestCaseData(name, returnType, parameterTypes).SetName($"{name}({string.Join(",", parameterTypes.Select(t => t.Name))})");

        private static TestCaseData Property(string name, Type type, bool hasGetter, bool hasSetter)
            => new TestCaseData(name, type, hasGetter, hasSetter).SetName($"{name}");

        private static TestCaseData Event(string name, Type handlerType)
            => new TestCaseData(name, handlerType).SetName(name);
    }
}
