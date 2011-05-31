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
using NAudio.Midi;

namespace NAudioWpfDemo.DrumMachineDemo
{
    /// <summary>
    /// Interaction logic for DrumPatternEditor.xaml
    /// </summary>
    public partial class DrumPatternEditor : UserControl
    {
        private DrumPattern pattern;
        private double gridSquareWidth = 20;

        public DrumPatternEditor()
        {
            InitializeComponent();
            this.pattern = new DrumPattern(4, 16);
            DrawPattern();
        }

        public DrumPattern DrumPattern
        {
            get { return pattern; }
        }

        private void DrawPattern()
        {
            
            for (int step = 0; step < pattern.Steps; step++)
            {
                for (int note = 0; note < pattern.Notes; note++)
                {
                    Rectangle r = new Rectangle();
                    r.Stroke = Brushes.Black;
                    r.Fill = Brushes.White; // fill it or we won't get mouse-clicks
                    r.StrokeThickness = 1;
                    r.Width = gridSquareWidth;
                    r.Height = gridSquareWidth;
                    r.SetValue(Canvas.LeftProperty, step * gridSquareWidth);
                    r.SetValue(Canvas.TopProperty, note * gridSquareWidth);
                    r.MouseLeftButtonUp += r_MouseLeftButtonUp;
                    //r.IsHitTestVisible = false;
                    r.Tag = false;
                    drumGridCanvas.Children.Add(r);
                }
            }
            //drumGridCanvas.MouseLeftButtonDown += r_MouseLeftButtonUp;

        }

        void r_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            /*Rectangle r = (Rectangle)sender;
            bool isChecked = (bool)r.Tag;
            r.Tag = !isChecked;
            r.Fill = new SolidColorBrush(isChecked ? Colors.White : Colors.Red);*/
            var p = e.GetPosition(drumGridCanvas);
            int step = (int)(p.X / gridSquareWidth);
            int note = (int)(p.Y / gridSquareWidth);
            pattern[note, step] = pattern[note, step] == 0 ? (byte)127 : (byte)0;
            Rectangle r = (Rectangle)sender;
            r.Fill = pattern[note, step] == 0 ? Brushes.White : Brushes.Red;
        }
    }


}
