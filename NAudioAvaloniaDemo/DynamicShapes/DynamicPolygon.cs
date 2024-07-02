using System.Collections.Specialized;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls.Shapes;

namespace NAudioAvaloniaDemo.DynamicShapes
{
    public class DynamicPolygon : Polygon
    {
        public DynamicPolygon()
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