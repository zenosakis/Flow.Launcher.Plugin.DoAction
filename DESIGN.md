# DoAction UI

- Native WPF controls inside Flow Launcher. Korean labels, system font, visible keyboard focus.
- Two independent groups: automatic input; shutdown/restart reservation.
- Single-column scrollable settings with contextual controls and explicit Save feedback.
- Use Windows SystemColors for accessible standalone surfaces and high contrast.
- Fullscreen has a quiet input area, a visible Stop button, and physical-key stop guidance.
- State always uses text; color is never the only indicator. No decorative animation.
- Actual physical key down permanently stops automation; never automatically resumes.
- Canceling automation does not cancel a power reservation. Warning cancels automation before displaying controls.
- Power reservations are session-only, expire on sleep/long scheduling gaps, and never auto-restore.
- ui-ux-pro-max design exploration informed spacing and simplicity. Native utility conventions take precedence over the generated landing-page typography/style suggestions.
- Icon: original white cursor and clockwise arrow on a cobalt rounded-square tile, transparent exterior, no text. Maintain a clear silhouette at 16px; use the multi-resolution ICO for Flow search results.
