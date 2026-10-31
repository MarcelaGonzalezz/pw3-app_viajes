/**
 * Gestión de paradas dinámicas (agregar, quitar, subir, bajar).
 *
 * Módulo ES6: se carga con <script type="module"> DESPUÉS (o junto) a direccion.js.
 * Si la vista no tiene #listaParadas, no hace nada.
 */
import { iniciarCampoDireccion } from './direccion.js';

const listaParadas = document.getElementById('listaParadas');

if (listaParadas) {
    const plantilla = document.getElementById('plantillaParada');
    const avisoVacio = document.getElementById('paradasVacio');
    const btnAgregar = document.getElementById('btnAgregarParada');

    // El binding de listas de ASP.NET exige índices consecutivos (0, 1, 2...).
    // Tras agregar, quitar o mover, se reescriben los name y el Orden (1..N).
    const renumerarParadas = () => {
        const items = listaParadas.querySelectorAll('[data-parada]');

        items.forEach((item, i) => {
            item.querySelectorAll('[name]').forEach(el => {
                el.name = el.name.replace(/Paradas\[[^\]]*\]/, `Paradas[${i}]`);
            });
            item.querySelector('[data-numero]').textContent = i + 1;
            item.querySelector('[data-orden]').value = i + 1;
        });

        if (avisoVacio) avisoVacio.hidden = items.length > 0;
    };

    const acciones = {
        subir: item => item.previousElementSibling?.before(item),
        bajar: item => item.nextElementSibling?.after(item),
        quitar: item => item.remove()
    };

    btnAgregar.addEventListener('click', () => {
        const nueva = plantilla.content.firstElementChild.cloneNode(true);
        listaParadas.append(nueva);

        iniciarCampoDireccion(nueva.querySelector('[data-direccion]'));
        renumerarParadas();
        nueva.querySelector('[data-buscador]').focus();
    });

    listaParadas.addEventListener('click', ({ target }) => {
        const boton = target.closest('[data-accion]');
        if (!boton) return;

        const item = boton.closest('[data-parada]');
        acciones[boton.dataset.accion]?.(item);
        renumerarParadas();
    });

    renumerarParadas();
}
