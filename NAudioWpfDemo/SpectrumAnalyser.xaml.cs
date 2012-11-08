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
            this.xScale = this.ActualWidth / (bins / binsPerPoint);
        }

        private const int binsPerPoint = 2; // reduce the number of points we plot for a less jagged line?
        private int updateCount;

        public void Update(Complex[] fftResults)
        {
            // no need to repaint too many frames per second
            if (updateCount++ % 2 == 0)
            {
                return;
            }

            if (fftResults.Length / 2 != bins)
            {
                this.bins = fftResults.Length / 2;
                CalculateXScale();
            }
            
            for (int n = 0; n < fftResults.Length / 2; n+= binsPerPoint)
            {
                double yPos = GetYPosLog(fftResults[n]);
                AddResult(n / binsPerPoint, yPos);
            }
        }

        private double GetYPosIntensityOnly(Complex c)
        {
            double yScale = 1000;
            double intensity = Math.Sqrt(c.X * c.X + c.Y * c.Y);
            double yPos = this.ActualHeight - intensity * yScale;
            return yPos;
        }

        private double GetYPosLog(Complex c)
        {
            // in theory should be 20x to get the power, but doesn't seem to give me sensible values (may be because we throw half the FFT away - bin 0 might need to be halved)
            double intensityDB = 10 * Math.Log(Math.Sqrt(c.X * c.X + c.Y * c.Y));
            double minDB = -96;
            if (intensityDB < minDB) intensityDB = minDB;
            double percent = intensityDB / minDB;
            // we want 0dB to be at the top (i.e. yPos = 0)
            double yPos = percent * this.ActualHeight;
            return yPos;
        }

        private double GetYPosAnotherTry(Complex c, int binNumber, int fftLength)
        {
            // this technique based on http://www.mathworks.com/support/tech-notes/1700/1702.html
            double abs = Math.Sqrt(c.X * c.X + c.Y * c.Y); // do not scale by FFT length as NAudio already does this.
            abs = abs * abs;
            if (binNumber != 0) abs *= 2;
            double yPos = abs * this.ActualHeight;
            return yPos;
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
