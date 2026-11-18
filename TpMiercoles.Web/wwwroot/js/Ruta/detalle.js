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

    // Paradas en orden: [{ lat, lng, texto, orden }, ...]
    var paradas = [];
    try {
        paradas = JSON.parse(mapElement.dataset.paradas || '[]');
    } catch (e) {
        console.error("Error al leer las paradas de la ruta:", e);
    }

    var map = L.map('map').setView([origenLat, origenLng], 13);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; OpenStreetMap contributors'
    }).addTo(map);

    // Marcadores de origen, paradas y destino
    L.marker([origenLat, origenLng]).addTo(map).bindPopup("<b>Origen:</b> " + origenTexto);

    paradas.forEach(function (parada) {
        L.circleMarker([parada.lat, parada.lng], {
            radius: 7,
            color: '#ffffff',
            weight: 2,
            fillColor: '#6c757d',
            fillOpacity: 1
        }).addTo(map).bindPopup("<b>Parada " + parada.orden + ":</b> " + parada.texto);
    });

    L.marker([destinoLat, destinoLng]).addTo(map).bindPopup("<b>Destino:</b> " + destinoTexto);

    // Todos los puntos en orden: sirven para encuadrar el mapa y para la curva aproximada
    var puntos = [[origenLat, origenLng]]
        .concat(paradas.map(function (p) { return [p.lat, p.lng]; }))
        .concat([[destinoLat, destinoLng]]);

    var hayRecorridoReal = false;

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
            hayRecorridoReal = true;
        } catch (e) {
            console.error("Error al parsear la geometría de la ruta:", e);
        }
    }

    // Sin recorrido real (sin key de ORS o con la cuota agotada): curva punteada aproximada
    if (!hayRecorridoReal) {
        CurvasAproximadas.dibujar(map, puntos, { color: '#0d6efd' });
        map.fitBounds(puntos, { padding: [50, 50] });

        var aviso = document.getElementById('mapaAviso');
        if (aviso) {
            aviso.textContent = 'Recorrido aproximado (línea punteada): el servicio de rutas no está disponible en este momento.';
            aviso.hidden = false;
        }
    }
});