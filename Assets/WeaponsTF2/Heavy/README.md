# Heavy

Abre `Assets/Scenes/Heavy Demo.unity` (o **Tools > Heavy > Open Demo**) y pulsa Play.
La escena copia el escenario jugable y ya tiene seleccionada `Assets/Classes/Heavy.asset`.
Para usarlo en otra escena, asigna ese asset a **Player Class > Selected Class**.
La escena original conserva Scout y la escena de inspeccion de armas conserva tu trabajo.

## Controles

- Clic izquierdo: acelerar la minigun y disparar; disparar la escopeta.
- Clic derecho: mantener girando la minigun sin gastar municion.
- Soltar ambos: frenar la minigun. Espera a terminar de frenar antes de cambiar de arma.
- 1 / 2, rueda o Q: cambiar de arma, con los controles existentes del proyecto.
- R: recargar la escopeta cartucho a cartucho. Disparar interrumpe la recarga si queda municion.
- Espacio: salto normal. Sin doble salto; no se puede iniciar un salto con la minigun girando.

## Configuracion

Heavy tiene 300 de vida y velocidad 5.75, frente a 10 del Scout.
La velocidad con la minigun girando es 2.75. Se conserva el salto y la gravedad de la escala del proyecto.

Minigun: 200 balas, 0.87 segundos para acelerar, 4 impactos por unidad de municion y cadencia de 0.1 segundos.
No recarga ni regenera municion. El primer segundo de giro tiene menor dano y mayor dispersion.

Escopeta: 6 cartuchos y 32 de reserva, 10 perdigones y cadencia de 0.625 segundos.
La velocidad del estado `@fire` coincide con esa cadencia.
El HUD muestra cartuchos / reserva. Solo se descuenta la reserva al completar la animacion de insercion.

Ambas armas del Heavy usan impactos instantaneos contra los enemigos existentes, con dispersion y dano que disminuye con la distancia.
La escala y el balance son una adaptacion al proyecto: no incluyen criticos aleatorios ni una reproduccion completa de TF2.
Se reutilizan los sonidos de disparo disponibles; faltan los audios originales de giro de la minigun.

## Comprobacion

Los scripts de juego y de editor compilaron sin errores con el compilador de Unity 6000.3.22f1.
Los assets se generaron e importaron en Unity. La prueba automatica de Play y la inspeccion visual no se completaron en esta sesion.

**Tools > Heavy > Validate in Play Mode** abre la demo y prueba salud, velocidad, giro sin consumo, disparo, dano, frenado, recarga, interrupcion, reservas y regreso al Scout.
Los resultados se escriben en `Temp/HeavyValidation.result`. Tiene un limite de 90 segundos y sale de Play al terminar.
**Tools > Heavy > Configure Heavy** vuelve a aplicar la configuracion base a los prefabs y a la clase; sobrescribe los ajustes de balance de esos assets.

## Giro y trazadoras

La minigun gira el hueso `v_minigun_barrel` despues de evaluar el Animator. Usa el eje local X del tambor (`Barrel Local Axis`); el hueso auxiliar `_end` no indica su eje longitudinal; no se rotan las manos ni la raiz del arma.
Acelera durante 0.87 segundos y frena durante 0.6 segundos. Al guardar el arma se restablece el giro.

Ambas armas muestran trazadoras doradas hacia el impacto real. Son efectos visuales: el dano sigue siendo instantaneo.
El material URP se encuentra en `Assets/WeaponsTF2/Shared/Resources/WeaponTracerMaterial.mat`. Las trazadoras se reutilizan con un limite de 48 por arma, desaparecen al cambiar de arma y se destruyen con ella.
En el componente Gun puedes ajustar `Tracer Width`, `Tracer Lifetime`, `Barrel Degrees Per Second`, `Barrel Spin Up Time` y `Barrel Spin Down Time`.

Los cambios de efectos compilan con Unity 6000.3.22f1; la comprobacion visual en Play sigue pendiente.
**Tools > Heavy > Validate Visual Assets** permite comprobar el hueso, aceleracion/frenado, material y reutilizacion de trazadoras en una escena de previsualizacion.

La estructura compartida con Scout se explica en `Assets/WeaponsTF2/README.md`.
