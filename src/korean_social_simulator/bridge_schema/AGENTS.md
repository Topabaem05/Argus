# AGENTS.md — bridge_schema/

Versioned Pydantic envelope protocol shared by Argus, Unity, replay, and physics paths. Pure schemas, no I/O.

## STRUCTURE
```
envelope.py     # BridgeEnvelope: versioned wrapper + _PAYLOAD_MODELS dispatch (type -> payload class)
events.py       # AgentSpawn/Move/Dialogue/Emotion, GroupUpdate, ConflictUpdate, UnityAck, Vec3, states
behavior.py     # AgentBehaviorIntentEvent, AgentAnimationEvent
environment.py  # EnvironmentLoadEvent, SimulationSummaryEvent, UiStatusEvent, LightingPreset
physics.py      # PhysicsRequest, PhysicsResult, PhysicsConstraints
errors.py       # StructuredError
__init__.py     # re-exports all public schema types via __all__
```

## WHERE TO LOOK
| Task | Location |
|------|----------|
| Add a message type | `events.py`/`behavior.py`/`environment.py` payload class + `envelope.py` `_PAYLOAD_MODELS` entry + `bridge/server.py` `_SUPPORTED_MESSAGE_TYPES` entry |
| Change envelope fields | `envelope.py` `BridgeEnvelope` (bumps `schema_version` semver) |
| Add a 3D value | `events.py` `Vec3` (x/y/z floats) |
| Physics contract | `physics.py` `PhysicsRequest`/`PhysicsResult` |

## CONVENTIONS
- Every payload model: `model_config = ConfigDict(extra="forbid")`.
- `BridgeEnvelope.schema_version` is semver, validated by `_SEMVER_PATTERN`. Bumping is a protocol change.
- `_PAYLOAD_MODELS: ClassVar[dict[str, type[BaseModel]]]` maps `type` string to payload Pydantic class. Envelope validates payload against the registered model on load.
- `Vec3` is the canonical 3D coordinate type; do not pass raw tuples across the boundary.
- `__all__` in `__init__.py` is the public surface; bridge/ imports from here, not from submodules directly.

## ANTI-PATTERNS
- Do not add fields without `extra="forbid"`; loose schemas break the protocol boundary.
- Do not reference `bridge/` (runtime) from `bridge_schema/` (pure schemas). Dependency is one-way: bridge -> bridge_schema.
- Do not bump `schema_version` without updating `configs/bridge.example.yaml` and replay fixtures.
