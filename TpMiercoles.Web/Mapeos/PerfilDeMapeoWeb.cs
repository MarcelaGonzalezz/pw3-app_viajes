using AutoMapper;
using TpMiercoles.Entidades;
using TpMiercoles.Servicios.DTOs;
using TpMiercoles.Servicios.DTOs.Direccion;
using TpMiercoles.Servicios.DTOs.Ruta;
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
            CreateMap<EnvioListadoDTO, EnvioListadoViewModel>();

            CreateMap<ViajeDetalleDTO, ViajeDetalleViewModel>();
            CreateMap<ParadaDTO, ParadaViewModel>();
            CreateMap<PaqueteAsignadoDTO, PaqueteAsignadoViewModel>();

            CreateMap<ViajeListadoDTO, ViajeListadoViewModel>();

            CreateMap<ViajeEdicionDTO, ViajeEdicionViewModel>();
            CreateMap<ViajeEdicionViewModel, ViajeEdicionDTO>();
            CreateMap<DireccionDTO, DireccionViewModel>();
            CreateMap<DireccionViewModel, DireccionDTO>();        
            CreateMap<ParadaViewModel, ParadaDTO>();             
            CreateMap<DayOfWeek, RutaDiaDTO>()                    
                .ConvertUsing(d => new RutaDiaDTO { DiaSemana = d });
            CreateMap<CrearRutaViewModel, CrearRutaDTO>();
        }
    }
}
