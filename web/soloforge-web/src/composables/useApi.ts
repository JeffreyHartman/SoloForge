import { ref } from 'vue'

const apiOnline = ref<boolean | null>(null)
const errorMessage = ref<string | null>(null)

export async function apiGet<T>(path: string, options?: { signal?: AbortSignal }): Promise<T> {
  const resp = await fetch(path, { headers: { Accept: 'application/json' }, signal: options?.signal })
  if (!resp.ok) {
    const text = await resp.text().catch(() => '')
    throw new Error(text || `${resp.status} ${resp.statusText}`)
  }
  return (await resp.json()) as T
}

export async function apiSend<T>(path: string, method: string, body?: unknown): Promise<T> {
  const headers: Record<string, string> = { Accept: 'application/json' }
  if (body !== undefined) {
    headers['Content-Type'] = 'application/json'
  }
  const resp = await fetch(path, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  })
  if (!resp.ok) {
    const text = await resp.text().catch(() => '')
    throw new Error(text || `${resp.status} ${resp.statusText}`)
  }
  if (resp.status === 204 || resp.headers.get('content-length') === '0') {
    return undefined as T
  }
  const text = await resp.text()
  if (!text) {
    return undefined as T
  }
  return JSON.parse(text) as T
}

export function useApi() {
  async function refreshHealth() {
    try {
      await apiGet('/api/health')
      apiOnline.value = true
    } catch {
      apiOnline.value = false
    }
  }

  function setError(err: unknown) {
    errorMessage.value = err instanceof Error ? err.message : String(err)
  }

  function clearError() {
    errorMessage.value = null
  }

  return {
    apiOnline,
    errorMessage,
    refreshHealth,
    setError,
    clearError,
    apiGet,
    apiSend,
  }
}
