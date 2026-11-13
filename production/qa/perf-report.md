# Performance report (Editor, desktop CPU)

Generated 2026-11-14 01:35. Times are a relative signal only: a phone CPU is several times slower.

- Audio: start-up cost **0.2 ms**; 19 clips built one per frame, worst frame **12.6 ms**, total 45.4 ms
- LoadLevel(1): **6.8 ms**
- LoadLevel(10): **10.1 ms**
- LoadLevel(20): **9.7 ms**
- Instrument check: 1024 B injected per call reads as **1195 B**
- Idle level: **0 B** of garbage per frame from the game's own Update code; 1.75 ms avg and 3.38 ms worst frame including the Editor
- Dragging a piece: **0 B** of garbage per frame (input handling plus Update); 1.75 ms avg and 3.00 ms worst frame including the Editor
