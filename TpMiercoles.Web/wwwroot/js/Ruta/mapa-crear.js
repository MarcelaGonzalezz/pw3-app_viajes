/**
 * Vista previa en el mapa de "Nueva ruta".
 * - Marca el origen, las paradas y el destino a medida que se eligen de la lista de direcciones,
 *   con los mismos pines que el mapa del detalle del viaje.
 * - Dibuja el recorrido cuando recorrido.js termina de calcular el total: no hace otra consulta,
 *   usa la misma respuesta de /api/recorrido (evento "recorrido:calculado").
 *   Con OpenRouteService es el camino real; con el mock (sin geometría) es una curva punteada aproximada.
 */
const contenedor = document.getElementById('mapaRuta');

if (contenedor && window.L) {
    // Centro inicial: CABA, hasta que se elija alguna dirección.
    const CENTRO_INICIAL = [-34.6037, -58.3816];
    const ZOOM_INICIAL = 11;
    const ZOOM_UN_PUNTO = 15;
    const COLOR_RECORRIDO = '#0d6efd';

    const mapa = L.map(contenedor).setView(CENTRO_INICIAL, ZOOM_INICIAL);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; OpenStreetMap contributors'
    }).addTo(mapa);

    // Dos capas: la del recorrido va abajo y la de los pines arriba.
    // En cada cambio se vacían y se vuelven a dibujar.
    const capaRecorrido = L.layerGroup().addTo(mapa);
    const capaMarcadores = L.layerGroup().addTo(mapa);

    /* ------------------------------ Pines ------------------------------ */

    // Mismos colores y forma que en el mapa del detalle del viaje (mapa-viaje.js).
    const ESTILO_PINES = {
        Origen: '#0a2a66',
        Destino: '#212529',
        Parada: '#6c757d'
    };

    // Pin en SVG: gota de color con un círculo blanco y una letra o número adentro.
    // La letra la arma este mismo script (O, D o el número de parada): no viene del usuario.
    const crearPin = (tipo, letra) => {
        const color = ESTILO_PINES[tipo];
        return L.divIcon({
            className: 'ruta-pin',
            html: `
                <svg xmlns="http://www.w3.org/2000/svg" width="30" height="42" viewBox="0 0 30 42">
                    <path d="M15 1C7.3 1 1 7.2 1 14.9 1 25.3 15 41 15 41s14-15.7 14-26.1C29 7.2 22.7 1 15 1z"
                          fill="${color}" stroke="#ffffff" stroke-width="2"/>
                    <circle cx="15" cy="15" r="8.5" fill="#ffffff"/>
                    <text x="15" y="19.5" text-anchor="middle" font-size="12" font-weight="700"
                          font-family="Arial, sans-serif" fill="${color}">${letra}</text>
                </svg>`,
            iconSize: [30, 42],
            iconAnchor: [15, 42],     // la punta del pin es la que marca la ubicación
            tooltipAnchor: [0, -38]   // el globito aparece arriba del pin
        });
    };

    // textContent en lugar de HTML: la dirección viene de un servicio externo (Nominatim).
    const globito = (titulo, texto) => {
        const div = document.createElement('div');
        const negrita = document.createElement('b');
        negrita.textContent = titulo;
        div.append(negrita, document.createElement('br'), texto);
        return div;
    };

    const agregarPin = (punto, tipo, letra, titulo) => {
        L.marker([punto.lat, punto.lng], { icon: crearPin(tipo, letra), riseOnHover: true })
            .bindTooltip(globito(titulo, punto.texto), { direction: 'top' })
            .addTo(capaMarcadores);
    };

    /* ------------------------ Lectura del formulario ------------------------ */

    const leerDireccion = campo => ({
        texto: campo.querySelector('[data-campo="texto"]').value,
        lat: Number(campo.querySelector('[data-campo="lat"]').value),
        lng: Number(campo.querySelector('[data-campo="lng"]').value)
    });

    // Sin coordenadas (0 o vacías) significa que todavía no se eligió una dirección de la lista.
    const estaElegida = d => d.lat !== 0 && d.lng !== 0;

    const campoPorNombre = nombre =>
        document.querySelector(`[name="${nombre}"]`).closest('[data-direccion]');

    const campoOrigen = campoPorNombre('DireccionOrigen.Latitud');
    const campoDestino = campoPorNombre('DireccionDestino.Latitud');
    const listaParadas = document.getElementById('listaParadas');

    // Los puntos elegidos, en el orden del recorrido: origen, paradas y destino.
    let puntosActuales = [];

    // Puntos para los que está dibujado el recorrido. Si cambian, la línea ya no corresponde.
    let puntosDelRecorrido = null;

    /* --------------------------- Marcadores --------------------------- */

    const actualizarMarcadores = () => {
        capaMarcadores.clearLayers();
        const puntos = [];

        const origen = leerDireccion(campoOrigen);
        if (estaElegida(origen)) {
            agregarPin(origen, 'Origen', 'O', 'Origen');
            puntos.push(origen);
        }

        // Las paradas se numeran en el orden en que están en la lista (se pueden reordenar).
        listaParadas?.querySelectorAll('[data-parada]').forEach((item, indice) => {
            const parada = leerDireccion(item.querySelector('[data-direccion]'));
            if (!estaElegida(parada)) return;

            agregarPin(parada, 'Parada', String(indice + 1), `Parada ${indice + 1}`);
            puntos.push(parada);
        });

        const destino = leerDireccion(campoDestino);
        if (estaElegida(destino)) {
            agregarPin(destino, 'Destino', 'D', 'Destino');
            puntos.push(destino);
        }

        puntosActuales = puntos.map(p => [p.lat, p.lng]);

        // Si cambió algún punto, la línea dibujada ya no corresponde:
        // se borra hasta que recorrido.js calcule el recorrido nuevo.
        if (JSON.stringify(puntosActuales) !== puntosDelRecorrido) {
            capaRecorrido.clearLayers();
            puntosDelRecorrido = null;
        }

        // Encuadre: un solo punto se centra; varios se encuadran todos juntos.
        if (puntosActuales.length === 1) {
            mapa.setView(puntosActuales[0], ZOOM_UN_PUNTO);
        } else if (puntosActuales.length > 1) {
            mapa.fitBounds(puntosActuales, { padding: [40, 40] });
        }
    };

    /* --------------------------- Recorrido --------------------------- */

    const dibujarRecorrido = geometriaJson => {
        capaRecorrido.clearLayers();
        puntosDelRecorrido = JSON.stringify(puntosActuales);

        if (geometriaJson) {
            try {
                // OpenRouteService: el camino real por las calles.
                const linea = L.geoJSON(JSON.parse(geometriaJson), {
                    style: { color: COLOR_RECORRIDO, weight: 5, opacity: 0.8 }
                }).addTo(capaRecorrido);

                mapa.fitBounds(linea.getBounds(), { padding: [40, 40] });
                return;
            } catch (error) {
                console.error('No se pudo leer la geometría del recorrido:', error);
            }
        }

        // Mock (o geometría inválida): curva punteada aproximada, igual que en el detalle de la ruta.
        if (window.CurvasAproximadas) {
            const curva = CurvasAproximadas.dibujar(mapa, puntosActuales, { color: COLOR_RECORRIDO });
            curva?.addTo(capaRecorrido);
        }
    };

    /* ------------------------ Cuándo se actualiza ------------------------ */

    // Igual que recorrido.js: direccion.js cambia la clase del campo al elegir o editar una dirección,
    // y paradas.js agrega, quita o reordena filas dentro de #listaParadas.
    const opciones = { subtree: true, childList: true, attributes: true, attributeFilter: ['class'] };
    const observador = new MutationObserver(actualizarMarcadores);

    observador.observe(campoOrigen, opciones);
    observador.observe(campoDestino, opciones);
    if (listaParadas) observador.observe(listaParadas, opciones);

    // recorrido.js avisa cuando terminó de calcular el total con /api/recorrido.
    document.addEventListener('recorrido:calculado', e => dibujarRecorrido(e.detail.geometria));

    // Si la vista se recargó con datos (por ejemplo, tras un error de validación), los marca de entrada.
    actualizarMarcadores();
}