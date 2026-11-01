document.addEventListener("DOMContentLoaded", function () {
    var mapElement = document.getElementById('map');
    if (!mapElement) return;

    var origenLat = parseFloat(mapElement.dataset.origenLat);
    var origenLng = parseFloat(mapElement.dataset.origenLng);
    var origenTexto = mapElement.dataset.origenTexto;

    var destinoLat = parseFloat(mapElement.dataset.destinoLat);
    var destinoLng = parseFloat(mapElement.dataset.destinoLng);
    var destinoTexto = mapElement.dataset.destinoTexto;
    var geometriaJson = mapElement.dataset.geometria;

    var map = L.map('map').setView([origenLat, origenLng], 13);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; OpenStreetMap contributors'
    }).addTo(map);

    // Marcadores de origen y destino
    L.marker([origenLat, origenLng]).addTo(map).bindPopup("<b>Origen:</b> " + origenTexto);
    L.marker([destinoLat, destinoLng]).addTo(map).bindPopup("<b>Destino:</b> " + destinoTexto);

    // Renderizar el recorrido real utilizando la geometría GeoJSON de ORS
    if (geometriaJson) {
        try {
            var geojsonObj = JSON.parse(geometriaJson);
            var routeLayer = L.geoJSON(geojsonObj, {
                style: {
                    color: '#0d6efd', // Color azul Bootstrap
                    weight: 5,
                    opacity: 0.8
                }
            }).addTo(map);

            // Ajustar el zoom automáticamente para que encuadre todo el recorrido
            map.fitBounds(routeLayer.getBounds(), { padding: [50, 50] });
        } catch (e) {
            console.error("Error al parsear la geometría de la ruta:", e);
        }
    }
});