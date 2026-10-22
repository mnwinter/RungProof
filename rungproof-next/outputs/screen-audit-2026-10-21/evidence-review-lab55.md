# Lab 5.5 independent native review

Reviewed 75 native frames: five invalid truth cases,69 cycle/recovery capture actions and final reset check. Five lab55-review sheets retain physical green rate lamp, four Signals and runtime. Loaded reviewer ladder scope only; no reviewer UI/backend operations.

Disabled fast-only/both/slow-only/neither, and enabled-neither, are dark with rate_lamp=False. Enabled fast-only repeatedly alternates physical green/dark matching rate_lamp: ON6987/OFF6999/ON7009/OFF7018/ON7028/OFF7039, continuing through7172. Enabling both selectors masks the lamp immediately in captured7189 and held7233. Slow-only starts dark7247/7258, then ON7269/7280/7290, OFF7301/7311, ON7321/7330, OFF7342/7353/7364, ON7374/7385, OFF7395/7405/7415, ON7425/7435, OFF7445/7455. Fast samples show more captured reversals over a comparable scan span than slow, supporting a bounded relative cadence difference. These samples do not establish exact200/500ms durations or every transition.

Neither-selected while enabled remains dark7472/7517. Fresh fast-only7530 is initially dark and repeatedly cycles7538..7605. Stop early/late is STOPPED7625, selectors/enable retained, lampFalse and physically dark across .8 seconds. Run7626 initially dark, then cycling resumes7636..7702. Enable OFF7719/7765 cancels flashing despite fast still selectedTrue. Reset and independent final check are STOPPED0 with all four pointsFalse and lampdark.

No new issue. Stop was preceded by a dark sample, so interruption of a known illuminated phase is unproved. Exact accepted-scan timing, instantaneous valid fast-to-slow switching without an intervening invalid combination, long-term drift and scan-continuous behavior remain unproven. No whole-scene PASS.
