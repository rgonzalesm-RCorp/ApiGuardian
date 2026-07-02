using System.ComponentModel.DataAnnotations.Schema;

public class TCartera
{
    private string _empresa = string.Empty;
    public string Empresa
    {
        get => _empresa;
        set => _empresa = value?.TrimEnd() ?? string.Empty;
    }

    private string _lote = string.Empty;
    public string Lote
    {
        get => _lote;
        set => _lote = value?.TrimEnd() ?? string.Empty;
    }

    private string _docid = string.Empty;
    public string Docid
    {
        get => _docid;
        set => _docid = value?.TrimEnd() ?? string.Empty;
    }

    private string _cliente = string.Empty;
    public string Cliente
    {
        get => _cliente;
        set => _cliente = value?.TrimEnd() ?? string.Empty;
    }

    private string _docidVendedor = string.Empty;
    public string DocidVendedor
    {
        get => _docidVendedor;
        set => _docidVendedor = value?.TrimEnd() ?? string.Empty;
    }

    private string _nombre = string.Empty;
    public string Nombre
    {
        get => _nombre;
        set => _nombre = value?.TrimEnd() ?? string.Empty;
    }
    public int? Idtipoventa { get; set; }
    public int? Idproyecto { get; set; }
    public int? Idventa { get; set; }
    public decimal? Cuotainicial { get; set; }
    public decimal? Totalventa { get; set; }
    public decimal? Totaldeuda { get; set; }
    private string _fecha = string.Empty;
    public string Fecha
    {
        get => _fecha;
        set => _fecha = value?.TrimEnd() ?? string.Empty;
    }
    private string _proyecto = string.Empty;
    public string Proyecto
    {
        get => _proyecto;
        set => _proyecto = value?.TrimEnd() ?? string.Empty;
    }
    public decimal? CuotasLotesVencidas { get; set; }
    private string? _ultimoPago = null;
    public string? UltimoPago
    {
        get => _ultimoPago;
        set => _ultimoPago = value?.TrimEnd() ?? null;
    }
    private string _estado = string.Empty;
    public string Estado
    {
        get => _estado;
        set => _estado = value?.TrimEnd() ?? string.Empty;
    }
    private string _trans = string.Empty;
    public string Trans
    {
        get => _trans;
        set => _trans = value?.TrimEnd() ?? string.Empty;
    }
    private string _nit = string.Empty;
    public string Nit
    {
        get => _nit;
        set => _nit = value?.TrimEnd() ?? string.Empty;
    }
    private string _telCel = string.Empty;
    public string TelCel
    {
        get => _telCel;
        set => _telCel = value?.TrimEnd() ?? string.Empty;
    }
    private string _telefono = string.Empty;
    public string Telefono
    {
        get => _telefono;
        set => _telefono = value?.TrimEnd() ?? string.Empty;
    }
    private string _direccion = string.Empty;
    public string Direccion
    {
        get => _direccion;
        set => _direccion = value?.TrimEnd() ?? string.Empty;
    }
    private string _email = string.Empty;
    public string Email
    {
        get => _email;
        set => _email = value?.TrimEnd() ?? string.Empty;
    }
    public int? Uv { get; set; }
    private string _mzno = string.Empty;
    public string Mzno
    {
        get => _mzno;
        set => _mzno = value?.TrimEnd() ?? string.Empty;
    }
    private string _nroLote = string.Empty;
    public string NroLote
    {
        get => _nroLote;
        set => _nroLote = value?.TrimEnd() ?? string.Empty;
    }
    public decimal PrecioLista { get; set; }

    private string _ciudadResidencia = string.Empty;
    public string CiudadResidencia
    {
        get => _ciudadResidencia;
        set => _ciudadResidencia = value?.TrimEnd() ?? string.Empty;
    }
    public decimal MontoCapitalVenc { get; set; }
    public decimal MontoInteresVenc { get; set; }
    public decimal MontoMulta { get; set; }
    public decimal MontoExpensa { get; set; }
    public string? FVencMasAnt { get; set; } = null;
    public string? FUltimoVenc {get; set;} = null;
}
public class CarteraCalculoBonoResidual
{
    public string DocId { get; set; } = string.Empty; 
    public string Estado { get; set; } = string.Empty; 
    public string Cliente { get; set; } = string.Empty; 
    public string Lote { get; set; } = string.Empty; 
}
public class TCuota
{
    public string Idproducto { get; set; } = string.Empty;
    public int Idproyecto { get; set; }
    public int LComplejoId { get; set; }
    public string Proyecto { get; set; } = string.Empty;
    public string Empresa { get; set; } = string.Empty;
    public int Idrecibo { get; set; }
    public int Idventa { get; set; }
    public int Idtipopago { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public int Idcliente { get; set; }
    public string Cliente { get; set; } = string.Empty;
    public string Docidcli { get; set; } = string.Empty;
    public int Idvendedor { get; set; }
    public string Vendedor { get; set; } = string.Empty;
    public string Docidven { get; set; } = string.Empty;
    public decimal Bono { get; set; }
    public decimal Amortizacion { get; set; }
    public decimal Capital { get; set; }
    public decimal Interes { get; set; }
    public decimal Seguro { get; set; }
    public decimal Expensa { get; set; }
    public decimal Multa { get; set; }
    public DateTime Fecha_Venta { get; set; }
    public DateTime Fecha_Pago { get; set; }
    public decimal Acuenta { get; set; }
    public decimal Totalpago { get; set; }
    public decimal Montodeuda { get; set; }
    public decimal Pagosacuenta { get; set; }
    public int Nrocuota { get; set; }
}

 public class Excedente
    {
        public string Empresa { get; set; } = string.Empty;
        public int Idalmacen { get; set; }
        public int Idventa { get; set; }
        public DateTime Fechaventa { get; set; }
        public string Idproducto { get; set; }= string.Empty;
        public int Idcliente { get; set; }
        public string Ci_Cliente { get; set; }= string.Empty;
        public string Nombre_Cliente { get; set; }= string.Empty;
        public int Idvendedor { get; set; }
        public string Ci_Vendedor { get; set; }= string.Empty;
        public string Nombre_Vendedor { get; set; }= string.Empty;
        public int Idtipoventa { get; set; }
        public decimal Precioventa { get; set; }
        public decimal Cuotainicial { get; set; }
        public decimal Valor_Ci { get; set; }
        public decimal Montoabonado { get; set; }
        public decimal Totaldeuda { get; set; }
        public string Tipoventa { get; set; }= string.Empty;
        public int Idestado_Venta { get; set; }
        public int Idestado { get; set; }
        public string Glosa { get; set; }= string.Empty;
        public string Nrodoc { get; set; }= string.Empty;
        public int Kit { get; set; }
        public int Comisionable { get; set; }
        public DateTime Modfecha { get; set; }
        public string Modhora { get; set; }= string.Empty;
        public decimal Bono { set; get; }
        public int IdProyecto_Guardian { set; get; }
    }

public class BrContacto
{
    public int TmpResidualContactoId { get; set; }
    public int LContactoId { get; set; }
    public string SCedulaIdentidad { get; set; } = string.Empty;
    public string SNombreCompleto { get; set; } = string.Empty;
    public int Codigo { get; set; }
    public int LPatrocinanteId { get; set; }
}
public class BrCuotaRed
{
    public int Id { get; set; }
    public string Empresa { get; set; } = string.Empty;
    public string ProductoId { get; set; } = string.Empty;
    public int ProyectoId { get; set; }
    public string Proyecto { get; set; } = string.Empty;
    public int ReciboId { get; set; }
    public int VentaId { get; set; }
    public int TipoPagoId { get; set; }
    public string TipoPago { get; set; } = string.Empty;
    public int ClienteId { get; set; }
    public string Cliente { get; set; } = string.Empty;
    public string DocumentoCliente { get; set; } = string.Empty;
    public int VendedorId { get; set; }
    public string Vendedor { get; set; } = string.Empty;
    public string DocumentoVendedor { get; set; } = string.Empty;
    public decimal Bono { get; set; }
    public int LContactoId { get; set; } 
    public int LPatrocinado1 { get; set; }
    public int LPatrocinado2 { get; set; }
    public int LPatrocinado3 { get; set; }
    public int LPatrocinado4 { get; set; }
    public int LPatrocinado5 { get; set; }
    public int LPatrocinado6 { get; set; }
    public int LPatrocinado7 { get; set; }
}


public class BrCalculoItem
{
    public int LContactoId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public int LContactoIdHijo { get; set; }
    public string NombreCompletoHijo { get; set; } = string.Empty;
    public string DocumentoHijo { get; set; } = string.Empty;
    public string Complejo { get; set; } = string.Empty;
    public string Empresa { get; set; } = string.Empty;
    public string ProductoId { get; set; } = string.Empty;
    public int Nivel { get; set; }
    public decimal Bono { get; set; }
    public decimal BonoResidual { get; set; }
    public bool ActivoMes { get; set; }
    public decimal PorcentajeComision { get; set; }
    public int LComplejoId { get; set; }
    public int LEmpresaId { get; set; }
    public bool EstaAlDia { get; set; }
}
public class BrContactoActivos
{
    public int LContactoId { get; set; } 
}




public class AdministracionRedEmpresaComplejo
{
    public string UsuarioCreacion { get; set; } = string.Empty;

    public DateTime FechaCreacion { get; set; }

    public string UsuarioModificacion { get; set; } = string.Empty;

    public DateTime FechaModificacion { get; set; }

    public int RedEmpresaComplejoId { get; set; }

    public int CicloId { get; set; }

    public int ContactoId { get; set; }

    public int ComplejoId { get; set; }

    public decimal Monto { get; set; }
}
public class TBonoCompleto
{
    public int Id { get; set; }

    public int IbonoCompleto { get; set; }

    public DateTime Fecha { get; set; }

    public int Generacion { get; set; }

    public int Padre_lcontacto_id { get; set; }

    public int Lciclo_id { get; set; }

    public int Lcontacto_id { get; set; }

    public string CedulaIdentidad { get; set; } = string.Empty;

    public int Proyecto { get; set; }

    public decimal Bono { get; set; }

    public decimal Porcentaje { get; set; }

    public decimal Pagar { get; set; }

    public int Cantidad { get; set; }
}