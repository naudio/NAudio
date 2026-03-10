using System.Collections.Specialized;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls.Shapes;

namespace NAudioAvaloniaDemo.DynamicShapes
{
    public class DynamicPolyline : Polyline
    {
        public DynamicPolyline()
        {
            var points = new AvaloniaList<Point>();
            points.CollectionChanged += OnPointsChanged;
            Points = points;
        }

        private void OnPointsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            InvalidateGeometry();
        }
    }
}