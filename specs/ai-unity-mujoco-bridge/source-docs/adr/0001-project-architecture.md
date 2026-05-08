# ADR 0001: Event-Driven Bridge Architecture

> **DEPRECATED:** MuJoCo integration has been removed. Deterministic local fallback is now the sole physics backend. This document is preserved for historical context.

# ADR 0001: Event-Driven Bridge Architecture

## Status

Proposed

## Context

The project already has a text simulation environment. The new requirement is to visualize text-based multi-agent behavior in Unity and use MuJoCo for selected physical events. A single monolithic application would tightly couple social cognition, 3D rendering, and physics, making testing and future replacement difficult.

The core decision is whether to embed everything in Unity, rewrite the simulation in Unity, or use a local bridge that keeps each subsystem independent.

## Decision

Use an event-driven bridge architecture.

- The existing text simulation remains authoritative for cognition and social events.
- A Python bridge server normalizes and streams events.
- Unity receives versioned JSON messages and renders them.
- MuJoCo is called only for selected physical events.
- Replay logs record all accepted events and generated physical results.

## Consequences

What becomes easier:

- The existing text simulation can remain unchanged.
- Unity work can focus on visualization and interaction.
- MuJoCo can be disabled or replaced.
- Events can be replayed deterministically.
- AI coding agents can implement small, isolated tasks.

What becomes harder:

- Message schemas must be maintained carefully.
- Multiple processes must be coordinated.
- WebSocket lifecycle and reconnection must be tested.
- Time synchronization must be explicit.
- Unity and Python tests must both be run.

Tradeoffs accepted:

- Slightly more infrastructure in exchange for cleaner boundaries.
- Event latency is acceptable for Sims-like visualization.
- Full real-time physical control is deferred.

## Alternatives Considered

- Embed the text simulation directly in Unity.
  - Rejected because it would mix AI logic with visualization and make Python reuse harder.
- Use MuJoCo as the main world simulator.
  - Rejected because MuJoCo is better suited for physics than Sims-like social UI and avatar presentation.
- Use only pre-rendered replay videos.
  - Rejected because interactive observer tools and runtime inspection are required.
- Use gRPC instead of WebSocket for MVP.
  - Deferred because WebSocket is easier to integrate with Unity clients and live visualization.

## Validation

This decision is working when:

- Unity can connect to the bridge locally.
- A text simulation fixture can spawn at least 20 robot agents.
- Dialogue and motion events render in sequence.
- MuJoCo can be disabled without breaking visualization.
- One physical event can be routed through MuJoCo and displayed in Unity.
- Replay logs can reproduce the same Unity-visible event sequence.
