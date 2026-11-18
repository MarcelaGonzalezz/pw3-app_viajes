using Microsoft.EntityFrameworkCore;


namespace TpMiercoles.Entidades
{
    public class ViajesDbContext : DbContext
    {
        public ViajesDbContext(DbContextOptions<ViajesDbContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Envio> Envios { get; set; }
        public DbSet<Viaje> Viajes { get; set; }
        public DbSet<Ruta> Rutas { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Viaje>()
                .ToTable("Viaje");

            modelBuilder.Entity<Ruta>()
                .HasOne(r => r.DireccionOrigen)
                .WithMany()
                .HasForeignKey(r => r.DireccionOrigenId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Ruta>()
                .HasOne(r => r.DireccionDestino)
                .WithMany()
                .HasForeignKey(r => r.DireccionDestinoId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Envio>()
               .HasOne(e => e.DireccionRetiro)
               .WithMany()
               .HasForeignKey(e => e.DireccionRetiroId)
               .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Envio>()
                .HasOne(e => e.DireccionEntrega)
                .WithMany()
                .HasForeignKey(e => e.DireccionEntregaId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Viaje>()
              .HasOne(v => v.DireccionOrigen)
              .WithMany()
              .HasForeignKey(v => v.DireccionOrigenId)
              .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Viaje>()
                .HasOne(v => v.DireccionDestino)
                .WithMany()
                .HasForeignKey(v => v.DireccionDestinoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Asignacion>()
                .HasOne(a => a.Viaje)
                .WithMany(v => v.Asignaciones)
                .HasForeignKey(a => a.ViajeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Parada>(parada =>
            {
                parada.HasOne(p => p.Ruta)
                    .WithMany(r => r.Paradas)
                    .HasForeignKey(p => p.RutaId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired(false);

                parada.HasOne(p => p.Viaje)
                    .WithMany(v => v.Paradas)
                    .HasForeignKey(p => p.ViajeId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired(false);

                // Una parada pertenece a una ruta o a un viaje, nunca a ambos ni a ninguno.
                parada.ToTable(t => t.HasCheckConstraint(
                    "CK_Parada_UnDuenio",
                    "([RutaId] IS NOT NULL AND [ViajeId] IS NULL) OR ([RutaId] IS NULL AND [ViajeId] IS NOT NULL)"));
            });

            modelBuilder.Entity<Envio>()
                .Property(e => e.Monto)
                .HasPrecision(18, 2);
        }
    }

    
}
