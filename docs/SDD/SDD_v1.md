# SDD v1 - CoronelExpress (Diseño / Arquitectura)

## Arquitectura (alto nivel)
ASP.NET Core MVC:
- Presentación: Controllers + Views (Razor)
- Lógica: Services (reglas de negocio)
- Datos: Data (DbContext/EF Core)
- Persistencia: Base de datos configurable

## Componentes
1) Controllers: reciben solicitudes y coordinan servicios.
2) Services: reglas de negocio (productos, pedidos, inventario).
3) Data/DbContext: acceso a datos con EF Core.
4) Views: UI del sistema (Razor Pages/Views).

## Flujo ejemplo (Pedido)
Usuario -> Productos -> Carrito -> Checkout/Order -> Confirmación -> Guardado en BD

## Decisiones técnicas
- Framework: ASP.NET Core MVC
- ORM: Entity Framework Core
- Control de configuración: /config con ejemplo controlado
- Línea base: tag v1.0 + Release; cambios posteriores con ramas y commits trazables.
