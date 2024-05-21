using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using NAudioAvaloniaDemo.DynamicShapes;

namespace NAudioAvaloniaDemo.VisualizationControls
{
    /// <summary>
    /// Interaction logic for PolygonWaveFormControl.xaml
    /// </summary>
    public partial class PolygonWaveFormControl : UserControl, IWaveFormRenderer
    {
        int renderPosition;
        double yTranslate = 40;
        double yScale = 40;
        double xScale = 2;
        int blankZone = 10;

        readonly DynamicPolygon waveForm = new DynamicPolygon();

        public PolygonWaveFormControl()
        {
            InitializeComponent();
            waveForm.Stroke = Foreground;
            waveForm.StrokeThickness = 1;
            waveForm.Fill = new SolidColorBrush(Colors.Bisque);
            mainCanvas.Children.Add(waveForm);
            SizeChanged += OnSizeChanged;
        }

        void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // We will remove everything as we are going to rescale vertically
            renderPosition = 0;
            ClearAllPoints();

            yTranslate = Bounds.Height / 2;
            yScale = Bounds.Height / 2;
        }

        private void ClearAllPoints()
        {
            waveForm.Points.Clear();
        }

        private int Points
        {
            get { return waveForm.Points.Count / 2; }
        }

        public void AddValue(float maxValue, float minValue)
        {
            int visiblePixels = (int)(Bounds.Width / xScale);
            if (visiblePixels > 0)
            {
                CreatePoint(maxValue, minValue);

                if (renderPosition > visiblePixels)
                {
                    renderPosition = 0;
                }
                int erasePosition = (renderPosition + blankZone) % visiblePixels;
                if (erasePosition < Points)
                {
                    double yPos = SampleToYPosition(0);
                    waveForm.Points[erasePosition] = new Point(erasePosition * xScale, yPos);
                    waveForm.Points[BottomPointIndex(erasePosition)] = new Point(erasePosition * xScale, yPos);
                }
            }
        }

        private int BottomPointIndex(int position)
        {
            return waveForm.Points.Count - position - 1;
        }

        private double SampleToYPosition(float value)
        {
            return yTranslate + value * yScale;
        }

        private void CreatePoint(float topValue, float bottomValue)
        {
            double topYPos = SampleToYPosition(topValue);
            double bottomYPos = SampleToYPosition(bottomValue);
            double xPos = renderPosition * xScale;
            if (renderPosition >= Points)
            {
                int insertPos = Points;
                waveForm.Points.Insert(insertPos, new Point(xPos, topYPos));
                waveForm.Points.Insert(insertPos + 1, new Point(xPos, bottomYPos));
            }
            else
            {
                waveForm.Points[renderPosition] = new Point(xPos, topYPos);
                waveForm.Points[BottomPointIndex(renderPosition)] = new Point(xPos, bottomYPos);
            }
            renderPosition++;
        }

        /// <summary>
        /// Clears the waveform and repositions on the left
        /// </summary>
        public void Reset()
        {
            renderPosition = 0;
            ClearAllPoints();
        }
    }
}
