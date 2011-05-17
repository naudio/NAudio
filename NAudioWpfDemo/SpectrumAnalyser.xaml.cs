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
        enum DrawMode
        {
            Polyline,
            Bars,
        }

        private double xScale = 200;
        private int bins = 512; // guess a 1024 size FFT, bins is half FFT size

        public SpectrumAnalyser()
        {
            InitializeComponent();
            CalculateXScale();
            this.SizeChanged += new SizeChangedEventHandler(SpectrumAnalyser_SizeChanged);
        }

        void SpectrumAnalyser_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CalculateXScale();            
        }

        private void CalculateXScale()
        {
            this.xScale = this.ActualWidth / bins;
        }

        public void Update(Complex[] fftResults)
        {
            if (fftResults.Length / 2 != bins)
            {
                this.bins = fftResults.Length / 2;
                CalculateXScale();
            }
            
            this.xScale = this.ActualWidth / bins;
            for (int n = 0; n < fftResults.Length / 2; n++)
            {
                //double yScale = 1000
                //double intensity = Math.Sqrt(fftResults[n].X * fftResults[n].X + fftResults[n].Y * fftResults[n].Y);
                //double yPos = this.ActualHeight - intensity * yScale;

                // can also try 5 * without the sqrt seems to give decent results
                double intensityDB = 10 * Math.Log(Math.Sqrt(fftResults[n].X * fftResults[n].X + fftResults[n].Y * fftResults[n].Y));
                double minDB = -96;
                if (intensityDB < minDB) intensityDB = minDB;
                double percent = intensityDB / minDB;
                // we want 0dB to be at the top (i.e. yPos = 0)
                double yPos = percent * this.ActualHeight;

                
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
            return bin * xScale; // Math.Log10(bin) * xScale;
        }
    }
}
