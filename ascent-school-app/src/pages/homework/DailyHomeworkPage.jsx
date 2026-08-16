import { useEffect, useState } from 'react'
import {
  Card, Select, DatePicker, Button, Input, Typography, App as AntApp,
  Row, Col, Space, Spin, Empty, Alert,
} from 'antd'
import { SaveOutlined } from '@ant-design/icons'
import dayjs from 'dayjs'
import api from '../../api/axiosInstance'

const { Title, Text } = Typography
const { TextArea } = Input

// Treat both new ('Active') and legacy ('Y') as active rows.
const isActive = (s) => ['Active', 'Y', null, undefined, ''].includes(s.status)

// Sentinel option value meaning "the whole class" (stored as section_id NULL).
const ALL_SECTIONS = 0

export default function DailyHomeworkPage() {
  const { message } = AntApp.useApp()

  const [date,       setDate]       = useState(dayjs())
  const [classes,    setClasses]    = useState([])
  const [sections,   setSections]   = useState([])
  const [subjects,   setSubjects]   = useState([])
  const [classId,    setClassId]    = useState(null)
  const [sectionIds, setSectionIds] = useState([ALL_SECTIONS])
  const [texts,      setTexts]      = useState({})   // { [subjectId]: description }
  const [mixed,      setMixed]      = useState(false)
  const [loading,    setLoading]    = useState(false)
  const [saving,     setSaving]     = useState(false)

  // Lookups on mount
  useEffect(() => {
    api.get('/school/master/classes').then(r => setClasses(r.data?.data || []))
    api.get('/school/master/subjects').then(r =>
      setSubjects((r.data?.data || []).filter(isActive)))
  }, [])

  const loadSections = async (cid) => {
    if (!cid) { setSections([]); return }
    try {
      const r = await api.get(`/school/master/sections?classId=${cid}`)
      setSections((r.data?.data || []).filter(isActive))
    } catch { setSections([]) }
  }

  const isClassWide = (secIds) => secIds.length === 0 || secIds.includes(ALL_SECTIONS)

  // Pre-fill the textboxes from homework already saved for this Class+Date. The
  // request omits sectionId so every section's rows come back, then we keep the
  // ones the current selection would replace.
  const loadExisting = async (cid, secIds, d) => {
    if (!cid || !d) { setTexts({}); setMixed(false); return }
    setLoading(true)
    try {
      const params = new URLSearchParams({
        classId:      cid,
        assignedDate: dayjs(d).format('YYYY-MM-DD'),
        pageSize:     200,
      })
      const r = await api.get(`/school/homework?${params}`)
      const rows = r.data?.data?.items || []
      const wide = isClassWide(secIds)

      // Class-wide rows (sectionId null) reach every section, so they always apply.
      const relevant = rows.filter(h =>
        h.sectionId == null || wide || secIds.includes(h.sectionId))

      const map = {}
      let conflict = false
      relevant.forEach(h => {
        if (!h.subjectId) return
        const text = h.description || ''
        if (map[h.subjectId] === undefined) map[h.subjectId] = text
        else if (map[h.subjectId] !== text) conflict = true
      })
      setTexts(map)
      setMixed(conflict)
    } catch {
      setTexts({})
      setMixed(false)
    } finally {
      setLoading(false)
    }
  }

  const onClassChange = (val) => {
    setClassId(val)
    setSectionIds([ALL_SECTIONS])
    setTexts({})
    setMixed(false)
    loadSections(val)
    loadExisting(val, [ALL_SECTIONS], date)
  }

  // "All Sections" and individual sections are mutually exclusive.
  const onSectionChange = (vals) => {
    const next = vals.includes(ALL_SECTIONS) && vals.length > 1
      ? (sectionIds.includes(ALL_SECTIONS) ? vals.filter(v => v !== ALL_SECTIONS) : [ALL_SECTIONS])
      : (vals.length === 0 ? [ALL_SECTIONS] : vals)
    setSectionIds(next)
    loadExisting(classId, next, date)
  }

  const onDateChange = (val) => {
    setDate(val)
    loadExisting(classId, sectionIds, val)
  }

  const setText = (subjectId, value) =>
    setTexts(prev => ({ ...prev, [subjectId]: value }))

  const handleSave = async () => {
    if (!classId) { message.warning('Select a class.'); return }
    if (!date)    { message.warning('Select a date.');  return }

    const items = subjects
      .filter(s => (texts[s.subjectId] || '').trim())
      .map(s => ({ subjectId: s.subjectId, description: texts[s.subjectId].trim() }))

    if (items.length === 0) { message.warning('Enter homework for at least one subject.'); return }

    // Empty list = whole class; the server stores that as one section_id NULL row set.
    const targets = isClassWide(sectionIds) ? [] : sectionIds

    setSaving(true)
    try {
      const r = await api.post('/school/homework/batch', {
        classId,
        sectionId:  null,
        sectionIds: targets,
        assignedDate: date.format('YYYY-MM-DD'),
        items,
      })
      message.success(r.data?.message || 'Homework saved.')
      loadExisting(classId, sectionIds, date)
    } catch (e) {
      message.error(e.message || 'Failed to save homework.')
    } finally {
      setSaving(false)
    }
  }

  const ready = classId && date

  return (
    <div>
      <Title level={4} style={{ marginBottom: 16 }}>Daily Homework</Title>

      <Card size="small" style={{ marginBottom: 16 }}>
        <Space size="large" wrap>
          <Space>
            <Text strong>Date</Text>
            <DatePicker value={date} onChange={onDateChange} format="DD-MMM-YYYY" allowClear={false} />
          </Space>
          <Space>
            <Text strong>Class</Text>
            <Select
              style={{ width: 180 }}
              placeholder="Select class"
              value={classId}
              onChange={onClassChange}
              options={classes.map(c => ({ label: c.className, value: c.classId }))}
            />
          </Space>
          <Space align="start">
            <Text strong style={{ lineHeight: '32px' }}>Section</Text>
            <Select
              mode="multiple"
              style={{ minWidth: 260 }}
              placeholder="All Sections (whole class)"
              value={sectionIds}
              onChange={onSectionChange}
              disabled={!classId}
              maxTagCount="responsive"
              options={[
                { label: 'All Sections (whole class)', value: ALL_SECTIONS },
                ...sections.map(s => ({ label: s.sectionName, value: s.sectionId })),
              ]}
            />
          </Space>
        </Space>
      </Card>

      {mixed && (
        <Alert
          type="warning"
          showIcon
          style={{ marginBottom: 16 }}
          message="This date already has different homework across sections"
          description="The boxes below show the first entry found for each subject. Saving replaces the day's homework for every section you selected."
        />
      )}

      <Card
        title="Subjects"
        extra={
          <Button
            type="primary"
            icon={<SaveOutlined />}
            loading={saving}
            disabled={!ready}
            onClick={handleSave}
          >
            Save
          </Button>
        }
      >
        {!ready ? (
          <Empty description="Select date and class to enter homework" />
        ) : subjects.length === 0 ? (
          <Empty description="No active subjects found. Add subjects in Master Data." />
        ) : (
          <Spin spinning={loading}>
            <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
              {subjects.map(s => (
                <Row key={s.subjectId} gutter={12} align="middle">
                  <Col xs={24} sm={6} md={5} lg={4}>
                    <Text strong>{s.subjectName}</Text>
                  </Col>
                  <Col xs={24} sm={18} md={19} lg={20}>
                    <TextArea
                      autoSize={{ minRows: 1, maxRows: 4 }}
                      value={texts[s.subjectId] || ''}
                      onChange={e => setText(s.subjectId, e.target.value)}
                      placeholder={`Homework for ${s.subjectName}`}
                    />
                  </Col>
                </Row>
              ))}
            </div>
          </Spin>
        )}
      </Card>
    </div>
  )
}
