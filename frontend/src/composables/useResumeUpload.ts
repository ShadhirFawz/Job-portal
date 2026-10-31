import { supabase } from '@/lib/supabase'

export async function uploadResumeFile(
  userId: string,
  file: File
): Promise<{ path: string; fileName: string; fileSize: number; fileType: string } | null> {
  const timestamp = Date.now()
  const cleanName = file.name.replace(/[^a-zA-Z0-9.-]/g, '_')
  const path = `${userId}/${timestamp}-${cleanName}`

  const { error } = await supabase.storage
    .from('resumes')
    .upload(path, file, {
      contentType: file.type || undefined,
      cacheControl: '3600',
      upsert: false
    })

  if (error) {
    console.error('Resume upload error:', error)
    return null
  }

  return {
    path,
    fileName: file.name,
    fileSize: file.size,
    fileType: file.type || 'application/octet-stream'
  }
}

export async function getResumeSignedUrl(path: string, expiresIn = 3600): Promise<string | null> {
  if (!path) return null
  const { data, error } = await supabase.storage
    .from('resumes')
    .createSignedUrl(path, expiresIn)

  if (error) {
    console.error('Signed URL error:', error)
    return null
  }
  return data.signedUrl
}

export async function deleteResumeFile(path: string): Promise<boolean> {
  if (!path) return false
  const { error } = await supabase.storage
    .from('resumes')
    .remove([path])

  if (error) {
    console.error('Delete resume file error:', error)
    return false
  }
  return true
}
