# Plataforma de Control de Tareas – ASP.NET Core 8

Aplicación web desarrollada en **ASP.NET Core 8 (MVC)** para la gestión de cursos, tareas, roles, entregas y calificaciones.  
Incluye autenticación por roles, navegación dinámica y administración educativa básica.  
Proyecto académico colaborativo desarrollado en equipo.

---

## Funcionalidades principales

### Autenticación y Roles
- Login con Claims + Cookies  
- Logout con limpieza de sesión  
- Roles:
  - Administrador  
  - Profesor  
  - Estudiante  
- Atributos personalizados de autorización:
  - `[ProfesorAuthorize]`
  - `[EstudianteAuthorize]`
  - `[ProfesorOAdminAuthorize]`

### Gestión Académica
- Cursos  
- Tareas  
- Entregas de estudiantes (Submissions)  
- Calificaciones  

### Características Arquitectónicas Avanzadas
- **Sistema de Auditoría:** Registro transaccional de acciones de usuarios con capacidad de exportación a CSV/JSON para cumplimiento y trazabilidad.
- **Eliminación Lógica (Soft Delete):** Implementación a nivel de base de datos para preservar el historial operativo en todas las entidades sin perder integridad referencial.
- **Gestión Académica Integral:** Estructura relacional robusta que soporta períodos académicos, ofertas de cursos dinámicas e inscripciones.
- **Dashboards Segregados:** Vistas y métricas renderizadas lógicamente según el nivel de acceso.

### Menú dinámico según rol
Renderizado desde `MenuServices` y `MenuItem`, mostrando solo las opciones permitidas.

### Base de Datos

Modelo relacional robusto con 12 entidades críticas, destacando:
- Users  
- Roles  
- UserRoles  
- Courses  
- CourseOfferings
- Enrollments
- Tareas  
- Submissions  
- AuditLogs

Configuraciones dentro de:

```text
Data/Configurations/
```

### Instalación del proyecto
1️. Clonar el repositorio
```text
git clone https://github.com/ejros2004/Plataforma-Academica.git
```

2️. Abrir en Visual Studio
	Archivo a abrir:
```text
Control De Tareas.sln
```

3️. Configurar cadena de conexión
Editar tu appsettings.json (local, NO subirlo):

```
JSON
"ConnectionStrings": {
  "DefaultConnection": "Server=TU_SERVIDOR\\INSTANCIA;Database=ControlDeTareasDB;User Id=USUARIO;Password=TU_PASSWORD;TrustServerCertificate=true"
}
```
📌 Cada desarrollador usa su propia conexión.
📌 No subir appsettings.json al repositorio.

### Migraciones (EF Core)

1. Crear una nueva migración:
```text
Add-Migration NombreDeMigracion
```

2.Aplicar migraciones existentes:
```text
Update-Database
```

3. Revertir última migración:
```text
Remove-Migration
```

### Cargar datos iniciales (Seed Data)
Después de ejecutar las migraciones, es obligatorio poblar la base de datos con la información inicial para que el sistema pueda funcionar correctamente.
1. Abrir **SQL Server Management Studio (SSMS)**.
2. Conectarse al servidor local.
3. Abrir el archivo ubicado en la siguiente ruta dentro del proyecto:
```text
Control De Tareas/Data/SeedData.sql
```
5. Ejecutar el script para insertar los datos iniciales en las tablas correspondientes.

### Usuarios de prueba del sistema
A continuación, las credenciales utilizadas en el proyecto:

🟥 Administrador
Correo:
```text
admin@sistema.com
```

Contraseña:
```text
admin123
```

🟦 Profesores
Usuarios:
```text
maria.gonzalez@sistema.com
```
```text
carlos.rodriguez@sistema.com
```
```text
ana.lopez@sistema.com
```
```text
jose.martinez@sistema.com
```

Contraseñas:
```text
admin123
```

🟩 Estudiantes
Usuarios:
```text
ana.martinez@sistema.com
```
```text
luis.hernandez@sistema.com
```
```text
sofia.ramirez@sistema.com
```
```text
carlos.garcia@sistema.com
```
```text
marta.lopez@sistema.com
```
```text
pedro.sanchez@sistema.com
```
```text
laura.diaz@sistema.com
```
```text
david.torres@sistema.com
```

Contraseñas:
```text
admin123
```

### Licencia
Proyecto académico — uso interno.
