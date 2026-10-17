# NAudio.Nam golden test data

These files let the tests check NAudio.Nam against the official C++ implementation,
[NeuralAmpModelerCore](https://github.com/sdatkinson/NeuralAmpModelerCore) (MIT, Copyright (c) 2023 Steven Atkinson).

| File | What it is |
| --- | --- |
| `A2.nam` | The A2 example model from NeuralAmpModelerCore (`example_models/A2.nam`, commit `0b3d3c9`): a `SlimmableContainer` (file version 0.7.0) with an A2-Lite and an A2-Full WaveNet, trained at 48 kHz. |
| `a2_input.f32` | 24,000 samples (0.5 s at 48 kHz) of raw little-endian float32 test signal: a quiet sweep, a loud power chord, seeded noise, an impulse and a silent tail. Made by `Generator/mkinput.py`. |
| `a2_lite_expected.f32` | NeuralAmpModelerCore's output for `a2_input.f32` with the slimmable size set to 0.0 (A2-Lite). |
| `a2_full_expected.f32` | The same with the size set to 1.0 (A2-Full). |

## Regenerating

The outputs were rendered with NeuralAmpModelerCore at commit `0b3d3c9` (core 0.6.0), built with its A2 fast path (`NAM_ENABLE_A2_FAST`), in 64-sample blocks after the model's default prewarm. Its generic WaveNet path gives the same output to within 4.3e-7.

```sh
git clone https://github.com/sdatkinson/NeuralAmpModelerCore && cd NeuralAmpModelerCore
git checkout 0b3d3c9 && git submodule update --init Dependencies/eigen
g++ -std=c++20 -O2 -DNAM_ENABLE_A2_FAST -I. -IDependencies/eigen -IDependencies/nlohmann \
    <this folder>/Generator/golden_gen.cpp NAM/*.cpp NAM/wavenet/*.cpp -o golden_gen
python3 <this folder>/Generator/mkinput.py a2_input.f32
./golden_gen example_models/A2.nam a2_input.f32 a2_lite_expected.f32 0 64
./golden_gen example_models/A2.nam a2_input.f32 a2_full_expected.f32 1 64
```

This reproduces the committed files byte for byte on x86-64 Linux with GCC.
