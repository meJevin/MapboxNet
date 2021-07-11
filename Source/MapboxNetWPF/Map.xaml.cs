using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using CefSharp;
using CefSharp.Wpf;
using MapboxNetCore;
using System.Collections.Generic;

namespace MapboxNetWPF
{
    /// <summary>
    /// Interaction logic for Map.xaml
    /// </summary>
    public partial class Map : UserControl, IMap
    {
        public event EventHandler Ready;
        public event EventHandler Styled;
        public event EventHandler Render;
        public event EventHandler CenterChanged;
        public event EventHandler ZoomChanged;
        public event EventHandler PitchChanged;
        public event EventHandler BearingChanged;
        public event EventHandler Reloading;
        public event EventHandler MapMouseDown;
        public event EventHandler MapMouseUp;
        public event EventHandler MapMouseMove;
        public event EventHandler<string> PointClicked;

        public string AccessToken
        {
            get => (string)GetValue(AccessTokenProperty);
            set => SetValue(AccessTokenProperty, value);
        }

        public static readonly DependencyProperty AccessTokenProperty 
            = DependencyProperty.Register(
                nameof(AccessToken), 
                typeof(string), 
                typeof(Map), 
                new PropertyMetadata("", UpdateAccessToken));

        static void UpdateAccessToken(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var map = obj as Map;
            map.Init();
        }


        public string MapStyle
        {
            get => (string)GetValue(MapStyleProperty);
            set => SetValue(MapStyleProperty, value);
        }

        public static readonly DependencyProperty MapStyleProperty 
            = DependencyProperty.Register(
                nameof(MapStyle),
                typeof(string),
                typeof(Map),
                new PropertyMetadata("mapbox://styles/mapbox/streets-v11", UpdateMapStyle));

        static void UpdateMapStyle(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var map = obj as Map;
            map.Init();
        }

        public bool RemoveAttribution
        {
            get => (bool)GetValue(RemoveAttributionProperty);
            set => SetValue(RemoveAttributionProperty, value);
        }

        public static readonly DependencyProperty RemoveAttributionProperty 
            = DependencyProperty.Register(
                nameof(RemoveAttribution),
                typeof(bool),
                typeof(Map),
                new PropertyMetadata(false, UpdateAttribution));

        static void UpdateAttribution(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var map = obj as Map;
            if (map.IsReady && !map._supressChangeEvents)
                if (map.RemoveAttribution && !map._supressChangeEvents)
                    map.SoftExecute("map.getContainer().classList.add('no-attrib');");
                else
                    map.SoftExecute("map.getContainer().classList.remove('no-attrib');");
        }

        public GeoLocation Center
        {
            get => (GeoLocation)GetValue(CenterProperty);
            set => SetValue(CenterProperty, value);
        }

        public static readonly DependencyProperty CenterProperty = DependencyProperty.Register(nameof(Center), typeof(GeoLocation), typeof(Map), new PropertyMetadata(new GeoLocation(), new PropertyChangedCallback(UpdateCenter)));

        static void UpdateCenter(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var map = obj as Map;
            if (map.IsReady && !map._supressChangeEvents)
                map.SoftInvoke.SetCenter(new { lon = map.Center.Longitude, lat = map.Center.Latitude });
        }
        
        public double Zoom
        {
            get => (double)GetValue(ZoomProperty);
            set => SetValue(ZoomProperty, value);
        }

        public static readonly DependencyProperty ZoomProperty = DependencyProperty.Register(nameof(Zoom), typeof(double), typeof(Map), new PropertyMetadata((double)0, new PropertyChangedCallback(UpdateZoom)));
        
        static void UpdateZoom(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var map = obj as Map;
            if (map.IsReady && !map._supressChangeEvents)
                map.SoftInvoke.SetZoom(map.Zoom);
        }
        
        public double Pitch
        {
            get => (double)GetValue(PitchProperty);
            set => SetValue(PitchProperty, value);
        }

        public static readonly DependencyProperty PitchProperty = DependencyProperty.Register(nameof(Pitch), typeof(double), typeof(Map), new PropertyMetadata((double)0, new PropertyChangedCallback(UpdatePitch)));

        static void UpdatePitch(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var map = obj as Map;
            if (map.IsReady && !map._supressChangeEvents)
                map.SoftInvoke.SetPitch(map.Pitch);
        }
        
        public double Bearing
        {
            get => (double)GetValue(BearingProperty);
            set => SetValue(BearingProperty, value);
        }

        public static readonly DependencyProperty BearingProperty = DependencyProperty.Register(nameof(Bearing), typeof(double), typeof(Map), new PropertyMetadata((double)0, new PropertyChangedCallback(UpdateBearing)));


        static void UpdateBearing(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var map = obj as Map;
            if (map.IsReady && !map._supressChangeEvents)
                map.SoftInvoke.SetBearing(map.Bearing);
        }

        bool _supressChangeEvents = false;
        bool _arePropertiesUpdated = false;
        
        public bool IsReady
        {
            get => (bool)GetValue(IsReadyProperty);
            private set => SetValue(IsReadyProperty, value);
        }

        public static readonly DependencyProperty IsReadyProperty = DependencyProperty.Register(nameof(IsReady), typeof(bool), typeof(Map), new PropertyMetadata(false));

        public dynamic Invoke
        {
            get
            {
                var expressionBuilder = new ExpressionBuilder("map");
                expressionBuilder.Execute = Execute;
                expressionBuilder.TransformToken = Core.ToLowerCamelCase;
                return expressionBuilder;
            }
        }

        public dynamic SoftInvoke
        {
            get
            {
                var expressionBuilder = new ExpressionBuilder("map");
                expressionBuilder.Execute = SoftExecute;
                expressionBuilder.TransformToken = Core.ToLowerCamelCase;
                return expressionBuilder;
            }
        }

        public dynamic LazyInvoke
        {
            get
            {
                var expressionBuilder = new ExpressionBuilder("map");
                expressionBuilder.Execute = Execute;
                expressionBuilder.TransformToken = Core.ToLowerCamelCase;
                expressionBuilder.ExecuteKey = "Eval";
                return expressionBuilder;
            }
        }

        ChromiumWebBrowser Browser;

        public Map()
        {
            InitializeComponent();

            if(!Cef.IsInitialized)
            {
                CefSettings settings = new CefSettings();
                settings.CefCommandLineArgs.Add("enable-gpu", "1");
                settings.CefCommandLineArgs.Add("enable-webgl", "1");
                settings.CefCommandLineArgs.Add("enable-begin-frame-scheduling", "1");
                settings.CefCommandLineArgs.Add("--off-screen-frame-rate", "60");
                //settings.SetOffScreenRenderingBestPerformanceArgs();

                Cef.Initialize(settings);
            }
        }

        void Init()
        {
            if (Browser != null)
            {
                MainGrid.Children.Remove(Browser);
                Browser = null;
                Reloading?.Invoke(this, null);
            }

            BrowserSettings browserSettings = new BrowserSettings();
            browserSettings.WindowlessFrameRate = 60;

            Browser = new ChromiumWebBrowser();
            Browser.IsBrowserInitializedChanged += WebView_IsBrowserInitializedChanged;
            Browser.BrowserSettings = browserSettings;
            MainGrid.Children.Add(Browser);
        }

        private void WebView_IsBrowserInitializedChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Browser.IsBrowserInitialized)
            {
#if DEBUG && SHOW_DEV_TOOLS_MAPBOX_NET
                Browser.ShowDevTools();
#endif
                Browser.ShowDevTools();
                var script = Core.GetFrameHTML(AccessToken, MapStyle);
                //webView.Address = "chrome://gpu";
                Browser.LoadHtml(script, "http://MapboxNet/");
                Browser.JavascriptObjectRepository.Register("relay", new Relay(OnNotify, this.Dispatcher), true);
            }
        }

        void OnReady()
        {
            IsReady = true;
            UpdateCenter(this, new DependencyPropertyChangedEventArgs());
            UpdateZoom(this, new DependencyPropertyChangedEventArgs());
            UpdatePitch(this, new DependencyPropertyChangedEventArgs());
            UpdateBearing(this, new DependencyPropertyChangedEventArgs());
            UpdateAttribution(this, new DependencyPropertyChangedEventArgs());
            _arePropertiesUpdated = true;
            Ready?.Invoke(this, null);
            return;
        }

        void OnNotify(string json)
        {       
            dynamic data = Core.DecodeJsonPlain(json);

            if (data.type == "ready")
            {
                OnReady();
                Render?.Invoke(this, null);
            }
            else if (data.type == "load")
            {
                Styled?.Invoke(this, null);
            }


            if (!_arePropertiesUpdated)
                return;

            if (data.type == "move")
            {
                CenterChanged?.Invoke(this, null);
                Render?.Invoke(this, null);

                _supressChangeEvents = true;
                Center = new GeoLocation(data.center.lat, data.center.lng);
                _supressChangeEvents = false;
            }
            else if (data.type == "zoom")
            {
                ZoomChanged?.Invoke(this, null);
                Render?.Invoke(this, null);

                _supressChangeEvents = true;
                Zoom = data.zoom;
                _supressChangeEvents = false;
            }
            else if (data.type == "pitch")
            {
                PitchChanged?.Invoke(this, null);
                Render?.Invoke(this, null);

                _supressChangeEvents = true;
                Pitch = data.pitch;
                _supressChangeEvents = false;
            }
            else if (data.type == "bearing")
            {
                BearingChanged?.Invoke(this, null);
                Render?.Invoke(this, null);

                _supressChangeEvents = true;
                Bearing = data.bearing;
                _supressChangeEvents = false;
            }
            else if (data.type == "markerClicked")
            {
                PointClicked?.Invoke(this, data.guid);
            }
            else if (data.type == "error")
            {

            }
            else if (data.type == "mouseDown")
            {
                MapMouseDown?.Invoke(this, new EventArgs());
            }
            else if (data.type == "mouseUp")
            {
                MapMouseUp?.Invoke(this, new EventArgs());
            }
            else if (data.type == "mouseMove")
            {
                MapMouseMove?.Invoke(this, new EventArgs());
            }
            else if (data.type == "pointClicked")
            {
                PointClicked?.Invoke(this, data.guid);
            }
        }

        public object SoftExecute(string expression)
        {
            try
            {
                return Execute(expression);
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public object Execute(string expression)
        {
            try
            {
                var task = Browser.EvaluateScriptAsync("exec", new object[] { expression });
                task.Wait();

                object result = null;
                JavascriptResponse response = task.Result;
                if (!task.IsFaulted && response.Success)
                {
                    result = response.Result;

                    if (result == null)
                    {
                        return null;
                    }
                }
                else
                {
                    throw new Exception(response.Message);
                }

                try
                {
                    var obj = Core.DecodeJsonPlain(result.ToString());
                    return obj;
                } catch(Exception e)
                {
                    // TODO lodge exception when using ToString() on the result in certain cases
                    return null;
                }
            }
            catch (Exception e)
            {
                throw e;
            }

            return null;
        }

        public async Task<object> ExecuteAsync(string expression)
        {
            try
            {
                var result = await Browser.EvaluateScriptAsync("exec", new object[] { expression });
                
                if (result.Result == null)
                {
                    return null;
                }
                
                return Core.DecodeJsonPlain(result.Result.ToString());
            } catch(Exception e)
            {
                throw e;
            }
        }

        public void FlyTo(GeoLocation loc, double zoom)
        {
            var code = $"flyTo({loc.Longitude}, {loc.Latitude}, {zoom});";

            Execute(code);
        }

        public async Task AddPoint(MapboxPoint p)
        {
            await Browser.EvaluateScriptAsync("addPoint", JsonConvert.SerializeObject(p));
        }

        public async Task AddPoints(List<MapboxPoint> p)
        {
            await Browser.EvaluateScriptAsync("addPoints", JsonConvert.SerializeObject(p));
        }

        public async Task RemovePoint(string GUID)
        {
            var code = $"removePoint({GUID});";

            await ExecuteAsync(code);
        }

        public async Task RemovePoints(List<string> GUIDs)
        {
            await Browser.EvaluateScriptAsync("removePoints", JsonConvert.SerializeObject(GUIDs));
        }

        public async Task ClearPoints()
        {
            var code = $"clearPoints();";

            await ExecuteAsync(code);
        }

        public Point2D Project(GeoLocation location)
        {
            var pointOnScreen = Invoke.Project(new[] { location.Longitude, location.Latitude });
            return new Point2D((double)pointOnScreen.x, (double)pointOnScreen.y);
        }

        public GeoLocation UnProject(Point2D point)
        {
            var location = Invoke.Unproject(new[] { point.X, point.Y });
            return new GeoLocation((double)location.lat, (double)location.lng);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyRaised(string propertyname)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));
        }
    }
}
