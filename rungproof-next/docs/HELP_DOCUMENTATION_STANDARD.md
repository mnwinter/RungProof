# Help-documentation standard

The files under `docs/help/` are an inspectable operator and integrator aid,
generated from the same contracts that the runtime validates.

## What every asset document contains

- catalog identity and admission state;
- physical bounds and model resources;
- every declared reusable signal with type, direction, unit, and description;
- every kinematic axis with its exported node, limits, units, and maximum rate;
- declared connectors, or an explicit statement that none are declared; and
- a boundary statement that asset documents do not assign PLC addresses,
  ownership, safety category, or live commissioning behavior.

## What every scene document contains

- scene identity, migrated-source reference, and purpose;
- every symbolic point with type, PC/PLC/SIM owner, and initial value;
- scene actions, point/equipment bindings, expected equipment, and declared
  simulation safe state;
- training machine-guide details where the source scene supplies them; and
- an explicit boundary separating simulator behavior from real safety circuits,
  PLC watchdog proof, and live commissioning.

## Generation and verification

Run these after modifying a catalog or scene contract:

```powershell
py -3 .\tools\generate_help_documents.py
py -3 .\tools\validate_help_documents.py
```

The validator is intentionally contract-based: it proves coverage of declared
I/O names and motion nodes, but it does not turn static documentation into a
claim that a real PLC, electrical circuit, or safety function has been tested.
