#!/usr/bin/env python3
"""Serve the local Unity Web build without exposing the repository or saves."""
import argparse
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path


class Handler(SimpleHTTPRequestHandler):
    extensions_map = {**SimpleHTTPRequestHandler.extensions_map, ".wasm": "application/wasm", ".data": "application/octet-stream"}

    def end_headers(self):
        self.send_header("Cache-Control", "no-store")
        super().end_headers()


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--port", type=int, default=8080)
    args = parser.parse_args()
    build = Path(__file__).resolve().parents[2] / "builds/web/local"
    if not (build / "index.html").is_file():
        parser.error("Build the game first: Unity > Pack It Perfect > Local Web > Build Browser Test")
    with ThreadingHTTPServer(("127.0.0.1", args.port), partial(Handler, directory=str(build))) as server:
        print(f"Pack It Perfect: http://localhost:{args.port} — Ctrl+C to stop", flush=True)
        try:
            server.serve_forever()
        except KeyboardInterrupt:
            pass
