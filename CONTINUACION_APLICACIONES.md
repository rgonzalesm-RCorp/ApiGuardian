# Continuidad: proceso de Aplicaciones

Estado al finalizar la sesión: el proyecto compila correctamente con `dotnet build ApiGuardian/ApiGuardian.sln --no-restore` (sin errores; permanecen advertencias preexistentes de nulabilidad).

## Retenciones

- La llamada `CALL RetencionEmpresa();` permanece comentada en `AplicacionesRepositorio.Datos.cs`.
- Se conserva la validación de que existan retenciones para el ciclo.
- Aplicaciones ya no elimina los datos de `tbl_retencionempresa` ni de `tbl_retencionempresa_exterior`; los genera otro proceso.
- En `retencion.sql`, `t_bono_liderazgo` está excluido de la definición versionada del procedimiento. Para que afecte a Guardian, el procedimiento almacenado de la base de datos debe actualizarse con ese script.

## Sincronización hacia BDQISHUR

- `BDQISHUR.dbo.AplicacionesComisionPorEmpresa` se elimina por ciclo y se vuelve a sincronizar desde Guardian en cada ejecución de Aplicaciones, incluida la vista previa.
- Para empresas nuevas es obligatorio registrar el mapeo en `BDQISHUR.dbo.AplicacionesEmpresaGuardianAsumeSion` antes de ejecutar Aplicaciones.
- Durante la carga se excluyen contactos con ID menor o igual a 3, el contacto 6474 y contactos dados de baja.

## Regla de monto a aplicar

La fuente de Aplicaciones ahora aplica la misma retención que el reporte `/Reportes/comision/servicio`:

- Sin factura: 16%, redondeado por empresa.
- Con factura: 0%.

Validación de solo lectura realizada para el ciclo 147:

| Concepto | Total |
|---|---:|
| Comisión + servicio | 132.333,97 |
| Retención según reporte | 17.273,20 |
| Total a aplicar | 115.060,77 |

Después del cambio, Aplicaciones también totaliza **115.060,77** para 188 comisionados. El reporte tiene 500 filas porque su nivel de detalle es comisionado por empresa.

## Restricción de cuotas

- Solo se obtienen y aplican cuotas con vencimiento anterior al primer día del próximo mes; esto incluye vencidas y las del mes actual.
- Existe una validación adicional antes de ejecutar el pago para omitir cuotas de meses posteriores.
- En una misma ejecución, una cuota se identifica por empresa, venta y número de cuota, y se marca como aplicada para impedir que el ciclo vuelva a aplicarla hasta agotar la comisión.

## Archivos principales modificados

- `src/Infrastructure/Repositories/Aplicaciones/AplicacionesRepositorio.cs`
- `src/Infrastructure/Repositories/Aplicaciones/AplicacionesRepositorio.Datos.cs`
- `src/Infrastructure/Repositories/Aplicaciones/AplicacionesRepositorio.Consultas.cs`
- `retencion.sql`
- `README.Aplicaciones.md`

