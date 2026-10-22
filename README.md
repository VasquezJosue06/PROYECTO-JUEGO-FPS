# Mercs vs. Machines

FPS 3D para PC, desarrollado en **Unity 6**. En esta versión, el jugador explora 2Fort, combate robots, recupera dosieres y alcanza el punto de extracción. Este documento resume el prototipo actual y compara sus funciones con el GDD de la Evaluación 1.

## Estado de los requisitos del GDD

| Requisito | Estado actual |
|---|---|
| Movimiento WASD a 5 unidades/s, ocho direcciones y salto de fuerza 5 | **Implementado.** El movimiento diagonal se normaliza. Scout y Heavy usan sus propias estadísticas. |
| Cámara controlada con ratón y cursor bloqueado | **Implementado.** La sensibilidad actual es de 0,12 grados por píxel. |
| Disparo, munición y recarga | **Implementado.** Las armas tienen cargador, efectos y recarga; la cadencia y duración varían según el arma. |
| Salud, HUD y pantalla de muerte | **Implementado.** Hay barra de vida, efectos al recibir daño y menú de muerte. El daño depende del arma o proyectil. |
| Robot con patrulla, persecución y ataque | **Implementado parcialmente.** La IA usa NavMesh, detecta hasta 20 unidades y ataca a 5. No se han completado todos los valores de visión, patrulla y ataque que especifica el GDD. |
| Dosier y objetivos de misión | **Implementado parcialmente.** El dosier se recoge automáticamente al acercarse a 1,5 unidades y se entrega en extracción. No se implementó la señal que actualizaría la ubicación aproximada del jugador para los robots cada 3 segundos. |
| Dos ambientes con efectos y medidas definidos | **No implementado según el GDD.** El mapa 2Fort es jugable, pero no se han configurado ni verificado los efectos de ambiente descritos. |
| Campaña de cuatro misiones y dificultad progresiva | **Pendiente.** El prototipo incluye una misión jugable; no contiene la campaña completa de cuatro misiones. |
| Puntuación, cronómetro y resumen de misión | **Pendiente.** No están implementados los sistemas de puntuación ni el resumen final descritos en el GDD. |
| Pagos o contribuciones del modelo F2P | **Excluido.** El GDD deja este sistema fuera del proyecto por alcance y complejidad. |

## Controles

- **W, A, S, D:** moverse · **Espacio:** saltar · **Ratón:** mirar y apuntar.
- **Clic izquierdo:** disparar · **R:** recargar.
- **F1 / F2:** seleccionar Scout / Heavy en 2Fort. Esta selección sirve para probar las clases y no representa una función final del juego.

## Escenas incluidas en la compilación

La lista de compilación contiene **Menú Principal**, **2Fort Playable** y **Escena de prueba**. El menú conduce a la escena jugable de 2Fort. La escena de prueba puede deshabilitarse desde Build Profiles si no se necesita en la entrega.

## Cómo abrir y compilar en Windows

1. Instala **Unity 6000.3.22f1** desde Unity Hub. Añade el módulo **Windows Build Support** si no está instalado.
2. Abre la carpeta raíz del proyecto, donde están `Assets`, `Packages` y `ProjectSettings`, y espera a que Unity importe los recursos.
3. En Unity, abre **File > Build Profiles** (o **File > Build Settings**, según la interfaz) y selecciona **Windows**.
4. Confirma que **Menú Principal** y **2Fort Playable** estén habilitadas. Mantén **Escena de prueba** solo si quieres incluirla.
5. Pulsa **Build** y guarda el resultado en `Ejecutable/`. Conserva juntos el archivo `.exe` y todos los archivos y carpetas que Unity genere a su lado.

La build de Windows se probó ejecutando el `.exe` y funcionó correctamente. Puedes descargar el archivo comprimido de **Build1** desde [este enlace de Google Drive](https://drive.google.com/file/d/1vnAWZ_B2vvTeUVNNkuItQ2pSQz9K65R2/view?usp=sharing). Descarga y extrae el archivo completo; luego ejecuta `MY FPS GAME.exe` desde la carpeta extraída. No muevas el `.exe` fuera de esa carpeta, porque necesita los archivos de datos que lo acompañan.

## Aclaraciones

La carpeta `Assets/maps/Textures/` se excluye del repositorio por su tamaño y no está incluida en esta copia. Por ello, al abrir el proyecto sin esas texturas, algunos materiales del mapa pueden verse distintos o incompletos. La build enlazada arriba se distribuye como versión jugable de referencia e incluye los recursos necesarios para ejecutarla.

**Documento de referencia:** GDD, *Mercs vs. Machines*, Evaluación 1 (INACAP 2026-2). Los estados reflejan el proyecto actual; “pendiente” o “parcial” indica que la función no cubre por completo los valores y el alcance descritos en el GDD.
