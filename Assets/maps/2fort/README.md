# 2Fort: preparación jugable

## Estado comprobado
La escena original Assets/Scenes/Escena 2fort.unity contiene el FBX desplazado (-7.90466, 0, 118.52348), sin jugador ni iluminación. Unity 6000.3.22f1 / URP 17.3.0. El documento recibido conserva el contexto histórico; los materiales resueltos se mantienen intactos.

## Crear y revisar
Tools > TF2 > 2Fort > Create Playable Scene crea Assets/maps/2fort/Scenes/2Fort Playable.unity. Si ya existe, simplemente la abre. Se reutilizan jugador existente con PlayerClass, armas, entrada, interfaz y audio de Escena de prueba, manteniendo sus referencias de escena. La escena original se conserva en disco.

Los assets nuevos viven en Assets/maps/2fort: Collision, Materials, Prefabs y Shaders. Las texturas y VMT originales conservan sus rutas y GUID. No mover a mano los assets importados.

La herramienta añade MeshCollider no convexo a geometría/props visibles, excluye skybox/plantillas/física auxiliar y elimina submeshes de agua de la malla de colisión. toolsnodraw conserva colisión: una superficie invisible puede seguir siendo sólida. Esta aproximación usa geometría visual; no reconstruye los hulls Source ni las entidades playerclip del BSP. Requiere recorrer escaleras, bordes, rejas, tejados y props; reducir colliders de decoración después de medir rendimiento.

Spawn_Exploration se busca con raycast y cápsula libre. Es provisional y puede quedar en un tejado. Colocarlo manualmente en un interior accesible y mover también Player al mismo punto antes de Play. La caída por debajo del mapa devuelve al jugador a este marcador. No hay natación, daño por ahogo ni respawn de muerte implementados.

## Puertas
Seleccionar el panel completo con sus handles y usar Tools > TF2 > 2Fort > Add Sliding Door To Selected Panel. La herramienta desempaqueta la instancia para permitir editar la jerarquía, crea un padre estacionario con trigger y TF2SlidingDoor. Ajustar Local Travel en los ejes LOCALES del padre, Speed y Close Delay. La dirección inicial es hacia arriba; las puertas laterales requieren ajuste. Revisar ambos lados y sincronizar las dos hojas mediante el mismo trigger cuando corresponda. No hay restricción de equipo ni detección de aplastamiento. Los prefabs guardados inicialmente necesitan actualizarse después de configurar puertas.

## Iluminación y materiales
Sol cálido, ambiente de tres colores y niebla lineal son una base de exploración, no una reproducción de las luces TF2. Pendiente recuperar light_environment, luces interiores y posiciones del BSP, ajustar ejes/offset, preparar UV2, lightmaps, probes y skybox.

Los cuatro reflectivity y las dos aguas reciben COPIAS de materiales; no se sobrescriben los originales. Reflectivity usa albedo gris aproximado. TF2/2Fort Water usa normal RGB animada, transparencia, Fresnel y niebla; no implementa refracción, reflexión real, overlay submarino ni mezcla por profundidad. La textura Source se espera como PNG RGB, no como Normal Map reimportada.

Pendiente WorldVertexTransition, basetexture2, ssbump (no asignar como normal convencional), envmaps, transparencia y emisión mediante rutas VMT confirmadas.

## Validación
Tools > TF2 > 2Fort > Audit Active Scene escribe Assets/maps/2fort/2Fort-audit.txt con jugador, colisiones, puertas y problemas de referencia básicos. No sustituye la prueba en Play. Recorrer RED/BLU, túneles, puente y agua; probar salto, armas, impacto contra paredes, entrada/salida de puertas y caída fuera del mapa.

2Fort_Player.prefab conserva componentes del jugador; Spawn y referencias UI externas pertenecen a la escena y deben reasignarse si se instancia este prefab en otra escena. El mapa no se añade automáticamente a Build Settings.

La petición temporal Temp/TF2Build.request permite crear una vez la escena al recompilar en Unity, únicamente cuando las escenas abiertas están guardadas. Resultado en Temp/TF2Build.result. No ejecuta nada sin ese archivo.

## Estado actualizado
La escena de exploración está guardada en Scenes/2Fort Playable.unity, con un jugador, 2.433 MeshCollider y cero puertas configuradas. Se conserva PlayerClass y los assets de Scout y Heavy. F1 selecciona Scout y F2 Heavy durante Play: aplica el controlador existente y reinicia vida/armas. EventSystem se recupera de la escena de prueba para conservar los botones de la interfaz. Todos los archivos específicos del mapa viven aquí: Scripts, Editor, Collision, Prefabs, Shaders, Scenes y Materials/Gameplay. Los materiales originales permanecen en Materials.

La escena aún requiere probar el recorrido y los cambios de clase en Play; no se afirma fidelidad visual a TF2 ni funcionamiento de puertas sin calibración.

## Ajuste de recorrido

La actualización `Tools > TF2 > 2Fort > Refresh Playable Scene` conserva la escena y reconstruye los colliders de toda malla visible y sólida. Agua, skybox, plantillas, física auxiliar y helpers de luces, environment, logic y sky camera quedan excluidos. El mapa SourceIO se importó con lights activas, pero trigger/info/logic apagados; estos helpers no son jugables.

El jugador conserva la caída vertical al moverse y usa un sondeo de suelo con normal caminable. Las escaleras usan la colisión original del mapa. En pendientes, el movimiento se proyecta sobre la superficie para conservar la velocidad y permitir subirlas. La cámara rota el Rigidbody en el paso físico y usa sensibilidad por píxel. Scout conserva su velocidad original de 10 unidades/s, Scattergun tiene 6/32 y la pistola 12/36. Las superficies opacas usan backface culling; TOOLSSKYBOX se vuelve transparente para mostrar el fondo de Unity.
