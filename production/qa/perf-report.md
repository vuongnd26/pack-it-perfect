# Performance report (Editor, desktop CPU)

Generated 2026-11-14 16:59. Times are a relative signal only: a phone CPU is several times slower.

- Audio: start-up cost **0.1 ms**; 19 clips built one per frame, worst frame **12.8 ms**, total 38.4 ms
- LoadLevel(1): **8.9 ms**
- LoadLevel(10): **9.6 ms**
- LoadLevel(20): **13.1 ms**
- Instrument check: 1024 B injected per call reads as **1222 B**
- Idle level: **20 B** of garbage per frame from the game's own Update code; 2.17 ms avg and 5.44 ms worst frame including the Editor
- Dragging a piece: **0 B** of garbage per frame (input handling plus Update); 2.44 ms avg and 5.45 ms worst frame including the Editor
