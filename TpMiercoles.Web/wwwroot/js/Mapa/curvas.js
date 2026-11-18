/**
 * Curvas aproximadas para los mapas cuando no hay recorrido real de ORS
 * (sin API key, o con la cuota agotada y usando el mock).
 * Une los puntos con curvas punteadas, como hacen las apps de transporte antes de elegir un recorrido.
 * No es el camino real: solo muestra qué punto se une con cuál.
 *
 * Uso: CurvasAproximadas.dibujar(mapa, [[lat, lng], [lat, lng], ...], { color: '#0d6efd' });
 */
(function () {
    // Cantidad de tramos rectos con los que se dibuja cada curva: más tramos = curva más suave.
    var SEGMENTOS = 24;

    // Cuánto se separa la curva de la línea recta, en proporción a la distancia entre los puntos.
    var CURVATURA = 0.2;

    // Curva de Bézier cuadrática entre dos puntos [lat, lng].
    function curvaEntre(desde, hasta) {
        var medioLat = (desde[0] + hasta[0]) / 2;
        var medioLng = (desde[1] + hasta[1]) / 2;
        var difLat = hasta[0] - desde[0];
        var difLng = hasta[1] - desde[1];

        // Punto de control: el punto medio corrido hacia un costado (perpendicular a la línea recta).
        var control = [medioLat - difLng * CURVATURA, medioLng + difLat * CURVATURA];

        var puntos = [];
        for (var i = 0; i <= SEGMENTOS; i++) {
            var t = i / SEGMENTOS;
            var u = 1 - t;
            puntos.push([
                u * u * desde[0] + 2 * u * t * control[0] + t * t * hasta[0],
                u * u * desde[1] + 2 * u * t * control[1] + t * t * hasta[1]
            ]);
        }
        return puntos;
    }

    // Dibuja una curva punteada entre cada par de puntos consecutivos.
    // Devuelve la capa dibujada, o null si no hay al menos dos puntos.
    function dibujar(mapa, puntos, opciones) {
        if (!mapa || !puntos || puntos.length < 2) return null;

        var o = opciones || {};
        var estilo = {
            color: o.color || '#0d6efd',
            weight: o.grosor || 4,
            opacity: 0.9,
            dashArray: '8 10',
            lineCap: 'round'
        };

        var capa = L.layerGroup();
        for (var i = 0; i < puntos.length - 1; i++) {
            L.polyline(curvaEntre(puntos[i], puntos[i + 1]), estilo).addTo(capa);
        }
        return capa.addTo(mapa);
    }

    window.CurvasAproximadas = { dibujar: dibujar };
})();