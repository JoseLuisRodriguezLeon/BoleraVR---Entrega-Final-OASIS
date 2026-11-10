# Bolera VR

Demo de bolos en realidad virtual para **Meta Quest 3**, hecho en Unity e inspirado en el bowling de Wii Sports.

El jugador toma la bola con el gatillo, balancea el brazo y la suelta para lanzar. La fuerza y la dirección salen del movimiento real del control, y el efecto (la curva) sale del giro de muñeca.

## Características

- **Lanzamiento con el gatillo:** la bola aparece en la mano, se sostiene con el gatillo y se lanza al soltarlo, con cualquiera de las dos manos.
- **Física de pinos:** los pinos caídos se detectan automáticamente después de cada tiro.
- **Puntaje completo:** strikes, spares y tiros extra en la última ronda.
- **Partidas configurables** de 1 a 10 rondas.
- **Menús en VR** que se usan apuntando con un rayo desde los controles.
- **Marcador en el mundo** sobre la pista, con avisos de strike y spare.
- **Vibración** al tomar la bola, al lanzar y al usar los menús.

## Requisitos

| Elemento | Versión |
| --- | --- |
| Visor | Meta Quest 3 con controles Touch Plus, en modo desarrollador |
| Unity | **6000.3.5f1** (Unity 6.3 LTS) con Android Build Support (OpenJDK, Android SDK & NDK Tools) |
| SDK | Meta XR All-in-One SDK (se instala con el proyecto) |

## Instalar el juego en el visor

1. Descarga `BoleraVR.apk` desde la sección **Releases** de este repositorio.
2. Conecta el visor al computador por USB. En el visor, acepta el aviso **Permitir depuración USB**.
3. Instala el APK de una de estas formas:
   - **Meta Quest Developer Hub:** arrastra el archivo APK a la ventana del dispositivo.
   - **Terminal:** `adb install -r BoleraVR.apk`
4. En el visor, abre la **Biblioteca**, filtra por **Fuentes desconocidas** (Unknown Sources) y abre **Bolera VR**.

## Abrir el proyecto en Unity

1. Clona el repositorio con **GitHub Desktop** (**File → Clone repository**) o con Git:
   ```bash
   git clone https://github.com/JoseLuisRodriguezLeon/BoleraVR---Entrega-Final-OASIS.git
   ```
   El repositorio usa **Git LFS** para los sonidos, la iluminación horneada y otros archivos binarios. GitHub Desktop y Git los descargan automáticamente; si GitHub Desktop pregunta, elige **Initialize Git LFS**. Si al abrir el proyecto no hay sonido o la escena se ve oscura, ejecuta `git lfs pull` dentro de la carpeta del proyecto.
2. En Unity Hub, elige **Add → Add project from disk** y selecciona la carpeta del proyecto. Ábrelo con la versión **6000.3.5f1**.
3. La primera vez tarda varios minutos, mientras Unity descarga los paquetes e importa los assets.
4. Abre la escena `Assets/Scenes/Bolera.unity`.
5. Si Unity pide importar **TMP Essential Resources**, acepta.

### Si aparecen avisos al abrir

- **Paquetes marcados con "invalid signature" en el Package Manager:** es un error conocido de Unity en esta versión del editor. Los paquetes vienen del Unity Registry y funcionan bien; se puede ignorar. No los quites.
- **El Package Manager no encuentra el Meta XR SDK:** agrega gratis el **Meta XR All-in-One SDK** desde el Unity Asset Store y luego impórtalo desde **Window → Package Manager → My Assets**.

## Controles

### En el visor

| Acción | Cómo |
| --- | --- |
| Usar los menús | Apunta con el rayo del control y presiona el **gatillo** |
| Tomar la bola | Presiona y mantén el **gatillo** de cualquier control |
| Lanzar | Balancea el brazo hacia los pinos y **suelta el gatillo** |
| Dar efecto | Gira la muñeca al momento de soltar |

Si sueltas la bola sin fuerza o hacia atrás, vuelve a la salida y no cuenta como tiro.

### En el editor (pruebas sin visor)

| Tecla | Acción |
| --- | --- |
| 1 a 9, 0 | Empezar con ese número de rondas (0 = 10) |
| Enter | Repetir el último número de rondas |
| B | Lanzar |
| I / K | Subir o bajar la velocidad del tiro |
| J / L | Mover el ángulo a la izquierda o la derecha |
| U / O | Cambiar el efecto |
| R | Reintentar |
| Esc | Salir (en la pantalla final) |

Estos controles solo funcionan dentro del editor de Unity; en el APK están desactivados.

## Cómo se juega

1. Elige el número de rondas (de 1 a 10) y presiona **Jugar**.
2. Cada ronda tiene hasta dos tiros:
   - **Strike:** derribar los 10 pinos en el primer tiro. Vale 10 más los dos tiros siguientes.
   - **Spare:** derribar los 10 pinos entre los dos tiros. Vale 10 más el tiro siguiente.
3. En la última ronda, un strike da dos tiros extra y un spare da uno.
4. Al terminar aparece el puntaje: **Reintentar** vuelve a la selección de rondas y **Salir** cierra el juego (pide confirmación con un segundo toque).

## Estructura del proyecto

Los scripts están en `Assets/Scripts`:

| Archivo | Función |
| --- | --- |
| `GameManager.cs` | Flujo de la partida: selección de rondas, tiro, conteo, barrido y fin |
| `BowlingScore.cs` | Cálculo del puntaje, independiente de la escena |
| `PinSetter.cs` | Coloca los 10 pinos, detecta los caídos y los reinicia |
| `BallLauncher.cs` | Física de la bola: sostenerla, lanzarla y aplicar la curva |
| `VRThrowInput.cs` | Lanzamiento con el gatillo según el movimiento del control |
| `VRPointer.cs`, `VRButton.cs` | Rayo de los controles y botones de los menús |
| `GameUI.cs` | Crea los paneles en VR y el marcador en el mundo |
| `DebugThrowInput.cs` | Controles de teclado para probar en el editor |
| `Editor/BowlingScoreChecks.cs` | Pruebas del puntaje (menú **Bolera → Probar puntaje**) |

## Generar el APK

1. Abre **File → Build Profiles** y verifica que **Android** sea la plataforma activa y que la escena `Bolera` esté en la lista.
2. Presiona **Build** y guarda el archivo en la carpeta `Builds/` (Git la ignora).

## Créditos

Los modelos 3D y los sonidos son de otros autores y se usan bajo sus licencias. La lista completa está en [CREDITS.md](CREDITS.md).

## Autor

Jota — Universidad de los Andes
