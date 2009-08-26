using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NAudio.Dsp;

namespace NAudioWpfDemo
{
    /// <summary>
    /// Interaction logic for SpectrumAnalyser.xaml
    /// </summary>
    public partial class SpectrumAnalyser : UserControl
    {
        private double yScale = 1000;
        private double xScale = 200;
        public SpectrumAnalyser()
        {
            InitializeComponent();
        }

        public void Update(Complex[] fftResults)
        {
            for (int n = 0; n < fftResults.Length / 2; n++)
            {
                double intensity = Math.Sqrt(fftResults[n].X * fftResults[n].X + fftResults[n].Y * fftResults[n].Y);
                double yPos = intensity * yScale;
                //double decibels = 10 * Math.Log10(fftResults[n].X * fftResults[n].X + fftResults[n].Y * fftResults[n].Y);
                //double yPos = decibels * -1.0;
                
                
                AddResult(n, yPos);
            }
        }

        private void AddResult(int index, double power)
        {
            Point p = new Point(CalculateXPos(index), power);
            if (index >= polyline1.Points.Count)
            {
                polyline1.Points.Add(p);
            }
            else
            {
                polyline1.Points[index] = p;
            }
        }

        private double CalculateXPos(int bin)
        {
            if (bin == 0) return 0;
            return Math.Log10(bin) * xScale;
        }
    }
}
