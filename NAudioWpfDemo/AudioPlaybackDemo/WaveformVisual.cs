using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows;

namespace NAudioWpfDemo
{
    class WaveFormVisual : FrameworkElement, IWaveFormRenderer
    {
        // Create a collection of child visual objects.
        private VisualCollection _children;
        private List<Point> maxPoints;
        private List<Point> minPoints;
        double yTranslate = 40;
        double yScale = 40;        

        public WaveFormVisual()
        {
            maxPoints = new List<Point>();
            minPoints = new List<Point>();
            _children = new VisualCollection(this);
            _children.Add(CreateWaveFormVisual());            
        }

        private DrawingVisual CreateWaveFormVisual()
        {
            DrawingVisual drawingVisual = new DrawingVisual();

            // Retrieve the DrawingContext in order to create new drawing content.
            DrawingContext drawingContext = drawingVisual.RenderOpen();
            if (maxPoints.Count > 0)
            {
                RenderPolygon(drawingContext);
            }
            
            //drawingContext.DrawGeometry
            // Create a rectangle and draw it in the DrawingContext.
            //Rect rect = new Rect(new System.Windows.Point(160, 100), new System.Windows.Size(320, 80));
            //drawingContext.DrawRectangle(System.Windows.Media.Brushes.LightBlue, (System.Windows.Media.Pen)null, rect);

            // Persist the drawing content.
            drawingContext.Close();

            return drawingVisual;
        }

        private void RenderPolygon(DrawingContext drawingContext)
        {
            var fillBrush = Brushes.LawnGreen;
            var borderPen = new Pen(Brushes.Black,1.0);

            PathFigure myPathFigure = new PathFigure();
            myPathFigure.StartPoint = maxPoints[0];

            //PolyLineSegment seg = new PolyLineSegment(

            PathSegmentCollection myPathSegmentCollection = new PathSegmentCollection();

            for (int i = 1; i < maxPoints.Count; i++)
            {
                myPathSegmentCollection.Add(new LineSegment(maxPoints[i], true));
            }
            for (int i = minPoints.Count - 1; i >= 0; i--)
            {
                myPathSegmentCollection.Add(new LineSegment(minPoints[i], true));
            }

            myPathFigure.Segments = myPathSegmentCollection;
            PathGeometry myPathGeometry = new PathGeometry();
            
            myPathGeometry.Figures.Add(myPathFigure);

            drawingContext.DrawGeometry(fillBrush, borderPen, myPathGeometry);
        }

        // Provide a required override for the VisualChildrenCount property.
        protected override int VisualChildrenCount
        {
            get { return _children.Count; }
        }

        // Provide a required override for the GetVisualChild method.
        protected override Visual GetVisualChild(int index)
        {
            if (index < 0 || index >= _children.Count)
            {
                throw new ArgumentOutOfRangeException();
            }

            return _children[index];
        }


        #region IWaveFormRenderer Members

        public void AddValue(float maxValue, float minValue)
        {
            int xpos = maxPoints.Count;
            maxPoints.Add(new Point(xpos, SampleToYPosition(minValue)));
            minPoints.Add(new Point(xpos, SampleToYPosition(maxValue)));
            _children.Clear();

            _children.Add(CreateWaveFormVisual());
            this.InvalidateVisual();
        }
        private double SampleToYPosition(float value)
        {
            return yTranslate + value * yScale;
        }
        #endregion
    }
}
