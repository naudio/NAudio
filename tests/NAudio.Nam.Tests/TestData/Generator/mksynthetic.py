# Writes seeded random-weight .nam models that exercise WaveNet and LSTM features
# the NeuralAmpModelerCore example models don't: gating, every supported
# activation, bottleneck != channels, a kernel>1 dilated head, an inactive
# layer1x1, per-layer kernel sizes, and a stacked LSTM. The weights are random, so
# the models sound like nothing in particular; they exist so the C# engines can be
# compared with the reference on these code paths.
#
# usage: python3 mksynthetic.py <output directory>
import json, os, random, sys

out = sys.argv[1]
META = {"loudness": -20.0}


def wavenet_weight_count(arrays):
    total = 0
    for la in arrays:
        ch, b = la["channels"], la.get("bottleneck", la["channels"])
        n = len(la["dilations"])
        ks = la.get("kernel_sizes") or [la["kernel_size"]] * n
        gm = la.get("gating_mode", ["none"] * n)
        l1 = la.get("layer1x1", {"active": True})["active"]
        total += la["input_size"] * ch
        for i in range(n):
            z = 2 * b if gm[i] == "gated" else b
            total += ks[i] * ch * z + z + z
            if l1:
                total += b * ch + ch
        head = la["head"]
        total += head["kernel_size"] * b * head["out_channels"] + (head["out_channels"] if head["bias"] else 0)
    return total + 1


def write(name, model):
    with open(os.path.join(out, name), "w") as f:
        json.dump(model, f)


rng = random.Random(20261018)
acts = ["Tanh", {"type": "LeakyReLU", "negative_slope": 0.2}, "Hardtanh", "SiLU", "Softsign",
        "Hardswish", "Fasttanh", {"type": "LeakyHardtanh", "min_val": -0.5, "max_val": 0.7, "min_slope": 0.1, "max_slope": 0.05},
        "Sigmoid", "ReLU"]
arrays = [
    {   # gated and plain layers, bottleneck < channels, a dilated kernel-3 head
        "input_size": 1, "condition_size": 1, "channels": 6, "bottleneck": 4,
        "kernel_sizes": [3, 2, 4, 3, 3, 2, 3, 5, 3, 2], "dilations": [1, 2, 4, 8, 16, 32, 64, 3, 9, 27],
        "activation": acts,
        "gating_mode": ["gated", "none", "gated", "none", "none", "gated", "none", "none", "gated", "none"],
        "secondary_activation": ["Sigmoid", None, "Tanh", None, None, "Sigmoid", None, None, "Softsign", None],
        "layer1x1": {"active": True, "groups": 1},
        "head": {"out_channels": 5, "kernel_size": 3, "head_dilation": 2, "bias": True},
    },
    {   # identity residual (layer1x1 inactive), legacy single kernel_size and activation
        "input_size": 6, "condition_size": 1, "channels": 5,
        "kernel_size": 3, "dilations": [1, 4, 16, 64, 128], "activation": "Tanh",
        "layer1x1": {"active": False, "groups": 1},
        "head": {"out_channels": 1, "kernel_size": 1, "bias": False},
    },
]
n = wavenet_weight_count(arrays)
write("synthetic_wavenet.nam", {
    "version": "0.7.0", "architecture": "WaveNet", "metadata": META, "sample_rate": 48000,
    "config": {"layers": arrays, "head": None, "head_scale": 0.02},
    "weights": [round(rng.gauss(0, 0.3), 6) for _ in range(n - 1)] + [0.3],
})

hidden, layers = 12, 2
weights = []
for l in range(layers):
    inputs = 1 if l == 0 else hidden
    for row in range(4 * hidden):  # weight matrix row by row: input columns, then recurrent ones
        weights += [round(rng.gauss(0, 2.0 if l == 0 else 0.5), 6) for _ in range(inputs)]
        weights += [round(rng.gauss(0, 0.2), 6) for _ in range(hidden)]
    weights += [round(rng.gauss(0, 0.1), 6) for _ in range(4 * hidden)]  # bias
    weights += [round(rng.gauss(0, 0.1), 6) for _ in range(2 * hidden)]  # initial hidden and cell state
weights += [round(rng.gauss(0, 0.5), 6) for _ in range(hidden)] + [0.01]  # head
write("synthetic_lstm.nam", {
    "version": "0.5.4", "architecture": "LSTM", "metadata": META, "sample_rate": 48000,
    "config": {"input_size": 1, "hidden_size": hidden, "num_layers": layers},
    "weights": weights,
})
