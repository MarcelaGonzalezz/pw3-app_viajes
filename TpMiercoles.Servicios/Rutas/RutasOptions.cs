using System;
using System.Collections.Generic;
using System.Text;

namespace TpMiercoles.Servicios.Rutas {
    public class RutasOptions {
        public const string Seccion = "Rutas";// el nombre del bloque en la config de appsetting.json o en los secretos de usuario
        //constante para no escribir el texto en Program.cs
        public string? OrsApiKey { get; set; }//APIKey de ORS, puede no existir. si no existe se activa el mock

        public string OrsBaseUrl { get; set; } = "https://api.openrouteservice.org/";

        public double MockFactorCalle { get; set; } = 1.3;//el mock calcula en linea recta, por calle se le agrega un 30% mas

        public double MockVelocidadKmh { get; set; } = 40;//velocidad mock promedio para calcular la duracion estimada del viaje

        public bool UsarOpenRouteService => !string.IsNullOrWhiteSpace(OrsApiKey);
        // decide que implementacion usar, es true si hay apikey
    }
}
