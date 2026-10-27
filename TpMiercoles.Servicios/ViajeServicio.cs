using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;
using TpMiercoles.Servicios.DTOs;

namespace TpMiercoles.Servicios
{
    public interface IViajeServicio
    {
        // Detalle de un viaje para su duenio. Devuelve null si el viaje no existe
        // o si la ruta que lo genero no pertenece al usuario indicado.
        Task<ViajeDetalleDTO?> ObtenerDetalleAsync(int viajeId, int usuarioId);

        // Viajes del usuario. estado null trae todos.
        Task<List<ViajeListadoDTO>> ListarAsync(int usuarioId, EstadoActividadViaje? estado);

        // Datos del viaje para cargar el formulario de edicion.
        Task<ViajeEdicionDTO?> ObtenerParaEditarAsync(int viajeId, int usuarioId);

        // Actualiza horarios y capacidad. Valida igual que la creacion.
        Task<ResultadoOperacionViaje> ActualizarAsync(int viajeId, int usuarioId, ViajeEdicionDTO datos);

        // Baja logica: deja el viaje en INACTIVO para que no siga buscando oportunidades.
        Task<ResultadoOperacionViaje> DesactivarAsync(int viajeId, int usuarioId);

        // Vuelve a poner el viaje en ACTIVO para que busque oportunidades de nuevo.
        Task<ResultadoOperacionViaje> ActivarAsync(int viajeId, int usuarioId);
    }

    public class ViajeServicio : IViajeServicio
    {
        private readonly IViajeRepositorio _repositorio;

        public ViajeServicio(IViajeRepositorio repositorio)
        {
            _repositorio = repositorio;
        }

        public async Task<ViajeDetalleDTO?> ObtenerDetalleAsync(int viajeId, int usuarioId)
        {
            Viaje? viaje = await _repositorio.ObtenerDetalleAsync(viajeId);

            if (viaje is null)
            {
                return null;
            }

            // El viaje es del usuario si la ruta que lo genero es suya.
            if (viaje.Ruta.UsuarioId != usuarioId)
            {
                return null;
            }

            List<ParadaDTO> paradas = viaje.Ruta.Paradas
                .OrderBy(p => p.Id)
                .Select((parada, indice) => new ParadaDTO
                {
                    Orden = indice + 1,
                    Direccion = parada.Direccion.Texto
                })
                .ToList();

            List<PaqueteAsignadoDTO> paquetes = viaje.Asignaciones
                .OrderBy(a => a.Id)
                .Select(a => new PaqueteAsignadoDTO
                {
                    AsignacionId = a.Id,
                    EnvioId = a.EnvioId,
                    Remitente = $"{a.Envio.Usuario.Nombre} {a.Envio.Usuario.Apellido}".Trim(),
                    DireccionRetiro = a.Envio.DireccionRetiro.Texto,
                    DireccionEntrega = a.Envio.DireccionEntrega.Texto,
                    FechaDesde = a.Envio.FechaDesde,
                    FechaHasta = a.Envio.FechaHasta,
                    Estado = a.Estado,
                    FechaAceptacion = a.FechaAceptacion,
                    FechaRetiro = a.FechaRetiro,
                    FechaEntrega = a.FechaEntrega
                })
                .ToList();

            int cantidadPaquetes = paquetes.Count;

            return new ViajeDetalleDTO
            {
                Id = viaje.Id,
                RutaId = viaje.RutaId,
                Estado = viaje.Estado,
                DireccionOrigen = viaje.DireccionOrigen.Texto,
                DireccionDestino = viaje.DireccionDestino.Texto,
                FechaSalida = viaje.FechaSalida,
                FechaLlegada = viaje.FechaLlegada,
                PesoMax = viaje.PesoMax,
                CapacidadMaxPaquetes = viaje.CapacidadMaxPaquetes,
                CantidadPaquetes = cantidadPaquetes,
                LugaresDisponibles = Math.Max(0, viaje.CapacidadMaxPaquetes - cantidadPaquetes),
                DiasSemana = viaje.Ruta.RutaDias.Select(d => d.DiaSemana).Distinct().OrderBy(d => d).ToList(),
                Paradas = paradas,
                Paquetes = paquetes
            };
        }

        public async Task<List<ViajeListadoDTO>> ListarAsync(int usuarioId, EstadoActividadViaje? estado)
        {
            List<Viaje> viajes = await _repositorio.ListarPorUsuarioAsync(usuarioId, estado);

            return viajes
                .Select(v => new ViajeListadoDTO
                {
                    Id = v.Id,
                    Estado = v.Estado,
                    DireccionOrigen = v.DireccionOrigen.Texto,
                    DireccionDestino = v.DireccionDestino.Texto,
                    FechaSalida = v.FechaSalida,
                    FechaLlegada = v.FechaLlegada,
                    CantidadPaquetes = v.Asignaciones.Count,
                    CapacidadMaxPaquetes = v.CapacidadMaxPaquetes
                })
                .ToList();
        }

        public async Task<ViajeEdicionDTO?> ObtenerParaEditarAsync(int viajeId, int usuarioId)
        {
            Viaje? viaje = await _repositorio.ObtenerParaModificarAsync(viajeId);

            if (viaje is null || viaje.Ruta.UsuarioId != usuarioId)
            {
                return null;
            }

            return ArmarDtoDeEdicion(viaje);
        }

        public async Task<ResultadoOperacionViaje> ActualizarAsync(int viajeId, int usuarioId, ViajeEdicionDTO datos)
        {
            Viaje? viaje = await _repositorio.ObtenerParaModificarAsync(viajeId);

            if (viaje is null || viaje.Ruta.UsuarioId != usuarioId)
            {
                return ResultadoOperacionViaje.NoExiste();
            }

            List<string> errores = Validar(datos, viaje.Asignaciones.Count);

            if (errores.Count > 0)
            {
                return ResultadoOperacionViaje.Invalido(errores);
            }

            viaje.FechaSalida = datos.FechaSalida;
            viaje.FechaLlegada = datos.FechaLlegada;
            viaje.PesoMax = datos.PesoMax;
            viaje.CapacidadMaxPaquetes = datos.CapacidadMaxPaquetes;

            await _repositorio.GuardarCambiosAsync();

            return ResultadoOperacionViaje.Exitoso();
        }

        // Decision: un viaje con paquetes asignados SI se puede desactivar.
        // Desactivar significa "no buscar mas oportunidades", no cancelar lo ya
        // comprometido: las asignaciones existentes quedan intactas y hay que
        // cumplirlas. La pantalla avisa cuantos envios siguen en pie.
        public Task<ResultadoOperacionViaje> DesactivarAsync(int viajeId, int usuarioId)
            => CambiarEstadoAsync(viajeId, usuarioId, EstadoActividadViaje.INACTIVO);

        public Task<ResultadoOperacionViaje> ActivarAsync(int viajeId, int usuarioId)
            => CambiarEstadoAsync(viajeId, usuarioId, EstadoActividadViaje.ACTIVO);

        // Si el viaje ya estaba en el estado pedido no escribe nada y contesta ok igual:
        // repetir la operacion no es un error.
        private async Task<ResultadoOperacionViaje> CambiarEstadoAsync(
            int viajeId, int usuarioId, EstadoActividadViaje nuevoEstado)
        {
            Viaje? viaje = await _repositorio.ObtenerParaModificarAsync(viajeId);

            if (viaje is null || viaje.Ruta.UsuarioId != usuarioId)
            {
                return ResultadoOperacionViaje.NoExiste();
            }

            if (viaje.Estado != nuevoEstado)
            {
                viaje.Estado = nuevoEstado;
                await _repositorio.GuardarCambiosAsync();
            }

            return ResultadoOperacionViaje.Exitoso(viaje.Asignaciones.Count);
        }

        // Mismas reglas que la creacion, acotadas a lo que un viaje puede editar.
        // Origen/destino y dias no se tocan aca: viven en la ruta.
        private static List<string> Validar(ViajeEdicionDTO datos, int paquetesAsignados)
        {
            List<string> errores = [];

            if (datos.FechaLlegada is not null && datos.FechaLlegada <= datos.FechaSalida)
            {
                errores.Add("La hora de llegada tiene que ser posterior a la de salida.");
            }

            if (datos.PesoMax <= 0)
            {
                errores.Add("El peso maximo tiene que ser mayor a cero.");
            }

            if (datos.CapacidadMaxPaquetes <= 0)
            {
                errores.Add("La capacidad tiene que ser mayor a cero.");
            }
            else if (datos.CapacidadMaxPaquetes < paquetesAsignados)
            {
                errores.Add($"El viaje ya tiene {paquetesAsignados} paquete(s) asignado(s): " +
                            "la capacidad no puede quedar por debajo de ese numero.");
            }

            return errores;
        }

        private static ViajeEdicionDTO ArmarDtoDeEdicion(Viaje viaje) => new()
        {
            Id = viaje.Id,
            FechaSalida = viaje.FechaSalida,
            FechaLlegada = viaje.FechaLlegada,
            PesoMax = viaje.PesoMax,
            CapacidadMaxPaquetes = viaje.CapacidadMaxPaquetes,
            Estado = viaje.Estado,
            DireccionOrigen = viaje.DireccionOrigen.Texto,
            DireccionDestino = viaje.DireccionDestino.Texto,
            CantidadPaquetes = viaje.Asignaciones.Count
        };
    }
}
