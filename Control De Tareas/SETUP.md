# SETUP – Guía de Instalación y Configuración

Este documento explica cómo instalar, configurar y ejecutar el proyecto **Control de Tareas** en un entorno local.

---

# 1. Requisitos previos

- **Windows 10/11**
- **Visual Studio 2022** (v17.8 o superior)
- **.NET SDK 8.0**
- **SQL Server 2019 o 2022**
- **SSMS** (SQL Server Management Studio)
- **Git**

---

# 2. Clonar el repositorio

```
git clone https://github.com/ejros2004/Plataforma-Academica.git
```

Abrir:

```
Control De Tareas.sln
```

---

# 3. Configuración de la Base de Datos

Editar el archivo:

```
appsettings.json
```

Con tu conexión local:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=TU_SERVIDOR\\INSTANCIA;Database=ControlDeTareasDB;User Id=USUARIO;Password=PASSWORD;TrustServerCertificate=true"
}
```

📌 **Este archivo es local y NO debe subirse al repositorio.**

---

# 4. Ejecutar Migraciones

Abrir **Package Manager Console**:

```
Update-Database
```

Esto crea las tablas:

- Users  
- Roles  
- UserRoles  
- Courses  
- Tareas  
- Submissions  

---

# 5. Cargar Datos Iniciales (Seed Data)

Después de ejecutar las migraciones, es obligatorio poblar la base de datos con la información inicial para que el sistema pueda funcionar correctamente.

1. Abrir **SQL Server Management Studio (SSMS)**.
2. Conectarse al servidor local.
3. Abrir el archivo ubicado en la siguiente ruta dentro del proyecto:

```
Data\SQL QUERY\seed_data.sql
```

4. Verificar que la base de datos seleccionada sea `ControlDeTareasDB`.
5. Ejecutar el script presionando `F5` o el botón **Execute**.

Este paso inserta los registros base necesarios (usuarios, roles y datos iniciales) para permitir el inicio de sesión y el uso normal del sistema.

---

# 6. Estructura del Proyecto

```
Controllers/
    HomeController.cs
    CursosController.cs
    TareasController.cs

Models/
    CursosVm.cs
    TareasVm.cs
    MenuItem.cs

Data/
    Entitys/
        Users.cs
        Roles.cs
        UserRoles.cs
        Courses.cs
        Tareas.cs
        Submissions.cs

    Configurations/
        UsersConfig.cs
        RolesConfig.cs
        CoursesConfig.cs
        TareasConfig.cs
        UserRolesConfig.cs
        SubmissionsConfig.cs

Services/
    MenuServices.cs
```

---

# 7. Variables de entorno (opcional)

```
ASPNETCORE_ENVIRONMENT=Development
ConnectionStrings__DefaultConnection=<cadena segura>
```

---

# 8. Troubleshooting

### Error: "Login failed for user"
* Verificar usuario y contraseña en la cadena de conexión.

### Error: "Cannot open database"
* Ejecutar `Update-Database`.

### Error: Problemas después de migrar pero no permite iniciar sesión
* Asegurarse de haber ejecutado el script `seed_data.sql`.

### Error: "sp_getapplock" en migraciones
* Confirmar que SQL Server esté iniciado.

### Error por appsettings.json del compañero
* Cada desarrollador debe tener su propia cadena de conexión local.

---

# 9. Comentarios en Código

Se documentaron las clases principales:

- Controladores  
- Entidades  
- ViewModels  
- Servicios  
- Configuraciones EF Core  
- DbContext  

Documentación agregada utilizando **XML Comments** para mejorar mantenibilidad y comprensión del código.

---

# 10. Onboarding

Un nuevo desarrollador debe seguir este flujo:

1. Clonar el repositorio  
2. Configurar su cadena de conexión en `appsettings.json`  
3. Ejecutar `Update-Database`  
4. Ejecutar el script `seed_data.sql`  
5. Ejecutar el proyecto  
6. Iniciar sesión según el rol asignado  

---
