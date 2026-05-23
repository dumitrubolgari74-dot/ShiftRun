# GOI Architecture (активный стек)

## Активные компоненты

| Компонент | Путь | Роль |
|-----------|------|------|
| `NewGoiController` | `Assets/_Scripts/GOI/NewGoiController.cs` | Управление телом и молотом (2D) |
| `HandIK` | `Assets/_Scripts/GOI/HandIK.cs` | Двухкостный IK на `Left1` / `Right1` |
| `CameraFollowObject` | `Assets/_Scripts/Common/CameraFollowObject.cs` | Камера следует за `Body` |
| `NewGoiPlayModeFix` | `Assets/_Scripts/GOI/Editor/NewGoiPlayModeFix.cs` | Восстановление parent молота после Play Mode |

## Сцена и префаб

- **Сцена:** `Assets/_Scenes/Game/Test.unity`
- **Префаб игрока:** `Assets/_Prefabs/Player.prefab`
- **IK-точки на молоте:** `Assets/_Prefabs/GOI/LeftHandIkTarget.prefab`, `RightHandIkTarget.prefab`

После реорганизации один раз выполните в Unity: **GOI → Apply Project Cleanup (Scene + Stones)** — заменит inline `Player` на prefab instance, починит камни (Collider2D), уберёт лишний `HandIK`, привяжет камеру.

## Управление (NewGoiController)

- **Без ЛКМ:** прицеливание молота по мыши.
- **ЛКМ на земле:** захват `hammerTip`, push тела.
- **ЛКМ в воздухе:** при удержании кнопки захват срабатывает, как только tip коснётся земли.

## HandIK

На каждой руке (`Left1`, `Right1`):

- `right` — правая / левая
- `hand1`, `hand2` — кости цепочки
- `target` — `LeftHandIkTarget` / `RightHandIkTarget` на молоте
- `matchTargetZ` — подтягивание по Z к цели

## Физика

- Игрок и молот: **Physics2D** (`Rigidbody2D`, `Collider2D`).
- Земля и камни: слой **Ground** (index 6), `groundLayers` / `obstructionLayers` = bit 64.
- Материалы: `Assets/_Materials/Physics2D/` (`Ground`, `Body`, `Hammer`).

## Build Settings

В билд добавлена сцена: `Assets/_Scenes/Game/Test.unity`.

## Legacy

Всё устаревшее — в `Assets/_Obsolete/`:

- `Scripts/` — `PlayerControl`, `Hand`, `SimpleGrabGoi`, `EnvScripts/`
- `Scenes/` — `sdf`, `GOI_Prototype`, `_Recovery`, бэкапы Test
- `Data/GOI/` — неиспользуемые ScriptableObject и материалы
- `GettingOverIt/` — референс

Старые заметки: `_Docs/PlayerBlackOut-*.legacy.md`.

## Не входит в GOI-стек

`Level_1/2/3`, `Meniu`, `GOI MAP 3.unity` — отдельные сцены, не трогались при уборке.
