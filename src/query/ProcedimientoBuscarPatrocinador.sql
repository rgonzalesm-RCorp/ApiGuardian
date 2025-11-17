DROP PROCEDURE IF EXISTS `sp_GetPadresHasta7Fijos`;
CREATE PROCEDURE sp_GetPadresHasta7Fijos(IN p_hijo INT)
BEGIN
    SELECT nivel, hijo, padre, snombrecompleto, cbaja
    FROM (

        SELECT 
            1 AS nivel,
            a1.lcontacto_id AS hijo,
            a1.lpatrocinante_id AS padre,
            p1.snombrecompleto,
            p1.cbaja
        FROM administracioncontacto a1
        LEFT JOIN administracioncontacto p1 ON p1.lcontacto_id = a1.lpatrocinante_id
        WHERE a1.lcontacto_id = p_hijo and p1.lcontacto_id > 0

        UNION ALL

        SELECT 
            2 AS nivel,
            a1.lcontacto_id AS hijo,
            a2.lpatrocinante_id AS padre,
            p2.snombrecompleto,
            p2.cbaja
        FROM administracioncontacto a1
        LEFT JOIN administracioncontacto a2 ON a2.lcontacto_id = a1.lpatrocinante_id
        LEFT JOIN administracioncontacto p2 ON p2.lcontacto_id = a2.lpatrocinante_id
        WHERE a1.lcontacto_id = p_hijo and p2.lcontacto_id > 0

        UNION ALL

        SELECT 
            3 AS nivel,
            a1.lcontacto_id AS hijo,
            a3.lpatrocinante_id AS padre,
            p3.snombrecompleto,
            p3.cbaja
        FROM administracioncontacto a1
        LEFT JOIN administracioncontacto a2 ON a2.lcontacto_id = a1.lpatrocinante_id
        LEFT JOIN administracioncontacto a3 ON a3.lcontacto_id = a2.lpatrocinante_id
        LEFT JOIN administracioncontacto p3 ON p3.lcontacto_id = a3.lpatrocinante_id
        WHERE a1.lcontacto_id = p_hijo and p3.lcontacto_id > 0

        UNION ALL

        SELECT 
            4 AS nivel,
            a1.lcontacto_id AS hijo,
            a4.lpatrocinante_id AS padre,
            p4.snombrecompleto,
            p4.cbaja
        FROM administracioncontacto a1
        LEFT JOIN administracioncontacto a2 ON a2.lcontacto_id = a1.lpatrocinante_id
        LEFT JOIN administracioncontacto a3 ON a3.lcontacto_id = a2.lpatrocinante_id
        LEFT JOIN administracioncontacto a4 ON a4.lcontacto_id = a3.lpatrocinante_id
        LEFT JOIN administracioncontacto p4 ON p4.lcontacto_id = a4.lpatrocinante_id
        WHERE a1.lcontacto_id = p_hijo and p4.lcontacto_id > 0

        UNION ALL

        SELECT 
            5 AS nivel,
            a1.lcontacto_id AS hijo,
            a5.lpatrocinante_id AS padre,
            p5.snombrecompleto,
            p5.cbaja
        FROM administracioncontacto a1
        LEFT JOIN administracioncontacto a2 ON a2.lcontacto_id = a1.lpatrocinante_id
        LEFT JOIN administracioncontacto a3 ON a3.lcontacto_id = a2.lpatrocinante_id
        LEFT JOIN administracioncontacto a4 ON a4.lcontacto_id = a3.lpatrocinante_id
        LEFT JOIN administracioncontacto a5 ON a5.lcontacto_id = a4.lpatrocinante_id
        LEFT JOIN administracioncontacto p5 ON p5.lcontacto_id = a5.lpatrocinante_id
        WHERE a1.lcontacto_id = p_hijo and p5.lcontacto_id > 0

        UNION ALL

        SELECT 
            6 AS nivel,
            a1.lcontacto_id AS hijo,
            a6.lpatrocinante_id AS padre,
            p6.snombrecompleto,
            p6.cbaja
        FROM administracioncontacto a1
        LEFT JOIN administracioncontacto a2 ON a2.lcontacto_id = a1.lpatrocinante_id
        LEFT JOIN administracioncontacto a3 ON a3.lcontacto_id = a2.lpatrocinante_id
        LEFT JOIN administracioncontacto a4 ON a4.lcontacto_id = a3.lpatrocinante_id
        LEFT JOIN administracioncontacto a5 ON a5.lcontacto_id = a4.lpatrocinante_id
        LEFT JOIN administracioncontacto a6 ON a6.lcontacto_id = a5.lpatrocinante_id
        LEFT JOIN administracioncontacto p6 ON p6.lcontacto_id = a6.lpatrocinante_id
        WHERE a1.lcontacto_id = p_hijo and p6.lcontacto_id > 0

        UNION ALL

        SELECT 
            7 AS nivel,
            a1.lcontacto_id AS hijo,
            a7.lpatrocinante_id AS padre,
            p7.snombrecompleto,
            p7.cbaja
        FROM administracioncontacto a1
        LEFT JOIN administracioncontacto a2 ON a2.lcontacto_id = a1.lpatrocinante_id
        LEFT JOIN administracioncontacto a3 ON a3.lcontacto_id = a2.lpatrocinante_id
        LEFT JOIN administracioncontacto a4 ON a4.lcontacto_id = a3.lpatrocinante_id
        LEFT JOIN administracioncontacto a5 ON a5.lcontacto_id = a4.lpatrocinante_id
        LEFT JOIN administracioncontacto a6 ON a6.lcontacto_id = a5.lpatrocinante_id
        LEFT JOIN administracioncontacto a7 ON a7.lcontacto_id = a6.lpatrocinante_id
        LEFT JOIN administracioncontacto p7 ON p7.lcontacto_id = a7.lpatrocinante_id
        WHERE a1.lcontacto_id = p_hijo and p7.lcontacto_id > 0

    ) AS b
    WHERE padre IS NOT NULL
    ORDER BY nivel;
END;
DROP PROCEDURE IF EXISTS `sp_GetPadresHasta7Activos`;
CREATE PROCEDURE sp_GetPadresHasta7Activos(IN p_hijo INT)
BEGIN
    DECLARE v_padre INT;
    DECLARE v_nombre VARCHAR(255);
    DECLARE v_cbaja TINYINT;
    DECLARE v_nivel INT DEFAULT 1;

    CREATE TEMPORARY TABLE IF NOT EXISTS tmp_padres (
        nivel INT,
        hijo INT,
        padre INT,
        snombrecompleto VARCHAR(255),
        cbaja TINYINT
    );

    SELECT lpatrocinante_id
    INTO v_padre
    FROM administracioncontacto
    WHERE lcontacto_id = p_hijo
    LIMIT 1;

    WHILE v_nivel <= 7 AND v_padre IS NOT NULL DO

        SELECT lcontacto_id, snombrecompleto, cbaja
        INTO v_padre, v_nombre, v_cbaja
        FROM administracioncontacto
        WHERE lcontacto_id = v_padre and lcontacto_id > 0
        LIMIT 1;

        IF v_cbaja = 0 THEN
            INSERT INTO tmp_padres(nivel, hijo, padre, snombrecompleto, cbaja)
            VALUES (v_nivel, p_hijo, v_padre, v_nombre, v_cbaja);
            SET v_nivel = v_nivel + 1;
        END IF;

        IF v_padre IS NOT NULL THEN
            SELECT lpatrocinante_id
            INTO v_padre
            FROM administracioncontacto
            WHERE lcontacto_id = v_padre
            LIMIT 1;
        END IF;
  
    END WHILE;

    SELECT * FROM tmp_padres where padre > 0 ORDER BY nivel;

    DROP TEMPORARY TABLE IF EXISTS tmp_padres;
END;
