import { supabase } from '@/lib/supabase'

// ─────────────────────────────────────────────────
// Bucket & path conventions
//   Bucket : organization-images   (separate from user profile images)
//   Paths  : organizations/{orgId}/avatar-{timestamp}.{ext}
//            organizations/{orgId}/cover-{timestamp}.{ext}
// ─────────────────────────────────────────────────

const BUCKET = 'organization-images'
const VALID_MIME = ['image/jpeg', 'image/jpg', 'image/png', 'image/webp', 'image/gif']
const MAX_BYTES = 5 * 1024 * 1024   // 5 MB

/** Build the canonical storage path for an organization image. */
function buildOrgImagePath(orgId: string, type: 'avatar' | 'cover', file: File): string {
    const ext = (file.name.split('.').pop() || 'jpg').toLowerCase()
    const ts  = Date.now()
    return `organizations/${orgId}/${type}-${ts}.${ext}`
}

/**
 * Upload an organization avatar or cover image.
 *
 * The file is stored at:
 *   organization-images/organizations/{orgId}/{type}-{timestamp}.{ext}
 *
 * Returns the storage path (relative to bucket root) which is what
 * should be persisted in `public.organizations.avatar_path / cover_path`.
 * Never stores full URLs — always resolve with getOrganizationImageUrl().
 */
export async function uploadOrganizationImage(
    organizationId: string,
    file: File,
    type: 'avatar' | 'cover'
): Promise<string | null> {
    // Client-side validation before hitting the network
    if (!VALID_MIME.includes(file.type)) {
        console.error('Invalid file type for organization image:', file.type)
        return null
    }
    if (file.size > MAX_BYTES) {
        console.error('Organization image exceeds 5 MB limit:', file.size)
        return null
    }

    const path = buildOrgImagePath(organizationId, type, file)

    const { error } = await supabase.storage
        .from(BUCKET)
        .upload(path, file, {
            contentType: file.type,
            cacheControl: '3600',
            upsert: true   // overwrite if a file with the same name exists
        })

    if (error) {
        console.error('Organization image upload error:', error.message)
        return null
    }

    return path
}

/**
 * Delete a previously-uploaded organization image from storage.
 * Call this when replacing an old avatar/cover so stale files are removed.
 */
export async function deleteOrganizationImage(path: string | null | undefined): Promise<void> {
    if (!path) return
    const { error } = await supabase.storage.from(BUCKET).remove([path])
    if (error) {
        console.warn('Could not delete old organization image:', error.message)
    }
}

/**
 * Resolve a stored path (e.g. "organizations/{id}/avatar-123.jpg") to
 * a fully-qualified public URL suitable for <img :src="..."/>.
 */
export function getOrganizationImageUrl(path: string | null | undefined): string | null {
    if (!path) return null
    const { data } = supabase.storage.from(BUCKET).getPublicUrl(path)
    return data.publicUrl
}
