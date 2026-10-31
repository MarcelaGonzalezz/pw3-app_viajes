/* =====================================================================
   Pantalla "Mis rutas"
   - Filtro por estado en el navegador (Activo por defecto)
   - Confirmación antes de desactivar una ruta
   ===================================================================== */
(() => {
    'use strict';

    const radios = document.querySelectorAll('input[name="filtroEstado"]');
    const tarjetas = document.querySelectorAll('.ruta-card');
    const vacio = document.getElementById('rutasVacio');
    const textoVacio = document.getElementById('rutasVacioTexto');

    const MENSAJES_VACIO = {
        Activo: 'No tenés rutas activas.',
        Inactivo: 'No tenés rutas inactivas.',
        Todas: 'Todavía no registraste ninguna ruta.'
    };

    function aplicarFiltro(filtro) {
        let visibles = 0;

        tarjetas.forEach(tarjeta => {
            const mostrar = filtro === 'Todas' || tarjeta.dataset.estado === filtro;
            tarjeta.hidden = !mostrar;
            if (mostrar) visibles++;
        });

        vacio.hidden = visibles > 0;
        textoVacio.textContent = MENSAJES_VACIO[filtro] ?? '';
    }

    radios.forEach(radio => {
        radio.addEventListener('change', () => aplicarFiltro(radio.value));
    });

    // Estado inicial: el radio marcado (Activo). Cubre también el caso en que el
    // navegador restaure otro radio al volver con el botón "atrás".
    const marcado = document.querySelector('input[name="filtroEstado"]:checked');
    aplicarFiltro(marcado ? marcado.value : 'Activo');

    // Confirmación al desactivar
    document.querySelectorAll('form[data-confirmar]').forEach(form => {
        form.addEventListener('submit', e => {
            if (!confirm(form.dataset.confirmar)) e.preventDefault();
        });
    });
})();
