import { supabase } from '@/lib/supabase'

const CANDIDATE_BASE_URLS = [
  (import.meta.env.VITE_API_URL as string)?.replace(/\/$/, ''),
  'http://localhost:50804/api',
  'http://localhost:5008/api',
  '/api',
].filter(Boolean) as string[]

let activeBaseUrl = CANDIDATE_BASE_URLS[0] || 'http://localhost:50804/api'

interface RequestOptions extends RequestInit {
  params?: Record<string, any>
}

async function getAuthToken(): Promise<string | null> {
  // 1. Try Supabase active session
  try {
    const { data } = await supabase.auth.getSession()
    if (data.session?.access_token) {
      return data.session.access_token
    }
  } catch {
    // Ignore and proceed to fallback
  }

  // 2. Fallback to localStorage Supabase session item
  try {
    for (let i = 0; i < localStorage.length; i++) {
      const key = localStorage.key(i)
      if (key && key.startsWith('sb-') && key.endsWith('-auth-token')) {
        const item = localStorage.getItem(key)
        if (item) {
          const parsed = JSON.parse(item)
          if (parsed?.access_token) return parsed.access_token
        }
      }
    }
  } catch {
    // Ignore
  }

  return null
}

function buildUrl(baseUrl: string, endpoint: string, params?: Record<string, any>): string {
  let url = `${baseUrl}/${endpoint.replace(/^\//, '')}`
  if (params) {
    const searchParams = new URLSearchParams()
    for (const [key, value] of Object.entries(params)) {
      if (value !== undefined && value !== null) {
        searchParams.append(key, String(value))
      }
    }
    const queryString = searchParams.toString()
    if (queryString) {
      url += (url.includes('?') ? '&' : '?') + queryString
    }
  }
  return url
}

export async function request<T>(endpoint: string, options: RequestOptions = {}): Promise<T> {
  const { params, headers, ...customConfig } = options

  const token = await getAuthToken()

  const reqHeaders: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(headers as Record<string, string>),
  }

  if (token) {
    reqHeaders['Authorization'] = `Bearer ${token}`
    reqHeaders['x-supabase-token'] = token
  }

  const config: RequestInit = {
    ...customConfig,
    headers: reqHeaders,
    credentials: customConfig.credentials || 'include',
  }

  // Try current active base URL first
  let lastError: any = null
  const urlsToTry = [activeBaseUrl, ...CANDIDATE_BASE_URLS.filter((u) => u !== activeBaseUrl)]

  for (const candidateBase of urlsToTry) {
    const targetUrl = buildUrl(candidateBase, endpoint, params)
    try {
      const response = await fetch(targetUrl, config)

      // Successfully reached server! Mark this base URL as active
      activeBaseUrl = candidateBase

      if (response.status === 204) {
        return {} as T
      }

      const responseText = await response.text()
      let data: any = null
      if (responseText) {
        try {
          data = JSON.parse(responseText)
        } catch {
          data = responseText
        }
      }

      if (!response.ok) {
        const errorMessage =
          (typeof data === 'object' && data?.message) ||
          (typeof data === 'string' && data) ||
          `Request failed with status ${response.status}`
        throw new Error(errorMessage)
      }

      return data as T
    } catch (err: any) {
      // If it's a network TypeError (Failed to fetch), try the next port/proxy
      if (err instanceof TypeError && err.message?.includes('fetch')) {
        lastError = err
        continue
      }
      // If it's an HTTP error with response from backend, throw directly
      throw err
    }
  }

  throw lastError || new Error('Failed to reach backend API.')
}

export const api = {
  get<T>(endpoint: string, params?: Record<string, any>, options?: RequestOptions): Promise<T> {
    return request<T>(endpoint, { method: 'GET', params, ...options })
  },

  post<T>(endpoint: string, body?: any, params?: Record<string, any>, options?: RequestOptions): Promise<T> {
    return request<T>(endpoint, {
      method: 'POST',
      body: body !== undefined ? JSON.stringify(body) : undefined,
      params,
      ...options,
    })
  },

  put<T>(endpoint: string, body?: any, params?: Record<string, any>, options?: RequestOptions): Promise<T> {
    return request<T>(endpoint, {
      method: 'PUT',
      body: body !== undefined ? JSON.stringify(body) : undefined,
      params,
      ...options,
    })
  },

  delete<T>(endpoint: string, params?: Record<string, any>, options?: RequestOptions): Promise<T> {
    return request<T>(endpoint, { method: 'DELETE', params, ...options })
  },
}
