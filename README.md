# CoronelExpress - Baseline (GCS Semana 2)

## Objetivo
Este repositorio funciona como depósito de Elementos de Configuración (CI) y demuestra la creación de una Línea Base (Baseline v1.0) con tags y Release para asegurar reproducibilidad, auditoría y trazabilidad.

## Estructura del repositorio
- /docs
  - /SRS -> requisitos (SRS)
  - /SDD -> diseño/arquitectura (SDD)
- /config -> configuración controlada (ejemplo)
- /tests -> pruebas placeholder
- /scripts -> scripts auxiliares
- CHANGELOG.md -> registro de cambios
- README.md -> guía

## Cómo ejecutar (ASP.NET Core)
1) Restaurar:
   dotnet restore
2) Ejecutar:
   dotnet run
3) Abrir la URL que salga en consola.

## Cómo crear Baseline (v1.0)
1) Verificar que existen: SRS_v1, SDD_v1, código mínimo, config.example.
2) Crear tag:
   git tag -a v1.0 -m "Baseline v1.0: SRS+SDD approved + minimal build"
   git push origin v1.0
3) Crear Release v1.0 en GitHub indicando qué incluye, qué está aprobado y cómo verificar.
