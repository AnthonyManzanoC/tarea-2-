# SRS v1 - CoronelExpress (Baseline Candidate)

## Alcance
Aplicación web (ASP.NET Core MVC) orientada a gestión de productos, pedidos y administración.

## Requisitos funcionales (RF)
- REQ-001: El sistema debe permitir registro e inicio de sesión de usuarios.
- REQ-002: El sistema debe permitir listar productos disponibles.
- REQ-003: El sistema debe permitir agregar productos a un carrito.
- REQ-004: El sistema debe permitir confirmar un pedido y generar un registro/número de orden.
- REQ-005: El sistema debe permitir que un administrador cree/edite/elimine productos.
- REQ-006: El sistema debe permitir consultar pedidos y su estado.

## Requisitos no funcionales (RNF)
- NFR-001: El repositorio debe permitir reconstruir una versión exacta mediante un tag (ej. v1.0).
- NFR-002: Los cambios deben tener trazabilidad mediante commits claros y control mínimo de cambios (ramas/PR).

## Criterios mínimos de aceptación
- Existen SRS v1 y SDD v1 en /docs.
- Existe config.example en /config.
- Existe un tag v1.0 y Release v1.0 como línea base.
- REQ-007: El sistema debe permitir actualizar el motivo de un turno/pedido existente.
