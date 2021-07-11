let map = null;
let ping = (args) => { console.log(args); };

let points = {
    "type": "FeatureCollection",
    features: [
    ],
};


(async () => {

    if (typeof CefSharp !== "undefined") {
        await CefSharp.BindObjectAsync("relay");


        ping = function (data) {
            relay.notify(JSON.stringify(data)).then(function (res) {

            });
        }
    }

})();

mapboxgl.accessToken = "MAPBOX_ACCESS_TOKEN";

map = new mapboxgl.Map({
    container: 'map',
    style: "MAPBOX_STYLE",
    center: [0, 0],
    zoom: 1,
    maxBounds: null,
});

map.on('load', function () {
    ping({
        "type": "load",
    });

    // Add a new source from our GeoJSON data and
    // set the 'cluster' option to true. GL-JS will
    // add the point_count property to your source data.
    map.addSource('points', {
        type: 'geojson',
        data: points,
        cluster: true,
        clusterMaxZoom: 14, // Max zoom to cluster points on
        clusterRadius: 35 // Radius of each cluster when clustering points (defaults to 50)
    });

    // Add clusters
    map.addLayer({
        id: 'clusters',
        type: 'circle',
        source: 'points',
        filter: ['has', 'point_count'],
        paint: {
            'circle-color': '#51bbd6',
            'circle-radius': 20
        }
    });

    // Add cluster point count
    map.addLayer({
        id: 'cluster-count',
        type: 'symbol',
        source: 'points',
        filter: ['has', 'point_count'],
        layout: {
            'text-field': '{point_count_abbreviated}',
            'text-font': ['DIN Offc Pro Medium', 'Arial Unicode MS Bold'],
            'text-size': 14
        }
    });

    // Add unclustered points
    map.addLayer({
        id: 'unclustered-point',
        type: 'circle',
        source: 'points',
        filter: ['!', ['has', 'point_count']],
        paint: {
            'circle-color': '#11b4da',
            'circle-radius': 5,
            'circle-stroke-width': 2,
            'circle-stroke-color': '#fff'
        }
    });

    // Clicking a cluster will zoom into it
    map.on('click', 'clusters', function (e) {
        let features = map.queryRenderedFeatures(e.point, {
            layers: ['clusters']
        });

        let clusterId = features[0].properties.cluster_id;

        map.getSource('points').getClusterExpansionZoom(
            clusterId,
            function (err, zoom) {
                if (err) return;

                map.easeTo({
                    center: features[0].geometry.coordinates,
                    zoom: zoom
                });
            }
        );
        
        ping({
            "type": "pointClusterClicked",
        });
    });

    let currentCenter = null;
    let currentZoom = null;
    let currentPitch = null;
    let currentBearing = null;

    map.on("render", function () {
        let newCenter = map.getCenter();

        if (currentCenter === null || currentCenter.lat != newCenter.lat || currentCenter.lng != newCenter.lng) {
            currentCenter = newCenter;
            ping({
                "type": "move",
                "center": newCenter,
            });
        }

        let newZoom = map.getZoom();

        if (currentZoom === null || currentZoom != newZoom) {
            currentZoom = newZoom;
            ping({
                "type": "zoom",
                "zoom": newZoom,
            });
        }

        let newPitch = map.getPitch();

        if (currentPitch === null || currentPitch != newPitch) {
            currentPitch = newPitch;
            ping({
                "type": "pitch",
                "pitch": newPitch,
            });
        }

        let newBearing = map.getBearing();

        if (currentBearing === null || currentBearing != newBearing) {
            currentBearing = newBearing;
            ping({
                "type": "bearing",
                "bearing": newBearing,
            });
        }
    });

    map.on("mousedown", function () {
        ping({
            "type": "mouseDown",
        });
    });

    map.on("mousemove", function () {
        ping({
            "type": "mouseMove",
        });
    });

    map.on("mouseup", function () {
        ping({
            "type": "mouseUp",
        });
    });

    map.on("mouseenter", "", function () {
        ping({
            "type": "mouseEnter",
        });
    });

    map.on("mouseleave", function () {
        ping({
            "type": "mouseLeave",
        });
    });

    map.on("dblclick", function () {
        ping({
            "type": "doubleClick",
        });
    });

    map.on('mouseenter', 'clusters', function () {
        map.getCanvas().style.cursor = 'pointer';
    });
    map.on('mouseleave', 'clusters', function () {
        map.getCanvas().style.cursor = '';
    });
    
    map.on('mouseenter', 'unclustered-point', function () {
        map.getCanvas().style.cursor = 'pointer';
    });
    map.on('mouseleave', 'unclustered-point', function () {
        map.getCanvas().style.cursor = '';
    });

    // Handle click on unclustered points
    map.on('click', 'unclustered-point', function (e) {
        console.log("Clicked on unclustered");

        let guid = e.features[0].properties.guid;
        
        ping({
            "type": "pointClicked",
            "guid": guid,
        });
    });

    ping({
        "type": "ready",
        "path": window.location.href,
    });
});

function flyTo(long, lat, zoom) {
    map.flyTo({
        center: [long, lat],
        zoom: zoom,
    });
}

function addPoint(jsonStrData) {
    let data = JSON.parse(jsonStrData);

    /*
        We're getting:
        string GUID
        double Latitude
        double Longitude
        object Properties
     */

    points.features.push({
        "type": "Feature",
        "properties": {
            ...data.Properties,
            "guid": data.GUID,
        },
        "geometry": {
            "type": "Point",
            "coordinates": [
                data.Longitude, 
                data.Latitude, 
                0 
            ],
        }
    });

    map.getSource('points').setData(points);
}

function addPoint(jsonStrData) {
    let data = JSON.parse(jsonStrData);

    for (let i = 0; i < data.length; ++i) {
        points.features.push({
            "type": "Feature",
            "properties": {
                ...data[i].Properties,
                "guid": data[i].GUID,
            },
            "geometry": {
                "type": "Point",
                "coordinates": [
                    data[i].Longitude, 
                    data[i].Latitude, 
                    0 
                ],
            }
        });
    }

    map.getSource('points').setData(points);
}

function removePoint(guid) {
    points.features = points.features.filter(f => f.properties.guid == guid);

    map.getSource('points').setData(points);
}

function removePoints(guidsJsonStr) {
    let data = JSON.parse(guidsJsonStr);

    for (let i = 0; i < data.length; ++i) {
        points.features = points.features.filter(f => data.findIndex(f.properties.guid) == -1);
    }

    map.getSource('points').setData(points);
}

function clearPoints() {
    points.features = [];

    map.getSource('points').setData(points);
}

function exec(expression) {
    var result = eval(expression);

    try {
        return JSON.stringify(result);
    } catch (e) {
        return "null";
    }
}
