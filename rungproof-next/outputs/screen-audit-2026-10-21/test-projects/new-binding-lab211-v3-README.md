# Lab2.11 full learner scanner flow V3

START latch; actual beam stops feed; actual barcode-complete latches clearance; LAD TON6000ms clears cycle/output. Timer DB211 unique.

6-second discharge clearance is a conservative QA parameter; no exit sensor exists, so exit is time based. Never writes PC or SIM feedback. Reset before new tote; Stop freezes plant but private phase may resume on Run.

Expected UI: START -> conveyor feed/beam TRUE -> conveyor OFF while real barcode scanner dwells -> scan_complete TRUE -> conveyor clears; after6 seconds conveyor OFF and qa_cycle/qa_clear FALSE. Other six v2 fixtures retain their existing boundary limitations. Compile validation proves input/schema/bindings only, not native process behavior. V2 preserved unchanged.
