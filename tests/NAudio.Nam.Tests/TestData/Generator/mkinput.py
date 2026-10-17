# Deterministic 0.5 s test signal at 48 kHz: quiet sweep, loud power chord with
# an attack envelope, seeded noise, an impulse, then silence for the tail.
import math, random, struct, sys
sr = 48000
x = []
for i in range(4800):  # 0.1 s quiet log sweep 80 Hz -> 4 kHz
    t = i / sr; f0, f1, T = 80.0, 4000.0, 0.1
    k = math.log(f1 / f0) / T
    x.append(0.05 * math.sin(2 * math.pi * f0 * (math.exp(k * t) - 1) / k))
for i in range(9600):  # 0.2 s loud E5 power chord
    t = i / sr
    env = min(1.0, t / 0.005) * math.exp(-t * 4)
    x.append(0.8 * env * (math.sin(2*math.pi*82.41*t) + 0.7*math.sin(2*math.pi*123.47*t) + 0.5*math.sin(2*math.pi*164.81*t)) / 2.2)
rng = random.Random(1234)
for i in range(4800):  # 0.1 s noise
    x.append(0.3 * (rng.random() * 2 - 1))
x.append(1.0)          # impulse
x += [0.0] * (24000 - len(x))  # silence tail
open(sys.argv[1], "wb").write(struct.pack("<%df" % len(x), *x))
