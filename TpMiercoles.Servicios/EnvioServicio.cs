using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;
using TpMiercoles.Servicios.DTOs;

namespace TpMiercoles.Servicios {
    public interface IEnvioServicio
    {
        Task<ResultadoOperacionEnvio> CrearAsync(EnvioDTO envio, int usuarioId);
        Task<List<EnvioListadoDTO>> ListarAsync(int usuarioId, EstadoEnvio? estado);
        Task<EnvioDetalleDTO?> ObtenerDetalleAsync(int envioId, int usuarioId);
        Task<ResultadoOperacionEnvio> CancelarAsync(int envioId, int usuarioId);
    }

    public class EnvioServicio : IEnvioServicio
    {
        private static readonly TimeZoneInfo ZonaArgentina =
            TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");

        private readonly IEnvioRepositorio _repositorio;
        private readonly IMapper _mapper;
        private readonly TimeProvider _reloj;

        public EnvioServicio(IEnvioRepositorio repositorio, IMapper mapper, TimeProvider reloj)
        {
            _repositorio = repositorio;
            _mapper = mapper;
            _reloj = reloj;
        }

        // Fecha "de hoy" en Argentina, sin depender de la zona horaria del servidor
        private DateTime Hoy() =>
            TimeZoneInfo.ConvertTime(_reloj.GetUtcNow(), ZonaArgentina).Date;

        public async Task<ResultadoOperacionEnvio> CrearAsync(EnvioDTO dto, int usuarioId)
        {
            if (dto.FechaHasta.Date < Hoy())
                return ResultadoOperacionEnvio.Invalido("La fecha no puede ser anterior a hoy.");

            var envio = _mapper.Map<Envio>(dto);
            envio.UsuarioId = usuarioId;
            envio.FechaDesde = Hoy();
            envio.Estado = EstadoEnvio.BuscandoConductor;

            await _repositorio.CrearAsync(envio);
            return ResultadoOperacionEnvio.Exitoso();
        }

        public async Task<List<EnvioListadoDTO>> ListarAsync(int usuarioId, EstadoEnvio? estado)
        {
            List<Envio> envios = await _repositorio.ListarPorUsuarioAsync(usuarioId, estado);
            return _mapper.Map<List<EnvioListadoDTO>>(envios);
        }

        public async Task<EnvioDetalleDTO?> ObtenerDetalleAsync(int envioId, int usuarioId)
        {
            Envio? envio = await _repositorio.ObtenerDetalleAsync(envioId);

            if (envio is null || envio.UsuarioId != usuarioId)
            {
                return null;
            }

            var dto = _mapper.Map<EnvioDetalleDTO>(envio);

            // Solo se muestra un conductor si hay una asignación activa (la más reciente)
            var asignacion = envio.Asignaciones
                .Where(a => a.Viaje != null && a.Estado != EstadoViaje.CANCELADO)
                .OrderByDescending(a => a.Id)
                .FirstOrDefault();

            if (asignacion?.Viaje is not null)
            {
                dto.TieneConductorAsignado = true;
                dto.ViajeId = asignacion.ViajeId;

                var conductor = asignacion.Viaje.Ruta?.Usuario;
                if (conductor is not null)
                {
                    var nombreCompleto = $"{conductor.Nombre} {conductor.Apellido}".Trim();
                    dto.ConductorNombre = string.IsNullOrWhiteSpace(nombreCompleto) ? null : nombreCompleto;
                    dto.ConductorEmail = conductor.Email;
                }

                dto.FechaSalidaViaje = asignacion.Viaje.FechaSalida;
                dto.FechaAceptacion = asignacion.FechaAceptacion;
                dto.FechaRetiro = asignacion.FechaRetiro;
                dto.FechaEntrega = asignacion.FechaEntrega;
            }

            return dto;
        }

        public async Task<ResultadoOperacionEnvio> CancelarAsync(int envioId, int usuarioId)
        {
            // Debe devolver la entidad con tracking e incluir Asignaciones
            Envio? envio = await _repositorio.ObtenerParaModificarAsync(envioId);

            if (envio is null || envio.UsuarioId != usuarioId)
            {
                return ResultadoOperacionEnvio.NoExiste();
            }

            if (envio.Estado != EstadoEnvio.BuscandoConductor)
            {
                return ResultadoOperacionEnvio.Invalido(
                    "Solo se pueden cancelar envíos que se encuentren en búsqueda de conductor.");
            }

            envio.Estado = EstadoEnvio.Cancelado;

            // Cancelar asignaciones pendientes para que el conductor no las siga viendo
            foreach (var asignacion in envio.Asignaciones.Where(a => a.Estado != EstadoViaje.CANCELADO))
            {
                asignacion.Estado = EstadoViaje.CANCELADO;
            }

                await _repositorio.GuardarCambiosAsync();

            return ResultadoOperacionEnvio.Exitoso();
        }
    }

}
