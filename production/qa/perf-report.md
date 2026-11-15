# Performance report (Editor, desktop CPU)

Generated 2026-11-15 13:11. Times are a relative signal only: a phone CPU is several times slower.

- Audio: start-up cost **0.2 ms**; 19 clips built one per frame, worst frame **12.8 ms**, total 42.7 ms
- LoadLevel(1): **5.8 ms**
- LoadLevel(10): **7.4 ms**
- LoadLevel(20): **6.9 ms**
- Instrument check: 1024 B injected per call reads as **1311 B**
- Idle level: **0 B** of garbage per frame from the game's own Update code; 2.15 ms avg and 7.49 ms worst frame including the Editor
- Dragging a piece: **0 B** of garbage per frame (input handling plus Update); 2.27 ms avg and 5.19 ms worst frame including the Editor
