import { useEffect, useState } from 'react'
import { Upload, Button, List, Typography, Popconfirm, App as AntApp, Tag, Space } from 'antd'
import { UploadOutlined, DeleteOutlined, FileImageOutlined, FilePdfOutlined, SoundOutlined, VideoCameraOutlined, PaperClipOutlined } from '@ant-design/icons'
import api, { apiError } from '../api/axiosInstance'
import { uploadToR2, MAX_IMAGE_BYTES, MAX_DOC_BYTES, MAX_VIDEO_BYTES } from '../api/r2Upload'

const { Text } = Typography

// classes: which file kinds this uploader accepts, e.g. ['image','doc','audio'] or ['image','doc','audio','video']
const KIND = {
  image: { mimes: ['image/jpeg', 'image/png', 'image/webp'], max: MAX_IMAGE_BYTES, label: 'image', accept: '.jpg,.jpeg,.png,.webp' },
  doc:   { mimes: ['application/pdf'],                        max: MAX_DOC_BYTES,   label: 'PDF',   accept: '.pdf' },
  audio: { mimes: ['audio/mpeg'],                             max: 10 * 1024 * 1024, label: 'audio', accept: '.mp3' },
  video: { mimes: ['video/mp4'],                              max: MAX_VIDEO_BYTES, label: 'video', accept: '.mp4' },
}

function classify(mime) {
  if ((mime || '').startsWith('image/')) return 'image'
  if (mime === 'application/pdf')        return 'doc'
  if ((mime || '').startsWith('audio/')) return 'audio'
  if ((mime || '').startsWith('video/')) return 'video'
  return 'other'
}

function iconFor(fileType) {
  if (fileType === 'image') return <FileImageOutlined />
  if (fileType === 'doc')   return <FilePdfOutlined />
  if (fileType === 'audio') return <SoundOutlined />
  if (fileType === 'video') return <VideoCameraOutlined />
  return <PaperClipOutlined />
}

const fmtSize = (kb) => kb == null ? '' : kb >= 1024 ? `${(kb / 1024).toFixed(1)} MB` : `${kb} KB`

export default function MediaUploader({ entityType, entityId, classes = ['image', 'doc', 'audio'], max = 3 }) {
  const { message } = AntApp.useApp()
  const [items,     setItems]     = useState([])
  const [loading,   setLoading]   = useState(false)
  const [uploading, setUploading] = useState(false)

  const acceptKinds = classes.map(k => KIND[k]).filter(Boolean)
  const acceptAttr  = acceptKinds.map(k => k.accept).join(',')

  const loadItems = async () => {
    if (!entityId) return
    setLoading(true)
    try {
      const r = await api.get(`/school/media?entityType=${entityType}&entityId=${entityId}`)
      setItems(r.data?.data || [])
    } catch { /* ignore */ } finally { setLoading(false) }
  }

  useEffect(() => { loadItems() }, [entityType, entityId])

  const beforeUpload = async (file) => {
    const kind = classify(file.type)
    if (!classes.includes(kind)) {
      message.error(`Only ${classes.join(' / ')} files are allowed here.`)
      return Upload.LIST_IGNORE
    }
    if (file.size > KIND[kind].max) {
      message.error(`${KIND[kind].label} must be ${fmtSize(Math.round(KIND[kind].max / 1024))} or smaller.`)
      return Upload.LIST_IGNORE
    }
    if (items.length >= max) {
      message.warning(`Maximum ${max} files.`)
      return Upload.LIST_IGNORE
    }
    setUploading(true)
    try {
      const fileUrl = await uploadToR2({ purpose: entityType, entityId, file })
      const res = await api.post('/school/media/attach', {
        entityType, entityId,
        fileName:   file.name,
        fileUrl,
        fileType:   kind,
        fileSizeKb: Math.round(file.size / 1024),
      })
      setItems(prev => [...prev, res.data?.data])
      message.success('File uploaded.')
    } catch (e) {
      message.error(e.message || 'Upload failed.')
    } finally {
      setUploading(false)
    }
    return Upload.LIST_IGNORE   // we manage the list ourselves
  }

  const remove = async (uploadId) => {
    try {
      await api.delete(`/school/media/${uploadId}`)
      setItems(prev => prev.filter(i => i.uploadId !== uploadId))
    } catch (e) { message.error(apiError(e, 'Failed to remove file.')) }
  }

  return (
    <div>
      <Upload beforeUpload={beforeUpload} showUploadList={false} accept={acceptAttr} multiple={false} disabled={uploading || items.length >= max}>
        <Button icon={<UploadOutlined />} loading={uploading} disabled={items.length >= max}>
          Upload file ({items.length}/{max})
        </Button>
      </Upload>
      <Text type="secondary" style={{ fontSize: 12, marginLeft: 8 }}>
        {classes.join(', ')} — {classes.includes('video') ? 'video ≤ 1 GB, ' : ''}image ≤ 1 MB, PDF ≤ 2 MB
      </Text>

      <List
        size="small"
        style={{ marginTop: 8 }}
        loading={loading}
        locale={{ emptyText: 'No files uploaded yet.' }}
        dataSource={items}
        renderItem={(it) => it && (
          <List.Item
            actions={[
              <Popconfirm key="d" title="Remove this file?" onConfirm={() => remove(it.uploadId)}>
                <Button size="small" danger icon={<DeleteOutlined />} />
              </Popconfirm>,
            ]}
          >
            <Space size={8}>
              {iconFor(it.fileType)}
              <a href={it.fileUrl} target="_blank" rel="noopener noreferrer">{it.fileName}</a>
              {it.fileType && <Tag>{it.fileType}</Tag>}
              <Text type="secondary" style={{ fontSize: 12 }}>{fmtSize(it.fileSizeKb)}</Text>
            </Space>
          </List.Item>
        )}
      />
    </div>
  )
}
