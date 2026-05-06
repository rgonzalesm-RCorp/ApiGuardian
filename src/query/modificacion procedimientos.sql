CALL sp_obtener_siguientes_pasos('COMISIONES', 142)
CALL sp_conf_ejecutar_paso('COMISIONES', 142, 'OBTENER VENTAS')
CALL sp_reiniciar_ciclo ('COMISIONES', 142)
CALL sp_cerrar_ciclo ('COMISIONES', 142)





drop PROCEDURE sp_conf_ejecutar_paso;

CREATE PROCEDURE sp_conf_ejecutar_paso (
    IN p_proceso_nombre VARCHAR(150),
    IN p_numero_ciclo INT,
    IN p_paso_nombre VARCHAR(150)
)
BEGIN
    DECLARE v_proceso_id INT DEFAULT NULL;
    DECLARE v_instancia_id INT DEFAULT NULL;
    DECLARE v_ciclo_id INT DEFAULT NULL;
    DECLARE v_paso_id INT DEFAULT NULL;
    DECLARE v_existe INT DEFAULT 0;
    DECLARE v_pendientes INT DEFAULT 0;

    DECLARE v_status BOOLEAN DEFAULT TRUE;
    DECLARE v_mensaje VARCHAR(255) DEFAULT 'OK';
    DECLARE v_next BOOLEAN DEFAULT TRUE;

    -- 1. PROCESO
    SELECT id INTO v_proceso_id
    FROM conf_procesos
    WHERE nombre = p_proceso_nombre
      AND estado = 1
    ORDER BY id DESC
    LIMIT 1;

    IF v_proceso_id IS NULL THEN
        SET v_status = FALSE;
        SET v_mensaje = 'El proceso no existe';
        SET v_next = FALSE;
    END IF;

    -- 2. INSTANCIA
    IF v_status THEN
        SELECT id INTO v_instancia_id
        FROM conf_proceso_instancias
        WHERE proceso_id = v_proceso_id
        ORDER BY id DESC LIMIT 1;

        IF v_instancia_id IS NULL THEN
            INSERT INTO conf_proceso_instancias (proceso_id, estado, fecha_inicio)
            VALUES (v_proceso_id, 'EN_PROCESO', NOW());

            SET v_instancia_id = LAST_INSERT_ID();
        END IF;
    END IF;

    -- 3. CICLO
    IF v_status THEN
        SELECT id INTO v_ciclo_id
        FROM conf_proceso_ciclos
        WHERE proceso_instancia_id = v_instancia_id
          AND numero_ciclo = p_numero_ciclo
          AND estado = 'EN_PROCESO'
        LIMIT 1;

        IF v_ciclo_id IS NULL THEN
            INSERT INTO conf_proceso_ciclos (
                proceso_instancia_id, numero_ciclo, estado, fecha_inicio
            ) VALUES (
                v_instancia_id, p_numero_ciclo, 'EN_PROCESO', NOW()
            );

            SET v_ciclo_id = LAST_INSERT_ID();
        END IF;
    END IF;

    -- 4. PASO
    IF v_status THEN
        SELECT id INTO v_paso_id
        FROM conf_pasos
        WHERE nombre = p_paso_nombre
          AND proceso_id = v_proceso_id
          AND estado = 1
        LIMIT 1;

        IF v_paso_id IS NULL THEN
            SET v_status = FALSE;
            SET v_mensaje = 'El paso no existe';
            SET v_next = FALSE;
        END IF;
    END IF;

    -- 5. DUPLICADO
    IF v_status THEN
        SELECT COUNT(*) INTO v_existe
        FROM conf_proceso_pasos
        WHERE proceso_ciclo_id = v_ciclo_id
          AND paso_id = v_paso_id;

        IF v_existe > 0 THEN
            SET v_status = FALSE;
            SET v_mensaje = 'Paso ya ejecutado';
            SET v_next = FALSE;
        END IF;
    END IF;

    -- 6. DEPENDENCIAS
    IF v_status THEN
        SELECT COUNT(*) INTO v_pendientes
        FROM conf_paso_dependencias pd
        LEFT JOIN conf_proceso_pasos pp
            ON pp.paso_id = pd.paso_requerido_id
            AND pp.proceso_ciclo_id = v_ciclo_id
        WHERE pd.paso_id = v_paso_id
          AND (pp.estado IS NULL OR pp.estado <> 'COMPLETADO');

        IF v_pendientes > 0 THEN
            SET v_status = FALSE;
            SET v_mensaje = 'Dependencias no cumplidas';
            SET v_next = FALSE;
        END IF;
    END IF;

    -- 7. INSERT
    IF v_status THEN
        INSERT INTO conf_proceso_pasos (
            proceso_ciclo_id, paso_id, estado, fecha_inicio, fecha_fin
        )
        VALUES (
            v_ciclo_id, v_paso_id, 'COMPLETADO', NOW(), NOW()
        );

        SET v_mensaje = 'Paso ejecutado correctamente';
        SET v_next = TRUE;
    END IF;

    SELECT v_status AS status, v_mensaje AS mensajes, v_next AS next;

END;

 DROP PROCEDURE sp_obtener_siguientes_pasos;

CREATE PROCEDURE sp_obtener_siguientes_pasos (
    IN p_proceso_nombre VARCHAR(150),
    IN p_numero_ciclo INT
)
BEGIN
    DECLARE v_proceso_id INT;
    DECLARE v_instancia_id INT;
    DECLARE v_ciclo_id INT;

    DECLARE v_status BOOLEAN DEFAULT TRUE; 
    DECLARE v_mensaje VARCHAR(255) DEFAULT 'OK'; 
    DECLARE v_next BOOLEAN DEFAULT FALSE;
    DECLARE v_id INT DEFAULT 0;
    DECLARE v_nombre VARCHAR(255) DEFAULT '';
    DECLARE v_orden INT DEFAULT 0;
    DECLARE v_es_obligatoria BOOLEAN DEFAULT FALSE;

    -- 1. Obtener proceso
    SELECT id INTO v_proceso_id
    FROM conf_procesos
    WHERE nombre = p_proceso_nombre
      AND estado = 1 
    ORDER BY id DESC
    LIMIT 1;

    IF v_proceso_id IS NULL THEN 
        set v_status = FALSE;
        set v_mensaje = 'El proceso no existe';
        set v_next = FALSE ;
        set v_id = 0;
        set v_nombre = '';
        set v_orden = 0;
        set v_es_obligatoria = FALSE;
        
        SELECT v_status AS status, v_mensaje AS mensajes, v_next AS next, v_id id, v_nombre nombre, v_orden orden, v_es_obligatoria esObligatoria;
    
    END IF;

    -- 2. Obtener instancia
    SELECT id INTO v_instancia_id
    FROM conf_proceso_instancias
    WHERE proceso_id = v_proceso_id
    ORDER BY id DESC
    LIMIT 1;

    -- 3. Obtener ciclo
    SELECT id INTO v_ciclo_id
    FROM conf_proceso_ciclos
    WHERE proceso_instancia_id = v_instancia_id
      AND numero_ciclo = p_numero_ciclo AND estado ='EN_PROCESO'
    LIMIT 1;
    

    -- 4. Pasos disponibles
    SELECT 
        p.id,
        p.nombre,
        p.orden,
        p.es_obligatorio INTO v_id, v_nombre, v_orden, v_es_obligatoria
    FROM conf_pasos p
    WHERE p.proceso_id = v_proceso_id
      AND p.estado = 1

    AND NOT EXISTS (
        SELECT 1
        FROM conf_proceso_pasos pp
        WHERE pp.proceso_ciclo_id = v_ciclo_id
          AND pp.paso_id = p.id
    )

    AND NOT EXISTS (
        SELECT 1
        FROM conf_paso_dependencias pd
        LEFT JOIN conf_proceso_pasos pp
            ON pp.paso_id = pd.paso_requerido_id
            AND pp.proceso_ciclo_id = v_ciclo_id
        WHERE pd.paso_id = p.id
          AND (pp.estado IS NULL OR pp.estado <> 'COMPLETADO')
    )

    ORDER BY p.orden;
    SELECT v_status AS status, v_mensaje AS mensajes, v_next AS next, v_id id, v_nombre nombre, v_orden orden, v_es_obligatoria esObligatoria;

END ;

 
 drop PROCEDURE sp_reiniciar_ciclo;
 

CREATE PROCEDURE sp_reiniciar_ciclo (
    IN p_proceso_nombre VARCHAR(150),
    IN p_numero_ciclo INT
)
BEGIN
    DECLARE v_proceso_id INT;
    DECLARE v_instancia_id INT;
    DECLARE v_ciclo_id INT;

    DECLARE v_status BOOLEAN DEFAULT TRUE;
    DECLARE v_mensaje VARCHAR(255) DEFAULT 'OK';
    DECLARE v_next BOOLEAN DEFAULT TRUE;

    SELECT id INTO v_proceso_id
    FROM conf_procesos
    WHERE nombre = p_proceso_nombre
      AND estado = 1
    ORDER BY  id DESC
    LIMIT 1;
    

    IF v_proceso_id IS NULL THEN
        SET v_status = FALSE;
        SET v_mensaje = 'Proceso no existe';
        SET v_next = FALSE;
    END IF;

    IF v_status THEN
        SELECT id INTO v_instancia_id
        FROM conf_proceso_instancias
        WHERE proceso_id = v_proceso_id
        ORDER BY id DESC LIMIT 1;

        IF v_instancia_id IS NULL THEN
            SET v_status = FALSE;
            SET v_mensaje = 'No hay instancia';
            SET v_next = FALSE;
        END IF;
    END IF;

    IF v_status THEN
        SELECT id INTO v_ciclo_id
        FROM conf_proceso_ciclos
        WHERE proceso_instancia_id = v_instancia_id
          AND numero_ciclo = p_numero_ciclo
          AND estado = 'EN_PROCESO'
        LIMIT 1;

        IF v_ciclo_id IS NULL THEN
            SET v_status = FALSE;
            SET v_mensaje = 'Ciclo cerrado o inexistente';
            SET v_next = FALSE;
        END IF;
    END IF;

    IF v_status THEN
        UPDATE conf_proceso_ciclos
        SET estado = 'REINICIADO', fecha_fin = NOW()
        WHERE id = v_ciclo_id;

        INSERT INTO conf_proceso_ciclos (
            proceso_instancia_id, numero_ciclo, estado, fecha_inicio
        )
        VALUES (
            v_instancia_id, p_numero_ciclo, 'EN_PROCESO', NOW()
        );

        SET v_mensaje = 'Ciclo reiniciado';
    END IF;

    SELECT v_status AS status, v_mensaje AS mensajes, v_next AS next;

END 



DROP PROCEDURE sp_cerrar_ciclo;

CREATE PROCEDURE sp_cerrar_ciclo (
    IN p_proceso_nombre VARCHAR(150),
    IN p_numero_ciclo INT
)
BEGIN
    DECLARE v_proceso_id INT;
    DECLARE v_instancia_id INT;
    DECLARE v_ciclo_id INT;
    DECLARE v_pendientes INT;

    DECLARE v_status BOOLEAN DEFAULT TRUE;
    DECLARE v_mensaje VARCHAR(255) DEFAULT 'OK';
    DECLARE v_next BOOLEAN DEFAULT FALSE;

    SELECT id INTO v_proceso_id
    FROM conf_procesos
    WHERE nombre = p_proceso_nombre
      AND estado = 1 
    ORDER BY id DESC
    LIMIT 1;

    IF v_proceso_id IS NULL THEN
        SET v_status = FALSE;
        SET v_mensaje = 'Proceso no existe';
    END IF;

    IF v_status THEN
        SELECT id INTO v_instancia_id
        FROM conf_proceso_instancias
        WHERE proceso_id = v_proceso_id
        ORDER BY id DESC LIMIT 1;

        IF v_instancia_id IS NULL THEN
            SET v_status = FALSE;
            SET v_mensaje = 'No hay instancia';
        END IF;
    END IF;

    IF v_status THEN
        SELECT id INTO v_ciclo_id
        FROM conf_proceso_ciclos
        WHERE proceso_instancia_id = v_instancia_id
          AND numero_ciclo = p_numero_ciclo
          AND estado = 'EN_PROCESO'
        LIMIT 1;

        IF v_ciclo_id IS NULL THEN
            SET v_status = FALSE;
            SET v_mensaje = 'Ciclo no válido';
        END IF;
    END IF;

    IF v_status THEN
        SELECT COUNT(*) INTO v_pendientes
        FROM conf_pasos p
        LEFT JOIN conf_proceso_pasos pp
            ON pp.paso_id = p.id
            AND pp.proceso_ciclo_id = v_ciclo_id
        WHERE p.proceso_id = v_proceso_id
          AND p.estado = 1
          AND p.es_obligatorio = 1
          AND (pp.estado IS NULL OR pp.estado <> 'COMPLETADO');

        IF v_pendientes > 0 THEN
            SET v_status = FALSE;
            SET v_mensaje = 'Faltan pasos obligatorios';
        END IF;
    END IF;

    IF v_status THEN
        UPDATE conf_proceso_ciclos
        SET estado = 'CERRADO', fecha_fin = NOW()
        WHERE id = v_ciclo_id;

        SET v_mensaje = 'Ciclo cerrado correctamente';
        SET v_next = FALSE;
    END IF;

    SELECT v_status AS status, v_mensaje AS mensajes, v_next AS next;

END
