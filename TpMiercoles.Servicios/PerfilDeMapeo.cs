using AutoMapper;
using TpMiercoles.Entidades;
using TpMiercoles.Servicios.DTOs;
using TpMiercoles.Servicios.DTOs.Direccion;
using TpMiercoles.Servicios.DTOs.Ruta;

namespace TpMiercoles.Servicios
{
    public class PerfilDeMapeo : Profile
    {
        public PerfilDeMapeo() {
            CreateMap<UsuarioDTO, Usuario>();
            CreateMap<UsuarioLoginDTO, Usuario>();
            CreateMap<Usuario, UsuarioSesionDTO>();
            CreateMap<CrearRutaDTO, Ruta>();
            CreateMap<DireccionDTO, Direccion>();
            CreateMap<Direccion, DireccionDTO>();
            CreateMap<RutaDiaDTO, RutaDia>();
            CreateMap<ParadaDTO, Parada>();
            CreateMap<RutaDia, RutaDiaDTO>();
            CreateMap<Parada, ParadaDTO>();
            CreateMap<Ruta, RutaDTO>();
        }
    }
}
