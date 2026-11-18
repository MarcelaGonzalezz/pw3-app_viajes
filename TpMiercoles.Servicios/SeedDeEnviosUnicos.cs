using Microsoft.Extensions.Logging;
using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;

namespace TpMiercoles.Servicios
{
    public interface ISeedDeEnviosUnicos
    {
        Task SembrarAsync();
    }

    // Los seeds publican siempre los mismos paquetes, y algunos vuelven a publicarlos cuando los
    // anteriores vencen o se aceptan. Asi aparecen dos "Plantas" o diez "Silla de escritorio"
    // identicas en oportunidades y en ganancias. Este paso corre despues de todos los seeds y,
    // entre los envios todavia no terminados de los usuarios de prueba, deja un solo envio con
    // cada nombre: los repetidos reciben otro nombre de la misma familia (otro mueble, otra caja...).
    // Si la familia ya no tiene nombres libres, el envio conserva el nombre y se le cambia el monto
    // ($500 mas cada vez), asi ninguno queda igual a otro.
    // El envio mas nuevo conserva el nombre original, porque algunos seeds buscan por nombre
    // su ultima tanda para no volver a publicarla.
    public class SeedDeEnviosUnicos : ISeedDeEnviosUnicos
    {
        // El primero de cada familia es el nombre que usan los seeds.
        private static readonly string[][] Familias =
        [
            // SeedDeGanancias
            ["Silla de escritorio", "Sillón individual", "Escritorio plegable", "Mesa ratona", "Estantería",
             "Biblioteca chica", "Cómoda", "Silla gamer", "Banqueta alta", "Mesa de luz", "Perchero de pie",
             "Puff grande", "Mesa plegable", "Baúl de madera", "Espejo de pie", "Zapatero"],
            ["Monitor", "Impresora", "Parlante bluetooth", "Microondas chico", "Cafetera", "Ventilador de pie",
             "Proyector", "Licuadora", "Aspiradora de mano", "Teclado musical", "Router y módem", "Caloventor",
             "Tostadora", "Pava eléctrica", "Batidora", "Plancha a vapor"],
            ["Sobre con documentos", "Carpeta con planos", "Libros de texto", "Contrato firmado", "Álbum de fotos",
             "Juego de llaves", "Lentes recetados", "Cuadro chico", "Remera bordada", "Set de pinceles", "Termo",
             "Agenda y cuadernos", "Mate y bombilla", "Bufanda tejida", "Auriculares con cable", "Billetera"],
            ["Caja de zapatos", "Caja de juguetes", "Caja de vajilla", "Caja de cables", "Caja de té",
             "Caja de lápices", "Caja de vinilos", "Caja de especias", "Caja de repuestos", "Caja de golosinas",
             "Caja de cosméticos", "Caja de rompecabezas", "Caja de velas", "Caja de botones", "Caja de fotos",
             "Caja de tornillos"],

            // SeedDeOportunidades
            ["Caja de libros", "Caja de revistas", "Caja de apuntes", "Caja de historietas", "Caja de manuales"],
            ["Notebook", "Tablet", "Consola de videojuegos", "Monitor portátil", "Disco externo", "Cámara de fotos"],
            ["Plantas", "Macetas con suculentas", "Helecho en maceta", "Bonsái", "Kit de huerta", "Orquídea"],
            ["Bicicleta", "Bicicleta plegable", "Monopatín eléctrico", "Tabla de surf", "Kayak inflable", "Cinta de correr"],
            ["Heladera chica", "Freezer chico", "Lavarropas chico", "Horno eléctrico", "Aire portátil"],
            ["Documentos", "Escrituras", "Facturas", "Expediente", "Certificados"]
        ];

        private static readonly string[] EmailsDePrueba =
        [
            SeedDeDesarrollo.EmailConductor,
            SeedDeDesarrollo.EmailOtroConductor,
            SeedDeDesarrollo.EmailCliente
        ];

        private readonly IUsuarioRepositorio _usuarios;
        private readonly IEnvioRepositorio _envios;
        private readonly ILogger<SeedDeEnviosUnicos> _logger;

        public SeedDeEnviosUnicos(
            IUsuarioRepositorio usuarios,
            IEnvioRepositorio envios,
            ILogger<SeedDeEnviosUnicos> logger)
        {
            _usuarios = usuarios;
            _envios = envios;
            _logger = logger;
        }

        public async Task SembrarAsync()
        {
            try
            {
                List<Envio> abiertos = [];

                foreach (string email in EmailsDePrueba)
                {
                    Usuario? usuario = await _usuarios.BuscarPorEmailAsync(email);
                    if (usuario is null)
                    {
                        continue;
                    }

                    abiertos.AddRange((await _envios.ListarPorUsuarioAsync(usuario.Id, null))
                        .Where(e => e.Estado != EstadoEnvio.Entregado && e.Estado != EstadoEnvio.Cancelado));
                }

                HashSet<string> usados = new(
                    abiertos.Select(e => Clave(e.Paquete.Descripcion)));

                // Nombre + monto de cada envio, para que el que conserva el nombre no repita monto.
                HashSet<(string Nombre, decimal Monto)> combinaciones = new(
                    abiertos.Select(e => (Clave(e.Paquete.Descripcion), e.Monto)));

                // Los repetidos, sin contar al mas nuevo de cada nombre (ese queda como esta).
                List<Envio> aRenombrar = abiertos
                    .GroupBy(e => Clave(e.Paquete.Descripcion))
                    .Where(g => g.Count() > 1)
                    .SelectMany(g => g.OrderByDescending(e => e.Id).Skip(1))
                    .OrderBy(e => e.FechaHasta)
                    .ThenBy(e => e.Id)
                    .ToList();

                foreach (Envio repetido in aRenombrar)
                {
                    Envio? editable = await _envios.ObtenerParaModificarAsync(repetido.Id);
                    if (editable is null)
                    {
                        continue;
                    }

                    string? nuevoNombre = NombreLibreDeLaFamilia(repetido.Paquete.Descripcion, usados);

                    if (nuevoNombre is not null)
                    {
                        editable.Paquete.Descripcion = nuevoNombre;
                        usados.Add(Clave(nuevoNombre));
                        combinaciones.Add((Clave(nuevoNombre), editable.Monto));
                    }
                    else
                    {
                        string nombre = Clave(editable.Paquete.Descripcion);
                        decimal monto = editable.Monto;
                        while (combinaciones.Contains((nombre, monto)))
                        {
                            monto += IncrementoDeMonto;
                        }

                        editable.Monto = monto;
                        combinaciones.Add((nombre, monto));
                    }
                }

                if (aRenombrar.Count > 0)
                {
                    await _envios.GuardarCambiosAsync();
                    _logger.LogInformation("Seed: se corrigieron {Cantidad} envíos repetidos.", aRenombrar.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al dejar unicos los envíos de prueba.");
            }
        }

        private const decimal IncrementoDeMonto = 500m;

        // Otro nombre de la misma familia que todavia no use nadie, o null si la familia
        // se agoto (o el nombre no es de ninguna): en ese caso se cambia el monto.
        private static string? NombreLibreDeLaFamilia(string original, HashSet<string> usados)
        {
            string[]? familia = Familias.FirstOrDefault(f => f.Any(n => Clave(n) == Clave(original)));

            return familia?.FirstOrDefault(n => !usados.Contains(Clave(n)));
        }

        private static string Clave(string nombre) => nombre.Trim().ToLowerInvariant();
    }
}
