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

            for (int i = 0; i < 250; ++i)
            {
                AddMarker();
            }
        }

        private void AddMarker()
        {
            string guid = Guid.NewGuid().ToString();
            Random random = new Random();

            var Latitude = (random.NextDouble() * 179) - 90;
            var Longitude = (random.NextDouble() * 179) - 90;

            var loc = new GeoLocation(Latitude, Longitude);

            Map.AddMarker(loc, guid);
            Markers.Add(loc, guid);
        }



        private void AddMarker_Click(object sender, RoutedEventArgs e)
        {
            AddMarker();
        }

        private void RemoveMarker_Click(object sender, RoutedEventArgs e)
        {
            Map.FlyTo(new GeoLocation(20, 40), 10);
        }

        private void Map_MarkerClicked(object sender, string e)
        {
            MessageBox.Show("Clicked marker with guid: " + e);
        }
    }
}
