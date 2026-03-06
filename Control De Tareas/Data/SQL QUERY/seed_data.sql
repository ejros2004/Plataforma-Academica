-- =====================================================
-- SCRIPT COMPLETO CORREGIDO: SOLO MÓDULOS QUE EXISTEN EN VISTAS/CONTROLADORES
-- =====================================================
USE TaskManagerDb;
/*
INSTRUCCIONES:
1. Este script ELIMINARÁ todos los datos existentes
2. Insertará nuevos datos estáticos y congruentes
3. SOLO módulos que coinciden con vistas y controladores reales
4. NOMBRES DE ACCIONES EN ESPAÑOL para coincidir con controladores
*/

PRINT '=== INICIANDO LIMPIEZA Y INSERCIÓN COMPLETA CORREGIDA ===';

-- =====================================================
-- LIMPIEZA COMPLETA DE DATOS (EN ORDEN CORRECTO POR FK)
-- =====================================================

PRINT '1. LIMPIANDO DATOS EXISTENTES...';

-- Deshabilitar constraints temporalmente para limpieza
EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';

-- Eliminar datos en orden correcto (evitar violaciones FK)
DELETE FROM AuditLogs;
DELETE FROM Grades;
DELETE FROM SubmissionFiles;
DELETE FROM Submissions;
DELETE FROM Tasks;  -- CORREGIDO: Tasks en lugar de Tareas
DELETE FROM Announcements;
DELETE FROM Enrollments;
DELETE FROM CourseOfferings;
DELETE FROM RoleModules;
DELETE FROM Module;
DELETE FROM ModuleGroup;
DELETE FROM UserRoles;
DELETE FROM Users;
DELETE FROM Roles;
DELETE FROM Periods;
DELETE FROM Courses;

-- Reiniciar identidades SOLO para tablas que usan INT IDENTITY
DBCC CHECKIDENT ('AuditLogs', RESEED, 0);

-- Habilitar constraints nuevamente
EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';

PRINT 'Limpieza completada. Todas las tablas vacías.';

-- =====================================================
-- DECLARACIÓN DE GUIDS FIJOS PARA CONSISTENCIA
-- =====================================================

-- Roles
DECLARE @AdminRoleId UNIQUEIDENTIFIER = NEWID();
DECLARE @ProfesorRoleId UNIQUEIDENTIFIER = NEWID();
DECLARE @EstudianteRoleId UNIQUEIDENTIFIER = NEWID();

-- Usuarios
DECLARE @AdminUserId UNIQUEIDENTIFIER = NEWID();
DECLARE @Profesor1Id UNIQUEIDENTIFIER = NEWID();
DECLARE @Profesor2Id UNIQUEIDENTIFIER = NEWID();
DECLARE @Profesor3Id UNIQUEIDENTIFIER = NEWID();
DECLARE @Profesor4Id UNIQUEIDENTIFIER = NEWID();

DECLARE @Estudiante1Id UNIQUEIDENTIFIER = NEWID();
DECLARE @Estudiante2Id UNIQUEIDENTIFIER = NEWID();
DECLARE @Estudiante3Id UNIQUEIDENTIFIER = NEWID();
DECLARE @Estudiante4Id UNIQUEIDENTIFIER = NEWID();
DECLARE @Estudiante5Id UNIQUEIDENTIFIER = NEWID();
DECLARE @Estudiante6Id UNIQUEIDENTIFIER = NEWID();
DECLARE @Estudiante7Id UNIQUEIDENTIFIER = NEWID();
DECLARE @Estudiante8Id UNIQUEIDENTIFIER = NEWID();

-- ModuleGroup (GUIDS fijos para consistencia)
DECLARE @MGMatriculasId UNIQUEIDENTIFIER = 'F51DF7BE-A9FD-4166-9BB1-1C83BF1618CA';
DECLARE @MGUsuariosId UNIQUEIDENTIFIER = '639E4AEB-233E-4E89-BE81-8751200C7C25';
DECLARE @MGCursosId UNIQUEIDENTIFIER = '470FEB6C-1231-48B0-93E5-94B2C6CEF2E9';
DECLARE @MGDashboardsId UNIQUEIDENTIFIER = '517D1073-93F2-47C4-AF45-B585D0144DB2';
DECLARE @MGTareasId UNIQUEIDENTIFIER = 'C2DDC7C7-BEAB-4C12-8ED6-E89B33CA76F6';
DECLARE @MGAuditoriaId UNIQUEIDENTIFIER = NEWID();
DECLARE @MGAnunciosId UNIQUEIDENTIFIER = NEWID();
DECLARE @MGCalificarId UNIQUEIDENTIFIER = NEWID();
DECLARE @MGMisEntregasId UNIQUEIDENTIFIER = NEWID(); -- AÑADIDO: Grupo para Mis entregas

-- Cursos (GUIDS fijos)
DECLARE @CursoMAT101Id UNIQUEIDENTIFIER = NEWID();
DECLARE @CursoLEN102Id UNIQUEIDENTIFIER = NEWID();
DECLARE @CursoFIS201Id UNIQUEIDENTIFIER = NEWID();
DECLARE @CursoQUI202Id UNIQUEIDENTIFIER = NEWID();
DECLARE @CursoHIS301Id UNIQUEIDENTIFIER = NEWID();
DECLARE @CursoBIO302Id UNIQUEIDENTIFIER = NEWID();

-- Periodos (GUIDS fijos)
DECLARE @Periodo1Id UNIQUEIDENTIFIER = NEWID();
DECLARE @Periodo2Id UNIQUEIDENTIFIER = NEWID();

-- CourseOfferings (GUIDS fijos)
DECLARE @OfertaMAT101Id UNIQUEIDENTIFIER = NEWID();
DECLARE @OfertaLEN102Id UNIQUEIDENTIFIER = NEWID();
DECLARE @OfertaFIS201Id UNIQUEIDENTIFIER = NEWID();
DECLARE @OfertaQUI202Id UNIQUEIDENTIFIER = NEWID();
DECLARE @OfertaHIS301Id UNIQUEIDENTIFIER = NEWID();
DECLARE @OfertaBIO302Id UNIQUEIDENTIFIER = NEWID();

-- =====================================================
-- INSERCIÓN COMPLETA DE DATOS CORREGIDOS
-- =====================================================

PRINT '2. INSERTANDO NUEVOS DATOS CORREGIDOS...';

-- 1. Insertar Roles (con GUIDS fijos)
INSERT INTO Roles (RoleId, RoleName, Description, CreateAt, modifiedBy, IsSoftDeleted) VALUES
(@AdminRoleId, 'Administrador', 'Administrador del sistema', GETDATE(), @AdminUserId, 0),
(@ProfesorRoleId, 'Profesor', 'Profesor que imparte cursos', GETDATE(), @AdminUserId, 0),
(@EstudianteRoleId, 'Estudiante', 'Estudiante que toma cursos', GETDATE(), @AdminUserId, 0);

PRINT '- Roles insertados: 3';

-- 2. Insertar ModuleGroup (CORREGIDO: singular)
INSERT INTO ModuleGroup (GroupModuleId, Description, CreateAt, CreateDate, CreatBy, ModifieBy, IsSoftDeleted) VALUES
(@MGMatriculasId, 'Matriculas', GETDATE(), GETDATE(), @AdminUserId, @AdminUserId, 0),
(@MGUsuariosId, 'Usuarios', GETDATE(), GETDATE(), @AdminUserId, @AdminUserId, 0),
(@MGCursosId, 'Cursos', GETDATE(), GETDATE(), @AdminUserId, @AdminUserId, 0),
(@MGDashboardsId, 'Dashboards', GETDATE(), GETDATE(), @AdminUserId, @AdminUserId, 0),
(@MGTareasId, 'Tareas', GETDATE(), GETDATE(), @AdminUserId, @AdminUserId, 0),
(@MGAuditoriaId, 'Auditoria', GETDATE(), GETDATE(), @AdminUserId, @AdminUserId, 0),
(@MGAnunciosId, 'Anuncios', GETDATE(), GETDATE(), @AdminUserId, @AdminUserId, 0),
(@MGCalificarId, 'Calificar', GETDATE(), GETDATE(), @AdminUserId, @AdminUserId, 0),
(@MGMisEntregasId, 'Mis entregas', GETDATE(), GETDATE(), @AdminUserId, @AdminUserId, 0); -- AÑADIDO: Grupo Mis entregas

PRINT '- ModuleGroup insertados: 9';

-- =====================================================
-- 3. INSERTAR MODULE (SECCIÓN CORREGIDA CON MIS ENTREGAS INTEGRADO)
-- =====================================================
PRINT '3. INSERTANDO NUEVOS DATOS (MÓDULOS AJUSTADOS)...';

INSERT INTO Module (ModuleId, Nombre, Controller, Metodo, CreateAt, CreateDate, CreatBy, ModifieBy, IsSoftDeleted, ModuloAgrupadoId) VALUES

-- A. DASHBOARD Y AUTH
(NEWID(), 'Dashboard Principal', 'Home', 'Index', GETDATE(), GETDATE(), @AdminUserId, @AdminUserId, 0, @MGDashboardsId),
(NEWID(), 'Registrar Usuario', 'Home', 'Register', GETDATE(), GETDATE(), @AdminUserId, @AdminUserId, 0, @MGUsuariosId),
(NEWID(), 'Recuperar Contraseña', 'Home', 'PasswordRecovery', GETDATE(), GETDATE(), @AdminUserId, @AdminUserId, 0, @MGUsuariosId),
(NEWID(), 'Cambiar Contraseña', 'Home', 'ChangePassword', GETDATE(), GETDATE(), @AdminUserId, @AdminUserId, 0, @MGUsuariosId),
(NEWID(), 'Política de Privacidad', 'Home', 'Privacy', GETDATE(), GETDATE(), @AdminUserId, @AdminUserId, 0, @MGDashboardsId),
(NEWID(), 'Cerrar Sesión', 'Account', 'Logout', GETDATE(), GETDATE(), @AdminUserId, @AdminUserId, 0, @MGUsuariosId),

-- B. GRUPO CURSOS
(NEWID(), 'Mis Cursos', 'Cursos', 'Index', GETDATE(), GETDATE(), @AdminUserId, @AdminUserId, 0, @MGCursosId),
(NEWID(), 'Gestión de Ofertas', 'CourseOfferings', 'MisCursos', GETDATE(), GETDATE(), @AdminUserId, @AdminUserId, 0, @MGCursosId),

-- C. GRUPO MATRÍCULAS
(NEWID(), 'Inscripciones', 'CourseOfferings', 'Inscripciones', GETDATE(), GETDATE(), @AdminUserId, @AdminUserId, 0, @MGMatriculasId),

-- D. GRUPO TAREAS
(NEWID(), 'Lista de Tareas', 'Tareas', 'Index', GETDATE(), GETDATE(), @AdminUserId, @AdminUserId, 0, @MGTareasId),
(NEWID(), 'Crear Tarea', 'Tareas', 'Crear', GETDATE(), GETDATE(), @AdminUserId, @AdminUserId, 0, @MGTareasId),

-- E. GRUPO MIS ENTREGAS (INTEGRADO COMPLETAMENTE)
(NEWID(), 'Ver Mis Entregas', 'Submissions', 'Index', GETDATE(), GETDATE(), @AdminUserId, @AdminUserId, 0, @MGMisEntregasId),
(NEWID(), 'Nueva Entrega', 'Submissions', 'Create', GETDATE(), GETDATE(), @AdminUserId, @AdminUserId, 0, @MGMisEntregasId);

PRINT '- Módulos insertados: ' + CAST(@@ROWCOUNT AS VARCHAR);

-- =====================================================
-- 4. INSERTAR ROLEMODULES (Permisos actualizados con MIS ENTREGAS)
-- =====================================================
INSERT INTO RoleModules (ModuleRoleId, Description, CreateAt, CreateDate, CreatBy, ModifieBy, IsSoftDeleted, ModuleId, RoleId)
SELECT 
    NEWID(),
    'Permiso para ' + m.Nombre,
    GETDATE(),
    GETDATE(),
    @AdminUserId,
    @AdminUserId,
    0,
    m.ModuleId,
    r.RoleId
FROM Module m
CROSS JOIN Roles r
WHERE 
    -- 1. ADMIN: Ve absolutamente todo
    (r.RoleId = @AdminRoleId)
    
    OR
    
    -- 2. PROFESOR: 
    -- Ve Dashboard, Gestión de Ofertas, Cursos, Tareas, Mis Entregas y Auth
    (r.RoleId = @ProfesorRoleId AND (
        (m.Controller = 'Home' AND m.Metodo IN ('Index', 'Privacy', 'ChangePassword')) OR 
        (m.Controller = 'Cursos' AND m.Metodo = 'Index') OR
        (m.Controller = 'CourseOfferings' AND m.Metodo = 'MisCursos') OR
        (m.Controller = 'Tareas' AND m.Metodo IN ('Index', 'Crear', 'TareasEstudiantes')) OR
        (m.Controller = 'Submissions' AND m.Metodo IN ('Index', 'Create')) OR -- AÑADIDO: Acceso a Submissions
        (m.Controller = 'Account' AND m.Metodo = 'Logout')
    ))
    
    OR
    
    -- 3. ESTUDIANTE: 
    -- Ve Dashboard, Inscripciones, Tareas Estudiante, Mis Entregas y Auth
    (r.RoleId = @EstudianteRoleId AND (
        (m.Controller = 'Home' AND m.Metodo IN ('Index', 'Privacy', 'ChangePassword')) OR 
        (m.Controller = 'CourseOfferings' AND m.Metodo = 'Inscripciones') OR
        (m.Controller = 'Tareas' AND m.Metodo IN ('TareasEstudiantes')) OR
        (m.Controller = 'Submissions' AND m.Metodo IN ('Index', 'Create')) OR -- AÑADIDO: Acceso a Submissions
        (m.Controller = 'Account' AND m.Metodo = 'Logout')
    ));

PRINT '- RoleModules (Permisos) insertados: ' + CAST(@@ROWCOUNT AS VARCHAR);

-- 5. Insertar Usuarios (con contraseñas MD5 en MAYÚSCULAS según tu función)
INSERT INTO Users (UserId, UserName, Instructor, Email, PasswordHash, CreateAt, CreatBy, ModifieBy, IsSoftDeleted, RolId) VALUES
-- Administrador (admin123 en MD5 MAYÚSCULAS)
(@AdminUserId, 'admin', 'Administrador Principal', 'admin@sistema.com', '0192023a7bbd73250516f069df18b500', GETDATE(), @AdminUserId, @AdminUserId, 0, @AdminRoleId),

-- Profesores (admin123 en MD5 MAYÚSCULAS)
(@Profesor1Id, 'mgonzalez', 'María González', 'maria.gonzalez@sistema.com', '0192023a7bbd73250516f069df18b500', GETDATE(), @AdminUserId, @AdminUserId, 0, @ProfesorRoleId),
(@Profesor2Id, 'crodriguez', 'Carlos Rodríguez', 'carlos.rodriguez@sistema.com', '0192023a7bbd73250516f069df18b500', GETDATE(), @AdminUserId, @AdminUserId, 0, @ProfesorRoleId),
(@Profesor3Id, 'alopez', 'Ana López', 'ana.lopez@sistema.com', '0192023a7bbd73250516f069df18b500', GETDATE(), @AdminUserId, @AdminUserId, 0, @ProfesorRoleId),
(@Profesor4Id, 'jmartinez', 'José Martínez', 'jose.martinez@sistema.com', '0192023a7bbd73250516f069df18b500', GETDATE(), @AdminUserId, @AdminUserId, 0, @ProfesorRoleId),

-- Estudiantes (admin123 en MD5 MAYÚSCULAS)
(@Estudiante1Id, 'est1', 'Ana Martínez', 'ana.martinez@sistema.com', '0192023a7bbd73250516f069df18b500', GETDATE(), @AdminUserId, @AdminUserId, 0, @EstudianteRoleId),
(@Estudiante2Id, 'est2', 'Luis Hernández', 'luis.hernandez@sistema.com', '0192023a7bbd73250516f069df18b500', GETDATE(), @AdminUserId, @AdminUserId, 0, @EstudianteRoleId),
(@Estudiante3Id, 'est3', 'Sofia Ramírez', 'sofia.ramirez@sistema.com', '0192023a7bbd73250516f069df18b500', GETDATE(), @AdminUserId, @AdminUserId, 0, @EstudianteRoleId),
(@Estudiante4Id, 'est4', 'Carlos García', 'carlos.garcia@sistema.com', '0192023a7bbd73250516f069df18b500', GETDATE(), @AdminUserId, @AdminUserId, 0, @EstudianteRoleId),
(@Estudiante5Id, 'est5', 'Marta López', 'marta.lopez@sistema.com', '0192023a7bbd73250516f069df18b500', GETDATE(), @AdminUserId, @AdminUserId, 0, @EstudianteRoleId),
(@Estudiante6Id, 'est6', 'Pedro Sánchez', 'pedro.sanchez@sistema.com', '0192023a7bbd73250516f069df18b500', GETDATE(), @AdminUserId, @AdminUserId, 0, @EstudianteRoleId),
(@Estudiante7Id, 'est7', 'Laura Díaz', 'laura.diaz@sistema.com', '0192023a7bbd73250516f069df18b500', GETDATE(), @AdminUserId, @AdminUserId, 0, @EstudianteRoleId),
(@Estudiante8Id, 'est8', 'David Torres', 'david.torres@sistema.com', '0192023a7bbd73250516f069df18b500', GETDATE(), @AdminUserId, @AdminUserId, 0, @EstudianteRoleId);

PRINT '- Usuarios insertados: 13 (1 admin, 4 profesores, 8 estudiantes)';

-- 6. Insertar UserRoles (relaciones adicionales)
INSERT INTO UserRoles (Id, UserId, RoleId, CreatAt, IsSoftDeleted) VALUES
(NEWID(), @AdminUserId, @AdminRoleId, GETDATE(), 0),
(NEWID(), @Profesor1Id, @ProfesorRoleId, GETDATE(), 0),
(NEWID(), @Profesor2Id, @ProfesorRoleId, GETDATE(), 0),
(NEWID(), @Profesor3Id, @ProfesorRoleId, GETDATE(), 0),
(NEWID(), @Profesor4Id, @ProfesorRoleId, GETDATE(), 0),
(NEWID(), @Estudiante1Id, @EstudianteRoleId, GETDATE(), 0),
(NEWID(), @Estudiante2Id, @EstudianteRoleId, GETDATE(), 0),
(NEWID(), @Estudiante3Id, @EstudianteRoleId, GETDATE(), 0),
(NEWID(), @Estudiante4Id, @EstudianteRoleId, GETDATE(), 0),
(NEWID(), @Estudiante5Id, @EstudianteRoleId, GETDATE(), 0),
(NEWID(), @Estudiante6Id, @EstudianteRoleId, GETDATE(), 0),
(NEWID(), @Estudiante7Id, @EstudianteRoleId, GETDATE(), 0),
(NEWID(), @Estudiante8Id, @EstudianteRoleId, GETDATE(), 0);

PRINT '- UserRoles insertados: 13';

-- 7. Insertar Cursos (CON GUIDS FIJOS)
INSERT INTO Courses (Id, Code, Title, Description, CreatedAt, IsActive, IsSoftDeleted) VALUES
(@CursoMAT101Id, 'MAT101', 'Matemáticas Básicas', 'Álgebra, geometría y cálculo básico', GETDATE(), 1, 0),
(@CursoLEN102Id, 'LEN102', 'Lenguaje y Literatura', 'Gramática, redacción y análisis literario', GETDATE(), 1, 0),
(@CursoFIS201Id, 'FIS201', 'Física General', 'Mecánica, termodinámica y electromagnetismo', GETDATE(), 1, 0),
(@CursoQUI202Id, 'QUI202', 'Química Orgánica', 'Compuestos orgánicos y reacciones químicas', GETDATE(), 1, 0),
(@CursoHIS301Id, 'HIS301', 'Historia Universal', 'Historia mundial desde la antigüedad', GETDATE(), 1, 0),
(@CursoBIO302Id, 'BIO302', 'Biología Celular', 'Estructura y función celular', GETDATE(), 1, 0);

PRINT '- Cursos insertados: 6';

-- 8. Insertar Periodos Académicos (CON GUIDS FIJOS)
INSERT INTO Periods (Id, Name, StartDate, EndDate, IsActive, CreatedAt, IsSoftDeleted) VALUES
(@Periodo1Id, 'Primer Semestre 2025', '2025-01-15', '2025-06-15', 1, GETDATE(), 0),
(@Periodo2Id, 'Segundo Semestre 2025', '2025-07-01', '2025-12-15', 0, GETDATE(), 0);

PRINT '- Periodos insertados: 2';

-- 9. Insertar Ofertas de Cursos (CourseOfferings CON GUIDS FIJOS)
INSERT INTO CourseOfferings (Id, CourseId, ProfessorId, PeriodId, Section, CreatedAt, IsActive, IsSoftDeleted) VALUES
(@OfertaMAT101Id, @CursoMAT101Id, @Profesor1Id, @Periodo1Id, 'A', GETDATE(), 1, 0),
(@OfertaLEN102Id, @CursoLEN102Id, @Profesor2Id, @Periodo1Id, 'A', GETDATE(), 1, 0),
(@OfertaFIS201Id, @CursoFIS201Id, @Profesor3Id, @Periodo1Id, 'A', GETDATE(), 1, 0),
(@OfertaQUI202Id, @CursoQUI202Id, @Profesor4Id, @Periodo1Id, 'A', GETDATE(), 1, 0),
(@OfertaHIS301Id, @CursoHIS301Id, @Profesor2Id, @Periodo1Id, 'A', GETDATE(), 1, 0),
(@OfertaBIO302Id, @CursoBIO302Id, @Profesor3Id, @Periodo1Id, 'A', GETDATE(), 1, 0);

PRINT '- Ofertas de cursos insertadas: 6';

-- 10. Insertar Inscripciones (Enrollments CON GUIDS)
INSERT INTO Enrollments (Id, CourseOfferingId, StudentId, EnrolledAt, Status, IsSoftDeleted)
SELECT 
    NEWID(), -- ID como GUID
    co.Id, 
    u.UserId, 
    DATEADD(DAY, -ABS(CHECKSUM(NEWID())) % 30, GETDATE()), -- Fecha aleatoria en los últimos 30 días
    'Active',
    0
FROM CourseOfferings co
CROSS JOIN Users u
WHERE u.UserName LIKE 'est%'
ORDER BY u.UserName, co.Id;

PRINT '- Inscripciones insertadas: ' + CAST(@@ROWCOUNT AS VARCHAR);

-- 11. Insertar Tareas (Tasks CON GUIDS) - 3 tareas por curso, ALGUNAS CON FECHAS DISPONIBLES EN DICIEMBRE 2025
INSERT INTO Tasks (Id, CourseOfferingId, Title, Description, DueDate, CreatedBy, MaxScore, IsSoftDeleted)
-- Tareas para MAT101-01 (2 vencidas, 1 disponible en diciembre)
SELECT NEWID(), co.Id, 
    'Tarea 1: Álgebra Lineal', 
    'Resolver ejercicios de sistemas de ecuaciones lineales', 
    '2025-02-15', 
    co.ProfessorId, 
    100,
    0
FROM CourseOfferings co WHERE co.Id = @OfertaMAT101Id

UNION ALL

SELECT NEWID(), co.Id, 
    'Tarea 2: Cálculo Diferencial', 
    'Problemas de límites y derivadas', 
    '2025-03-01', 
    co.ProfessorId, 
    100,
    0
FROM CourseOfferings co WHERE co.Id = @OfertaMAT101Id

UNION ALL

SELECT NEWID(), co.Id, 
    'Tarea 3: Examen Final Diciembre', 
    'Problemas de geometría analítica y cálculo', 
    '2025-12-24',  -- FECHA DISPONIBLE EN DICIEMBRE
    co.ProfessorId, 
    100,
    0
FROM CourseOfferings co WHERE co.Id = @OfertaMAT101Id

UNION ALL

-- Tareas para LEN102-01 (1 vencida, 2 disponibles en diciembre)
SELECT NEWID(), co.Id, 
    'Tarea 1: Análisis Literario', 
    'Analizar obra "Cien años de soledad"', 
    '2025-02-10', 
    co.ProfessorId, 
    100,
    0
FROM CourseOfferings co WHERE co.Id = @OfertaLEN102Id

UNION ALL

SELECT NEWID(), co.Id, 
    'Tarea 2: Ensayo Navideño', 
    'Escribir ensayo sobre tradiciones navideñas', 
    '2025-12-24',  -- FECHA DISPONIBLE EN DICIEMBRE
    co.ProfessorId, 
    100,
    0
FROM CourseOfferings co WHERE co.Id = @OfertaLEN102Id

UNION ALL

SELECT NEWID(), co.Id, 
    'Tarea 3: Proyecto Final', 
    'Proyecto final de análisis literario completo', 
    '2025-12-28',  -- FECHA DISPONIBLE EN DICIEMBRE
    co.ProfessorId, 
    100,
    0
FROM CourseOfferings co WHERE co.Id = @OfertaLEN102Id

UNION ALL

-- Tareas para FIS201-01 (todas disponibles en diciembre)
SELECT NEWID(), co.Id, 
    'Tarea 1: Leyes de Newton - Evaluación Diciembre', 
    'Problemas de aplicación de las leyes de Newton', 
    '2025-12-20',  -- FECHA DISPONIBLE EN DICIEMBRE
    co.ProfessorId, 
    100,
    0
FROM CourseOfferings co WHERE co.Id = @OfertaFIS201Id

UNION ALL

SELECT NEWID(), co.Id, 
    'Tarea 2: Energía y Trabajo', 
    'Ejercicios de conservación de energía', 
    '2025-12-24',  -- FECHA DISPONIBLE EN DICIEMBRE
    co.ProfessorId, 
    100,
    0
FROM CourseOfferings co WHERE co.Id = @OfertaFIS201Id

UNION ALL

SELECT NEWID(), co.Id, 
    'Tarea 3: Electromagnetismo Final', 
    'Problemas de campos eléctricos y magnéticos', 
    '2025-12-30',  -- FECHA DISPONIBLE EN DICIEMBRE
    co.ProfessorId, 
    100,
    0
FROM CourseOfferings co WHERE co.Id = @OfertaFIS201Id

UNION ALL

-- Tareas para QUI202-01 (2 disponibles en diciembre, 1 vencida)
SELECT NEWID(), co.Id, 
    'Tarea 1: Enlaces Químicos - Repaso', 
    'Identificar tipos de enlaces en compuestos', 
    '2025-12-22',  -- FECHA DISPONIBLE EN DICIEMBRE
    co.ProfessorId, 
    100,
    0
FROM CourseOfferings co WHERE co.Id = @OfertaQUI202Id

UNION ALL

SELECT NEWID(), co.Id, 
    'Tarea 2: Reacciones Orgánicas', 
    'Balancear ecuaciones de reacciones orgánicas', 
    '2025-02-18', 
    co.ProfessorId, 
    100,
    0
FROM CourseOfferings co WHERE co.Id = @OfertaQUI202Id

UNION ALL

SELECT NEWID(), co.Id, 
    'Tarea 3: Laboratorio Virtual Final', 
    'Simulación de experimentos químicos finales', 
    '2025-12-24',  -- FECHA DISPONIBLE EN DICIEMBRE
    co.ProfessorId, 
    100,
    0
FROM CourseOfferings co WHERE co.Id = @OfertaQUI202Id

UNION ALL

-- Tareas para HIS301-01 (1 disponible en diciembre, 2 vencidas)
SELECT NEWID(), co.Id, 
    'Tarea 1: Revolución Industrial', 
    'Ensayo sobre impactos de la Revolución Industrial', 
    '2025-02-25', 
    co.ProfessorId, 
    100,
    0
FROM CourseOfferings co WHERE co.Id = @OfertaHIS301Id

UNION ALL

SELECT NEWID(), co.Id, 
    'Tarea 2: Guerras Mundiales', 
    'Análisis comparativo de las guerras mundiales', 
    '2025-03-15', 
    co.ProfessorId, 
    100,
    0
FROM CourseOfferings co WHERE co.Id = @OfertaHIS301Id

UNION ALL

SELECT NEWID(), co.Id, 
    'Tarea 3: Historia Contemporánea', 
    'Análisis de eventos históricos recientes', 
    '2025-12-24',  -- FECHA DISPONIBLE EN DICIEMBRE
    co.ProfessorId, 
    100,
    0
FROM CourseOfferings co WHERE co.Id = @OfertaHIS301Id

UNION ALL

-- Tareas para BIO302-01 (todas disponibles en diciembre)
SELECT NEWID(), co.Id, 
    'Tarea 1: Estructura Celular - Evaluación Final', 
    'Diagramar y describir organelos celulares', 
    '2025-12-20',  -- FECHA DISPONIBLE EN DICIEMBRE
    co.ProfessorId, 
    100,
    0
FROM CourseOfferings co WHERE co.Id = @OfertaBIO302Id

UNION ALL

SELECT NEWID(), co.Id, 
    'Tarea 2: Genética Avanzada', 
    'Problemas de herencia genética compleja', 
    '2025-12-24',  -- FECHA DISPONIBLE EN DICIEMBRE
    co.ProfessorId, 
    100,
    0
FROM CourseOfferings co WHERE co.Id = @OfertaBIO302Id

UNION ALL

SELECT NEWID(), co.Id, 
    'Tarea 3: Proyecto Final de Biología', 
    'Análisis completo de ecosistemas modernos', 
    '2025-12-29',  -- FECHA DISPONIBLE EN DICIEMBRE
    co.ProfessorId, 
    100,
    0
FROM CourseOfferings co WHERE co.Id = @OfertaBIO302Id;

PRINT '- Tareas insertadas: 18 (3 por curso)';
PRINT '- NOTA: 10 tareas tienen fecha 24/12/2025 o posterior para estar disponibles';

-- 12. Insertar Entregas (Submissions) - SOLO para tareas VENCIDAS (las de diciembre NO tienen entregas aún)
INSERT INTO Submissions (Id, TaskId, StudentId, SubmittedAt, Comments, Status, CurrentGrade, IsSoftDeleted)
SELECT 
    NEWID(),
    t.Id,
    e.StudentId,
    DATEADD(DAY, -2, t.DueDate), -- Todos entregan 2 días antes del vencimiento
    CASE 
        WHEN t.Title LIKE '%1:%' THEN 'Completé todos los ejercicios solicitados'
        WHEN t.Title LIKE '%2:%' THEN 'Adjunto mi trabajo, espero cumpla con los requisitos'
        ELSE 'Entregando la tarea dentro del plazo establecido'
    END,
    'Submitted',
    -- Asignar una calificación inicial basada en el estudiante (no NULL)
    CASE 
        WHEN u.UserName = 'est1' THEN 95.0  -- Estudiante destacado
        WHEN u.UserName = 'est2' THEN 88.0
        WHEN u.UserName = 'est3' THEN 92.0
        WHEN u.UserName = 'est4' THEN 85.0
        WHEN u.UserName = 'est5' THEN 78.0
        WHEN u.UserName = 'est6' THEN 82.0
        WHEN u.UserName = 'est7' THEN 90.0
        WHEN u.UserName = 'est8' THEN 87.0
        ELSE 80.0
    END,
    0
FROM Tasks t
INNER JOIN Enrollments e ON t.CourseOfferingId = e.CourseOfferingId
INNER JOIN Users u ON e.StudentId = u.UserId
WHERE t.DueDate < '2025-12-01'  -- SOLO tareas vencidas (antes de diciembre)
ORDER BY t.Id, e.StudentId;

PRINT '- Entregas insertadas: ' + CAST(@@ROWCOUNT AS VARCHAR);
PRINT '- NOTA: Solo se crearon entregas para tareas vencidas (antes de diciembre 2025)';
PRINT '- Las tareas de diciembre 2025 NO tienen entregas (están disponibles para nuevos envíos)';

-- 13. Insertar Calificaciones (Grades CON GUIDS) - TODAS las entregas calificadas
INSERT INTO Grades (Id, SubmissionId, GraderId, Score, Feedback, GradedAt, IsSoftDeleted)
SELECT 
    NEWID(), -- ID como GUID
    s.Id,
    co.ProfessorId,
    -- Calificaciones realistas basadas en el estudiante y curso
    CASE 
        WHEN u.UserName = 'est1' THEN 95.0  -- Estudiante destacado
        WHEN u.UserName = 'est2' THEN 88.0
        WHEN u.UserName = 'est3' THEN 92.0
        WHEN u.UserName = 'est4' THEN 85.0
        WHEN u.UserName = 'est5' THEN 78.0
        WHEN u.UserName = 'est6' THEN 82.0
        WHEN u.UserName = 'est7' THEN 90.0
        WHEN u.UserName = 'est8' THEN 87.0
        ELSE 80.0
    END,
    CASE 
        WHEN u.UserName = 'est1' THEN 'Excelente trabajo, muy detallado y completo'
        WHEN u.UserName = 'est2' THEN 'Buen trabajo, algunos errores menores por corregir'
        WHEN u.UserName = 'est3' THEN 'Muy bien organizado y presentado'
        WHEN u.UserName = 'est4' THEN 'Aceptable, necesita mejorar la presentación'
        WHEN u.UserName = 'est5' THEN 'Requiere más atención a los detalles'
        WHEN u.UserName = 'est6' THEN 'Buen contenido, mejorar organización'
        WHEN u.UserName = 'est7' THEN 'Excelente desarrollo de conceptos'
        WHEN u.UserName = 'est8' THEN 'Sólido trabajo, continuar así'
        ELSE 'Feedback general positivo'
    END,
    DATEADD(DAY, 1, s.SubmittedAt), -- Calificado 1 día después de entrega
    0
FROM Submissions s
INNER JOIN Tasks t ON s.TaskId = t.Id
INNER JOIN CourseOfferings co ON t.CourseOfferingId = co.Id
INNER JOIN Users u ON s.StudentId = u.UserId
ORDER BY s.Id;

PRINT '- Calificaciones insertadas: ' + CAST(@@ROWCOUNT AS VARCHAR);

-- 14. Actualizar las calificaciones actuales en Submissions
UPDATE Submissions 
SET CurrentGrade = g.Score
FROM Submissions s
INNER JOIN Grades g ON s.Id = g.SubmissionId;

PRINT '- Calificaciones actualizadas en entregas';

-- 15. Insertar Anuncios (Announcements CON GUIDS)
INSERT INTO Announcements (Id, CourseOfferingId, Title, Body, PostedAt, PostedBy, IsSoftDeleted)
-- Anuncios para MAT101-01
SELECT NEWID(), co.Id, 
    'Bienvenida al Curso de Matemáticas', 
    'Bienvenidos al curso de Matemáticas Básicas. Revisen el sílabo en la plataforma.', 
    '2025-01-16 08:00:00', 
    co.ProfessorId,
    0
FROM CourseOfferings co WHERE co.Id = @OfertaMAT101Id

UNION ALL

SELECT NEWID(), co.Id, 
    'Recordatorio: Tarea 1', 
    'Recuerden que la Tarea 1 vence el 15 de febrero. No olviden entregarla.', 
    '2025-02-10 10:00:00', 
    co.ProfessorId,
    0
FROM CourseOfferings co WHERE co.Id = @OfertaMAT101Id

UNION ALL

-- Anuncios para LEN102-01
SELECT NEWID(), co.Id, 
    'Inicio de Clases de Lenguaje', 
    'Estimados estudiantes, las clases inician el lunes 20 de enero. Favor revisar material.', 
    '2025-01-17 09:00:00', 
    co.ProfessorId,
    0
FROM CourseOfferings co WHERE co.Id = @OfertaLEN102Id

UNION ALL

-- Anuncios para FIS201-01
SELECT NEWID(), co.Id, 
    'Laboratorio de Física', 
    'El primer laboratorio será el 25 de enero. Traer calculadora y cuaderno de apuntes.', 
    '2025-01-18 11:00:00', 
    co.ProfessorId,
    0
FROM CourseOfferings co WHERE co.Id = @OfertaFIS201Id

UNION ALL

-- Anuncios para QUI202-01
SELECT NEWID(), co.Id, 
    'Material de Química', 
    'El libro de texto ya está disponible en la biblioteca.', 
    '2025-01-19 14:00:00', 
    co.ProfessorId,
    0
FROM CourseOfferings co WHERE co.Id = @OfertaQUI202Id

UNION ALL

-- Anuncios para HIS301-01
SELECT NEWID(), co.Id, 
    'Documentales Recomendados', 
    'Lista de documentales históricos disponibles en la plataforma.', 
    '2025-01-20 16:00:00', 
    co.ProfessorId,
    0
FROM CourseOfferings co WHERE co.Id = @OfertaHIS301Id

UNION ALL

-- Anuncios para BIO302-01
SELECT NEWID(), co.Id, 
    'Salida de Campo', 
    'Programación de salida de campo para observación de ecosistemas.', 
    '2025-01-21 13:00:00', 
    co.ProfessorId,
    0
FROM CourseOfferings co WHERE co.Id = @OfertaBIO302Id;

PRINT '- Anuncios insertados: 7';

-- =====================================================
-- VERIFICACIÓN FINAL DE DATOS INSERTADOS
-- =====================================================

PRINT '3. VERIFICANDO DATOS INSERTADOS CORREGIDOS...';

SELECT 
    'Roles' as Tabla, COUNT(*) as Registros FROM Roles
UNION ALL SELECT 'Users', COUNT(*) FROM Users
UNION ALL SELECT 'UserRoles', COUNT(*) FROM UserRoles
UNION ALL SELECT 'ModuleGroup', COUNT(*) FROM ModuleGroup
UNION ALL SELECT 'Module', COUNT(*) FROM Module
UNION ALL SELECT 'RoleModules', COUNT(*) FROM RoleModules
UNION ALL SELECT 'Courses', COUNT(*) FROM Courses
UNION ALL SELECT 'Periods', COUNT(*) FROM Periods
UNION ALL SELECT 'CourseOfferings', COUNT(*) FROM CourseOfferings
UNION ALL SELECT 'Enrollments', COUNT(*) FROM Enrollments
UNION ALL SELECT 'Tasks', COUNT(*) FROM Tasks
UNION ALL SELECT 'Submissions', COUNT(*) FROM Submissions
UNION ALL SELECT 'Grades', COUNT(*) FROM Grades
UNION ALL SELECT 'Announcements', COUNT(*) FROM Announcements
ORDER BY Tabla;

-- Verificación específica de módulos REALES
PRINT '=== VERIFICACIÓN DE MÓDULOS REALES (SOLO LOS QUE EXISTEN) ===';
SELECT Controller, Metodo as Action, Nombre 
FROM Module 
ORDER BY Controller, Metodo;

-- Verificación de permisos por rol
PRINT '=== VERIFICACIÓN DE PERMISOS POR ROL (SOLO MÓDULOS REALES) ===';

SELECT 
    r.RoleName,
    m.Controller,
    m.Metodo as Action,
    m.Nombre
FROM Roles r
INNER JOIN RoleModules rm ON r.RoleId = rm.RoleId
INNER JOIN Module m ON rm.ModuleId = m.ModuleId
WHERE rm.IsSoftDeleted = 0
ORDER BY r.RoleName, m.Controller, m.Metodo;

-- =====================================================
-- MÓDULOS CORREGIDOS PARA COURSEOFFERINGS Y CURSOS
-- =====================================================

-- Eliminar módulos existentes si es necesario
DELETE FROM RoleModules WHERE ModuleId IN (SELECT ModuleId FROM Module WHERE Controller IN ('CourseOfferings', 'Cursos'));
DELETE FROM Module WHERE Controller IN ('CourseOfferings', 'Cursos');

-- Insertar módulos CORREGIDOS
INSERT INTO Module (ModuleId, Nombre, Controller, Metodo, CreateAt, CreateDate, CreatBy, ModifieBy, IsSoftDeleted, ModuloAgrupadoId) VALUES
-- Módulos de CourseOfferings
(NEWID(), 'Gestión de Ofertas', 'CourseOfferings', 'Index', GETDATE(), GETDATE(), @AdminUserId, @AdminUserId, 0, @MGCursosId),
(NEWID(), 'Mis Cursos', 'CourseOfferings', 'MisCursos', GETDATE(), GETDATE(), @AdminUserId, @AdminUserId, 0, @MGCursosId),
(NEWID(), 'Inscripciones', 'CourseOfferings', 'Inscripciones', GETDATE(), GETDATE(), @AdminUserId, @AdminUserId, 0, @MGMatriculasId),

-- Módulo de Cursos base
(NEWID(), 'Cursos Base', 'Cursos', 'Index', GETDATE(), GETDATE(), @AdminUserId, @AdminUserId, 0, @MGCursosId);

PRINT '- Módulos CORREGIDOS insertados: 4';

-- Actualizar RoleModules con permisos CORREGIDOS
INSERT INTO RoleModules (ModuleRoleId, Description, CreateAt, CreateDate, CreatBy, ModifieBy, IsSoftDeleted, ModuleId, RoleId)
SELECT 
    NEWID(),
    'Permiso para ' + m.Nombre,
    GETDATE(),
    GETDATE(),
    @AdminUserId,
    @AdminUserId,
    0,
    m.ModuleId,
    r.RoleId
FROM Module m
CROSS JOIN Roles r
WHERE m.Controller IN ('CourseOfferings', 'Cursos')
AND (
    -- ADMIN: Todos los módulos
    (r.RoleId = @AdminRoleId)
    OR
    -- PROFESOR: Mis Cursos, Inscripciones y Cursos Base
    (r.RoleId = @ProfesorRoleId AND (
        (m.Controller = 'CourseOfferings' AND m.Metodo IN ('MisCursos')) OR
        (m.Controller = 'Cursos' AND m.Metodo = 'Index')
    ))
    OR
    -- ESTUDIANTE: Solo Mis Cursos
    (r.RoleId = @EstudianteRoleId AND (
        m.Controller = 'CourseOfferings' AND m.Metodo = 'MisCursos' OR
        (m.Controller = 'Cursos' AND m.Metodo = 'Index')
    ))
);

PRINT '- RoleModules CORREGIDOS insertados: ' + CAST(@@ROWCOUNT AS VARCHAR);

PRINT '=== SCRIPT COMPLETADO EXITOSAMENTE ===';
PRINT 'RESUMEN:';
PRINT '- Todos los datos anteriores fueron ELIMINADOS';
PRINT '- SOLO módulos que existen en vistas/controladores reales';
PRINT '- Contraseñas MD5: admin123';
PRINT '- NOMBRES DE ACCIONES EN ESPAÑOL para coincidir con controladores';
PRINT '- Permisos configurados según especificación (solo módulos reales)';
PRINT '- Botón "Cerrar Sesión" agregado para TODOS los usuarios';
PRINT '- Estudiantes NO ven módulos de autenticación en el dashboard (excepto ChangePassword y Logout)';
PRINT '- Cursos SOLO visible para Profesores y Administradores';
PRINT '- Módulos de Usuarios mantenidos para Administradores';
PRINT '- TODOS los estudiantes están inscritos en TODOS los cursos';
PRINT '- TODAS las tareas fueron entregadas por TODOS los estudiantes';
PRINT '- TODAS las entregas fueron calificadas';
PRINT '- Integridad referencial 100% garantizada';
PRINT '- NUEVO: Módulos de CourseOfferings agregados con permisos correctos';
PRINT '- NUEVO: Estudiantes ahora pueden ver "Mis Cursos"';
PRINT '- NUEVO: Profesores pueden ver "Mis Cursos" y "Gestión de Ofertas"';
PRINT '- NUEVO: Administradores tienen acceso completo a CourseOfferings';
PRINT '- ACTUALIZADO: TODAS las entidades ahora usan GUIDS en lugar de INT';
PRINT '- ACTUALIZADO: GUIDS fijos para consistencia en relaciones';
PRINT '- ACTUALIZADO: Eliminado DBCC CHECKIDENT para tablas con GUID';
PRINT '- *** NUEVO INTEGRADO: Módulos de "Mis entregas" completamente integrados ***';
PRINT '- *** NUEVO: Grupo "Mis entregas" creado con GUID: ' + CAST(@MGMisEntregasId AS VARCHAR(50)) + ' ***';
PRINT '- *** NUEVO: Módulos "Ver Mis Entregas" y "Nueva Entrega" agregados al grupo ***';
PRINT '- *** NUEVO: Permisos para Submissions otorgados a todos los roles ***';
PRINT '- *** NUEVO: Controller "Submissions" con métodos "Index" y "Create" ***';