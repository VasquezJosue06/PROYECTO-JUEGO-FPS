# Armas TF2: Scout y Heavy

Este sistema usa las clases existentes (`Classes`, `PlayerClass`) y el componente `Gun` de cada prefab.
No se han incluido AR, Glock ni otras armas en esta configuracion.

## Dónde editar

| Necesito cambiar | Archivo o Inspector |
| --- | --- |
| Vida, velocidad y armas permitidas | `Assets/Classes/Scout.asset` o `Heavy.asset` |
| Municion, cadencia, dispersion y dano | Componente `Gun` del prefab del arma |
| Color, material, velocidad y longitud de cola | `Assets/Prefabs/TF2/Shared/Resources/WeaponTracer.prefab` |
| Ancho y desvanecimiento al llegar al impacto | `Tracer Width` y `Tracer Lifetime` en `Gun` |
| Giro del tambor | Campos `Barrel...` de la minigun |
| Animaciones | Animator Controller de cada arma |

## Prefabs de jugador

- `Assets/Prefabs/TF2/Scout/ScatterGun.prefab`
- `Assets/Prefabs/TF2/Scout/ScoutPistol.prefab`
- `Assets/Prefabs/TF2/Heavy/HeavyMinigun.prefab`
- `Assets/Prefabs/TF2/Heavy/HeavyShotgun.prefab`

Los cuatro usan impacto instantaneo y tienen asignado el mismo prefab visual de trazadora.
Las trazadoras no aplican dano: una estela corta avanza hasta el impacto calculado por Gun y se desvanece alli. El shader suaviza los bordes y aclara el centro.
En el prefab compartido, `Travel Speed` controla la velocidad visual (160) y `Tail Length` la longitud maxima (1.4). `Tracer Lifetime` controla el desvanecimiento final; no la duracion del recorrido.
Scout conserva su municion, cadencia, dispersion y dano anterior de 10 por impacto; no se aplica caida de dano por distancia a sus dos armas.
Heavy conserva la configuracion establecida. Reserva -1 significa ilimitada; 0 significa vacia.

## Código

`Assets/Script/Gun.cs` conserva el componente original, sus campos del Inspector y su identificador.
Se reparte la implementacion en archivos `partial` dentro de `Assets/Script/Weapons`:

- `Gun.Firing.cs`: impactos, dispersion, sonido y fogonazo del disparo animado.
- `Gun.Reloading.cs`: recarga, transferencia de reserva, clips y retroceso.
- `Gun.Minigun.cs`: aceleracion, frenado, estado de disparo y giro del hueso.
- `Gun.Viewmodel.cs`: camara del arma y restauracion de capas.
- `Effects/HitscanTracerPool.cs`: reutiliza las trazadoras y limpia los efectos.
- `Effects/WeaponTracerVisual.cs`: construye la linea con el aspecto definido por el prefab.

Todos los archivos `Gun.*` forman **el mismo componente Gun**. No se agregan como componentes distintos.
`PlayerShoothing` continua leyendo controles y cambiando armas. `PlayerClass` aplica la clase y `Classes` guarda su configuracion.
Los campos existentes no se han renombrado, por lo que se conservan las referencias de escenas y prefabs.

## Añadir otra arma TF2

1. Duplica el prefab TF2 mas parecido y asigna el modelo y su Animator.
2. Usa un solo modo de animacion: `Animated Shotgun`, `Animated Magazine` o `Animated Minigun`.
3. Ajusta municion, cadencia, perdigones y dano en Gun. Asigna el punto de salida o conserva un hueso llamado `muzzle`.
4. Activa `Hitscan` y `Visible Tracers`; asigna `WeaponTracer` a `Tracer Prefab`.
5. Asigna el nuevo prefab a la clase correspondiente. No agregues otro script de disparo al jugador.

## Validación

`Tools > TF2 > Validate Weapons` comprueba los cuatro prefabs, sus clips, el efecto compartido, las clases y los casos de reserva finita/vacia/ilimitada.
Escribe el resultado en `Temp/TF2WeaponsValidation.result` y no cambia la escena abierta.
`Tools > Heavy > Validate Visual Assets` comprueba el giro y la reutilizacion de trazadoras.

La compilacion y las referencias en disco se verificaron durante esta reorganizacion.
La validacion dentro del editor y la comprobacion visual en Play quedan pendientes porque Unity no estaba abierto.

## Fogonazo

`Assets/Prefabs/TF2/Shared/Resources/WeaponMuzzleFlash.prefab` controla el destello compartido: `Size` cambia el tamano y `Duration` su duracion. Sigue la boca sin heredar la escala de los huesos. Al interrumpir la recarga, se evalua la pose de disparo antes de generar los efectos.

Las trazadoras se emiten en LateUpdate, despues de evaluar la animacion y sincronizar las camaras. Conservan el impacto calculado al disparar y salen de la boca ya actualizada, incluso al interrumpir la recarga. Su ancho base es 0.035, con mayor brillo y opacidad.
