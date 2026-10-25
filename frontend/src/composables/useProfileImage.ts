import { supabase } from '@/lib/supabase'

export async function uploadProfileImage(
    userId: string,
    file: File,
    type: 'avatar' | 'cover'
): Promise<string | null> {
    const ext = file.name.split('.').pop()
    const timestamp = Date.now()
    const path = `users/${userId}/${type}-${timestamp}.${ext}`

    const { error } = await supabase.storage
        .from('profile-images')
        .upload(path, file, {
            cacheControl: '3600',
            upsert: false
        })

    if (error) {
        console.error('Upload error:', error)
        return null
    }

    // Return the path (we store path, not full URL)
    return path
}

export function getPublicUrl(path?: string | null): string | undefined {
    if (!path) return undefined
    const { data } = supabase.storage.from('profile-images').getPublicUrl(path)
    return data.publicUrl
}