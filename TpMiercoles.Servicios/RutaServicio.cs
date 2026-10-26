using AutoMapper;
using System;
using System.Collections.Generic;
using System.Text;
using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;
using TpMiercoles.Servicios.DTOs.Direccion;
using TpMiercoles.Servicios.DTOs.Ruta;

namespace TpMiercoles.Servicios
{
    public class RutaServicio
    {
        private readonly IRutaRepositorio _rutaRepositorio;
        private readonly IUsuarioRepositorio _usuarioRepositorio;
        private readonly IMapper _mapper;
        public RutaServicio(IRutaRepositorio rutaRepositorio, IUsuarioRepositorio usuarioRepositorio, IMapper mapper)
        {
            _rutaRepositorio = RutaRepositorio;
            _rutaRepositorio = usuarioRepositorio;
            _mapper = mapper;
        }
        public async Task<RutaDTO> CrearRutaAsync(int usuarioId, CrearRutaDTO rutaDTO)
        {
            Usuario usuario =  await _usuarioRepositorio.BuscarPorIdAsync(usuarioId)
    ?? throw new NoEncontradoException("Usuario no encontrado."); 

            ValidarRuta(rutaDTO);

            Ruta ruta = _mapper.Map<Ruta>(rutaDTO);
            ruta.Usuario = usuario;
            await _rutaRepositorio.CrearRutaAsync(ruta);
            return _mapper.Map<RutaDTO>(ruta);
        }

        private void ValidarRuta(CrearRutaDTO rutaDTO)
        {

            var origen = Clave(rutaDTO.DireccionOrigen);
            var destino = Clave(rutaDTO.DireccionDestino);

            if (origen == destino)
                throw new ValidacionRutaException("El origen y el destino deben ser diferentes.");

            if (rutaDTO.RutaDias.Count == 0)
                throw new ValidacionRutaException("Debe seleccionar al menos un día.");

            if (rutaDTO.PesoMax <= 0 || rutaDTO.CapacidadMaxPaquetes <= 0)
                throw new ValidacionRutaException("Peso y capacidad deben ser mayores que cero.");

            if (rutaDTO.HoraSalida >= rutaDTO.HoraLlegada)
                throw new ValidacionRutaException("La hora de salida debe ser anterior a la de llegada.");
            if (rutaDTO.FechaFin < rutaDTO.FechaInicio)
                throw new ValidacionRutaException("La fecha de fin no puede ser anterior a la de inicio.");

            var paradasVistas = new HashSet<(decimal, decimal)>();

            foreach (var p in rutaDTO.Paradas)
            {
                var clave = Clave(p.Direccion);
                if (clave == origen || clave == destino)
                    throw new ValidacionRutaException("La parada no puede ser igual a la direccion de origen o destino");

                if (!paradasVistas.Add(clave))   
                    throw new ReglaNegocioException("No se pueden repetir paradas.");
            }
            if()
        }
        private (decimal Lat, decimal Lng) Clave(DireccionDTO d)
    => (Math.Round(d.Latitud, 5), Math.Round(d.Longitud, 5));
    }
}
