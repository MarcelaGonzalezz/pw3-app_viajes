/**
 * Mapa del detalle del viaje.
 * Pide los datos a /api/viajes/{id}/mapa y dibuja:
 *  - el recorrido habitual en azul (siempre),
 *  - el recorrido sugerido con los paquetes en naranja, más finito y por debajo del azul:
 *    lo que se ve en naranja es por donde conviene desviarse. Se puede ocultar desde el mapa.
 *  - marcadores de origen, destino, paradas, retiros y entregas.
 */
document.addEventListener('DOMContentLoaded', async () => {
    const contenedor = document.getElementById('mapaViaje');
    if (!contenedor || typeof L === 'undefined') return;

    const aviso = document.getElementById('mapaViajeAviso');
    const resumen = document.getElementById('mapaViajeResumen');

    // Relleno + borde oscuro. El sugerido es más finito para que el habitual sea el protagonista.
    const LINEAS = {
        habitual: { relleno: '#0d6efd', borde: '#0a2a66', grosor: 6, grosorBorde: 10 },
        sugerido: { relleno: '#fd7e14', borde: '#8a3c00', grosor: 4, grosorBorde: 7 }
    };


    const mostrarAviso = texto => {
        if (!aviso) return;
        aviso.textContent = texto;
        aviso.hidden = !texto;
    };

    const escapar = texto => {
        const div = document.createElement('div');
        div.textContent = texto ?? '';
        return div.innerHTML;
    };

    const etiqueta = punto => {
        switch (punto.tipo) {
            case 'Origen': return 'Origen';
            case 'Destino': return 'Destino';
            case 'Parada': return `Parada ${punto.orden}`;
            case 'Retiro': return `Retiro · Envío #${punto.envioId}`;
            case 'Entrega': return `Entrega · Envío #${punto.envioId}`;
            default: return punto.tipo;
        }
    };

    const formatearKm = km =>
        km.toLocaleString('es-AR', { minimumFractionDigits: 1, maximumFractionDigits: 1 });

    // Arma la línea como un grupo: primero el borde (más ancho y oscuro) y encima el relleno.
    const crearLinea = (geometriaJson, estilo, pane) => {
        if (!geometriaJson) return null;
        try {
            const geometria = JSON.parse(geometriaJson);
            return L.layerGroup([
                L.geoJSON(geometria, { pane, style: { color: estilo.borde, weight: estilo.grosorBorde, opacity: 1 } }),
                L.geoJSON(geometria, { pane, style: { color: estilo.relleno, weight: estilo.grosor, opacity: 1 } })
            ]);
        } catch (e) {
            console.error('Geometría inválida', e);
            return null;
        }
    };

    /* ----------------------------- Datos ----------------------------- */

    let datos;
    try {
        const respuesta = await fetch(contenedor.dataset.url, { headers: { Accept: 'application/json' } });

        if (respuesta.status === 401) {
            mostrarAviso('Tu sesión expiró. Volvé a iniciar sesión para ver el mapa.');
            return;
        }
        if (!respuesta.ok) {
            mostrarAviso('No se pudo cargar el mapa del viaje.');
            return;
        }

        datos = await respuesta.json();
    } catch {
        mostrarAviso('No se pudo conectar con el servidor para cargar el mapa.');
        return;
    }

    /* ----------------------------- Mapa ----------------------------- */

    const mapa = L.map(contenedor);

    // Mapa base de OpenStreetMap (el mismo que usa el detalle de la ruta).
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; OpenStreetMap contributors'
    }).addTo(mapa);

    // Capas con orden fijo: el sugerido siempre debajo del habitual, aunque se oculte y se vuelva a mostrar.
    mapa.createPane('recorridoSugerido').style.zIndex = 410;
    mapa.createPane('recorridoHabitual').style.zIndex = 420;

    const sugerido = crearLinea(datos.geometriaConEnvios, LINEAS.sugerido, 'recorridoSugerido');
    const habitual = crearLinea(datos.geometriaBase, LINEAS.habitual, 'recorridoHabitual');

    habitual?.addTo(mapa);

    if (sugerido) {
        // Visible de entrada; el conductor lo puede destildar desde el control del mapa.
        sugerido.addTo(mapa);
        L.control.layers(null, { 'Mostrar recorrido sugerido': sugerido }, { collapsed: false }).addTo(mapa);
    }
    // Sin recorrido real (sin key de ORS o con la cuota agotada): curvas punteadas aproximadas.
    const aLatLng = punto => [Number(punto.latitud), Number(punto.longitud)];
    let hayAproximado = false;

    if (!habitual) {
        // Recorrido habitual: origen → paradas → destino.
        const recorridoHabitual = datos.puntos
            .filter(p => p.tipo === 'Origen' || p.tipo === 'Parada' || p.tipo === 'Destino')
            .map(aLatLng);
        if (CurvasAproximadas.dibujar(mapa, recorridoHabitual, { color: LINEAS.habitual.relleno })) {
            hayAproximado = true;
        }
    }

    if (!sugerido) {
        // Un tramo retiro → entrega por cada paquete.
        for (const retiro of datos.puntos.filter(p => p.tipo === 'Retiro')) {
            const entrega = datos.puntos.find(p => p.tipo === 'Entrega' && p.envioId === retiro.envioId);
            if (entrega) {
                CurvasAproximadas.dibujar(mapa, [aLatLng(retiro), aLatLng(entrega)],
                    { color: LINEAS.sugerido.relleno, grosor: 3 });
                hayAproximado = true;
            }
        }
    }
    /* --------------------------- Marcadores --------------------------- */


    // Pin de ubicación en SVG: forma de gota con un círculo blanco y una letra adentro.
    const ESTILO_PINES = {
        Origen: { color: '#0a2a66', letra: () => 'O' },
        Destino: { color: '#212529', letra: () => 'D' },
        Parada: { color: '#6c757d', letra: p => String(p.orden ?? '') },
        Retiro: { color: '#198754', letra: () => 'R' },
        Entrega: { color: '#dc3545', letra: () => 'E' }
    };

    const crearPin = punto => {
        const estilo = ESTILO_PINES[punto.tipo] ?? ESTILO_PINES.Parada;
        const svg = `
            <svg xmlns="http://www.w3.org/2000/svg" width="30" height="42" viewBox="0 0 30 42">
                <path d="M15 1C7.3 1 1 7.2 1 14.9 1 25.3 15 41 15 41s14-15.7 14-26.1C29 7.2 22.7 1 15 1z"
                      fill="${estilo.color}" stroke="#ffffff" stroke-width="2"/>
                <circle cx="15" cy="15" r="8.5" fill="#ffffff"/>
                <text x="15" y="19.5" text-anchor="middle" font-size="12" font-weight="700"
                      font-family="Arial, sans-serif" fill="${estilo.color}">${escapar(estilo.letra(punto))}</text>
            </svg>`;

        return L.divIcon({
            className: 'mv-pin',
            html: svg,
            iconSize: [30, 42],
            iconAnchor: [15, 42],     // la punta del pin es la que marca la ubicación
            tooltipAnchor: [0, -38]   // el globito aparece arriba del pin
        });
    };

    const limites = [];
    for (const punto of datos.puntos) {
        const posicion = [Number(punto.latitud), Number(punto.longitud)];
        limites.push(posicion);

        L.marker(posicion, { icon: crearPin(punto), riseOnHover: true })
            .addTo(mapa)
            .bindTooltip(`<b>${escapar(etiqueta(punto))}</b><br>${escapar(punto.texto)}`, { direction: 'top' });
    }

    if (limites.length > 0) {
        mapa.fitBounds(limites, { padding: [50, 50] });
    }

    /* ----------------------------- Textos ----------------------------- */

    if (hayAproximado) {
        mostrarAviso('Recorrido aproximado (líneas punteadas): el servicio de rutas no está disponible en este momento.');
    }

    if (resumen && datos.distanciaBaseKm != null) {
        const partes = [`Recorrido habitual: ${formatearKm(datos.distanciaBaseKm)} km`];
        if (datos.distanciaConEnviosKm != null) {
            partes.push(`Con paquetes: ${formatearKm(datos.distanciaConEnviosKm)} km`);
            partes.push(`Desvío: +${formatearKm(Math.max(0, datos.desvioKm ?? 0))} km`);
        }
        resumen.textContent = partes.join(' · ') + (datos.distanciasEstimadas ? ' (estimado)' : '');    }
});