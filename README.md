# Mercs vs. Machines

**FPS 3D para PC, desarrollado en Unity 6.** El jugador explora el mapa 2Fort, combate robots, recoge dosieres y llega a extracción. Este README compara la versión actual con el GDD de Evaluación 1; describe el prototipo, no una campaña completa.

## Requisitos del GDD

| Requisito | Estado en esta versión |
|---|---|
| Movimiento WASD a 5 unidades/s, ocho direcciones, salto de fuerza 5 | **Cubierto.** Normaliza diagonales; las clases Scout y Heavy aplican sus propias estadísticas. |
| Cámara con ratón, cursor bloqueado, giro vertical limitado | **Cubierto.** Ratón y cursor bloqueado funcionan; la sensibilidad actual es 0,12 grados por píxel. |
| Disparo, cadencia de 0,15 s, munición, recarga con R | **Cubierto.** Armas con cargador, efectos y recarga; valores y comportamiento varían por arma, no se garantiza el cargador genérico ni recarga de 2 s para todas. |
| 100 de vida, daño, HUD y menú de muerte | **Cubierto.** Hay barra de vida, efectos de impacto y pantalla de muerte. El daño depende del arma/proyectil y no se garantiza un valor uniforme de 10. |
| Robot: patrulla, persecución y ataque | **Parcial.** La IA existente usa NavMesh, visión de hasta 20 unidades y ataque a 5; no implementa el ángulo de visión de 120° ni garantiza patrulla a 3 unidades/s, espera/rastreo y cadencia/daño de 0,8 s/10 indicados en el GDD. |
| Dosier recogible y objetivo de misión | **Parcial.** Se recoge automáticamente a 1,5 unidades y se transporta a extracción; el GDD pide  señal de ubicación aproximada cada 3 s para los robots, que no está implementada. |
| Dos ambientes con efectos y medidas definidos | **No cubierto.** Hay un mapa jugable 2Fort, pero las zonas y parámetros de ambiente del GDD no están implementados ni comprobados como requisito. |
| Campaña de cuatro misiones con progresión | **Fuera de alcance actual.** Hay una misión/escena jugable configurada, no la campaña de 4 misiones y conteos descrita. |
| Puntuación, cronómetro y resumen de misión | **Pendiente.** No se encontró implemento el sistema de puntuación ni del resumen solicitado. |
| Pagos/contribuciones del modelo F2P | **Excluido deliberadamente en el GDD.** No forma parte del prototipo. |

## Controles y contenido

- **WASD:** moverse · **Espacio:** saltar · **Ratón:** mirar y apuntar.
- **Clic izquierdo:** disparar · **R:** recargar · **E:** interactuar/recoger cuando la escena lo permita.
- **F1 / F2:** seleccionar Scout / Heavy en 2Fort, pensado mas como algo para testear las clases y no parte del gameplay.
- La compilación incluye **Menú Principal**, **2Fort Playable** y **Escena de prueba**. El menú inicia la escena jugable de 2Fort.

## Abrir y compilar en Windows

1. Instala **Unity 6000.3.22f1** con Unity Hub, incluyendo **Windows Build Support (IL2CPP)** si vas a elegir IL2CPP.
2. Abre en Unity Hub la carpeta raíz que contiene `Assets`, `Packages` y `ProjectSettings`. Espera a que Unity importe los recursos y resuelva los paquetes.
3. En Unity abre **File > Build Profiles** (o **File > Build Settings**, según la interfaz), selecciona **Windows**, confirma que las escenas de arriba estén habilitadas y elige **Build**.
4. Guarda el resultado en `Ejecutable/`. Ejecuta el `.exe` generado desde esa carpeta; conserva junto a él la carpeta de datos y cualquier archivo asociado que Unity produzca.

El proyecto fija la versión **6000.3.22f1** y las tres escenas anteriores en Build Settings. En este equipo está el editor Unity, pero no se encontró una carpeta instalada de `WindowsStandaloneSupport`; por ello no se pudo verificar ni generar aquí una compilación Windows.