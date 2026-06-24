# NAudioConsoleTest

Interactive + scriptable harness for exercising NAudio against real audio hardware. 44 tests
across ASIO, WASAPI, WinMM, DirectSound, MediaFoundation, DMO, and DSP — most need a device,
some are headless. Run with no arguments for the Spectre menu, or use the CLI for scripting.

## CLI

```
NAudioConsoleTest                                            # interactive menu
NAudioConsoleTest list                                       # every test id + description
NAudioConsoleTest describe <id>                              # show a test's parameters
NAudioConsoleTest run <id> [--key=value ...]                 # run one test
NAudioConsoleTest run-batch <plan.json> [--out=dir]          # run a batch — see BatchPlans/
NAudioConsoleTest diagnose [--format=json|md] [--out=path]   # snapshot host audio infrastructure
```

Exit codes: `0` pass, `1` fail, `2` usage/parse error, `3` unknown test. `run-batch` returns
the count of non-Pass results (capped at 255).

### Running a single test from the command line

```
# from the repo root
dotnet run --project NAudioConsoleTest -- describe Asio.ShowChannelLevels
dotnet run --project NAudioConsoleTest -- run Asio.ShowChannelLevels --driver="Focusrite USB ASIO" --inputChannels=0,1 --duration=3s
```

Both `--key=value` and `--key value` work. TimeSpan params accept `"5s"`, `"500ms"`,
`"00:00:30"`, or a bare seconds number.

Parameters tagged `[cli-only]` in `describe` (e.g. `maxDuration` on playback tests) are skipped
in interactive mode — the user controls stop via ESC there.

### Batch runs and host diagnostics

See [BatchPlans/README.md](BatchPlans/README.md) for the JSON plan schema and the canonical
plans (`enumeration-smoke.json`, `asio-functional-sample.json`). Reports land in
`Reports/<timestamp>/summary.{json,md}` and are gitignored.

## Adding a new test

1. **Create the test class** in the appropriate backend folder (e.g.
   `Wasapi/Tests/WasapiMyNewTest.cs`). Implement [`IConsoleTest`](Shared/Testing/IConsoleTest.cs):

   ```csharp
   sealed class WasapiMyNewTest : IConsoleTest
   {
       public string Id => "Wasapi.MyNew";
       public string Description => "One-line description";
       public MenuPath? MenuLocation => new("WASAPI", "My new test", Group: "Info", Order: 0);

       public IReadOnlyList<TestParameter> Parameters =>
       [
           new("device", typeof(string), Required: true, Help: "render device name",
               ChoiceProvider: WasapiDevices.RenderDeviceNames),
           new("duration", typeof(TimeSpan), Required: false, Default: TimeSpan.FromSeconds(5)),
       ];

       public TestResult Run(TestContext ctx)
       {
           var device = ctx.Get<string>("device");
           var duration = ctx.Get<TimeSpan>("duration");
           // ... do the work, poll ctx.Cancellation, branch on ctx.Interactive for richer UI ...
           return TestResult.Pass($"Did the thing for {duration.TotalSeconds:F0}s",
               new Dictionary<string, string> { ["device"] = device });
       }
   }
   ```

2. **Register it** in [Shared/Testing/TestRegistration.cs](Shared/Testing/TestRegistration.cs)
   — one `TestRegistry.Register(new MyNewTest());` line. There's no reflection-based discovery.

3. **Build and verify** with `dotnet build NAudioConsoleTest/NAudioConsoleTest.csproj`, then
   `dotnet run --project NAudioConsoleTest -- describe Wasapi.MyNew`.

### Parameter conventions

[`TestParameter`](Shared/Testing/TestParameter.cs) supports:

- `Choices` — static list (sample rates, encoding modes).
- `ChoiceProvider` — lazy callback for runtime-derived choices (installed drivers, devices).
- `CliOnly: true` — skipped in interactive mode (used for caps like `maxDuration`).
- `InteractivePrompter` — custom picker delegate for richer menu UX (see
  `AsioDrivers.PickInputChannels` for the multi-select example); CLI parsing is untouched.

### Result conventions

[`TestResult`](Shared/Testing/TestResult.cs) — `Pass(message, diagnostics)`,
`Fail(message, diagnostics)`, `Skipped(reason, diagnostics)`, `NotAutomatable(reason)`.
Throwing is fine — `ConsoleTestRunner` converts exceptions to `Fail`.

The `diagnostics` dictionary is surfaced in `run` console output and embedded in batch JSON
reports — put the numbers you'd want to diff between runs there (frame counts, drift values,
output file sizes, etc.).

### Interactive vs CLI behaviour

[`TestContext.Interactive`](Shared/Testing/TestContext.cs) is `true` when launched from the
menu, `false` from `run`/`run-batch`. Use it to:

- Render Spectre tables / live meters in menu mode, plain stdout in CLI mode.
- Poll `Console.KeyAvailable` for ESC-to-stop in menu mode only.
- Skip prompts (or use the `CliOnly` flag) for parameters that don't make sense interactively.

[`LiveMeterRenderer`](Shared/Testing/LiveMeterRenderer.cs) gives you an in-place dBFS bar
display for level-meter tests — see `AsioShowChannelLevelsTest` for usage.

## Folder layout

```
NAudioConsoleTest/
├── Asio/Tests/             — IConsoleTest implementations, grouped by backend
├── Wasapi/Tests/
├── WinMM/Tests/
├── DirectSound/Tests/
├── MediaFoundation/Tests/
├── Dmo/Tests/
├── Dsp/Tests/
├── BatchPlans/             — JSON plans for run-batch (see README there)
├── Shared/Testing/         — IConsoleTest contract, CLI dispatcher, batch runner,
│                             diagnostics collector, registry, prompter, menu renderer
└── Program.cs              — top-level menu + CLI entry point
```
