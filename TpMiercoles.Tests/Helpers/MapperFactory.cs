using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using TpMiercoles.Servicios;
using TpMiercoles.Web.Mapeos;

namespace TpMiercoles.Tests.Helpers
{
    public static class MapperFactory
    {
        public static IMapper Crear()
        {
            var configuracion = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<PerfilDeMapeo>();
                cfg.AddProfile<PerfilDeMapeoWeb>();
            }, NullLoggerFactory.Instance);

            return configuracion.CreateMapper();
        }
    }
}
