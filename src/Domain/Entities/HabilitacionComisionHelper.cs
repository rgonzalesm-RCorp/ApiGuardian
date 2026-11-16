public static class HabilitacionComisionHelper
{
    public static readonly int[] TiposContratoEspeciales =
    {
        TiposContratosDiccionario.TiposContratosDiccionarioGrd.UPGRADE,
        TiposContratosDiccionario.TiposContratosDiccionarioGrd.RECUPERACION,
        TiposContratosDiccionario.TiposContratosDiccionarioGrd.RECOMPRA,
        TiposContratosDiccionario.TiposContratosDiccionarioGrd.CASOSESPECIALES,
        TiposContratosDiccionario.TiposContratosDiccionarioGrd.REVERSION,
    };

    public static readonly int[] TiposComisionablesEspecialesCnx =
    {
        TiposContratosDiccionario.TiposContratosDiccionarioCnx.UPGRADE,
        TiposContratosDiccionario.TiposContratosDiccionarioCnx.RECUPERACION,
        TiposContratosDiccionario.TiposContratosDiccionarioCnx.RECOMPRA,
        TiposContratosDiccionario.TiposContratosDiccionarioCnx.CASOSESPECIALES,
        TiposContratosDiccionario.TiposContratosDiccionarioCnx.REVERSION,
    };

    public static HashSet<int> GetContactosHabilitadosQueGeneranComision(IEnumerable<ItemHabilitacionComision> habilitaciones)
    {
        return habilitaciones
            .Where(item => item.GeneraComisiones)
            .Select(item => item.LContactoId)
            .ToHashSet();
    }

    public static HashSet<int> GetContactosBloqueadosParaComision(IEnumerable<ItemHabilitacionComision> habilitaciones)
    {
        return habilitaciones
            .Where(item => !item.GeneraComisiones)
            .Select(item => item.LContactoId)
            .ToHashSet();
    }

    // Lista única de la regla "solo especiales": de los asesores cuyas ventas del ciclo son todas especiales
    // se quitan los que tienen habilitación manual que genera comisiones (la habilitación manda).
    public static HashSet<int> GetContactosSoloEspeciales(IEnumerable<int> contactosSoloEspeciales, IEnumerable<ItemHabilitacionComision> habilitaciones)
    {
        var habilitados = GetContactosHabilitadosQueGeneranComision(habilitaciones);

        return contactosSoloEspeciales
            .Where(contactoId => !habilitados.Contains(contactoId))
            .ToHashSet();
    }
}
