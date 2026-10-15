# Ladder instruction help

The instruction-help pane is generated from `LadderInstructionCatalog`, the
authoritative list of instructions currently exposed by the editor. Each entry
contains:

- a Siemens TIA-style display name;
- a Rockwell Studio 5000-style display name;
- purpose and accepted operands;
- deterministic offline scan behavior;
- validation, lifecycle, and vendor-boundary restrictions;
- a concise example.

Selecting an item in the instruction tree changes the help context. Open the
pane with **HELP (F1)**, the View or Tools menu, or the F1 key. Changing the
selector inside the help pane does not insert or modify program logic.

The help describes RungProof's tested offline semantics. Vendor-familiar names
do not imply Siemens or Rockwell project compatibility, instruction-set
certification, online help access, physical PLC timing, or commissioning proof.
