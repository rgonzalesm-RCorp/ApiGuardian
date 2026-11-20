using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Api.Services.Simulacion;

// F7-T1 y F7-T2: Bono Residual (COMISION RESIDUAL). Lógica copiada de BonoResidualService.ConstruirListadoResidualAsync
// y GuardarBonoResidualAsync, sin control de proceso (R5) ni endpoints (R10).
public sealed partial class SimulacionService
{
    public async Task<(bool Exito, string Mensaje)> BonoResidualAsync(string logId, string usuario, int lCicloId, string inicio, string fin)
    {
        var datos = await _repositorio.GetDatosCalculoResidualAsync(logId, lCicloId, inicio, fin);
        if (!datos.Success)
            return (false, datos.Mensaje);

        var habilitaciones = await _habilitacionLectura.GetHabilitaciones(logId, usuario, lCicloId);
        if (!habilitaciones.Success)
            return (false, habilitaciones.Mensaje);

        var configuracion = await _brConfiguracionLectura.GetConfiguracion(logId, usuario);
        if (!configuracion.Success)
            return (false, configuracion.Mensaje);

        // Solo simulación: no se consulta la cartera (sin filtro de vencidos) y no se exige estar activo.
        var listado = ConstruirListadoResidual(lCicloId, datos.Cuotas, datos.Contactos, habilitaciones.Data, configuracion.Data);

        if (listado.Count == 0)
            return (true, "No existen registros habilitados para generar bono residual.");

        var bonoCompleto = listado
            .GroupBy(x => new { x.Nivel, x.LContactoId, x.LContactoIdHijo, x.DocumentoHijo, x.LComplejoId })
            .Select(g => new ItemBonoCompleto
            {
                Id = 0,
                Nivel = g.Key.Nivel,
                LContactoId = g.Key.LContactoId,
                LContactoIdHijo = g.Key.LContactoIdHijo,
                DocumentoHijo = g.Key.DocumentoHijo,
                LComplejoId = g.Key.LComplejoId,
                TotalBono = g.Sum(x => x.Bono),
                TotalPago = g.Sum(x => x.BonoResidual),
                Cantidad = g.Count(),
                LCicloId = lCicloId,
            })
            .ToList();

        var redEmpresaComplejo = listado
            .GroupBy(x => new { x.LContactoId, x.LComplejoId, x.Complejo, x.Empresa })
            .Select(g => new ItemRedEmpresaComplejo
            {
                LRedEmpresaComplejoId = 0,
                LCicloId = lCicloId,
                LContactoId = g.Key.LContactoId,
                LComplejoId = g.Key.LComplejoId,
                DMonto = g.Sum(x => x.BonoResidual),
                Usuario = g.Key.Complejo + " - " + g.Key.Empresa + " - " + g.Key.LComplejoId.ToString(),
            })
            .ToList();

        var maestro = redEmpresaComplejo
            .GroupBy(x => x.LContactoId)
            .Select(g => new ItemAdministracionBonoResidual
            {
                Usuario = usuario,
                LBonoResidualId = 0,
                LCicloId = lCicloId,
                LContactoId = g.Key,
                DTotalBono = g.Sum(x => x.DMonto),
                Detalle = listado
                    .Where(x => x.LContactoId == g.Key)
                    .Select(h => new AdministracionBonoResidualDetalle
                    {
                        LbonoresidualDetalleId = 0,
                        LbonoresidualId = 0,
                        LempresaId = 0,
                        LcomplejoId = h.LComplejoId,
                        Nivel = h.Nivel,
                        Producto = h.ProductoId,
                        FechaPago = h.FechaPago,
                        Monto = h.Bono,
                        PorcentajeComision = h.PorcentajeComision,
                        Comision = h.BonoResidual,
                    })
                    .ToList(),
            })
            .ToList();

        var guardadoResidual = await _repositorio.InsertBonoResidualAsync(logId, usuario, maestro);
        if (!guardadoResidual.Success)
            return (false, guardadoResidual.Mensaje);
        var guardadoCompleto = await _repositorio.InsertBonoCompletoAsync(logId, bonoCompleto);
        if (!guardadoCompleto.Success)
            return (false, guardadoCompleto.Mensaje);
        var guardadoRed = await _repositorio.InsertRedEmpresaComplejoAsync(logId, usuario, redEmpresaComplejo);
        if (!guardadoRed.Success)
            return (false, guardadoRed.Mensaje);

        return (true, $"Bono residual: {listado.Count} registros de cálculo, {maestro.Count} contactos.");
    }

    // Basado en BonoResidualService.ConstruirListadoResidualAsync. La cuota se toma por nivel (LPatrocinado1..7).
    // Diferencias de la simulación: calcula para todos los patrocinadores de la red, estén o no activos en el ciclo,
    // y no revisa la cartera (vencidos). Se mantienen fuera los bloqueados por habilitación (genera_comisiones = 0).
    private List<BrCalculoItem> ConstruirListadoResidual(
        int lCicloId,
        List<BrCuotaRed> cuotas,
        List<BrContacto> contactosTmp,
        IEnumerable<ItemHabilitacionComision> habilitaciones,
        IEnumerable<DetailsBrConfiguracion> configuracion)
    {
        var contactos = contactosTmp.ToDictionary(x => x.LContactoId, x => x);
        var bloqueados = HabilitacionComisionHelper.GetContactosBloqueadosParaComision(habilitaciones);

        // Los contactos configurados (misma clave que red) se incluyen aunque estén bloqueados; cobran 0 más abajo.
        bool Incluido(int contactoId) => contactoId > 0 && (!bloqueados.Contains(contactoId) || _noComprimir.Contains(contactoId));

        var configs = configuracion.Where(x => x.LCicloId == lCicloId).ToList();
        var terreno = configs.Where(x => x.TipoProductoId == 1).ToDictionary(x => x.Nivel, x => x);
        var membresia = configs.Where(x => x.TipoProductoId == 2).ToDictionary(x => x.Nivel, x => x);

        var listado = new List<BrCalculoItem>();
        foreach (var item in cuotas)
        {
            for (var nivel = 1; nivel <= 7; nivel++)
            {
                int patrocinado = PatrocinadoNivel(item, nivel);
                contactos.TryGetValue(patrocinado, out var contacto);
                terreno.TryGetValue(nivel, out var configTerreno);
                membresia.TryGetValue(nivel, out var configMembresia);

                var porcentaje = string.Equals(item.Empresa?.Trim(), "ADVEL", StringComparison.OrdinalIgnoreCase)
                    ? configMembresia?.PorcentajeComision ?? 0
                    : configTerreno?.PorcentajeComision ?? 0;
                listado.Add(new BrCalculoItem
                {
                    LContactoId = patrocinado,
                    NombreCompleto = contacto?.SNombreCompleto ?? "",
                    Documento = contacto?.SCedulaIdentidad ?? "",
                    LContactoIdHijo = item.LContactoId,
                    NombreCompletoHijo = item.Cliente,
                    DocumentoHijo = item.DocumentoCliente,
                    Nivel = nivel,
                    Bono = item.Bono,
                    BonoResidual = item.Bono * porcentaje / 100,
                    ActivoMes = true,
                    PorcentajeComision = porcentaje,
                    LComplejoId = item.ProyectoId,
                    Complejo = item.Proyecto,
                    Empresa = item.Empresa,
                    ProductoId = item.ProductoId,
                    FechaPago = item.FechaPago,
                    EstaAlDia = true,
                });
            }
        }

        // Sin filtro de activos ni de cartera: solo se quitan los niveles sin patrocinador y los bloqueados.
        listado = listado.Where(x => Incluido(x.LContactoId)).ToList();
        // Comisión residual cero configurada (misma clave que red).
        foreach (var calculo in listado.Where(x => _noComprimir.Contains(x.LContactoId)))
            calculo.BonoResidual = 0;

        return listado;
    }

    private static int PatrocinadoNivel(BrCuotaRed item, int nivel) => nivel switch
    {
        1 => item.LPatrocinado1,
        2 => item.LPatrocinado2,
        3 => item.LPatrocinado3,
        4 => item.LPatrocinado4,
        5 => item.LPatrocinado5,
        6 => item.LPatrocinado6,
        _ => item.LPatrocinado7,
    };
}
