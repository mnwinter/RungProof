# Ladder Find All and cross-reference

RungProof builds the search index from the same immutable Ladder IR that is
validated and executed by the offline controller. Results therefore represent
real project declarations and operands, not incidental text copied from the
visible editor.

## Indexed entities

- tag declarations, data types, roles, and symbolic scene bindings;
- program blocks/routines and network/rung declarations;
- task/OB declarations and their scheduled entry blocks;
- contact, comparison, timer, counter, reset, coil, move, and math operands;
- CALL/JSR target blocks.

References are classified as declaration, read, write, call, or scheduled
entry. Timer and counter members resolve to their root instance: searching for
`parts` includes `parts.Q`, `parts.DN`, `parts.ACC`, `parts.CV`, `parts.PRE`,
and `parts.PV` uses when present.

Find All is case-insensitive substring search across the full semantic index.
Cross-reference is an exact symbol/root-symbol lookup. Double-clicking a use
site selects its block/routine and network/rung. Double-clicking a tag
declaration returns to the tag table.

This is an offline RungProof index. It does not read a Siemens or Rockwell
project database, browse a connected controller, or claim vendor project-file
compatibility.
