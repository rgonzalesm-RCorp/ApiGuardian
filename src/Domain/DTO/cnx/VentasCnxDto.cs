public class ItemVentaCnx
{
    public int EmpresaId { get; set; }

    public int LContratoId { get; set; }
    public DateTime DFecha { get; set; }
    public string? SManzano { get; set; }
    public string? Empresa { get; set; }
    public string? SLote { get; set; }
    public decimal DPrecio { get; set; }
    public int LComplejoId { get; set; }
    public int IdVenta { get; set; }
    public string Lote { get; set; } = string.Empty;
    public string? SUV { get; set; }
    public decimal PrecioInicial { get; set; }
    public decimal SCuotaInicial { get; set; }
    public decimal SCuotaInicialOriginal { get; set; }
    public int IdCliente { get; set; }
    public string TelefonoFijo { get; set; } = string.Empty;
    public string TelefonoMovil { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public DateTime FechaNacimiento { get; set; }
    public string Direccion { get; set; } = string.Empty;
    public int IdPaisResidencia { get; set; }
    public string? SCedulaIdentidad { get; set; }
    public string? SCiudad { get; set; }
    public DateTime FechaRegistro { get; set; }
    public string SNombreCompleto { get; set; } = string.Empty;
    public string STelefonoOficina { get; set; } = string.Empty;
    public string SContrasena { get; set; } = string.Empty;

     public int VendedorId { get; set; }
    public string TelefonoFijoVendedor { get; set; } = string.Empty;
    public string TelefonoMovilVendedor { get; set; } = string.Empty;
    public string CorreoVendedor { get; set; } = string.Empty;
    public DateTime FechaNacimientoVendedor { get; set; }
    public string DireccionVendedor { get; set; } = string.Empty;
    public int IdPaisResidenciaVendedor { get; set; }
    public string? SCedulaIdentidadVendedor { get; set; }
    public DateTime FechaRegistroVendedor { get; set; }
    public string SNombreCompletoVendedor { get; set; } = string.Empty;
    public string STelefonoOficinaVendedor { get; set; } = string.Empty;
    public string SContrasenaVendedor { get; set; } = string.Empty;
    public string? SCiudadVendedor { get; set; }

    public string? Complejo { get; set; }
    public int TipoVenta { get; set; }
    public decimal PorcentajeCuotaInicial { get; set; }

    public int EstadoVentaRezagadasCicloId { get; set; } = 1;
    public DateTime FechaRegistroGrd { get; set; } = DateTime.Now;
    public decimal ValorCi { get; set; }
    public string SeccionId { get; set; } = "";
    public string Glosa { get; set; } = "";
    public string NombreTipoComision { get; set; } = "";
    public int TipoComisionable { get; set; } = 0;
    
}
public class RequestProcesoPrincipal
{
    public string Tipo { get; set; } = string.Empty;
    public bool Rezagada { get; set; } = false;
    public string Paso { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public int LCicloId { get; set; } = 0;
}
public class RequestSaveVtaPersonal
{
    public List<VentaPersonalComisionDto> ListaComision { get; set; } = new List<VentaPersonalComisionDto>();
    public int LCicloId { get; set; }
    public string Usuario { get; set; } = string.Empty; 
}
public class RequestGuardarVentaGRD
{
    public List<ItemVentaCnx> ListaSeleccionado { get; set; } = new List<ItemVentaCnx>();
    public List<ItemVentaCnx> NoListaSeleccionado { get; set; } = new List<ItemVentaCnx>();
    public string Usuario { get; set; } = string.Empty;
    public bool Rezagada { get; set; } = false;
    public int LCicloId { get; set; } = 0;
    public bool EsEspecial { get; set; } = false;
}
public class RequestGuardarVentaGrupo
{
    public List<ItemComisionVentaGrupoDto> ListaComision { get; set; } = new List<ItemComisionVentaGrupoDto>();
    public int LCicloId { get; set; }
    public string Usuario { get; set; } = string.Empty; 
}
