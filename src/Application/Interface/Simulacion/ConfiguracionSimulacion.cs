namespace ApiGuardian.Application.Interfaces.Simulacion;

// Sección "Simulacion" de appsettings.json. Los valores se leen, no se codifican en el motor.
public class ConfiguracionSimulacion
{
    public string ZonaHoraria { get; set; } = "America/La_Paz";
    public string NombreTablaEjecucion { get; set; } = "comisiones_light_ejecucion";
    public string UsuarioCron { get; set; } = "cron";

    // D-25: una corrida EN_CURSO más vieja que esto se considera abandonada y se cierra como ERROR.
    public int HorasMaximasEnCurso { get; set; } = 12;

    // Disparo manual por HTTP (POST api/simulacion/ejecutar). Desactivado por defecto: la API no tiene autenticacion.
    public bool HabilitarEndpointManual { get; set; } = false;
}
