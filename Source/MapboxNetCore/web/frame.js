<!DOCTYPE html>
<html>

<head>
    <meta charset='utf-8' />
    <title>Display a map</title>
    <meta name='viewport' content='initial-scale=1,maximum-scale=1,user-scalable=no' />
	<script>
		{{mapbox-gl.js}}
	</script>
	<style>
		{{mapbox-gl.css}}
	</style>
    <style>
        body {
            margin: 0;
            padding: 0;
        }
        
        #map {
            position: absolute;
            top: 0;
            bottom: 0;
            width: 100%;
        }

		.no-attrib .mapboxgl-ctrl-attrib {
			display:none !important;
		}
		
      .mapMarker {
        width: 50px;
        height: 50px;
        border-radius: 50%;
        background-image: url('https://docs.mapbox.com/help/demos/custom-markers-gl-js/mapbox-icon.png');
        background-size: cover;
        cursor: pointer;
      }

		.no-attrib .mapboxgl-ctrl-logo {
			display:none !important;
		}
    </style>
</head>

<body>

    <div id='map'></div>
    <script>

        let markers = [];
		var map = null;

		(async () =>
		{
			await CefSharp.BindObjectAsync("relay");


			var ping = function(data) {

				//parentMap.notify(JSON.stringify(data));
	
				relay.notify(JSON.stringify(data)).then(function (res)
				{
				
				});
			}

			mapboxgl.accessToken = {{accessToken}};
			map = new mapboxgl.Map({
				container: 'map', // container id
				style: {{style}}, // stylesheet location
				center: [0,0], // starting position [lng, lat]
				zoom: 19, // starting zoom
				maxBounds: null,
			});

			map.on('load', function() {
				ping({
					"type": "load",
				});
			});

			ping({
				"type": "ready",
				"path": window.location.href,
			});
		
			var currentCenter = null;
			var currentZoom = null;
			var currentPitch = null;
			var currentBearing = null;

			map.on("render", function() {
				var newCenter = map.getCenter();

				if(currentCenter === null || currentCenter.lat != newCenter.lat || currentCenter.lng != newCenter.lng) {
					currentCenter = newCenter;
					ping({
						"type": "move",
						"center": newCenter,
					});
				}
			
				var newZoom = map.getZoom();

				if(currentZoom === null || currentZoom != newZoom) {
					currentZoom = newZoom;
					ping({
						"type": "zoom",
						"zoom": newZoom,
					});
				}
			
				var newPitch = map.getPitch();

				if(currentPitch === null || currentPitch != newPitch) {
					currentPitch = newPitch;
					ping({
						"type": "pitch",
						"pitch": newPitch,
					});
				}
			
				var newBearing = map.getBearing();

				if(currentBearing === null || currentBearing != newBearing) {
					currentBearing = newBearing;
					ping({
						"type": "bearing",
						"bearing": newBearing,
					});
				}
			});

			map.on("mousedown", function() {
				ping({
					"type": "mouseDown",
				});
			});

			map.on("mousemove", function() {
				ping({
					"type": "mouseMove",
				});
			});

			map.on("mouseup", function() {
				ping({
					"type": "mouseUp",
				});
			});

			map.on("mouseenter", "", function() {
				ping({
					"type": "mouseEnter",
				});
			});

			map.on("mouseleave", function() {
				ping({
					"type": "mouseLeave",
				});
			});
            
            map.on("click", function(e) {
                console.log(e.originalEvent.target);
                if (e.originalEvent.target.className.includes("mapMarker")) {
                    ping({
                        "type": "markerClicked",
                        "guid": e.originalEvent.target.id,
                    });
                }
                else {
                    ping({
                        "type": "click",
                    });
                }
            });

			map.on("click", function() {
			});

			map.on("dblclick", function() {
				ping({
					"type": "doubleClick",
				});
			});

		})();
		

		function exec(expression) {
			var result = eval(expression);

			try {
				return JSON.stringify(result);
			} catch(e) {
				return "null";
			}
		}

		function run(expression) {
			var f = new Function("(function() { " + expression + " })()");
			f();
		}

		function addImage(id, base64) {
			var img = new Image();
			img.onload = function () {
				map.addImage(id, img);
			}
			img.onerror = function (errorMsg, url, lineNumber, column, errorObj) {
				ping({
					"type": "error",
					"info": errorMsg,
				});
			}
			
			img.src = "data:image/png;base64," + base64;
		}
        
        function addMarker(long, lat, guid) {
            var el = document.createElement('div');
            el.className = 'mapMarker';
            el.id = guid;
            
            // make a marker for each feature and add to the map
            let marker = new mapboxgl.Marker(el)
                .setLngLat([long, lat])
                .addTo(map);

            markers.push(marker);
        }

        function removeMarker(guid) {
            let foundMarker;
            let i = -1;
            for (i = 0; i < markers.length; ++i) {
                if (markers[i].getElement().tag == guid) {
                    foundMarker = markers[i];
                }
            }

            if (foundMarker) {
                foundMarker.remove();
                markers.splice(i, 1);
            }
        }

        function clearMarkers() {
            for (let i  = 0; i < markers.length; ++i) {
                markers[i].remove();
            }

            markers = [];
        }

    </script>

</body>

</html>