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

namespace NAudioWpfDemo.DrumMachineDemo
{
    /// <summary>
    /// Interaction logic for DrumPatternEditor.xaml
    /// </summary>
    public partial class DrumPatternEditor : UserControl
    {
        public DrumPatternEditor()
        {
            double gridSquareWidth = 20;
            InitializeComponent();
            for (int n = 0; n < 16; n++)
            {
                for (int j = 0; j < 4; j++)
                {
                    Rectangle r = new Rectangle();
                    r.Stroke = Brushes.Black;
                    r.Fill = Brushes.White; // fill it or we won't get mouse-clicks
                    r.StrokeThickness = 1;
                    r.Width = gridSquareWidth;
                    r.Height = gridSquareWidth;
                    r.SetValue(Canvas.LeftProperty, n * gridSquareWidth);
                    r.SetValue(Canvas.TopProperty, j * gridSquareWidth);
                    r.MouseLeftButtonUp += r_MouseLeftButtonUp;
                    r.Tag = false;
                    drumGridCanvas.Children.Add(r);
                }
            }
        }

        void r_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Rectangle r = (Rectangle)sender;
            bool isChecked = (bool)r.Tag;
            r.Tag = !isChecked;
            r.Fill = new SolidColorBrush(isChecked ? Colors.White : Colors.Red);
        }
    }
}
