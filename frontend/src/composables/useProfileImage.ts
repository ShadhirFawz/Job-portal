import { supabase } from '@/lib/supabase'

// ─────────────────────────────────────────────────
// Bucket & path conventions
//   Bucket : profile-images
//   Paths  : users/{userId}/avatar-{timestamp}.{ext}
//            users/{userId}/cover-{timestamp}.{ext}
// ─────────────────────────────────────────────────

const BUCKET = 'profile-images'
const VALID_MIME = ['image/jpeg', 'image/jpg', 'image/png', 'image/webp', 'image/gif']
const MAX_BYTES = 5 * 1024 * 1024   // 5 MB

/** Build the canonical storage path for a user image. */
function buildUserImagePath(userId: string, type: 'avatar' | 'cover', file: File): string {
    const ext = (file.name.split('.').pop() || 'jpg').toLowerCase()
    const ts  = Date.now()
    return `users/${userId}/${type}-${ts}.${ext}`
}

/**
 * Upload a user profile avatar or cover image.
 *
 * The file is stored at:
 *   profile-images/users/{userId}/{type}-{timestamp}.{ext}
 *
 * Returns the storage path (relative to bucket root) which is what
 * should be persisted in `public.users.avatar_path / cover_path`.
 * Never stores full URLs — always resolve with getProfileImageUrl().
 */
export async function uploadProfileImage(
    userId: string,
    file: File,
    type: 'avatar' | 'cover'
): Promise<string | null> {
    // Client-side validation before hitting the network
    if (!VALID_MIME.includes(file.type)) {
        console.error('Invalid file type for profile image:', file.type)
        return null
    }
    if (file.size > MAX_BYTES) {
        console.error('Profile image exceeds 5 MB limit:', file.size)
        return null
    }

    const path = buildUserImagePath(userId, type, file)

    const { error } = await supabase.storage
        .from(BUCKET)
        .upload(path, file, {
            contentType: file.type,
            cacheControl: '3600',
            upsert: true   // overwrite if a file with the same name exists
        })

    if (error) {
        console.error('Profile image upload error:', error.message)
        return null
    }

    return path
}

/**
 * Delete a previously-uploaded user profile image from storage.
 * Call this when replacing an old avatar/cover so stale files are removed.
 */
export async function deleteProfileImage(path: string | null | undefined): Promise<void> {
    if (!path) return
    const { error } = await supabase.storage.from(BUCKET).remove([path])
    if (error) {
        console.warn('Could not delete old profile image:', error.message)
    }
}

/**
 * Resolve a stored path (e.g. "users/{id}/avatar-123.jpg") to
 * a fully-qualified public URL suitable for <img :src="..."/>.
 */
export function getProfileImageUrl(path?: string | null): string | undefined {
    if (!path) return undefined
    const { data } = supabase.storage.from(BUCKET).getPublicUrl(path)
    return data.publicUrl
}

/** @deprecated  Use getProfileImageUrl() instead */
export const getPublicUrl = getProfileImageUrl
