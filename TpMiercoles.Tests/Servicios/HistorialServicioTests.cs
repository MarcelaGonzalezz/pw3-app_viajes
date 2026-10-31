using Moq;
using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;
using TpMiercoles.Servicios;
using TpMiercoles.Servicios.DTOs.Historial;

namespace TpMiercoles.Tests.Servicios
{
    public class HistorialServicioTests
    {
        private const int UsuarioId = 5;

        private readonly Mock<IHistorialRepositorio> _repositorioMock;
        private readonly HistorialServicio _servicio;

        public HistorialServicioTests()
        {
            _repositorioMock = new Mock<IHistorialRepositorio>();
            _repositorioMock
                .Setup(r => r.ListarViajesRealizadosAsync(It.IsAny<int>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Viaje>());
            _repositorioMock
                .Setup(r => r.ListarEnviosFinalizadosAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<Envio>());

            _servicio = new HistorialServicio(_repositorioMock.Object);
        }

        [Fact]
        public async Task ObtenerAsync_SinOperaciones_DevuelveTodoEnCero()
        {
            var resultado = await _servicio.ObtenerAsync(UsuarioId, null, null, null);

            Assert.Equal(0, resultado.ViajesRealizados);
            Assert.Equal(0, resultado.EnviosTransportados);
            Assert.Equal(0m, resultado.GananciaEfectiva);
            Assert.Empty(resultado.Items);
        }

        [Fact]
        public async Task ObtenerAsync_ConsultaSoloLasOperacionesDelUsuarioIndicado()
        {
            await _servicio.ObtenerAsync(UsuarioId, null, null, null);

            _repositorioMock.Verify(r => r.ListarViajesRealizadosAsync(UsuarioId, It.IsAny<DateTime>()), Times.Once);
            _repositorioMock.Verify(r => r.ListarEnviosFinalizadosAsync(UsuarioId), Times.Once);
        }

        [Fact]
        public async Task ObtenerAsync_LaGananciaEfectivaSumaSoloLosPaquetesEntregados()
        {
            Viaje viaje = CrearViaje(1, new DateTime(2026, 9, 10, 8, 0, 0),
                CrearAsignacion(EstadoEnvio.Entregado, EstadoViaje.CONFIRMADO, 4000m),
                CrearAsignacion(EstadoEnvio.Entregado, EstadoViaje.CONFIRMADO, 5500m),
                CrearAsignacion(EstadoEnvio.EnCamino, EstadoViaje.CONFIRMADO, 9999m),   // todavia no se entrego
                CrearAsignacion(EstadoEnvio.Entregado, EstadoViaje.CANCELADO, 8888m));  // asignacion cancelada
            ConfigurarViajes(viaje);

            var resultado = await _servicio.ObtenerAsync(UsuarioId, null, null, null);

            Assert.Equal(1, resultado.ViajesRealizados);
            Assert.Equal(2, resultado.EnviosTransportados);
            Assert.Equal(9500m, resultado.GananciaEfectiva);

            HistorialItemDTO item = Assert.Single(resultado.Items);
            Assert.Equal(TipoOperacionHistorial.ViajeRealizado, item.Tipo);
            Assert.Equal(2, item.CantidadPaquetes);
            Assert.Equal(9500m, item.Monto);
        }

        [Fact]
        public async Task ObtenerAsync_ElResumenAcumulaVariosViajes()
        {
            ConfigurarViajes(
                CrearViaje(1, new DateTime(2026, 9, 1, 8, 0, 0),
                    CrearAsignacion(EstadoEnvio.Entregado, EstadoViaje.CONFIRMADO, 4000m),
                    CrearAsignacion(EstadoEnvio.Entregado, EstadoViaje.CONFIRMADO, 5500m),
                    CrearAsignacion(EstadoEnvio.Entregado, EstadoViaje.CONFIRMADO, 2500m)),
                CrearViaje(2, new DateTime(2026, 9, 8, 8, 0, 0),
                    CrearAsignacion(EstadoEnvio.Entregado, EstadoViaje.CONFIRMADO, 7000m),
                    CrearAsignacion(EstadoEnvio.Entregado, EstadoViaje.CONFIRMADO, 3500m)));

            var resultado = await _servicio.ObtenerAsync(UsuarioId, null, null, null);

            Assert.Equal(2, resultado.ViajesRealizados);
            Assert.Equal(5, resultado.EnviosTransportados);
            Assert.Equal(22500m, resultado.GananciaEfectiva);
        }

        [Fact]
        public async Task ObtenerAsync_ListaLasOperacionesDeLaMasRecienteALaMasAntigua()
        {
            ConfigurarViajes(
                CrearViaje(1, new DateTime(2026, 9, 1, 8, 0, 0), CrearAsignacion(EstadoEnvio.Entregado, EstadoViaje.CONFIRMADO, 1000m)),
                CrearViaje(2, new DateTime(2026, 10, 1, 8, 0, 0), CrearAsignacion(EstadoEnvio.Entregado, EstadoViaje.CONFIRMADO, 1000m)),
                CrearViaje(3, new DateTime(2026, 9, 15, 8, 0, 0), CrearAsignacion(EstadoEnvio.Entregado, EstadoViaje.CONFIRMADO, 1000m)));

            var resultado = await _servicio.ObtenerAsync(UsuarioId, null, null, null);

            Assert.Equal(new[] { 2, 3, 1 }, resultado.Items.Select(i => i.Id));
        }

        [Fact]
        public async Task ObtenerAsync_ElEnvioEntregadoUsaLaFechaDeEntregaYElCanceladoLaFechaLimite()
        {
            Envio entregado = CrearEnvio(10, EstadoEnvio.Entregado, 2500m, "Repuesto",
                fechaHasta: new DateTime(2026, 9, 30),
                asignaciones: [new Asignacion { FechaEntrega = new DateTime(2026, 9, 12, 11, 0, 0) }]);
            Envio cancelado = CrearEnvio(11, EstadoEnvio.Cancelado, 3500m, "Documentos",
                fechaHasta: new DateTime(2026, 9, 20));
            ConfigurarEnvios(entregado, cancelado);

            var resultado = await _servicio.ObtenerAsync(UsuarioId, null, null, null);

            HistorialItemDTO itemEntregado = resultado.Items.Single(i => i.Id == 10);
            HistorialItemDTO itemCancelado = resultado.Items.Single(i => i.Id == 11);

            Assert.Equal(TipoOperacionHistorial.EnvioEntregado, itemEntregado.Tipo);
            Assert.Equal(new DateTime(2026, 9, 12, 11, 0, 0), itemEntregado.Fecha);
            Assert.Equal("Repuesto", itemEntregado.Descripcion);
            Assert.Equal(TipoOperacionHistorial.EnvioCancelado, itemCancelado.Tipo);
            Assert.Equal(new DateTime(2026, 9, 20), itemCancelado.Fecha);
        }

        [Fact]
        public async Task ObtenerAsync_LosEnviosEnviadosNoSumanGananciaNiViajes()
        {
            ConfigurarEnvios(CrearEnvio(10, EstadoEnvio.Entregado, 2500m, "Repuesto"));

            var resultado = await _servicio.ObtenerAsync(UsuarioId, null, null, null);

            Assert.Equal(0, resultado.ViajesRealizados);
            Assert.Equal(0, resultado.EnviosTransportados);
            Assert.Equal(0m, resultado.GananciaEfectiva);
            Assert.Single(resultado.Items);
            Assert.Equal(1, resultado.TotalEnviosEntregados);
        }

        [Fact]
        public async Task ObtenerAsync_ElFiltroPorTipoReduceLaListaPeroNoElResumen()
        {
            ConfigurarViajes(CrearViaje(1, new DateTime(2026, 9, 1, 8, 0, 0),
                CrearAsignacion(EstadoEnvio.Entregado, EstadoViaje.CONFIRMADO, 4000m)));
            ConfigurarEnvios(
                CrearEnvio(10, EstadoEnvio.Entregado, 2500m, "Repuesto"),
                CrearEnvio(11, EstadoEnvio.Cancelado, 3500m, "Documentos"));

            var resultado = await _servicio.ObtenerAsync(UsuarioId, TipoOperacionHistorial.EnvioCancelado, null, null);

            HistorialItemDTO item = Assert.Single(resultado.Items);
            Assert.Equal(11, item.Id);
            Assert.Equal(1, resultado.ViajesRealizados);
            Assert.Equal(4000m, resultado.GananciaEfectiva);
            Assert.Equal(3, resultado.TotalOperaciones);
            Assert.Equal(1, resultado.TotalEnviosEntregados);
            Assert.Equal(1, resultado.TotalEnviosCancelados);
        }

        [Fact]
        public async Task ObtenerAsync_ElFiltroDeFechasIncluyeElDiaHastaCompleto()
        {
            ConfigurarViajes(
                CrearViaje(1, new DateTime(2026, 9, 9, 23, 59, 0), CrearAsignacion(EstadoEnvio.Entregado, EstadoViaje.CONFIRMADO, 1000m)),
                CrearViaje(2, new DateTime(2026, 9, 10, 0, 0, 0), CrearAsignacion(EstadoEnvio.Entregado, EstadoViaje.CONFIRMADO, 2000m)),
                CrearViaje(3, new DateTime(2026, 9, 20, 18, 30, 0), CrearAsignacion(EstadoEnvio.Entregado, EstadoViaje.CONFIRMADO, 3000m)),
                CrearViaje(4, new DateTime(2026, 9, 21, 0, 0, 0), CrearAsignacion(EstadoEnvio.Entregado, EstadoViaje.CONFIRMADO, 4000m)));

            var resultado = await _servicio.ObtenerAsync(
                UsuarioId, null, new DateTime(2026, 9, 10), new DateTime(2026, 9, 20));

            Assert.Equal(new[] { 3, 2 }, resultado.Items.Select(i => i.Id));
            Assert.Equal(2, resultado.ViajesRealizados);
            Assert.Equal(5000m, resultado.GananciaEfectiva);
        }

        [Fact]
        public async Task ObtenerAsync_CuandoDesdeEsPosteriorAHasta_LosIntercambia()
        {
            ConfigurarViajes(
                CrearViaje(1, new DateTime(2026, 9, 15, 8, 0, 0), CrearAsignacion(EstadoEnvio.Entregado, EstadoViaje.CONFIRMADO, 1000m)),
                CrearViaje(2, new DateTime(2026, 10, 15, 8, 0, 0), CrearAsignacion(EstadoEnvio.Entregado, EstadoViaje.CONFIRMADO, 2000m)));

            var resultado = await _servicio.ObtenerAsync(
                UsuarioId, null, new DateTime(2026, 9, 30), new DateTime(2026, 9, 1));

            HistorialItemDTO item = Assert.Single(resultado.Items);
            Assert.Equal(1, item.Id);
        }

        // ---------- Helpers ----------

        private void ConfigurarViajes(params Viaje[] viajes)
            => _repositorioMock
                .Setup(r => r.ListarViajesRealizadosAsync(UsuarioId, It.IsAny<DateTime>()))
                .ReturnsAsync(viajes.ToList());

        private void ConfigurarEnvios(params Envio[] envios)
            => _repositorioMock
                .Setup(r => r.ListarEnviosFinalizadosAsync(UsuarioId))
                .ReturnsAsync(envios.ToList());

        private static Viaje CrearViaje(int id, DateTime fechaSalida, params Asignacion[] asignaciones)
            => new()
            {
                Id = id,
                FechaSalida = fechaSalida,
                DireccionOrigen = new Direccion { Texto = "Ramos Mejía" },
                DireccionDestino = new Direccion { Texto = "Microcentro" },
                Asignaciones = asignaciones.ToList()
            };

        private static Asignacion CrearAsignacion(EstadoEnvio estadoEnvio, EstadoViaje estadoAsignacion, decimal monto)
            => new()
            {
                Estado = estadoAsignacion,
                Envio = CrearEnvio(0, estadoEnvio, monto, "Paquete")
            };

        private static Envio CrearEnvio(
            int id, EstadoEnvio estado, decimal monto, string descripcion,
            DateTime? fechaHasta = null, List<Asignacion>? asignaciones = null)
            => new()
            {
                Id = id,
                Estado = estado,
                Monto = monto,
                FechaDesde = new DateTime(2026, 9, 1),
                FechaHasta = fechaHasta ?? new DateTime(2026, 9, 5),
                DireccionRetiro = new Direccion { Texto = "Liniers" },
                DireccionEntrega = new Direccion { Texto = "Once" },
                Paquete = new Paquete { PesoKg = 1, Tamanio = TamanioEnvio.Pequenio, Descripcion = descripcion },
                Asignaciones = asignaciones ?? []
            };
    }
}
