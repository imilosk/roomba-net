# Mission observations (i3+, daredevil 2.6.0)

What the robot actually reports during a full cleaning mission, captured through the MQTT proxy
(`UseTls: false` profile) by logging every message. Two whole-house missions observed:
#622 on 2026-10-28 and #623 on 2026-10-29. Notes here are about *behaviour on the wire*, and each
one ends with what RoombaNet should do about it.

---

## 1. Mission timeline (#623, all UTC)

| Time | Event | Signals |
|---|---|---|
| 07:21:45 | `start` accepted | `cycle: clean`, `phase: run`, `operatingMode: 2` |
| 08:35:26 | battery low, heads home mid-mission | `phase: hmMidMsn` |
| 08:36:49 | docked, **evac** | `phase: evac`, dock `302` → `303` |
| 08:37:10 | evac done | dock `301`, `bbrun.nEvacs` 526 → 527 |
| 08:37:14 | recharging | `phase: charge`, `notReady: 15` briefly, `rechrgTm` 09:48:35, `expireTm` 10:07:13 |
| 09:40:46 | resumes (~81 %) | `phase: run`, dock `300` |
| 09:56:30 | mission complete, heads home | `phase: hmPostMsn` |
| 09:58:30 | docked, **evac** | `phase: evac`, dock `302` → `303` |
| 09:58:45 | done | `cycle: none`, `phase: charge`, `nEvacs` 528, `bbmssn.nMssnOk` +1 |
| 09:58:46 | post-mission map errors | `notReady: 39` → `68` (16 s) → `0` |

Mission #622 followed the same shape: run ~72 min, charge ~64 min, final stretch ~14 min. Total
wall-clock ≈ 2 h 35 min, of which ~1 h on the dock.

## 2. Findings → what RoombaNet should do

### Telemetry cadence
- **The robot sends very little.** Over ~105 min: 471 `wifistat` (signal, every ~13 s) but only
  47 shadow deltas — 25 `cleanMissionStatus`, 8 `batPct`, 4 `dock`, a handful of `bb*` counters.
  The full shadow is only sent on connect; `shadow/get` is ignored.
  → `RequestShadow()` is a no-op on this firmware; the proxy's retained full state is the only
  way a late-joining client gets a complete snapshot.
- **`batPct` updates only every ~12 min, in ~14 % steps** (100, 82, 68, 54, 40, 26, 15…).
  → Expose `batteryUpdatedAt`; do not present the value as live. Drain ≈ 1 %/min cleaning,
  charge ≈ 1.1 %/min.
- **No `pose` at all.** The i3 does not report position, so a live map over local MQTT is impossible.
- **Same-second flip-flops**: every phase change arrives as a burst like
  `hmMidMsn → run → hmMidMsn` within one second.
  → Debounce the API updates channel/SSE (emit after ~1 s of quiet).

### Mission state
- **Resume ETA is reported, not estimated.** While charging mid-mission, `rechrgTm` (unix) is the
  latest expected resume and `expireTm` the deadline after which the mission is abandoned. Both are
  `0` otherwise. The robot resumed 8 min *before* `rechrgTm`.
  → Show "resumes by ~HH:MM" in the status summary.
- `mssnM` stays `0` the whole mission; elapsed time must be computed from `mssnStrtTm`.
- `operatingMode`: `2` cleaning, `0` while charging mid-mission and idle.
- **`mssnNavStats` is only published after the mission ends.** Until then it describes an older
  mission (we saw a February mission ID in it). → Check `missionId` before showing nav stats.

### Dock / evac
- `dock.state` describes the **Clean Base**, not whether the robot is on it: `300` idle (also while
  the robot is away), `302` evac starting, `303` evacuating, `301` evac done/ready, `364` docking,
  `350–360` evac errors (360 = communication problem, seen once on #622).
  → Don't derive "docked" from `dock.state`; wire `350–360` to `DescribeEvacError()` (currently unused).
- **Evac confirmation**: `phase: evac` + `302 → 303 → 301`, and `bbrun.nEvacs` increments within
  ~5 s of the end. An evac takes ~15 s. → `evac` command should wait ≤30 s for these before
  reporting success.
- The robot does evac on a **mid-mission** return too (it did on #623, not on #622).
- `dock.pn` alternates between `"unknown"` and the real part number across messages.
  → Never let `"unknown"` overwrite a known value when merging (CLI `StateReader` and API `DeepMerge`).

### Maps
- `pmaps` stayed `[]` after both missions, and both ended with `notReady 39` ("Clean Map problem")
  then `68` ("Map was unavailable"), even though `pmapLearningAllowed` and `mapUploadAllowed` are
  `true`.
- The robot reports `connected: false` (no cloud). On the i3, persistent maps live in the iRobot
  cloud, so the likely cause is that **map save needs the cloud**. Unconfirmed.
  → Keep post-mission `notReady` codes visible in history (they clear after ~16 s and are easy to
  miss), and document that mapping needs cloud access.

### Clock
- Earlier suspicion of a wrong robot clock was a false alarm: `mssnStrtTm` and the mission ULID
  match real time. The stale date came from `mssnNavStats` (see above).

## 3. Lifetime stats after #623
`bbmssn`: 623 missions, 482 ok, 94 cancelled, 47 failed, avg mission 106 min, avg cycle 80 min.
`bbrun`: 359 h run time, 528 evacs, 105 stuck events. Battery: 261 charge cycles, `estCap` 1725 mAh
(Panasonic, manufactured 2021-05). These are exposed only in the raw shadow today.
