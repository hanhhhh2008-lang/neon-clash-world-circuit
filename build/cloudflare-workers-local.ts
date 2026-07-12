// Local-only fallback used when the Windows sandbox cannot start Miniflare.
// Production builds resolve the real Cloudflare worker binding instead.
export const env: { DB?: unknown } = {};
