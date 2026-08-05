export function readJson<T>(key: string, fallback: T): T {
  const value = localStorage.getItem(key)
  if (!value) return fallback

  try {
    return JSON.parse(value) as T
  } catch {
    localStorage.removeItem(key)
    return fallback
  }
}

export function writeJson(key: string, value: unknown): void {
  localStorage.setItem(key, JSON.stringify(value))
}
