using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MapboxNetCore
{
    public interface IMap
    {
        event EventHandler Ready;
        event EventHandler Styled;
        event EventHandler Render;
        event EventHandler CenterChanged;
        event EventHandler ZoomChanged;
        event EventHandler PitchChanged;
        event EventHandler BearingChanged;
        event EventHandler Reloading;
        public event EventHandler MapMouseDown;
        public event EventHandler MapMouseUp;
        public event EventHandler MapMouseMove;
        // Returns GUID of point
        public event EventHandler<string> PointClicked;
        // Cluster of points clicked event?

        string AccessToken { get; set; }
        string  MapStyle { get; set; }
        bool RemoveAttribution { get; set; }
        GeoLocation Center { get; set; }
        double Zoom { get; set; }
        double Pitch { get; set; }
        double Bearing { get; set; }
        bool IsReady { get; }

        dynamic Invoke { get; }
        dynamic SoftInvoke { get; }
        dynamic LazyInvoke { get; }

        object SoftExecute(string expression);
        object Execute(string expression);
        Task<object> ExecuteAsync(string expression);

        Point2D Project(GeoLocation location);
        GeoLocation UnProject(Point2D point);

        public void FlyTo(GeoLocation loc, double zoom);

        Task AddPoint(MapboxPoint p);
        Task AddPoints(List<MapboxPoint> p);
        Task RemovePoint(string GUID);
        Task RemovePoints(List<string> GUIDs);
        Task ClearPoints();
    }
}
