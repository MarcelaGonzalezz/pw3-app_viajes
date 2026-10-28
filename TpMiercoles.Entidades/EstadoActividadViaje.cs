namespace TpMiercoles.Entidades
{
    // Estado de actividad de un Viaje (columna "estado" del ER).
    // Un viaje INACTIVO queda dado de baja logicamente y deja de buscar oportunidades.
    public enum EstadoActividadViaje
    {
        ACTIVO = 0,
        INACTIVO = 1
    }
}
