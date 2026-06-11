public static class HabilitacionComisionHelper
{
    public static readonly int[] TiposContratoEspeciales =
    {
        TiposContratosDiccionario.TiposContratosDiccionarioGrd.UPGRADE,
        TiposContratosDiccionario.TiposContratosDiccionarioGrd.RECUPERACION,
        TiposContratosDiccionario.TiposContratosDiccionarioGrd.RECOMPRA,
        TiposContratosDiccionario.TiposContratosDiccionarioGrd.CASOSESPECIALES,
    };

    public static readonly int[] TiposComisionablesEspecialesCnx =
    {
        TiposContratosDiccionario.TiposContratosDiccionarioCnx.UPGRADE,
        TiposContratosDiccionario.TiposContratosDiccionarioCnx.RECUPERACION,
        TiposContratosDiccionario.TiposContratosDiccionarioCnx.RECOMPRA,
        TiposContratosDiccionario.TiposContratosDiccionarioCnx.CASOSESPECIALES,
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
}
