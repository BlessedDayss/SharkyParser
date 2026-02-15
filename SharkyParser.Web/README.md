# SharkyParser Web

Angular 19 SPA — web interface for Sharky Parser PRO.

## Getting Started

```bash
npm install
npm start
```

App: **http://localhost:4200**

> **Note:** API must be running on port 5000. See [root README](../README.md).

## Build

```bash
npm run build
```

Output in `dist/sharky-parser.web/`.

## Development

- `ng serve` — dev server with hot reload
- `ng build` — production build
- `ng test` — unit tests (Karma)

## Proxy

`/api` is proxied to `http://localhost:5000` (see `proxy.conf.json`).
