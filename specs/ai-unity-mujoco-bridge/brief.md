# Feature Brief: Unity Bridge with Deterministic Physics

> **DEPRECATED:** MuJoCo integration has been removed. Deterministic local fallback is now the sole physics backend. This document is preserved for historical context.

## Summary

Build the communication and embodiment layer that connects an existing text-based population simulation to a Unity 3D client and a MuJoCo physical-event service. The result should let observers watch simulated agents as cute biped robot avatars discussing, arguing, moving, gathering, separating, and occasionally performing abstract physical interactions such as push, stumble, fall, and recover.

## User Problem

The text simulation can produce rich social behavior, but it is hard to inspect, explain, and demonstrate because the output is textual. Researchers and developers need a 3D Sims-like visualization layer that shows who is talking to whom, where groups form, how conflict escalates, and what happens when physical interaction is requested.

## Goals

- Preserve the existing text simulation as the source of truth.
- Define a stable AI-to-Unity WebSocket protocol.
- Define Unity-to-bridge acknowledgement and observer-control messages.
- Define MuJoCo request/result messages for selected physical events.
- Render agents as cute biped robot avatars in Unity.
- Add a legal and testable robot asset import plan.
- Support deterministic replay logs.
- Provide strict acceptance criteria and verification commands.

## Non-Goals

- Do not rewrite the text simulation engine.
- Do not create a full Sims clone.
- Do not implement full-body MuJoCo walking for every avatar.
- Do not simulate graphic harm or injury.
- Do not require paid robot assets for MVP.
- Do not implement online multiplayer.
- Do not stream raw LLM prompts into Unity.
- Do not commit third-party assets unless redistribution is legally allowed.

## Primary User Flow

1. Developer starts the local bridge server.
2. Developer opens the Unity project.
3. Unity connects to the bridge and sends `unity.ready`.
4. The existing text simulation starts or replays a scenario.
5. The bridge streams canonical events to Unity.
6. Unity spawns cute robot avatars for agents.
7. Agents move, face each other, display dialogue, and show emotion indicators.
8. When a physical interaction event occurs, the bridge requests a MuJoCo evaluation.
9. MuJoCo returns a high-level physical outcome.
10. Unity plays the appropriate visual animation.
11. The entire sequence is saved as a replay log.

## Expected Output

- Documentation-ready implementation plan.
- Versioned bridge message schemas.
- Unity scene architecture.
- MuJoCo service architecture.
- Robot asset selection and import checklist.
- Test plan covering Python, Unity, MuJoCo, and replay.
- Implementation tasks for AI coding agents.

## Main Risks

- Unity asset licensing may prevent redistribution.
- Robot asset may be cute but not rigged correctly.
- MuJoCo humanoid physics may be too expensive for multiple agents.
- Message ordering may break visual consistency.
- Unity main-thread constraints may cause freezes if networking is handled incorrectly.
- AI coding agents may overbuild a game instead of implementing the bridge.

## Completion Definition

The feature is complete when a local text simulation fixture can spawn at least 20 robot agents in Unity, stream movement/dialogue/emotion events through the bridge, process at least one MuJoCo physical event or deterministic fallback, save a replay log, and pass the required Python and Unity tests.
