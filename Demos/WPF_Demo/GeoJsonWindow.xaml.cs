using MapboxNetCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DemosWPF
{
    /// <summary>
    /// Interaction logic for GeoJsonWindow.xaml
    /// </summary>
    public partial class GeoJsonWindow : Window
    {
        Dictionary<GeoLocation, string> Markers = new Dictionary<GeoLocation, string>();

        public GeoJsonWindow(string accessToken)
        {
            InitializeComponent();
            Map.AccessToken = accessToken;
        }

        private void Map_Styled(object sender, EventArgs e)
        {
            // Making two layers, one is a geojson polygon, other is a set of pikachu markers/images

            // Converted using the JSON to C# anonymous type converter
            // source: https://docs.mapbox.com/mapbox-gl-js/example/geojson-polygon/
            // converter: https://jsfiddle.net/aliashrafx/c7pxomjb/39/

            //Map.Invoke.AddLayer(pikachuLayer);
        }

        private async Task AddPoint()
        {
            string guid = Guid.NewGuid().ToString();
            Random random = new Random();

            var Longitude = -180 + (random.NextDouble() * 360);
            var Latitude = -90 + (random.NextDouble() * 180);

            var loc = new GeoLocation(Latitude, Longitude);

            await Map.AddPoint(new MapboxPoint() 
            {
                GUID = guid,
                Latitude = Latitude,
                Longitude = Longitude,
                Properties = 
                new 
                {
                    someProp1 = false,
                    someProp2 = 123,
                    someProp3 = "str",
                },
            });
            Markers.Add(loc, guid);
        }



        private async void AddMarker_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < 100; ++i)
            {
               await AddPoint();
            }
        }

        private void RemoveMarker_Click(object sender, RoutedEventArgs e)
        {
            Map.FlyTo(new GeoLocation(20, 40), 10);
        }

        private void Map_PointClicked(object sender, string e)
        {
            MessageBox.Show("Clicked point with guid: " + e);
        }
    }
}
