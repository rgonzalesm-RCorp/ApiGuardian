namespace ApiGuardian.Infrastructure.Repositories;

// Fragmentos SQL (MySQL) que dependen de administracionciclo.moneda.
internal static class SqlMonedaCiclo
{
    // Separador con la moneda del ciclo para el resumen administraciondescuentociclo.sdetalles:
    // ciclo BS -> ' Bs. - '; ciclo SUS (o sin moneda) -> ' $us. - ' (texto histórico).
    // Requiere el parámetro @LCicloId.
    public const string SeparadorMontoDescuento =
        "IFNULL((SELECT CASE WHEN UPPER(TRIM(IFNULL(CI.moneda, ''))) = 'BS' THEN ' Bs. - ' ELSE ' $us. - ' END"
        + " FROM administracionciclo CI WHERE CI.lciclo_id = @LCicloId), ' $us. - ')";
}
