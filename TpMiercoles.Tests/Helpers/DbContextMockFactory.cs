using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using TpMiercoles.Entidades;

namespace TpMiercoles.Tests.Helpers
{
    public sealed class DbContextMockFactory
    {
        public Mock<ViajesDbContext> Contexto { get; }
        public Mock<DbSet<Usuario>> Usuarios { get; }
        public Mock<DbSet<Viaje>> Viajes { get; }
        public Mock<DbSet<Envio>> Envios { get; }

        public DbContextMockFactory(
            IEnumerable<Usuario>? usuariosIniciales = null,
            IEnumerable<Viaje>? viajesIniciales = null,
             IEnumerable<Envio>? enviosIniciales = null)
        {
            var usuarios = usuariosIniciales?.ToList() ?? [];
            Usuarios = usuarios.BuildMockDbSet();

            var viajes = viajesIniciales?.ToList() ?? [];
            Viajes = viajes.BuildMockDbSet();

            var envios = enviosIniciales?.ToList() ?? [];
            Envios = envios.BuildMockDbSet();
            Contexto = new Mock<ViajesDbContext>(
                new DbContextOptionsBuilder<ViajesDbContext>().Options);
            Contexto.Object.Usuarios = Usuarios.Object;
            Contexto.Object.Viajes = Viajes.Object;
            Contexto.Object.Envios = Envios.Object;
            Contexto
                .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
        }
    }
}
