using Moq;
using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;
using TpMiercoles.Servicios;
using TpMiercoles.Servicios.DTOs;
using TpMiercoles.Tests.Helpers;

namespace TpMiercoles.Tests.Servicios
{
    public class ViajeServicioTests
    {
        private readonly Mock<IViajeRepositorio> _repositorioMock;
        private readonly ViajeServicio _servicio;

        public ViajeServicioTests()
        {
            _repositorioMock = new Mock<IViajeRepositorio>();
            _servicio = new ViajeServicio(_repositorioMock.Object);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ObtenerDetalleAsync_CuandoElViajeNoExisteONoEsDelUsuario_DevuelveNull(bool esDeOtroUsuario)
        {
            Viaje? viaje = esDeOtroUsuario
                ? ViajeTestData.CrearViaje(id: 10, usuarioId: 99)
                : null;

            _repositorioMock
                .Setup(r => r.ObtenerDetalleAsync(10))
                .ReturnsAsync(viaje);

            var resultado = await _servicio.ObtenerDetalleAsync(10, 5);

            Assert.Null(resultado);
        }

        [Fact]
        public async Task ObtenerDetalleAsync_CuandoElViajeEsDelUsuario_DevuelveElDetalleCompleto()
        {
            DateTime fechaSalida = new(2026, 11, 2, 8, 0, 0);
            DateTime fechaLlegada = fechaSalida.AddMinutes(90);
            DateTime fechaAceptacion = new(2026, 10, 30, 12, 0, 0);

            var viaje = ViajeTestData.CrearViaje(
                id: 10,
                usuarioId: 5,
                fechaSalida: fechaSalida,
                fechaLlegada: fechaLlegada,
                pesoMax: 25,
                capacidadMaxPaquetes: 4,
                paradas:
                [
                    ViajeTestData.CrearParada(2, "Liniers"),
                    ViajeTestData.CrearParada(1, "Ciudadela")
                ],
                dias:
                [
                    ViajeTestData.CrearDia(1, DayOfWeek.Monday),
                    ViajeTestData.CrearDia(2, DayOfWeek.Wednesday),
                    ViajeTestData.CrearDia(3, DayOfWeek.Monday)
                ],
                asignaciones:
                [
                    ViajeTestData.CrearAsignacion(
                        id: 2,
                        envioId: 101,
                        nombreRemitente: "Juan",
                        apellidoRemitente: "Pérez",
                        direccionRetiro: "Ciudadela",
                        direccionEntrega: "Centro",
                        estado: EstadoViaje.CONFIRMADO,
                        fechaRetiro: new DateTime(2026, 10, 31, 9, 0, 0)),
                    ViajeTestData.CrearAsignacion(
                        id: 1,
                        envioId: 100,
                        direccionRetiro: "Liniers",
                        direccionEntrega: "Once",
                        estado: EstadoViaje.PENDIENTE,
                        fechaAceptacion: fechaAceptacion)
                ]);
            viaje.DireccionOrigen = ViajeTestData.CrearDireccion(20, "Ramos Mejía");
            viaje.DireccionDestino = ViajeTestData.CrearDireccion(21, "Microcentro");

            _repositorioMock
                .Setup(r => r.ObtenerDetalleAsync(10))
                .ReturnsAsync(viaje);

            var resultado = await _servicio.ObtenerDetalleAsync(10, 5);

            Assert.NotNull(resultado);
            Assert.Equal(10, resultado!.Id);
            Assert.Equal(10, resultado.RutaId);
            Assert.Equal(EstadoActividadViaje.ACTIVO, resultado.Estado);
            Assert.Equal("Ramos Mejía", resultado.DireccionOrigen);
            Assert.Equal("Microcentro", resultado.DireccionDestino);
            Assert.Equal(fechaSalida, resultado.FechaSalida);
            Assert.Equal(fechaLlegada, resultado.FechaLlegada);
            Assert.Equal(25, resultado.PesoMax);
            Assert.Equal(4, resultado.CapacidadMaxPaquetes);
            Assert.Equal(2, resultado.CantidadPaquetes);
            Assert.Equal(2, resultado.LugaresDisponibles);
            Assert.Equal([DayOfWeek.Monday, DayOfWeek.Wednesday], resultado.DiasSemana);

            Assert.Collection(
                resultado.Paradas,
                parada =>
                {
                    Assert.Equal(1, parada.Orden);
                    Assert.Equal("Ciudadela", parada.Direccion);
                },
                parada =>
                {
                    Assert.Equal(2, parada.Orden);
                    Assert.Equal("Liniers", parada.Direccion);
                });

            Assert.Collection(
                resultado.Paquetes,
                paquete =>
                {
                    Assert.Equal(1, paquete.AsignacionId);
                    Assert.Equal(100, paquete.EnvioId);
                    Assert.Equal("Carla Méndez", paquete.Remitente);
                    Assert.Equal("Liniers", paquete.DireccionRetiro);
                    Assert.Equal("Once", paquete.DireccionEntrega);
                    Assert.Equal(EstadoViaje.PENDIENTE, paquete.Estado);
                    Assert.Equal(fechaAceptacion, paquete.FechaAceptacion);
                },
                paquete =>
                {
                    Assert.Equal(2, paquete.AsignacionId);
                    Assert.Equal(101, paquete.EnvioId);
                    Assert.Equal("Juan Pérez", paquete.Remitente);
                    Assert.Equal("Ciudadela", paquete.DireccionRetiro);
                    Assert.Equal("Centro", paquete.DireccionEntrega);
                    Assert.Equal(EstadoViaje.CONFIRMADO, paquete.Estado);
                    Assert.Equal(new DateTime(2026, 10, 31, 9, 0, 0), paquete.FechaRetiro);
                });
        }

        [Fact]
        public async Task ObtenerDetalleAsync_CuandoHayMasPaquetesQueCapacidad_LugaresDisponiblesEsCero()
        {
            var viaje = ViajeTestData.CrearViaje(
                id: 10,
                usuarioId: 5,
                capacidadMaxPaquetes: 1,
                asignaciones:
                [
                    ViajeTestData.CrearAsignacion(1, envioId: 100),
                    ViajeTestData.CrearAsignacion(2, envioId: 101)
                ]);
            _repositorioMock
                .Setup(r => r.ObtenerDetalleAsync(10))
                .ReturnsAsync(viaje);

            var resultado = await _servicio.ObtenerDetalleAsync(10, 5);

            Assert.NotNull(resultado);
            Assert.Equal(2, resultado!.CantidadPaquetes);
            Assert.Equal(0, resultado.LugaresDisponibles);
        }

        [Fact]
        public async Task ListarAsync_CuandoHayViajes_DevuelveElListadoMapeado()
        {
            DateTime primeraSalida = new(2026, 11, 2, 8, 0, 0);
            DateTime segundaSalida = new(2026, 11, 3, 9, 0, 0);

            var viajeActivo = ViajeTestData.CrearViaje(
                id: 1,
                estado: EstadoActividadViaje.ACTIVO,
                fechaSalida: primeraSalida,
                fechaLlegada: primeraSalida.AddHours(1),
                capacidadMaxPaquetes: 4,
                asignaciones:
                [
                    ViajeTestData.CrearAsignacion(1, envioId: 100),
                    ViajeTestData.CrearAsignacion(2, envioId: 101)
                ]);
            var viajeInactivo = ViajeTestData.CrearViaje(
                id: 2,
                estado: EstadoActividadViaje.INACTIVO,
                fechaSalida: segundaSalida,
                fechaLlegada: segundaSalida.AddHours(2),
                capacidadMaxPaquetes: 2,
                asignaciones:
                [
                    ViajeTestData.CrearAsignacion(3, envioId: 102)
                ]);

            _repositorioMock
                .Setup(r => r.ListarPorUsuarioAsync(5, null))
                .ReturnsAsync([viajeActivo, viajeInactivo]);

            var resultado = await _servicio.ListarAsync(5, null);

            Assert.Collection(
                resultado,
                viaje =>
                {
                    Assert.Equal(1, viaje.Id);
                    Assert.Equal(EstadoActividadViaje.ACTIVO, viaje.Estado);
                    Assert.Equal("Ramos Mejía", viaje.DireccionOrigen);
                    Assert.Equal("Microcentro", viaje.DireccionDestino);
                    Assert.Equal(primeraSalida, viaje.FechaSalida);
                    Assert.Equal(primeraSalida.AddHours(1), viaje.FechaLlegada);
                    Assert.Equal(2, viaje.CantidadPaquetes);
                    Assert.Equal(4, viaje.CapacidadMaxPaquetes);
                },
                viaje =>
                {
                    Assert.Equal(2, viaje.Id);
                    Assert.Equal(EstadoActividadViaje.INACTIVO, viaje.Estado);
                    Assert.Equal(segundaSalida, viaje.FechaSalida);
                    Assert.Equal(segundaSalida.AddHours(2), viaje.FechaLlegada);
                    Assert.Equal(1, viaje.CantidadPaquetes);
                    Assert.Equal(2, viaje.CapacidadMaxPaquetes);
                });
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ObtenerParaEditarAsync_CuandoElViajeNoExisteONoEsDelUsuario_DevuelveNull(bool esDeOtroUsuario)
        {
            Viaje? viaje = esDeOtroUsuario
                ? ViajeTestData.CrearViaje(id: 10, usuarioId: 99)
                : null;
            _repositorioMock
                .Setup(r => r.ObtenerParaModificarAsync(10))
                .ReturnsAsync(viaje);

            var resultado = await _servicio.ObtenerParaEditarAsync(10, 5);

            Assert.Null(resultado);
        }

        [Fact]
        public async Task ObtenerParaEditarAsync_CuandoElViajeEsDelUsuario_DevuelveLosDatosDeEdicion()
        {
            DateTime fechaSalida = new(2026, 11, 2, 8, 0, 0);
            DateTime fechaLlegada = fechaSalida.AddMinutes(90);
            var viaje = ViajeTestData.CrearViaje(
                id: 10,
                usuarioId: 5,
                estado: EstadoActividadViaje.INACTIVO,
                fechaSalida: fechaSalida,
                fechaLlegada: fechaLlegada,
                pesoMax: 30,
                capacidadMaxPaquetes: 6,
                asignaciones:
                [
                    ViajeTestData.CrearAsignacion(1, envioId: 100),
                    ViajeTestData.CrearAsignacion(2, envioId: 101)
                ]);
            viaje.DireccionOrigen = ViajeTestData.CrearDireccion(20, "Ramos Mejía");
            viaje.DireccionDestino = ViajeTestData.CrearDireccion(21, "Microcentro");

            _repositorioMock
                .Setup(r => r.ObtenerParaModificarAsync(10))
                .ReturnsAsync(viaje);

            var resultado = await _servicio.ObtenerParaEditarAsync(10, 5);

            Assert.NotNull(resultado);
            Assert.Equal(10, resultado!.Id);
            Assert.Equal(fechaSalida, resultado.FechaSalida);
            Assert.Equal(fechaLlegada, resultado.FechaLlegada);
            Assert.Equal(30, resultado.PesoMax);
            Assert.Equal(6, resultado.CapacidadMaxPaquetes);
            Assert.Equal(EstadoActividadViaje.INACTIVO, resultado.Estado);
            Assert.Equal("Ramos Mejía", resultado.DireccionOrigen);
            Assert.Equal("Microcentro", resultado.DireccionDestino);
            Assert.Equal(2, resultado.CantidadPaquetes);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ActualizarAsync_CuandoElViajeNoExisteONoEsDelUsuario_DevuelveNoEncontrado(bool esDeOtroUsuario)
        {
            Viaje? viaje = esDeOtroUsuario
                ? ViajeTestData.CrearViaje(id: 10, usuarioId: 99)
                : null;

            _repositorioMock
                .Setup(r => r.ObtenerParaModificarAsync(10))
                .ReturnsAsync(viaje);

            var resultado = await _servicio.ActualizarAsync(10, 5, CrearDatosEdicion());

            Assert.True(resultado.NoEncontrado);
            Assert.False(resultado.EsExitoso);
            _repositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Never);
        }

        [Fact]
        public async Task ActualizarAsync_CuandoLosDatosSonValidos_ActualizaYGuardaLosCambios()
        {
            var viaje = ViajeTestData.CrearViaje(
                id: 10,
                usuarioId: 5,
                fechaSalida: new DateTime(2026, 10, 1, 8, 0, 0),
                fechaLlegada: new DateTime(2026, 10, 1, 9, 0, 0),
                pesoMax: 25,
                capacidadMaxPaquetes: 4,
                asignaciones: [ViajeTestData.CrearAsignacion(1, envioId: 100)]);
            _repositorioMock
                .Setup(r => r.ObtenerParaModificarAsync(10))
                .ReturnsAsync(viaje);

            DateTime nuevaSalida = new(2026, 12, 1, 9, 0, 0);
            DateTime nuevaLlegada = nuevaSalida.AddHours(1);
            var datos = CrearDatosEdicion(nuevaSalida, nuevaLlegada, pesoMax: 40, capacidad: 6);

            var resultado = await _servicio.ActualizarAsync(10, 5, datos);

            Assert.True(resultado.EsExitoso);
            Assert.Equal(nuevaSalida, viaje.FechaSalida);
            Assert.Equal(nuevaLlegada, viaje.FechaLlegada);
            Assert.Equal(40, viaje.PesoMax);
            Assert.Equal(6, viaje.CapacidadMaxPaquetes);
            _repositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Once);
        }

        [Fact]
        public async Task ActualizarAsync_CuandoLosDatosSonInvalidos_DevuelveTodosLosErrores()
        {
            var viaje = ViajeTestData.CrearViaje(id: 10, usuarioId: 5);
            _repositorioMock
                .Setup(r => r.ObtenerParaModificarAsync(10))
                .ReturnsAsync(viaje);

            DateTime salida = new(2026, 11, 2, 9, 0, 0);
            var datos = CrearDatosEdicion(salida, salida, pesoMax: 0, capacidad: 0);

            var resultado = await _servicio.ActualizarAsync(10, 5, datos);

            Assert.False(resultado.EsExitoso);
            Assert.Equal(3, resultado.Errores.Count);
            Assert.Contains(
                "La hora de llegada tiene que ser posterior a la de salida.",
                resultado.Errores);
            Assert.Contains("El peso maximo tiene que ser mayor a cero.", resultado.Errores);
            Assert.Contains("La capacidad tiene que ser mayor a cero.", resultado.Errores);
            _repositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Never);
        }

        [Fact]
        public async Task ActualizarAsync_CuandoLaCapacidadEsMenorALosPaquetesAsignados_DevuelveError()
        {
            var viaje = ViajeTestData.CrearViaje(
                id: 10,
                usuarioId: 5,
                asignaciones:
                [
                    ViajeTestData.CrearAsignacion(1, envioId: 100),
                    ViajeTestData.CrearAsignacion(2, envioId: 101),
                    ViajeTestData.CrearAsignacion(3, envioId: 102)
                ]);
            _repositorioMock
                .Setup(r => r.ObtenerParaModificarAsync(10))
                .ReturnsAsync(viaje);

            var datos = CrearDatosEdicion(capacidad: 2);

            var resultado = await _servicio.ActualizarAsync(10, 5, datos);

            Assert.False(resultado.EsExitoso);
            Assert.Contains(
                resultado.Errores,
                error => error.Contains("3 paquete(s) asignado(s)")
                         && error.Contains("la capacidad no puede quedar por debajo"));
            _repositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Never);
        }

        [Fact]
        public async Task ActualizarAsync_CuandoLaCapacidadIgualaLosPaquetesAsignados_EsValido()
        {
            var viaje = ViajeTestData.CrearViaje(
                id: 10,
                usuarioId: 5,
                asignaciones:
                [
                    ViajeTestData.CrearAsignacion(1, envioId: 100),
                    ViajeTestData.CrearAsignacion(2, envioId: 101),
                    ViajeTestData.CrearAsignacion(3, envioId: 102)
                ]);
            _repositorioMock
                .Setup(r => r.ObtenerParaModificarAsync(10))
                .ReturnsAsync(viaje);

            var datos = CrearDatosEdicion(capacidad: 3);

            var resultado = await _servicio.ActualizarAsync(10, 5, datos);

            Assert.True(resultado.EsExitoso);
            Assert.Equal(3, viaje.CapacidadMaxPaquetes);
            _repositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Once);
        }

        [Fact]
        public async Task DesactivarAsync_CuandoElViajeEsDeOtroUsuario_DevuelveNoEncontrado()
        {
            var viaje = ViajeTestData.CrearViaje(
                id: 10,
                usuarioId: 99,
                estado: EstadoActividadViaje.ACTIVO);
            _repositorioMock
                .Setup(r => r.ObtenerParaModificarAsync(10))
                .ReturnsAsync(viaje);

            var resultado = await _servicio.DesactivarAsync(10, 5);

            Assert.True(resultado.NoEncontrado);
            Assert.False(resultado.EsExitoso);
            Assert.Equal(EstadoActividadViaje.ACTIVO, viaje.Estado);
            _repositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Never);
        }

        [Fact]
        public async Task ActivarAsync_CuandoElViajeEsDeOtroUsuario_DevuelveNoEncontrado()
        {
            var viaje = ViajeTestData.CrearViaje(
                id: 10,
                usuarioId: 99,
                estado: EstadoActividadViaje.INACTIVO);
            _repositorioMock
                .Setup(r => r.ObtenerParaModificarAsync(10))
                .ReturnsAsync(viaje);

            var resultado = await _servicio.ActivarAsync(10, 5);

            Assert.True(resultado.NoEncontrado);
            Assert.False(resultado.EsExitoso);
            Assert.Equal(EstadoActividadViaje.INACTIVO, viaje.Estado);
            _repositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Never);
        }

        [Fact]
        public async Task DesactivarAsync_CuandoElViajeEstaActivo_LoDesactivaYGuarda()
        {
            var viaje = ViajeTestData.CrearViaje(
                id: 10,
                usuarioId: 5,
                estado: EstadoActividadViaje.ACTIVO,
                asignaciones:
                [
                    ViajeTestData.CrearAsignacion(1, envioId: 100),
                    ViajeTestData.CrearAsignacion(2, envioId: 101)
                ]);
            _repositorioMock
                .Setup(r => r.ObtenerParaModificarAsync(10))
                .ReturnsAsync(viaje);

            var resultado = await _servicio.DesactivarAsync(10, 5);

            Assert.True(resultado.EsExitoso);
            Assert.Equal(EstadoActividadViaje.INACTIVO, viaje.Estado);
            Assert.Equal(2, resultado.PaquetesAfectados);
            _repositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Once);
        }

        [Fact]
        public async Task ActivarAsync_CuandoElViajeEstaInactivo_LoActivaYGuarda()
        {
            var viaje = ViajeTestData.CrearViaje(
                id: 10,
                usuarioId: 5,
                estado: EstadoActividadViaje.INACTIVO,
                asignaciones: [ViajeTestData.CrearAsignacion(1, envioId: 100)]);
            _repositorioMock
                .Setup(r => r.ObtenerParaModificarAsync(10))
                .ReturnsAsync(viaje);

            var resultado = await _servicio.ActivarAsync(10, 5);

            Assert.True(resultado.EsExitoso);
            Assert.Equal(EstadoActividadViaje.ACTIVO, viaje.Estado);
            Assert.Equal(1, resultado.PaquetesAfectados);
            _repositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Once);
        }

        [Fact]
        public async Task DesactivarAsync_CuandoElViajeYaEstaInactivo_NoGuardaCambios()
        {
            var viaje = ViajeTestData.CrearViaje(
                id: 10,
                usuarioId: 5,
                estado: EstadoActividadViaje.INACTIVO,
                asignaciones: [ViajeTestData.CrearAsignacion(1, envioId: 100)]);
            _repositorioMock
                .Setup(r => r.ObtenerParaModificarAsync(10))
                .ReturnsAsync(viaje);

            var resultado = await _servicio.DesactivarAsync(10, 5);

            Assert.True(resultado.EsExitoso);
            Assert.Equal(EstadoActividadViaje.INACTIVO, viaje.Estado);
            Assert.Equal(1, resultado.PaquetesAfectados);
            _repositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Never);
        }

        [Fact]
        public async Task ActivarAsync_CuandoElViajeYaEstaActivo_NoGuardaCambios()
        {
            var viaje = ViajeTestData.CrearViaje(
                id: 10,
                usuarioId: 5,
                estado: EstadoActividadViaje.ACTIVO,
                asignaciones: [ViajeTestData.CrearAsignacion(1, envioId: 100)]);
            _repositorioMock
                .Setup(r => r.ObtenerParaModificarAsync(10))
                .ReturnsAsync(viaje);

            var resultado = await _servicio.ActivarAsync(10, 5);

            Assert.True(resultado.EsExitoso);
            Assert.Equal(EstadoActividadViaje.ACTIVO, viaje.Estado);
            Assert.Equal(1, resultado.PaquetesAfectados);
            _repositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Never);
        }

        private static ViajeEdicionDTO CrearDatosEdicion(
            DateTime? salida = null,
            DateTime? fechaLlegada = null,
            double pesoMax = 30,
            int capacidad = 6)
        {
            return new ViajeEdicionDTO
            {
                FechaSalida = salida ?? ViajeTestData.FechaSalida.AddDays(1),
                FechaLlegada = fechaLlegada,
                PesoMax = pesoMax,
                CapacidadMaxPaquetes = capacidad
            };
        }
    }
}
