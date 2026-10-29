/**
 * Returns ordinal suffix for a given day (1st, 2nd, 3rd, 4th, etc.)
 */
export function getOrdinal(day: number): string {
  if (day > 3 && day < 21) return 'th'
  switch (day % 10) {
    case 1:
      return 'st'
    case 2:
      return 'nd'
    case 3:
      return 'rd'
    default:
      return 'th'
  }
}

const MONTH_NAMES = [
  'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
  'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec',
]

/**
 * Format date in exact format: e.g. "7th Dec 2026"
 */
export function formatExactDate(date: Date): string {
  const day = date.getDate()
  const month = MONTH_NAMES[date.getMonth()]
  const year = date.getFullYear()
  return `${day}${getOrdinal(day)} ${month} ${year}`
}

/**
 * Format date in relative ago format if within 7 days,
 * otherwise in exact date format (e.g. "7th Dec 2026").
 */
export function formatRelativeOrExact(dateInput: string | Date): { isExact: boolean; formatted: string } {
  const date = typeof dateInput === 'string' ? new Date(dateInput) : dateInput
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()

  // Future or negative guard
  if (diffMs < 0) {
    return { isExact: false, formatted: 'just now' }
  }

  const diffSeconds = Math.floor(diffMs / 1000)
  const diffMinutes = Math.floor(diffSeconds / 60)
  const diffHours = Math.floor(diffMinutes / 60)
  const diffDays = Math.floor(diffHours / 24)

  // Within 1 week (less than 7 days)
  if (diffDays < 7) {
    if (diffSeconds < 60) return { isExact: false, formatted: 'just now' }
    if (diffMinutes === 1) return { isExact: false, formatted: '1 minute ago' }
    if (diffMinutes < 60) return { isExact: false, formatted: `${diffMinutes} minutes ago` }
    if (diffHours === 1) return { isExact: false, formatted: '1 hour ago' }
    if (diffHours < 24) return { isExact: false, formatted: `${diffHours} hours ago` }
    if (diffDays === 1) return { isExact: false, formatted: 'yesterday' }
    return { isExact: false, formatted: `${diffDays} days ago` }
  }

  // 1 week or older -> exact date format: "7th Dec 2026"
  return { isExact: true, formatted: formatExactDate(date) }
}

/**
 * Formats job dates according to specification:
 * - If created_at and updated_at are dissimilar (> 60s apart), display "Updated on [7th Dec 2026]" or "Updated [X days ago]".
 * - Otherwise, display "Posted on [7th Dec 2026]" or "Posted [X days ago]".
 */
export function formatJobDate(
  createdAt: string | Date | null | undefined,
  updatedAt?: string | Date | null | undefined
): string {
  if (!createdAt) return ''

  const cDate = typeof createdAt === 'string' ? new Date(createdAt) : createdAt
  const uDate = updatedAt ? (typeof updatedAt === 'string' ? new Date(updatedAt) : updatedAt) : null

  // Check if dissimilar (more than 60 seconds apart)
  const isDissimilar = uDate && Math.abs(uDate.getTime() - cDate.getTime()) > 60 * 1000

  if (isDissimilar && uDate) {
    const { isExact, formatted } = formatRelativeOrExact(uDate)
    return isExact ? `Updated on ${formatted}` : `Updated ${formatted}`
  }

  const { isExact, formatted } = formatRelativeOrExact(cDate)
  return isExact ? `Posted on ${formatted}` : `Posted ${formatted}`
}

