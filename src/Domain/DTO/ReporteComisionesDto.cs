namespace ApiGuardian.Models
{
    public class VentaItem
    {
        public string? Fecha { get; set; }
        public string? NumeroVenta { get; set; }
        public string? Tipo { get; set; }
        public decimal CuotaInicial { get; set; }
        public decimal Porcentaje { get; set; }
        public decimal Comision { get; set; }
    }

    public class VentaGrupoItem
    {
        public string? Asesor { get; set; }
        public string? BasadoEn { get; set; }
        public string? Generacion { get; set; }
        public string? Tipo { get; set; }

        public decimal VentaPersonal { get; set; }
        public decimal PorcentajeComision { get; set; }
        public decimal Comision { get; set; }
    }
    public class BonoRedisualItem
    {
        public int Tipo { get; set; }
        public decimal PocentajeGeneracion { get; set; }
        public decimal Bxl { get; set; }
        public decimal Terrenos { get; set; }
        public decimal Total { get; set; }
    }
    public class BonoLiderazgoItem
    {
        public int Cantidad { get; set; }
        public decimal Comision { get; set; }
    }
    public class Encabezado
    {
        public string? NombreCompleto { get; set; }
        public string? Ciclo { get; set; }
        public DateTime Inicio { get; set; }
        public DateTime Fin { get; set; }
        public int MesActividad { get; set; }
    }
    public class BonoCarrera
    {
        public string? NivelCiclo { get; set; }
        public decimal CantidadVentas { get; set; }
    }
    public class ReporteComisionesDto
    {
        public string? NombreAsesor { get; set; }
        public string? CodigoAsesor { get; set; }
        public string? Mes { get; set; }
        public string? RangoFecha { get; set; }
        public int MesesActividad { get; set; }
        public Encabezado Encabezado { get; set; } = new Encabezado();
        public IEnumerable<VentaItem> VentasPersonales { get; set; } = new List<VentaItem>();
        public IEnumerable<VentaGrupoItem> VentasGrupo { get; set; }  = new List<VentaGrupoItem>();
        public IEnumerable<BonoRedisualItem> BonoRedisual { get; set; }  = new List<BonoRedisualItem>();
        public IEnumerable<BonoLiderazgoItem> BonoLiderazgo { get; set; }  = new List<BonoLiderazgoItem>();
        public IEnumerable<BonoCarrera> BonoCarrera { get; set; }  = new List<BonoCarrera>();
        public DataComision Comisiones { get; set; } = new DataComision();
    }
}
