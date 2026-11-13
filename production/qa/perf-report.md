# Performance report (Editor, desktop CPU)

Generated 2026-11-14 01:11. Times are a relative signal only: a phone CPU is several times slower.

- Audio: start-up cost **0.2 ms**; 19 clips built one per frame, worst frame **12.8 ms**, total 42.6 ms
- LoadLevel(1): **5.3 ms**
- LoadLevel(10): **8.1 ms**
- LoadLevel(20): **8.7 ms**
- Instrument check: 1024 B injected per call reads as **1201 B**
- Idle level: **0 B** of garbage per frame from the game's own Update code; 1.69 ms avg and 3.14 ms worst frame including the Editor
- Dragging a piece: **0 B** of garbage per frame (input handling plus Update); 1.80 ms avg and 3.26 ms worst frame including the Editor
