/* =====================================================================
   Formulario "Nuevo viaje"
   - Buscador de direcciones con Nominatim (origen, destino y paradas)
   - Alta / baja / reordenamiento de paradas con renumeración de índices
   ===================================================================== */
(() => {
    'use strict';

    const NOMINATIM_URL = 'https://nominatim.openstreetmap.org/search';
    const DEBOUNCE_MS = 1000;   // la política de Nominatim pide máx. 1 request por segundo
    const MIN_CARACTERES = 3;

    /* ------------------------- Búsqueda en Nominatim ------------------------- */

    async function buscarDirecciones(texto) {
        const params = new URLSearchParams({
            q: texto,
            format: 'jsonv2',
            addressdetails: 1,
            countrycodes: 'ar',
            limit: 5
        });

        const resp = await fetch(`${NOMINATIM_URL}?${params}`, {
            headers: { 'Accept-Language': 'es' }
        });
        if (!resp.ok) throw new Error('Error al buscar la dirección');

        const resultados = await resp.json();
        return resultados.map(item => ({
            id: item.place_id,
            texto: item.display_name,
            latitud: item.lat,     // string con punto decimal
            longitud: item.lon
        }));
    }

    /* ------------------------ Campo de dirección ------------------------ */

    function referencias(cont) {
        return {
            buscador: cont.querySelector('[data-buscador]'),
            lista: cont.querySelector('[data-lista]'),
            texto: cont.querySelector('[data-campo="texto"]'),
            lat: cont.querySelector('[data-campo="lat"]'),
            lng: cont.querySelector('[data-campo="lng"]'),
            error: cont.querySelector('[data-error]')
        };
    }

    function tieneSeleccion(c) {
        return c.lat.value !== '' && c.lng.value !== '';
    }

    function limpiarSeleccion(cont, c) {
        c.texto.value = '';
        c.lat.value = '';
        c.lng.value = '';
        cont.classList.remove('direccion-field--ok');
    }

    function mostrarMensaje(c, mensaje) {
        c.lista.innerHTML = '';
        const li = document.createElement('li');
        li.className = 'sin-accion';
        li.textContent = mensaje;
        c.lista.appendChild(li);
    }

    function mostrarResultados(cont, c, resultados) {
        c.lista.innerHTML = '';

        if (resultados.length === 0) {
            mostrarMensaje(c, 'Sin resultados');
            return;
        }

        resultados.forEach(r => {
            const li = document.createElement('li');
            li.textContent = r.texto;               // textContent evita inyección de HTML
            li.tabIndex = 0;
            li.addEventListener('click', () => seleccionar(cont, c, r));
            li.addEventListener('keydown', e => {
                if (e.key === 'Enter') seleccionar(cont, c, r);
            });
            c.lista.appendChild(li);
        });
    }

    function seleccionar(cont, c, r) {
        c.buscador.value = r.texto;
        c.texto.value = r.texto;
        c.lat.value = r.latitud;
        c.lng.value = r.longitud;
        c.lista.innerHTML = '';
        c.error.textContent = '';
        cont.classList.remove('direccion-field--error');
        cont.classList.add('direccion-field--ok');
    }

    function iniciarCampoDireccion(cont) {
        if (cont.dataset.iniciado) return;
        cont.dataset.iniciado = '1';

        const c = referencias(cont);
        let temporizador = null;
        let ultimoTermino = '';

        // Si se recargó la vista con una dirección ya elegida, se marca como válida
        if (tieneSeleccion(c)) cont.classList.add('direccion-field--ok');

        // Enter no debe enviar el formulario
        c.buscador.addEventListener('keydown', e => {
            if (e.key === 'Enter') e.preventDefault();
        });

        c.buscador.addEventListener('input', () => {
            const termino = c.buscador.value.trim();

            // Si edita el texto, la selección anterior deja de ser válida
            limpiarSeleccion(cont, c);
            clearTimeout(temporizador);

            if (termino.length < MIN_CARACTERES) {
                c.lista.innerHTML = '';
                ultimoTermino = '';
                return;
            }

            temporizador = setTimeout(async () => {
                if (termino === ultimoTermino) return;
                ultimoTermino = termino;

                try {
                    const resultados = await buscarDirecciones(termino);
                    // Descarta respuestas viejas si el usuario siguió escribiendo
                    if (c.buscador.value.trim() !== termino) return;
                    mostrarResultados(cont, c, resultados);
                } catch {
                    mostrarMensaje(c, 'No se pudo buscar. Intentá de nuevo.');
                }
            }, DEBOUNCE_MS);
        });
    }

    function iniciarTodosLosCampos(raiz) {
        raiz.querySelectorAll('[data-direccion]').forEach(iniciarCampoDireccion);
    }

    // Cierra las listas al hacer clic fuera
    document.addEventListener('click', e => {
        document.querySelectorAll('[data-direccion]').forEach(cont => {
            if (!cont.contains(e.target)) {
                const lista = cont.querySelector('[data-lista]');
                if (lista) lista.innerHTML = '';
            }
        });
    });

    /* ------------------------------ Paradas ------------------------------ */

    const listaParadas = document.getElementById('listaParadas');
    const plantilla = document.getElementById('plantillaParada');
    const avisoVacio = document.getElementById('paradasVacio');
    const btnAgregar = document.getElementById('btnAgregarParada');

    // El binding de listas de ASP.NET exige índices consecutivos (0, 1, 2...).
    // Después de agregar, quitar o mover, se reescriben los nombres y el Orden (1..N).
    function renumerarParadas() {
        const items = listaParadas.querySelectorAll('[data-parada]');

        items.forEach((item, i) => {
            item.querySelectorAll('[name]').forEach(el => {
                el.name = el.name.replace(/Paradas\[[^\]]*\]/, `Paradas[${i}]`);
            });
            item.querySelector('[data-numero]').textContent = i + 1;
            item.querySelector('[data-orden]').value = i + 1;
        });

        avisoVacio.hidden = items.length > 0;
    }

    btnAgregar.addEventListener('click', () => {
        const fragmento = plantilla.content.cloneNode(true);
        listaParadas.appendChild(fragmento);

        const nueva = listaParadas.lastElementChild;
        iniciarCampoDireccion(nueva.querySelector('[data-direccion]'));
        renumerarParadas();
        nueva.querySelector('[data-buscador]').focus();
    });

    listaParadas.addEventListener('click', e => {
        const boton = e.target.closest('[data-accion]');
        if (!boton) return;

        const item = boton.closest('[data-parada]');
        switch (boton.dataset.accion) {
            case 'subir':
                item.previousElementSibling?.before(item);
                break;
            case 'bajar':
                item.nextElementSibling?.after(item);
                break;
            case 'quitar':
                item.remove();
                break;
        }
        renumerarParadas();
    });

    /* ------------------------- Validación al enviar ------------------------- */

    document.getElementById('formRuta').addEventListener('submit', e => {
        let primero = null;

        document.querySelectorAll('[data-direccion]').forEach(cont => {
            const c = referencias(cont);
            if (!tieneSeleccion(c)) {
                cont.classList.add('direccion-field--error');
                c.error.textContent = 'Elegí una dirección de la lista (o quitá la parada).';
                primero ??= cont;
            }
        });

        if (primero) {
            e.preventDefault();
            primero.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
    });

    /* ------------------------------ Arranque ------------------------------ */

    iniciarTodosLosCampos(document);
    renumerarParadas();
})();
