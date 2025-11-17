using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;

namespace ApiGuardian.Infrastructure.Repositories;

public class AdministracionContratoRepository : IAdministracionContratoRepository
{
    private readonly DapperContext _context;

    public AdministracionContratoRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AdministracionContrato>> GetAllAdministracionContrato(int page, int pageSize, string? search)
    {
        try
        {
            var query = $@"select 
                            C.lcontrato_id lcontratoId
                            , C.dtfechaadd fechaRegistro
                            , C.dtfecha fecha
                            , C.snroventa nroVenta
                            , P.snombrecompleto Propietario
                            , AC.snombre complejo
                            , C.smanzano nroMnzo
                            , C.slote nroLote
                            , C.dprecio precio
                            , C.dcuota_inicial cuotaInicial
                            , C.lestado estado
                            , ATC.snombre TipoContrato
                            , A.snombrecompleto Asesor
                        from administracioncontrato C
                        inner join administracioncontacto P on P.lcontacto_id = C.lcontacto_id
                        inner join administracioncontacto A on A.lcontacto_id = C.lasesor_id
                        inner join administracioncomplejo AC on AC.lcomplejo_id = C.lcomplejo_id
                        inner join administraciontipocontrato ATC on ATC.ltipocontrato_id = C.ltipocontrato_id
                        WHERE P.snombrecompleto LIKE '%{ search }%' OR C.snroventa LIKE '%{search}%'
                        LIMIT {pageSize} OFFSET {page}; ";
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<AdministracionContrato>(query);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return Enumerable.Empty<AdministracionContrato>();
        }
        
    }
}
