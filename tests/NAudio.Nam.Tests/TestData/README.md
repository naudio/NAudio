# NAudio.Nam golden test data

These files let the tests check NAudio.Nam against the official C++ implementation,
[NeuralAmpModelerCore](https://github.com/sdatkinson/NeuralAmpModelerCore) (MIT, Copyright (c) 2023 Steven Atkinson).
Every `*_expected.f32` file is NeuralAmpModelerCore's output for `input.f32`, as raw
little-endian float32.

| File | What it is |
| --- | --- |
| `input.f32` | 24,000 samples (0.5 s at 48 kHz) of test signal: a quiet sweep, a loud power chord, seeded noise, an impulse and a silent tail. Made by `Generator/mkinput.py`. |
| `A2.nam` | The A2 example model from NeuralAmpModelerCore (`example_models/A2.nam`): a `SlimmableContainer` (file version 0.7.0) with an A2-Lite and an A2-Full WaveNet. |
| `a2_lite_expected.f32`, `a2_full_expected.f32` | `A2.nam` with the slimmable size set to 0.0 (A2-Lite) and 1.0 (A2-Full). |
| `wavenet_a1_standard.nam` | NeuralAmpModelerCore's A1 "standard" WaveNet example (`example_models/wavenet_a1_standard.nam`): two layer arrays of 16 and 8 channels, tanh, legacy config keys. |
| `lstm.nam` | NeuralAmpModelerCore's LSTM example (`example_models/lstm.nam`), hidden size 3. |
| `slimmable_container.nam` | NeuralAmpModelerCore's mixed container example (`example_models/slimmable_container.nam`): an LSTM, a small tanh WaveNet and a larger ReLU WaveNet. Rendered at sizes 0, 0.5 and 1 (`slimmable_container_s*_expected.f32`). |
| `synthetic_wavenet.nam` | A seeded random-weight WaveNet from `Generator/mksynthetic.py`, covering what the examples don't: gated and plain layers, every supported activation, bottleneck smaller than channels, per-layer kernel sizes, a kernel-3 dilated head, and an identity residual (layer1x1 inactive). |
| `synthetic_lstm.nam` | A seeded random-weight two-layer LSTM (hidden size 12) from `Generator/mksynthetic.py`. |

The synthetic models' weights are random, so they don't sound like any real gear.
They exist so the C# engines can be checked against the reference on those code paths.

## Regenerating

The outputs were rendered with NeuralAmpModelerCore at commit `0b3d3c9` (core 0.6.0),
built with its A2 fast path (`NAM_ENABLE_A2_FAST`), in 64-sample blocks after each
model's default prewarm. For the A2 model, the core's generic WaveNet path gives the
same output to within 4.3e-7. The A1 "standard" model is more sensitive to summation
order: an `-O3 -march=native` build of the core differs from these files by up to
1.7e-5, so its test uses a looser tolerance.

```sh
git clone https://github.com/sdatkinson/NeuralAmpModelerCore && cd NeuralAmpModelerCore
git checkout 0b3d3c9 && git submodule update --init Dependencies/eigen
g++ -std=c++20 -O2 -DNAM_ENABLE_A2_FAST -I. -IDependencies/eigen -IDependencies/nlohmann \
    <this folder>/Generator/golden_gen.cpp NAM/*.cpp NAM/wavenet/*.cpp -o golden_gen
python3 <this folder>/Generator/mkinput.py input.f32
python3 <this folder>/Generator/mksynthetic.py .
cp example_models/{A2,wavenet_a1_standard,lstm,slimmable_container}.nam .

./golden_gen A2.nam input.f32 a2_lite_expected.f32 0 64
./golden_gen A2.nam input.f32 a2_full_expected.f32 1 64
for m in wavenet_a1_standard lstm synthetic_wavenet synthetic_lstm; do
    ./golden_gen $m.nam input.f32 ${m}_expected.f32 1 64
done
for s in 0 0.5 1; do
    ./golden_gen slimmable_container.nam input.f32 slimmable_container_s${s}_expected.f32 $s 64
done
```

This reproduces the committed files byte for byte on x86-64 Linux with GCC.
