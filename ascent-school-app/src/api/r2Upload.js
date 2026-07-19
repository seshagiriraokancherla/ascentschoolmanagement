import api from './axiosInstance'

// Upload a file directly to Cloudflare R2 via a presigned URL.
//   1. ask our API for a presigned PUT URL (server builds the key from `purpose`)
//   2. PUT the file straight to R2 (raw fetch — no app auth headers)
//   3. return the permanent public URL to store in the DB
//
// purpose: 'student-photo' | 'homework' | 'announcement' | 'event'
export async function uploadToR2({ purpose, entityId, file }) {
  const presign = await api.post('/school/uploads/presign', {
    purpose,
    entityId,
    fileName:    file.name,
    contentType: file.type || 'application/octet-stream',
  })
  const { uploadUrl, publicUrl } = presign.data?.data || {}
  if (!uploadUrl) throw new Error('Could not get an upload URL (is R2 configured?).')

  const res = await fetch(uploadUrl, {
    method:  'PUT',
    body:    file,
    headers: { 'Content-Type': file.type || 'application/octet-stream' },
  })
  if (!res.ok) throw new Error(`Upload to storage failed (HTTP ${res.status}).`)

  return publicUrl
}

// Size/type caps (bytes). Videos handled separately (1 GB) in the events screen.
export const MAX_IMAGE_BYTES = 1 * 1024 * 1024   // 1 MB
export const MAX_DOC_BYTES   = 2 * 1024 * 1024   // 2 MB
export const MAX_VIDEO_BYTES = 1024 * 1024 * 1024 // 1 GB

export const IMAGE_TYPES = ['image/jpeg', 'image/png', 'image/webp']
export const DOC_TYPES   = ['application/pdf']
export const AUDIO_TYPES = ['audio/mpeg']              // mp3
export const VIDEO_TYPES = ['video/mp4']
