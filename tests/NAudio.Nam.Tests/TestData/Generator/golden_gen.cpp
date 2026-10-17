// Renders a raw float32 input through a .nam model with NeuralAmpModelerCore,
// producing the golden files the NAudio.Nam tests compare against.
// usage: golden_gen <model.nam> <input.f32> <output.f32> <slim 0..1> <blocksize>
#include <cstdio>
#include <cstdlib>
#include <filesystem>
#include <fstream>
#include <iostream>
#include <vector>
#include "NAM/dsp.h"
#include "NAM/get_dsp.h"
#include "NAM/slimmable.h"

int main(int argc, char** argv)
{
  if (argc != 6) { std::cerr << "usage\n"; return 1; }
  auto model = nam::get_dsp(std::filesystem::path(argv[1]));
  double slim = std::atof(argv[4]);
  int block = std::atoi(argv[5]);
  if (auto* s = dynamic_cast<nam::SlimmableModel*>(model.get())) s->SetSlimmableSize(slim);
  std::ifstream in(argv[2], std::ios::binary | std::ios::ate);
  const size_t bytes = in.tellg();
  in.seekg(0);
  std::vector<float> x(bytes / 4);
  in.read(reinterpret_cast<char*>(x.data()), bytes);
  model->Reset(48000.0, block); // prewarms
  std::cerr << "prewarm samples " << model->GetPrewarmSamples() << "\n";
  std::vector<double> ib(block), ob(block);
  std::vector<float> y(x.size());
  for (size_t o = 0; o < x.size(); o += block)
  {
    int n = (int)std::min<size_t>(block, x.size() - o);
    for (int i = 0; i < n; i++) ib[i] = x[o + i];
    double* ip = ib.data(); double* op = ob.data();
    model->process(&ip, &op, n);
    for (int i = 0; i < n; i++) y[o + i] = (float)ob[i];
  }
  std::ofstream out(argv[3], std::ios::binary);
  out.write(reinterpret_cast<const char*>(y.data()), y.size() * 4);
  return 0;
}
