-- =====================================================================
-- Verificación de nivel_consolidado_mes en reportesmontesion (SOLO LECTURA)
-- Regla: Monte Sion nunca baja el rango, así que el nivel de partida de un ciclo
-- debe ser el MAX(nivel_ciclo) alcanzado en ciclos anteriores (0 si no tiene historial).
-- Cambiar @CicloId / @CI según lo que se quiera revisar.
-- =====================================================================
SET @CicloId = 149;
SET @CI = '5887508';

-- 1) Historial de una persona por CI
SELECT c.lcontacto_id, c.snombrecompleto, c.lnivel_id AS lnivel_id_hoy,
       r.lciclo_id, r.nivel_consolidado_mes, r.nivel_ciclo, r.subieron_nivel, r.niveles_escalados,
       r.susuarioadd, r.dtfechaadd
FROM reportesmontesion r
INNER JOIN administracioncontacto c ON c.lcontacto_id = r.lcontacto_id
WHERE c.scedulaidentidad = @CI
ORDER BY r.lciclo_id;

-- 2) Detalle del ciclo: filas cuyo nivel de partida no coincide con el máximo histórico previo
SELECT x.*,
       CASE WHEN x.nivel_ciclo > x.partida_esperada THEN 1 ELSE 0 END AS subieron_esperado,
       CASE WHEN x.nivel_ciclo > x.partida_esperada THEN x.nivel_ciclo - x.partida_esperada ELSE 0 END AS escalados_esperado
FROM (
    SELECT c.scedulaidentidad AS CI, c.snombrecompleto AS Nombre, r.lcontacto_id,
           r.nivel_consolidado_mes AS partida_guardada,
           COALESCE((SELECT MAX(h.nivel_ciclo) FROM reportesmontesion h
                     WHERE h.lcontacto_id = r.lcontacto_id AND h.lciclo_id < r.lciclo_id), 0) AS partida_esperada,
           r.nivel_ciclo, r.subieron_nivel, r.niveles_escalados,
           c.lnivel_id AS lnivel_id_hoy, c.susuariomod, c.dtfechamod
    FROM reportesmontesion r
    INNER JOIN administracioncontacto c ON c.lcontacto_id = r.lcontacto_id
    WHERE r.lciclo_id = @CicloId
) x
WHERE x.partida_guardada <> x.partida_esperada
ORDER BY x.partida_guardada, x.Nombre;

-- 3) Resumen por ciclo (todos los ciclos)
SELECT x.lciclo_id,
       COUNT(*) AS total_filas,
       SUM(x.partida_guardada < x.partida_esperada) AS partida_menor,
       SUM(x.partida_guardada = 0 AND x.partida_esperada > 0) AS partida_en_cero,
       SUM(x.partida_guardada > x.partida_esperada) AS partida_mayor,
       SUM(x.niveles_escalados <> CASE WHEN x.nivel_ciclo > x.partida_esperada
                                       THEN x.nivel_ciclo - x.partida_esperada ELSE 0 END) AS escalados_incorrectos
FROM (
    SELECT r.lciclo_id, r.nivel_consolidado_mes AS partida_guardada, r.nivel_ciclo, r.niveles_escalados,
           COALESCE((SELECT MAX(h.nivel_ciclo) FROM reportesmontesion h
                     WHERE h.lcontacto_id = r.lcontacto_id AND h.lciclo_id < r.lciclo_id), 0) AS partida_esperada
    FROM reportesmontesion r
) x
GROUP BY x.lciclo_id
ORDER BY x.lciclo_id DESC;

-- 4) Contactos cuyo lnivel_id actual es menor al máximo rango alcanzado en Monte Sion
SELECT c.lcontacto_id, c.scedulaidentidad AS CI, c.snombrecompleto AS Nombre,
       COALESCE(c.lnivel_id, 0) AS lnivel_id_hoy, h.max_nivel, h.ultimo_ciclo,
       c.susuariomod, c.dtfechamod
FROM administracioncontacto c
INNER JOIN (SELECT lcontacto_id, MAX(nivel_ciclo) AS max_nivel, MAX(lciclo_id) AS ultimo_ciclo
            FROM reportesmontesion GROUP BY lcontacto_id) h ON h.lcontacto_id = c.lcontacto_id
WHERE COALESCE(c.lnivel_id, 0) < h.max_nivel
ORDER BY h.max_nivel DESC, c.snombrecompleto;

-- 5) Modificaciones masivas de administracioncontacto (posible origen de rangos bajados)
SELECT dtfechamod, susuariomod, COUNT(*) AS contactos
FROM administracioncontacto
WHERE dtfechamod >= '2026-07-01'
GROUP BY dtfechamod, susuariomod
HAVING COUNT(*) > 50
ORDER BY dtfechamod;
