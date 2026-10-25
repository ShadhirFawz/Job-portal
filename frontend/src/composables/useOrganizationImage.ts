import { supabase } from '@/lib/supabase'

export async function uploadOrganizationImage(
    organizationId: string,
    file: File,
    type: 'avatar' | 'cover'
): Promise<string | null> {
    const ext = file.name.split('.').pop()
    const timestamp = Date.now()
    const path = `organizations/${organizationId}/${type}-${timestamp}.${ext}`

    const { error } = await supabase.storage
        .from('profile-images')
        .upload(path, file, {
            cacheControl: '3600',
            upsert: false
        })

    if (error) {
        console.error('Organization image upload error:', error)
        return null
    }

    return path
}

export function getOrganizationImageUrl(path: string | null): string | null {
    if (!path) return null
    const { data } = supabase.storage.from('profile-images').getPublicUrl(path)
    return data.publicUrl
}