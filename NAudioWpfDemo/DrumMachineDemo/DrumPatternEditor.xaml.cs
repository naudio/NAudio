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
        private double namesColumnWidth = 100;

        public DrumPatternEditor()
        {
            InitializeComponent();
            var notes = new string[] { "Kick", "Snare", "Closed Hats", "Open Hats" };
            this.pattern = new DrumPattern(notes, 16);
            DrawNoteNames();        
            DrawPattern(namesColumnWidth);
            DrawGridLines(namesColumnWidth);
        }

        public DrumPattern DrumPattern
        {
            get { return pattern; }
        }

        private void DrawNoteNames()
        {
            for (int note = 0; note < pattern.Notes; note++)
            {
                var tb = new TextBlock();
                tb.Text = pattern.NoteNames[note];
                tb.SetValue(Canvas.LeftProperty, 0.0);
                tb.SetValue(Canvas.TopProperty, note * gridSquareWidth);
                tb.Foreground = Brushes.Gray;
                tb.FontFamily = new FontFamily("Segoe UI");
                tb.FontSize = 12;
                drumGridCanvas.Children.Add(tb);
            }
        }

        private void DrawGridLines(double startX)
        {
            for (int step = 0; step <= pattern.Steps; step++)
            {
                // vertical lines
                Line l = new Line();
                l.X1 = l.X2 = startX + step * gridSquareWidth;
                l.Y1 = 0;
                l.Y2 = pattern.Notes * gridSquareWidth;
                l.Stroke = step % 4 == 0 ? Brushes.Gray : Brushes.LightGray;
                l.StrokeThickness = 1;
                drumGridCanvas.Children.Add(l);
            }

            for (int note = 0; note <= pattern.Notes; note++)
            {
                Line l = new Line();
                l.X1 = 0; // extend back to encompass names too
                l.X2 = startX + pattern.Steps * gridSquareWidth;
                l.Y1 = l.Y2 = note * gridSquareWidth;                
                l.Stroke = Brushes.Gray;
                l.StrokeThickness = 1;
                drumGridCanvas.Children.Add(l);
            }
        }

        private void DrawPattern(double startX)
        {            
            for (int step = 0; step < pattern.Steps; step++)
            {
                for (int note = 0; note < pattern.Notes; note++)
                {
                    Rectangle r = new Rectangle();
                    r.Fill = Brushes.White; // fill it or we won't get mouse-clicks
                    r.Width = gridSquareWidth;
                    r.Height = gridSquareWidth;
                    r.SetValue(Canvas.LeftProperty, startX + step * gridSquareWidth);
                    r.SetValue(Canvas.TopProperty, note * gridSquareWidth);
                    r.MouseLeftButtonUp += r_MouseLeftButtonUp;
                    //r.IsHitTestVisible = false;
                    r.Tag = new PatternIndex(note, step);
                    drumGridCanvas.Children.Add(r);
                }
            }
            //drumGridCanvas.MouseLeftButtonDown += r_MouseLeftButtonUp;
        }

        void r_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Rectangle r = (Rectangle)sender;
            PatternIndex p = (PatternIndex)r.Tag;
            
            pattern[p.Note, p.Step] = pattern[p.Note, p.Step] == 0 ? (byte)127 : (byte)0;
            r.Fill = pattern[p.Note, p.Step] == 0 ? Brushes.White : Brushes.LightSalmon;
        }

        class PatternIndex
        {
            public PatternIndex(int note, int step)
            {
                this.Note = note;
                this.Step = step;
            }
            public int Note { get; private set; }
            public int Step { get; private set; }
        }
    }
}
