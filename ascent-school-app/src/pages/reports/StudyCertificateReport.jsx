import { useState } from 'react'
import {
  Select, Button, Row, Col, Typography, Divider, App as AntApp,
  Card, Descriptions, Input, Form,
} from 'antd'
import { FilePdfOutlined, SearchOutlined } from '@ant-design/icons'
import { useBrandingStore } from '../../store/brandingStore'
import { generateStudyCertificate } from './reportUtils'
import api, { apiError } from '../../api/axiosInstance'
import dayjs from 'dayjs'

const { Text, Title } = Typography

const PURPOSE_OPTIONS = [
  { value: 'general',         label: 'General' },
  { value: 'bank',            label: 'Bank' },
  { value: 'passport',        label: 'Passport' },
  { value: 'scholarship',     label: 'Scholarship' },
  { value: 'fee concession',  label: 'Fee Concession' },
  { value: 'transfer',        label: 'Transfer' },
  { value: 'higher studies',  label: 'Higher Studies' },
]

export default function StudyCertificateReport() {
  const { message } = AntApp.useApp()
  const schoolName  = useBrandingStore(s => s.branding.displayName)

  const [search,    setSearch]    = useState('')
  const [options,   setOptions]   = useState([])
  const [searching, setSearching] = useState(false)
  const [student,   setStudent]   = useState(null)   // full student DTO
  const [loading,   setLoading]   = useState(false)
  const [issuedFor, setIssuedFor] = useState('general')

  // Search students by name / admission no
  const handleSearch = async (val) => {
    if (!val || val.length < 2) { setOptions([]); return }
    setSearching(true)
    try {
      const r = await api.get(`/school/students?search=${encodeURIComponent(val)}`)
      const list = r.data?.data || []
      setOptions(list.map(s => ({
        value: s.studentId,
        label: `${s.studentName} (${s.admissionNo}) — ${s.className}`,
        raw:   s,
      })))
    } catch { setOptions([]) }
    finally { setSearching(false) }
  }

  // Load full student detail on selection
  const handleSelect = async (studentId) => {
    setLoading(true)
    setStudent(null)
    try {
      const r = await api.get(`/school/students/${studentId}`)
      setStudent(r.data?.data)
    } catch (e) { message.error(apiError(e, 'Failed to load student details.')) }
    finally { setLoading(false) }
  }

  const handleGenerate = () => {
    if (!student) return
    generateStudyCertificate({
      schoolName,
      studentName:  student.studentName,
      fatherName:   student.fatherName,
      motherName:   student.motherName,
      className:    student.className,
      sectionName:  student.sectionName,
      academicYear: student.academicYear,
      dateOfBirth:  student.dateOfBirth
        ? dayjs(student.dateOfBirth).format('DD MMMM YYYY')
        : '',
      gender:       student.gender,
      admissionNo:  student.admissionNo,
      issuedFor,
    })
  }

  return (
    <div>
      <Row gutter={12} align="middle" style={{ marginBottom: 16 }}>
        <Col flex="320px">
          <Select
            showSearch
            style={{ width: '100%' }}
            placeholder="Search student by name or admission no…"
            filterOption={false}
            onSearch={handleSearch}
            onSelect={handleSelect}
            options={options}
            loading={searching}
            suffixIcon={<SearchOutlined />}
            notFoundContent={searching ? 'Searching…' : 'Type to search'}
          />
        </Col>
        <Col>
          <Select
            style={{ width: 180 }}
            value={issuedFor}
            onChange={setIssuedFor}
            options={PURPOSE_OPTIONS}
          />
        </Col>
        <Col>
          <Button
            type="primary"
            icon={<FilePdfOutlined />}
            onClick={handleGenerate}
            disabled={!student}
            loading={loading}
          >
            Generate PDF
          </Button>
        </Col>
      </Row>

      {student && (
        <>
          <Divider style={{ margin: '8px 0' }} />

          {/* Certificate preview */}
          <Card
            style={{
              maxWidth: 600, margin: '0 auto',
              border: '2px solid #1677ff',
              borderRadius: 8,
            }}
          >
            <div style={{ textAlign: 'center', marginBottom: 12 }}>
              <Title level={4} style={{ color: '#1677ff', marginBottom: 2 }}>{schoolName}</Title>
              <Divider style={{ margin: '8px 0' }} />
              <Title level={5} style={{ letterSpacing: 2, marginBottom: 0 }}>STUDY CERTIFICATE</Title>
            </div>

            <Text italic style={{ color: '#888', display: 'block', marginBottom: 12 }}>
              To Whom It May Concern
            </Text>

            <Text style={{ lineHeight: 2 }}>
              This is to certify that{' '}
              <Text strong>{student.studentName}</Text>{' '}
              (Adm No: {student.admissionNo}),{' '}
              {student.gender?.toLowerCase() === 'female' ? 'daughter' : 'son'} of{' '}
              <Text strong>{[student.fatherName, student.motherName].filter(Boolean).join(' and ') || '—'}</Text>,
              is a bonafide student of this institution.{' '}
              {student.gender?.toLowerCase() === 'female' ? 'Her' : 'His'}/Her particulars are as follows:
            </Text>

            <Descriptions
              column={1}
              size="small"
              style={{ marginTop: 16, marginBottom: 16 }}
              labelStyle={{ fontWeight: 600, width: 140 }}
            >
              <Descriptions.Item label="Class">
                {student.className}{student.sectionName ? ` — ${student.sectionName}` : ''}
              </Descriptions.Item>
              <Descriptions.Item label="Academic Year">{student.academicYear || '—'}</Descriptions.Item>
              <Descriptions.Item label="Date of Birth">
                {student.dateOfBirth ? dayjs(student.dateOfBirth).format('DD MMMM YYYY') : '—'}
              </Descriptions.Item>
            </Descriptions>

            <Text>
              This certificate is issued on request for{' '}
              <Text strong>{issuedFor}</Text> purposes.
            </Text>

            <Row justify="space-between" style={{ marginTop: 32 }}>
              <Col>
                <Text type="secondary" style={{ fontSize: 11 }}>
                  Date: {new Date().toLocaleDateString('en-IN', { day: '2-digit', month: 'long', year: 'numeric' })}
                </Text>
              </Col>
              <Col style={{ textAlign: 'right' }}>
                <div style={{ borderTop: '1px solid #666', paddingTop: 4, minWidth: 160 }}>
                  <Text style={{ fontSize: 11 }}>Principal / Head of Institution</Text>
                </div>
              </Col>
            </Row>
          </Card>

          <div style={{ textAlign: 'center', marginTop: 16 }}>
            <Button type="primary" icon={<FilePdfOutlined />} size="large" onClick={handleGenerate}>
              Download PDF
            </Button>
          </div>
        </>
      )}
    </div>
  )
}
