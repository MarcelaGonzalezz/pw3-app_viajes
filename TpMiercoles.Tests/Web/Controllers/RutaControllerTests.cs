using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using System.Security.Claims;
using TpMiercoles.Entidades;
using TpMiercoles.Entidades.Excepciones;
using TpMiercoles.Servicios;
using TpMiercoles.Servicios.Excepciones;
using TpMiercoles.Servicios.DTOs;
using TpMiercoles.Servicios.DTOs.Ruta;
using TpMiercoles.Servicios.DTOs.Direccion;
using TpMiercoles.Tests.Helpers;
using TpMiercoles.Web.Controllers;
using TpMiercoles.Web.Infraestructura;
using TpMiercoles.Web.Models;
using Xunit;

namespace TpMiercoles.Tests.Controllers; 

public class RutaControllerTests
{
    private const int UsuarioId = 7;
    private const string NombreUsuario = "Juan Perez";

    private readonly IMapper _mapper;
    private readonly Mock<IRutaServicio> _servicioMock;
    private readonly Mock<ISesionWeb> _sesionMock;
    private readonly RutaController _controller;

    public RutaControllerTests()
    {
        _mapper = MapperFactory.Crear();
        _servicioMock = new Mock<IRutaServicio>();
        _sesionMock = new Mock<ISesionWeb>();

        _sesionMock
            .Setup(s => s.UsuarioIdRequerido)
            .Returns(UsuarioId);
        _servicioMock
            .Setup(s => s.CrearRutaAsync(It.IsAny<int>(), It.IsAny<CrearRutaDTO>()))
            .Returns(Task.CompletedTask);
        _servicioMock
            .Setup(s => s.ObtenerRutasActivasDeUnUsuarioAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<RutaDTO>());

        var httpContext = new DefaultHttpContext { User = CrearUsuarioAutenticado() };
        _controller = new RutaController(_mapper, _servicioMock.Object, _sesionMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            },
            TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
        };
    }

    [Fact]
    public void Controller_TieneAtributoAuthorize()
    {
        Assert.True(Attribute.IsDefined(typeof(RutaController), typeof(AuthorizeAttribute)));
    }

    [Fact]
    public void Crear_Get_DevuelveVistaConFechaInicioEnHoy()
    {
        var resultado = _controller.Crear();

        var vista = Assert.IsType<ViewResult>(resultado);
        var modelo = Assert.IsType<CrearRutaViewModel>(vista.Model);
        Assert.Equal(DateTime.Today, modelo.FechaInicio);
    }

    [Fact]
    public async Task Crear_Post_CuandoModelStateEsInvalido_DevuelveVistaSinCrear()
    {
        _controller.ModelState.AddModelError(nameof(CrearRutaViewModel.PesoMax), "El peso debe ser mayor que cero.");
        var modelo = CrearRutaValida();

        var resultado = await _controller.Crear(modelo);

        var vista = Assert.IsType<ViewResult>(resultado);
        Assert.Same(modelo, vista.Model);
        _servicioMock.Verify(
            s => s.CrearRutaAsync(It.IsAny<int>(), It.IsAny<CrearRutaDTO>()),
            Times.Never);
    }

    [Fact]
    public async Task Crear_Post_CuandoDatosSonValidos_CreaLaRutaYRedirigeAlHome()
    {
        var modelo = CrearRutaValida();
        int? usuarioIdEnviado = null;
        CrearRutaDTO? dtoEnviado = null;

        _servicioMock
            .Setup(s => s.CrearRutaAsync(It.IsAny<int>(), It.IsAny<CrearRutaDTO>()))
            .Callback<int, CrearRutaDTO>((id, dto) =>
            {
                usuarioIdEnviado = id;
                dtoEnviado = dto;
            })
            .Returns(Task.CompletedTask);

        var resultado = await _controller.Crear(modelo);

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Index", redireccion.ActionName);
        Assert.Equal("Home", redireccion.ControllerName);

        Assert.Equal(UsuarioId, usuarioIdEnviado);
        Assert.NotNull(dtoEnviado);
        Assert.Equal(modelo.DireccionOrigen.Texto, dtoEnviado!.DireccionOrigen.Texto);
        Assert.Equal(modelo.DireccionOrigen.Latitud, dtoEnviado.DireccionOrigen.Latitud);
        Assert.Equal(modelo.DireccionOrigen.Longitud, dtoEnviado.DireccionOrigen.Longitud);
        Assert.Equal(modelo.DireccionDestino.Texto, dtoEnviado.DireccionDestino.Texto);
        Assert.Equal(modelo.DireccionDestino.Latitud, dtoEnviado.DireccionDestino.Latitud);
        Assert.Equal(modelo.DireccionDestino.Longitud, dtoEnviado.DireccionDestino.Longitud);
        Assert.Equal(modelo.PesoMax, dtoEnviado.PesoMax);
        Assert.Equal(modelo.CapacidadMaxPaquetes, dtoEnviado.CapacidadMaxPaquetes);
        Assert.Equal(modelo.FechaInicio, dtoEnviado.FechaInicio);
        Assert.Equal(modelo.FechaFin, dtoEnviado.FechaFin);
        Assert.Equal(modelo.HoraSalida, dtoEnviado.HoraSalida);
        Assert.Equal(modelo.HoraLlegada, dtoEnviado.HoraLlegada);
        Assert.Equal(
            modelo.RutaDias,
            dtoEnviado.RutaDias.Select(d => d.DiaSemana).ToList());
        var parada = Assert.Single(dtoEnviado.Paradas);
        Assert.Equal(1, parada.Orden);
        Assert.Equal("Parada 1", parada.Direccion.Texto);

        _servicioMock.Verify(
            s => s.CrearRutaAsync(UsuarioId, It.IsAny<CrearRutaDTO>()),
            Times.Once);
    }

    [Fact]
    public async Task Crear_Post_CuandoLaValidacionDeRutaFalla_MuestraElMensajeDeError()
    {
        var modelo = CrearRutaValida();
        const string mensaje = "El origen y el destino deben ser diferentes.";

        _servicioMock
            .Setup(s => s.CrearRutaAsync(It.IsAny<int>(), It.IsAny<CrearRutaDTO>()))
            .ThrowsAsync(new ValidacionRutaException(mensaje));

        var resultado = await _controller.Crear(modelo);

        var vista = Assert.IsType<ViewResult>(resultado);
        Assert.Same(modelo, vista.Model);
        Assert.Contains(
            _controller.ModelState.Values.SelectMany(v => v.Errors),
            error => error.ErrorMessage == mensaje);
    }

    [Fact]
    public async Task Obtener_DevuelveVistaConLasRutasYElNombreDelUsuario()
    {
        var rutasDTO = new List<RutaDTO> { CrearRutaDTOValida() };

        _servicioMock
            .Setup(s => s.ObtenerRutasDeUnUsuarioAsync(UsuarioId))
            .ReturnsAsync(rutasDTO);

        var resultado = await _controller.Obtener();

        var vista = Assert.IsType<ViewResult>(resultado);
        var modelo = Assert.IsType<ObtenerRutasViewModel>(vista.Model);
        Assert.Equal(NombreUsuario, modelo.NombreUsuario);

        var ruta = Assert.Single(modelo.Rutas);
        var origen = rutasDTO[0];
        Assert.Equal(origen.Id, ruta.Id);
        Assert.Equal(origen.DireccionOrigen.Texto, ruta.DireccionOrigen.Texto);
        Assert.Equal(origen.DireccionDestino.Texto, ruta.DireccionDestino.Texto);
        Assert.Equal(origen.PesoMax, ruta.PesoMax);
        Assert.Equal(origen.CapacidadMaxPaquetes, ruta.CapacidadMaxPaquetes);
        Assert.Equal(origen.FechaInicio, ruta.FechaInicio);
        Assert.Equal(origen.FechaFin, ruta.FechaFin);
        Assert.Equal(origen.HoraSalida, ruta.HoraSalida);
        Assert.Equal(origen.HoraLlegada, ruta.HoraLlegada);
        Assert.Equal(origen.Estado, ruta.Estado);
        Assert.Equal(
            new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Friday },
            ruta.RutaDias);
        var parada = Assert.Single(ruta.Paradas);
        Assert.Equal(1, parada.Orden);
        Assert.Equal("Parada 1", parada.Direccion.Texto);

        _servicioMock.Verify(
            s => s.ObtenerRutasDeUnUsuarioAsync(UsuarioId),
            Times.Once);
    }

    [Fact]
    public async Task Obtener_CuandoElUsuarioNoTieneRutas_DevuelveListaVacia()
    {
        var resultado = await _controller.Obtener();

        var vista = Assert.IsType<ViewResult>(resultado);
        var modelo = Assert.IsType<ObtenerRutasViewModel>(vista.Model);
        Assert.Empty(modelo.Rutas);
        Assert.Equal(NombreUsuario, modelo.NombreUsuario);
    }

    [Fact]
    public async Task Desactivar_CuandoOperacionExitosa_RedirigeAObtenerConMensaje()
    {
        const int rutaId = 10;
        _servicioMock
            .Setup(s => s.DesactivarRutaAsync(rutaId, UsuarioId))
            .Returns(Task.CompletedTask);

        var resultado = await _controller.Desactivar(rutaId);

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal(nameof(RutaController.Obtener), redireccion.ActionName);
        Assert.Equal("La ruta fue desactivada correctamente. Los viajes comprometidos siguen en pie.", _controller.TempData["Mensaje"]);
        _servicioMock.Verify(s => s.DesactivarRutaAsync(rutaId, UsuarioId), Times.Once);
    }

    [Fact]
    public async Task Desactivar_CuandoVolverADetalleEsTrue_RedirigeADetalle()
    {
        const int rutaId = 10;
        _servicioMock
            .Setup(s => s.DesactivarRutaAsync(rutaId, UsuarioId))
            .Returns(Task.CompletedTask);

        var resultado = await _controller.Desactivar(rutaId, volverADetalle: true);

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal(nameof(RutaController.Detalle), redireccion.ActionName);
        Assert.NotNull(redireccion.RouteValues);
        Assert.Equal(rutaId, redireccion.RouteValues["id"]);
        Assert.Equal("La ruta fue desactivada correctamente. Los viajes comprometidos siguen en pie.", _controller.TempData["Mensaje"]);
        _servicioMock.Verify(s => s.DesactivarRutaAsync(rutaId, UsuarioId), Times.Once);
    }

    [Fact]
    public async Task Desactivar_CuandoRutaNoExiste_DevuelveNotFound()
    {
        const int rutaId = 99;
        _servicioMock
            .Setup(s => s.DesactivarRutaAsync(rutaId, UsuarioId))
            .ThrowsAsync(new RutaNoEncontradaException("La ruta no fue encontrada."));

        var resultado = await _controller.Desactivar(rutaId);

        Assert.IsType<NotFoundResult>(resultado);
        _servicioMock.Verify(s => s.DesactivarRutaAsync(rutaId, UsuarioId), Times.Once);
    }

    [Fact]
    public async Task Desactivar_CuandoUsuarioNoTienePermisos_DevuelveForbid()
    {
        const int rutaId = 10;
        _servicioMock
            .Setup(s => s.DesactivarRutaAsync(rutaId, UsuarioId))
            .ThrowsAsync(new UnauthorizedAccessException("No tienes permisos para modificar esta ruta."));

        var resultado = await _controller.Desactivar(rutaId);

        Assert.IsType<ForbidResult>(resultado);
        _servicioMock.Verify(s => s.DesactivarRutaAsync(rutaId, UsuarioId), Times.Once);
    }

    [Fact]
    public async Task Activar_CuandoOperacionExitosa_RedirigeAObtenerConMensaje()
    {
        const int rutaId = 10;
        _servicioMock
            .Setup(s => s.ActivarRutaAsync(rutaId, UsuarioId))
            .Returns(Task.CompletedTask);

        var resultado = await _controller.Activar(rutaId);

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal(nameof(RutaController.Obtener), redireccion.ActionName);
        Assert.Equal("La ruta fue reactivada correctamente y volverá a buscar oportunidades.", _controller.TempData["Mensaje"]);
        _servicioMock.Verify(s => s.ActivarRutaAsync(rutaId, UsuarioId), Times.Once);
    }

    [Fact]
    public async Task Editar_Get_CuandoRutaExisteYEsDelUsuario_DevuelveVistaConModelo()
    {
        const int rutaId = 10;
        var dto = CrearEditarRutaDTOValida(rutaId);
        _servicioMock
            .Setup(s => s.ObtenerParaEditarAsync(rutaId, UsuarioId))
            .ReturnsAsync(dto);

        var resultado = await _controller.Editar(rutaId);

        var vista = Assert.IsType<ViewResult>(resultado);
        var modelo = Assert.IsType<EditarRutaViewModel>(vista.Model);
        Assert.Equal(rutaId, modelo.Id);
        Assert.Equal(dto.DireccionOrigen.Texto, modelo.DireccionOrigen.Texto);
        Assert.Equal(dto.DireccionDestino.Texto, modelo.DireccionDestino.Texto);
        Assert.Equal(dto.PesoMax, modelo.PesoMax);
        Assert.Equal(dto.CapacidadMaxPaquetes, modelo.CapacidadMaxPaquetes);
        Assert.Equal(dto.FechaInicio, modelo.FechaInicio);
        Assert.Equal(dto.FechaFin, modelo.FechaFin);
        Assert.Equal(dto.HoraSalida, modelo.HoraSalida);
        Assert.Equal(dto.HoraLlegada, modelo.HoraLlegada);
        _servicioMock.Verify(s => s.ObtenerParaEditarAsync(rutaId, UsuarioId), Times.Once);
    }

    [Fact]
    public async Task Editar_Get_CuandoRutaNoExiste_DevuelveNotFound()
    {
        const int rutaId = 99;
        _servicioMock
            .Setup(s => s.ObtenerParaEditarAsync(rutaId, UsuarioId))
            .ThrowsAsync(new RutaNoEncontradaException("La ruta no fue encontrada."));

        var resultado = await _controller.Editar(rutaId);

        Assert.IsType<NotFoundResult>(resultado);
        _servicioMock.Verify(s => s.ObtenerParaEditarAsync(rutaId, UsuarioId), Times.Once);
    }

    [Fact]
    public async Task Editar_Get_CuandoUsuarioNoTienePermisos_DevuelveForbid()
    {
        const int rutaId = 10;
        _servicioMock
            .Setup(s => s.ObtenerParaEditarAsync(rutaId, UsuarioId))
            .ThrowsAsync(new UnauthorizedAccessException("No tienes permisos para ver esta ruta."));

        var resultado = await _controller.Editar(rutaId);

        Assert.IsType<ForbidResult>(resultado);
        _servicioMock.Verify(s => s.ObtenerParaEditarAsync(rutaId, UsuarioId), Times.Once);
    }

    [Fact]
    public async Task Editar_Post_CuandoModelStateEsInvalido_DevuelveVistaSinModificar()
    {
        const int rutaId = 10;
        var modelo = CrearEditarRutaValida(rutaId);
        _controller.ModelState.AddModelError(nameof(EditarRutaViewModel.PesoMax), "El peso debe ser mayor que cero.");

        var resultado = await _controller.Editar(rutaId, modelo);

        var vista = Assert.IsType<ViewResult>(resultado);
        Assert.Same(modelo, vista.Model);
        _servicioMock.Verify(
            s => s.EditarRutaAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<EditarRutaDTO>()),
            Times.Never);
    }

    [Fact]
    public async Task Editar_Post_CuandoOperacionExitosa_RedirigeADetalleConMensaje()
    {
        const int rutaId = 10;
        var modelo = CrearEditarRutaValida(rutaId);
        _servicioMock
            .Setup(s => s.EditarRutaAsync(UsuarioId, rutaId, It.IsAny<EditarRutaDTO>()))
            .Returns(Task.CompletedTask);

        var resultado = await _controller.Editar(rutaId, modelo);

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal(nameof(RutaController.Detalle), redireccion.ActionName);
        Assert.NotNull(redireccion.RouteValues);
        Assert.Equal(rutaId, redireccion.RouteValues["id"]);
        Assert.Equal("La ruta fue modificada exitosamente.", _controller.TempData["Mensaje"]);
        _servicioMock.Verify(s => s.EditarRutaAsync(UsuarioId, rutaId, It.IsAny<EditarRutaDTO>()), Times.Once);
    }

    [Fact]
    public async Task Editar_Post_CuandoRutaNoExiste_DevuelveNotFound()
    {
        const int rutaId = 99;
        var modelo = CrearEditarRutaValida(rutaId);
        _servicioMock
            .Setup(s => s.EditarRutaAsync(UsuarioId, rutaId, It.IsAny<EditarRutaDTO>()))
            .ThrowsAsync(new RutaNoEncontradaException("La ruta no fue encontrada."));

        var resultado = await _controller.Editar(rutaId, modelo);

        Assert.IsType<NotFoundResult>(resultado);
        _servicioMock.Verify(s => s.EditarRutaAsync(UsuarioId, rutaId, It.IsAny<EditarRutaDTO>()), Times.Once);
    }

    [Fact]
    public async Task Editar_Post_CuandoUsuarioNoTienePermisos_DevuelveForbid()
    {
        const int rutaId = 10;
        var modelo = CrearEditarRutaValida(rutaId);
        _servicioMock
            .Setup(s => s.EditarRutaAsync(UsuarioId, rutaId, It.IsAny<EditarRutaDTO>()))
            .ThrowsAsync(new UnauthorizedAccessException("No tienes permisos para modificar esta ruta."));

        var resultado = await _controller.Editar(rutaId, modelo);

        Assert.IsType<ForbidResult>(resultado);
        _servicioMock.Verify(s => s.EditarRutaAsync(UsuarioId, rutaId, It.IsAny<EditarRutaDTO>()), Times.Once);
    }

    [Fact]
    public async Task Editar_Post_CuandoFallaValidacionDeNegocio_DevuelveVistaConError()
    {
        const int rutaId = 10;
        var modelo = CrearEditarRutaValida(rutaId);
        const string error = "El origen y el destino deben ser diferentes.";
        _servicioMock
            .Setup(s => s.EditarRutaAsync(UsuarioId, rutaId, It.IsAny<EditarRutaDTO>()))
            .ThrowsAsync(new ValidacionRutaException(error));

        var resultado = await _controller.Editar(rutaId, modelo);

        var vista = Assert.IsType<ViewResult>(resultado);
        Assert.Same(modelo, vista.Model);
        Assert.Contains(
            _controller.ModelState.Values.SelectMany(v => v.Errors),
            e => e.ErrorMessage == error);
        _servicioMock.Verify(s => s.EditarRutaAsync(UsuarioId, rutaId, It.IsAny<EditarRutaDTO>()), Times.Once);
    }


    private static ClaimsPrincipal CrearUsuarioAutenticado()
    {
        var identidad = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Name, NombreUsuario) },
            authenticationType: "TestAuth");
        return new ClaimsPrincipal(identidad);
    }

    private static CrearRutaViewModel CrearRutaValida() => new()
    {
        DireccionOrigen = new DireccionViewModel { Texto = "Origen 123", Latitud = -34.60m, Longitud = -58.38m },
        DireccionDestino = new DireccionViewModel { Texto = "Destino 456", Latitud = -34.70m, Longitud = -58.50m },
        PesoMax = 25.5,
        CapacidadMaxPaquetes = 10,
        FechaInicio = new DateTime(2026, 11, 1),
        FechaFin = new DateTime(2026, 12, 1),
        HoraSalida = new TimeOnly(8, 0),
        HoraLlegada = new TimeOnly(12, 30),
        RutaDias = [DayOfWeek.Monday, DayOfWeek.Wednesday],
        Paradas =
        [
            new ParadaViewModel
            {
                Orden = 1,
                Direccion = new DireccionViewModel { Texto = "Parada 1", Latitud = -34.65m, Longitud = -58.44m }
            }
        ]
    };

    private static RutaDTO CrearRutaDTOValida() => new()
    {
        Id = 1,
        DireccionOrigen = new DireccionDTO { Texto = "Origen 123", Latitud = -34.60m, Longitud = -58.38m },
        DireccionDestino = new DireccionDTO { Texto = "Destino 456", Latitud = -34.70m, Longitud = -58.50m },
        PesoMax = 25.5,
        CapacidadMaxPaquetes = 10,
        FechaInicio = new DateTime(2026, 11, 1),
        FechaFin = new DateTime(2026, 12, 1),
        HoraSalida = new TimeOnly(8, 0),
        HoraLlegada = new TimeOnly(12, 30),
        Estado = EstadoRuta.ACTIVA,
        RutaDias =
        [
            new RutaDiaDTO { DiaSemana = DayOfWeek.Monday },
            new RutaDiaDTO { DiaSemana = DayOfWeek.Friday }
        ],
        Paradas =
        [
            new ParadaDTO
            {
                Orden = 1,
                Direccion = new DireccionDTO { Texto = "Parada 1", Latitud = -34.65m, Longitud = -58.44m }
            }
        ]
    };

    private static EditarRutaViewModel CrearEditarRutaValida(int id = 10) => new()
    {
        Id = id,
        DireccionOrigen = new DireccionViewModel { Texto = "Origen 123", Latitud = -34.60m, Longitud = -58.38m },
        DireccionDestino = new DireccionViewModel { Texto = "Destino 456", Latitud = -34.70m, Longitud = -58.50m },
        PesoMax = 25.5,
        CapacidadMaxPaquetes = 10,
        FechaInicio = new DateTime(2026, 11, 1),
        FechaFin = new DateTime(2026, 12, 1),
        HoraSalida = new TimeOnly(8, 0),
        HoraLlegada = new TimeOnly(12, 30),
        RutaDias = [DayOfWeek.Monday, DayOfWeek.Wednesday],
        Paradas =
        [
            new ParadaViewModel
            {
                Orden = 1,
                Direccion = new DireccionViewModel { Texto = "Parada 1", Latitud = -34.65m, Longitud = -58.44m }
            }
        ]
    };

    private static EditarRutaDTO CrearEditarRutaDTOValida(int id = 10) => new()
    {
        Id = id,
        DireccionOrigen = new DireccionDTO { Texto = "Origen 123", Latitud = -34.60m, Longitud = -58.38m },
        DireccionDestino = new DireccionDTO { Texto = "Destino 456", Latitud = -34.70m, Longitud = -58.50m },
        PesoMax = 25.5,
        CapacidadMaxPaquetes = 10,
        FechaInicio = new DateTime(2026, 11, 1),
        FechaFin = new DateTime(2026, 12, 1),
        HoraSalida = new TimeOnly(8, 0),
        HoraLlegada = new TimeOnly(12, 30),
        RutaDias =
        [
            new RutaDiaDTO { DiaSemana = DayOfWeek.Monday },
            new RutaDiaDTO { DiaSemana = DayOfWeek.Wednesday }
        ],
        Paradas =
        [
            new ParadaDTO
            {
                Orden = 1,
                Direccion = new DireccionDTO { Texto = "Parada 1", Latitud = -34.65m, Longitud = -58.44m }
            }
        ]
    };

}
