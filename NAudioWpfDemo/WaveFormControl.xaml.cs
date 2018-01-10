using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace NAudioWpfDemo
{
    /// <summary>
    /// Interaction logic for WaveFormControl.xaml
    /// </summary>
    public partial class WaveFormControl : UserControl, IWaveFormRenderer
    {
        int renderPosition;
        double yTranslate = 40;
        double yScale = 40;
        int blankZone = 10;
        
        List<Line> lines = new List<Line>();

        public WaveFormControl()
        {
            InitializeComponent();
            SizeChanged += WaveFormControl_SizeChanged;
        }

        void WaveFormControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // To just remove what is on the right of the now cursor:
            /*int remove = mainCanvas.Children.Count - x;
            mainCanvas.Children.RemoveRange(0, remove);*/

            // We will remove everything as we are going to rescale vertically
            renderPosition = 0;
            ClearAllLines();

            yTranslate = ActualHeight / 2;
            yScale = ActualHeight / 2;
        }

        private void ClearAllLines()
        {
            //mainCanvas.Children.Clear();
            for (int n = 0; n < lines.Count; n++)
            {
                lines[n].Visibility = Visibility.Collapsed;
            }
        }

        public void AddValue(float maxValue, float minValue)
        {
            int pixelWidth = (int)ActualWidth;
            if (pixelWidth > 0)
            {
                Line line = CreateLine(maxValue, minValue);

                if (renderPosition > ActualWidth)
                {
                    renderPosition = 0;
                }
                int erasePosition = (renderPosition + blankZone) % pixelWidth;
                if (erasePosition < lines.Count)
                {
                    lines[erasePosition].Visibility = Visibility.Collapsed;
                }
            }
        }

        private Line CreateLine(float maxValue, float minValue)
        {
            Line line;
            if (renderPosition >= lines.Count)
            {
                line = new Line();
                lines.Add(line);
                mainCanvas.Children.Add(line);
            }
            else
            {
                line = lines[renderPosition];
            }
            line.Stroke = Foreground;
            line.X1 = renderPosition;
            line.X2 = renderPosition;
            line.Y1 = yTranslate + minValue * yScale;
            line.Y2 = yTranslate + maxValue * yScale;
            renderPosition++;
            line.Visibility = Visibility.Visible;
            return line;
        }

        /// <summary>
        /// Clears the waveform and repositions on the left
        /// </summary>
        public void Reset()
        {
            renderPosition = 0;
            ClearAllLines();
        }
    }
}
