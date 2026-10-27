using AutoMapper;
using TpMiercoles.Entidades;
using TpMiercoles.Servicios.DTOs;
using TpMiercoles.Web.Models;

namespace TpMiercoles.Web.Mapeos
{
    public class PerfilDeMapeoWeb : Profile
    {
        public PerfilDeMapeoWeb()
        {
            CreateMap<RegistroUsuarioViewModel, UsuarioDTO>()
                 .ForMember(d => d.ContraseniaPlana, o => o.MapFrom(s => s.Contrasenia));
            CreateMap<LoginUsuarioViewModel, UsuarioLoginDTO>()
                .ForMember(d => d.ContraseniaPlana, o => o.MapFrom(s => s.Contrasenia));
            CreateMap<NuevoEnvioViewModel, EnvioDTO>();
        }
    }
}
