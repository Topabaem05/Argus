# Project Brief: Unity Bridge with Deterministic Physics for Argus

## Summary

Add a local bridge layer that lets Argus simulation output drive a Unity 3D visualization with cute biped robot avatars and deterministic local physics evaluation.

> **Note:** MuJoCo integration has been removed. The deterministic local fallback is now the sole physics backend.

## Product Goal

Make Argus simulations observable as replayable, inspectable 3D scenes while preserving Argus as the source of truth for text simulation, agent cognition, scenario progression, safety, metrics, and reports.

## MVP Definition

The MVP is complete when a local Argus fixture can:

- produce bridge-compatible events,
- stream them to a Unity client over localhost WebSocket,
- spawn robot avatars,
- display movement, dialogue, emotion, group, and conflict cues,
- process at least one abstract physical event through deterministic fallback,
- save deterministic replay JSONL,
- pass Python tests and available Unity tests,
- document exact verification results.

## Non-Goals

- Do not build a Sims clone.
- Do not rewrite Argus simulation.
- Do not implement full-body locomotion for every avatar.
- Do not simulate graphic harm.
- Do not add online multiplayer.
- Do not call LLMs from Unity.
- Do not require paid assets or asset-store automation for MVP.

## Success Criteria

- Existing Argus dry-run behavior remains intact.
- Bridge dependencies are optional.
- Bridge protocol is versioned and tested.
- Replay output is deterministic.
- Localhost security defaults are enforced.
- Unity main-thread safety is handled by a queue.
- Robot asset legality is documented before import.

