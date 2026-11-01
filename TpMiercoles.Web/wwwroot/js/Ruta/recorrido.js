/**
 * Calcula automáticamente el total del recorrido (distancia y duración)
 * cada vez que cambian el origen, el destino o las paradas.
 * Usa el endpoint /api/recorrido.
 */
const panel = document.getElementById('recorridoTotal');
const contenido = document.getElementById('recorridoContenido');

if (panel && contenido) {
    const ESPERA_MS = 500;
    let temporizador;
    let controlador;
    let ultimaFirma = null;

    /* ------------------------ Lectura del formulario ------------------------ */

    const leerDireccion = campo => ({
        texto: campo.querySelector('[data-campo="texto"]').value,
        latitud: Number(campo.querySelector('[data-campo="lat"]').value),
        longitud: Number(campo.querySelector('[data-campo="lng"]').value)
    });

    // Sin coordenadas (0 o vacías) significa que no se eligió una dirección de la lista.
    const estaCompleta = d => d.texto && d.latitud !== 0 && d.longitud !== 0;

    const campoPorNombre = nombre =>
        document.querySelector(`[name="${nombre}"]`).closest('[data-direccion]');

    const leerRecorrido = () => ({
        origen: leerDireccion(campoPorNombre('DireccionOrigen.Latitud')),
        destino: leerDireccion(campoPorNombre('DireccionDestino.Latitud')),
        paradas: [...document.querySelectorAll('#listaParadas [data-parada]')].map(item => ({
            orden: Number(item.querySelector('[data-orden]').value),
            direccion: leerDireccion(item.querySelector('[data-direccion]'))
        }))
    });

    /* ------------------------------ Panel ------------------------------ */

    const formatearDuracion = minutos => {
        if (minutos < 60) return `${minutos} min`;
        return `${Math.floor(minutos / 60)} h ${minutos % 60} min`;
    };

    const formatearKm = km =>
        km.toLocaleString('es-AR', { minimumFractionDigits: 1, maximumFractionDigits: 1 });

    const mostrarAviso = (texto, esError = false) => {
        const aviso = document.createElement('p');
        aviso.className = 'recorrido-total__aviso';
        aviso.textContent = texto;
        contenido.replaceChildren(aviso);
        panel.classList.toggle('recorrido-total--error', esError);
    };

    const crearDato = (etiqueta, valor) => {
        const dato = document.createElement('div');
        dato.className = 'recorrido-dato';

        const textoEtiqueta = document.createElement('span');
        textoEtiqueta.className = 'recorrido-dato__etiqueta';
        textoEtiqueta.textContent = etiqueta;

        const textoValor = document.createElement('span');
        textoValor.className = 'recorrido-dato__valor';
        textoValor.textContent = valor;

        dato.append(textoEtiqueta, textoValor);
        return dato;
    };

    const mostrarResumen = (km, minutos) => {
        panel.classList.remove('recorrido-total--error');
        contenido.replaceChildren(
            crearDato('Distancia total', `${formatearKm(km)} km`),
            crearDato('Duración', formatearDuracion(minutos))
        );
    };

    /* ------------------------------ Cálculo ------------------------------ */

    const reiniciar = aviso => {
        controlador?.abort();
        ultimaFirma = null;
        mostrarAviso(aviso);
    };

    const actualizar = async () => {
        const recorrido = leerRecorrido();

        if (!estaCompleta(recorrido.origen) || !estaCompleta(recorrido.destino)) {
            reiniciar('Elegí el origen y el destino para ver el total del recorrido.');
            return;
        }

        if (recorrido.paradas.some(p => !estaCompleta(p.direccion))) {
            reiniciar('Completá las paradas (o quitalas) para ver el total del recorrido.');
            return;
        }

        // Si nada cambió desde el último cálculo, no se vuelve a consultar.
        const firma = JSON.stringify(recorrido);
        if (firma === ultimaFirma) return;
        ultimaFirma = firma;

        controlador?.abort();
        controlador = new AbortController();
        mostrarAviso('Calculando recorrido...');

        try {
            const respuesta = await fetch('/api/recorrido', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(recorrido),
                signal: controlador.signal
            });

            if (respuesta.status === 401) {
                ultimaFirma = null;
                mostrarAviso('Tu sesión expiró. Volvé a iniciar sesión.', true);
                return;
            }

            const datos = await respuesta.json();

            if (!respuesta.ok) {
                ultimaFirma = null;
                mostrarAviso(datos.error ?? 'No se pudo calcular el recorrido.', true);
                return;
            }

            mostrarResumen(datos.distanciaKm, datos.duracionMinutos);
        } catch (error) {
            if (error.name === 'AbortError') return; // el usuario cambió algo: llega un cálculo nuevo
            ultimaFirma = null;
            mostrarAviso('No se pudo conectar con el servidor.', true);
        }
    };

    /* ------------------------ Cuándo se recalcula ------------------------ */

    const programar = () => {
        clearTimeout(temporizador);
        temporizador = setTimeout(actualizar, ESPERA_MS);
    };

    // direccion.js agrega/quita la clase del campo al elegir o editar una dirección,
    // y parada.js agrega, quita o reordena filas dentro de #listaParadas.
    const opciones = { subtree: true, childList: true, attributes: true, attributeFilter: ['class'] };
    const observador = new MutationObserver(programar);

    observador.observe(campoPorNombre('DireccionOrigen.Latitud'), opciones);
    observador.observe(campoPorNombre('DireccionDestino.Latitud'), opciones);

    const listaParadas = document.getElementById('listaParadas');
    if (listaParadas) observador.observe(listaParadas, opciones);

    // Si la vista se recargó con datos (por ejemplo, tras un error de validación), calcula de entrada.
    programar();
}