# Batch plans

JSON plans for the `run-batch` subcommand of `NAudioConsoleTest`. Each plan is a sequence of
test ids with optional `params`. The runner executes them non-interactively, emits
`Reports/<timestamp>/summary.json` + `summary.md`, and returns the count of non-Pass results
as the exit code.

## Schema

```json
{
  "name": "Display name (optional — defaults to filename)",
  "stopOnFail": false,
  "timeoutSeconds": 60,
  "tests": [
    { "id": "Some.TestId" },
    { "id": "Other.Test", "params": { "key": "value", "duration": "5s" } },
    { "id": "Slow.Test", "params": { "...": "..." }, "timeoutSeconds": 120 }
  ]
}
```

- `params` values are strings, numbers, or booleans — same shape the CLI's
  `--key=value` syntax produces. TimeSpan params accept `"5s"`, `"500ms"`, `"00:00:30"`, or a
  bare number of seconds.
- `timeoutSeconds` defaults to 5 min; can be set globally and overridden per-test.
- Line comments (`//`) and trailing commas are permitted.

## Plans in this folder

- **`enumeration-smoke.json`** — portable. Runs all four parameter-free enumeration tests
  (ASIO drivers, WASAPI endpoints, DirectSound devices, MediaFoundation transforms). Use
  as a CI smoke check.
- **`asio-functional-sample.json`** — template. Demonstrates parameterised entries; you
  need to edit the driver name to match a device installed on your machine.

## Running

```
NAudioConsoleTest run-batch BatchPlans/enumeration-smoke.json
NAudioConsoleTest run-batch BatchPlans/asio-functional-sample.json --out=Reports/asio
```

The first line of the report header is the host snapshot from `diagnose`, so any failure
report carries the OS/runtime/device list it ran against.
