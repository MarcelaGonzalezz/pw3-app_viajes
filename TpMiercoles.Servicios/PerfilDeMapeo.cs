using AutoMapper;
using TpMiercoles.Entidades;
using TpMiercoles.Servicios.DTOs;
using TpMiercoles.Servicios.DTOs.Ruta;

namespace TpMiercoles.Servicios
{
    public class PerfilDeMapeo : Profile
    {
        public PerfilDeMapeo() {
            CreateMap<UsuarioDTO, Usuario>();
            CreateMap<UsuarioLoginDTO, Usuario>();
            CreateMap<Usuario, UsuarioSesionDTO>();
            CreateMap<RutaDTO, Ruta>();
        }
    }
}
