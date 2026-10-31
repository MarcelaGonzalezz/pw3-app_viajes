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
            CreateMap<Envio, EnvioDTO>()
                .ForMember(d => d.Origen, o => o.MapFrom(s => s.DireccionRetiro))
                .ForMember(d => d.Destino, o => o.MapFrom(s => s.DireccionEntrega));
            CreateMap<Envio, EnvioListadoDTO>()
    .ForMember(d => d.Origen, o => o.MapFrom(s => s.DireccionRetiro.Texto))
    .ForMember(d => d.Destino, o => o.MapFrom(s => s.DireccionEntrega.Texto))
    .ForMember(d => d.PesoKg, o => o.MapFrom(s => s.Paquete.PesoKg))
    .ForMember(d => d.Tamanio, o => o.MapFrom(s => s.Paquete.Tamanio))
    .ForMember(d => d.Descripcion, o => o.MapFrom(s => s.Paquete.Descripcion));

            CreateMap<Envio, EnvioDetalleDTO>()
                .ForMember(d => d.Origen, o => o.MapFrom(s => s.DireccionRetiro.Texto))
                .ForMember(d => d.Destino, o => o.MapFrom(s => s.DireccionEntrega.Texto))
                .ForMember(d => d.PesoKg, o => o.MapFrom(s => s.Paquete.PesoKg))
                .ForMember(d => d.Tamanio, o => o.MapFrom(s => s.Paquete.Tamanio))
                .ForMember(d => d.Descripcion, o => o.MapFrom(s => s.Paquete.Descripcion))
                .ForMember(d => d.TieneConductorAsignado, o => o.Ignore())
                .ForMember(d => d.ViajeId, o => o.Ignore())
                .ForMember(d => d.ConductorNombre, o => o.Ignore())
                .ForMember(d => d.ConductorEmail, o => o.Ignore())
                .ForMember(d => d.FechaSalidaViaje, o => o.Ignore())
                .ForMember(d => d.FechaAceptacion, o => o.Ignore())
                .ForMember(d => d.FechaRetiro, o => o.Ignore())
                .ForMember(d => d.FechaEntrega, o => o.Ignore());
            CreateMap<NuevoPaqueteDTO, Paquete>();

            CreateMap<EnvioDTO, Envio>()
                .ForMember(d => d.DireccionRetiro, o => o.MapFrom(s => s.Origen))
                .ForMember(d => d.DireccionEntrega, o => o.MapFrom(s => s.Destino))
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.UsuarioId, o => o.Ignore())
                .ForMember(d => d.Usuario, o => o.Ignore())
                .ForMember(d => d.DireccionRetiroId, o => o.Ignore())
                .ForMember(d => d.DireccionEntregaId, o => o.Ignore())
                .ForMember(d => d.FechaDesde, o => o.Ignore())
                .ForMember(d => d.Asignaciones, o => o.Ignore())
                .ForMember(d => d.Estado, o => o.Ignore());
        }
    }
}
