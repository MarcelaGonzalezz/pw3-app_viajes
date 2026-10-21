using AutoMapper;
using TpMiercoles.Entidades;
using TpMiercoles.Servicios.DTOs;

namespace TpMiercoles.Servicios
{
    public class PerfilDeMapeo : Profile
    {
        public PerfilDeMapeo() {
            CreateMap<UsuarioDTO, Usuario>();

        }
    }
}
