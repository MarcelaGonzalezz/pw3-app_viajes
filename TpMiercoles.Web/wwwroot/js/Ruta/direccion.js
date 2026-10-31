/**
 * Campo de dirección con autocompletado (Nominatim / OpenStreetMap).
 *
 * Es un módulo ES6: se carga con <script type="module">.
 * Inicializa automáticamente todos los [data-direccion] de la página
 * y exporta funciones para campos agregados dinámicamente.
 */

const NOMINATIM_URL = 'https://nominatim.openstreetmap.org/search';
const DEBOUNCE_MS = 1000; // la política de Nominatim pide máx. 1 request por segundo
const MIN_CARACTERES = 3;

const CLASE_OK = 'direccion-field--ok';
const CLASE_ERROR = 'direccion-field--error';

/* ------------------------- Búsqueda en Nominatim ------------------------- */

const buscarDirecciones = async (texto, signal) => {
    const params = new URLSearchParams({
        q: texto,
        format: 'jsonv2',
        addressdetails: 1,
        countrycodes: 'ar',
        limit: 5
    });

    const resp = await fetch(`${NOMINATIM_URL}?${params}`, {
        headers: { 'Accept-Language': 'es' },
        signal
    });
    if (!resp.ok) throw new Error('Error al buscar la dirección');

    const resultados = await resp.json();
    return resultados.map(({ place_id, display_name, lat, lon }) => ({
        id: place_id,
        texto: display_name,
        latitud: lat,   // string con punto decimal
        longitud: lon
    }));
};

/* ------------------------ Helpers del campo ------------------------ */

const referencias = cont => ({
    buscador: cont.querySelector('[data-buscador]'),
    lista: cont.querySelector('[data-lista]'),
    texto: cont.querySelector('[data-campo="texto"]'),
    lat: cont.querySelector('[data-campo="lat"]'),
    lng: cont.querySelector('[data-campo="lng"]'),
    error: cont.querySelector('[data-error]')
});

const tieneSeleccion = ({ lat, lng }) => lat.value !== '' && lng.value !== '';

const limpiarSeleccion = (cont, { texto, lat, lng }) => {
    texto.value = '';
    lat.value = '';
    lng.value = '';
    cont.classList.remove(CLASE_OK);
};

const mostrarMensaje = ({ lista }, mensaje) => {
    const li = document.createElement('li');
    li.className = 'sin-accion';
    li.textContent = mensaje;
    lista.replaceChildren(li);
};

const seleccionar = (cont, refs, resultado) => {
    const { buscador, texto, lat, lng, lista, error } = refs;

    buscador.value = resultado.texto;
    texto.value = resultado.texto;
    lat.value = resultado.latitud;
    lng.value = resultado.longitud;
    lista.replaceChildren();
    error.textContent = '';
    cont.classList.remove(CLASE_ERROR);
    cont.classList.add(CLASE_OK);
};

const mostrarResultados = (cont, refs, resultados) => {
    if (resultados.length === 0) {
        mostrarMensaje(refs, 'Sin resultados');
        return;
    }

    const items = resultados.map(resultado => {
        const li = document.createElement('li');
        li.textContent = resultado.texto; // textContent evita inyección de HTML
        li.tabIndex = 0;
        li.addEventListener('click', () => seleccionar(cont, refs, resultado));
        li.addEventListener('keydown', e => {
            if (e.key === 'Enter') seleccionar(cont, refs, resultado);
        });
        return li;
    });

    refs.lista.replaceChildren(...items);
};

/* ------------------------ Inicialización ------------------------ */

export const iniciarCampoDireccion = cont => {
    if (!cont || cont.dataset.iniciado) return;
    cont.dataset.iniciado = '1';

    const refs = referencias(cont);
    const { buscador, lista } = refs;
    let temporizador;
    let controlador;

    // Si la vista se recargó con una dirección ya elegida, se marca como válida
    if (tieneSeleccion(refs)) {
        cont.classList.remove(CLASE_ERROR);
        cont.classList.add(CLASE_OK);
    }

    // Enter no debe enviar el formulario
    buscador.addEventListener('keydown', e => {
        if (e.key === 'Enter') e.preventDefault();
    });

    buscador.addEventListener('input', () => {
        const termino = buscador.value.trim();

        // Si edita el texto, la selección anterior deja de ser válida
        limpiarSeleccion(cont, refs);
        clearTimeout(temporizador);
        controlador?.abort(); // cancela la búsqueda anterior en curso

        if (termino.length < MIN_CARACTERES) {
            lista.replaceChildren();
            return;
        }

        temporizador = setTimeout(async () => {
            controlador = new AbortController();

            try {
                const resultados = await buscarDirecciones(termino, controlador.signal);
                mostrarResultados(cont, refs, resultados);
            } catch (error) {
                if (error.name === 'AbortError') return; // el usuario siguió escribiendo
                mostrarMensaje(refs, 'No se pudo buscar. Intentá de nuevo.');
            }
        }, DEBOUNCE_MS);
    });
};

export const iniciarTodosLosCampos = (raiz = document) => {
    raiz.querySelectorAll('[data-direccion]').forEach(iniciarCampoDireccion);
};

/* ------------------------ Listeners globales ------------------------ */

// Cierra las listas al hacer clic fuera del campo
document.addEventListener('click', ({ target }) => {
    document.querySelectorAll('[data-direccion]').forEach(cont => {
        if (!cont.contains(target)) {
            cont.querySelector('[data-lista]')?.replaceChildren();
        }
    });
});

// Validación al enviar: todas las direcciones deben venir de la lista
document.getElementById('formRuta')?.addEventListener('submit', e => {
    let primero = null;

    document.querySelectorAll('[data-direccion]').forEach(cont => {
        const refs = referencias(cont);
        if (tieneSeleccion(refs)) return;

        cont.classList.add(CLASE_ERROR);
        refs.error.textContent = 'Elegí una dirección de la lista.';
        primero ??= cont;
    });

    if (primero) {
        e.preventDefault();
        primero.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
});

/* ------------------------ Arranque ------------------------ */

iniciarTodosLosCampos();
