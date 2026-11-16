# Performance report (Editor, desktop CPU)

Generated 2026-11-16 00:18. Times are a relative signal only: a phone CPU is several times slower.

- Audio: start-up cost **0.2 ms**; 19 clips built one per frame, worst frame **12.8 ms**, total 42.7 ms
- LoadLevel(1): **7.1 ms**
- LoadLevel(10): **4.1 ms**
- LoadLevel(20): **2.4 ms**
- Instrument check: 1024 B injected per call reads as **1311 B**
- Idle level: **0 B** of garbage per frame from the game's own Update code; 1.64 ms avg and 2.92 ms worst frame including the Editor
- Dragging a piece: **0 B** of garbage per frame (input handling plus Update); 1.80 ms avg and 3.52 ms worst frame including the Editor
