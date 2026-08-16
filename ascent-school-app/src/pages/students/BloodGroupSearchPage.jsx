import { useState } from 'react'
import {
  Card, Select, Button, Table, Typography, Tag, Space, Row, Col, App as AntApp,
} from 'antd'
import { SearchOutlined, FilePdfOutlined, FileExcelOutlined } from '@ant-design/icons'
import { useBrandingStore } from '../../store/brandingStore'
import { exportPdf, exportCsv } from '../reports/reportUtils'
import api, { apiError } from '../../api/axiosInstance'

const { Title, Text } = Typography

const BLOOD_GROUPS = ['A+', 'A-', 'B+', 'B-', 'AB+', 'AB-', 'O+', 'O-']

const BLOOD_GROUP_COLORS = {
  'A+': 'red', 'A-': 'volcano',
  'B+': 'blue', 'B-': 'geekblue',
  'AB+': 'purple', 'AB-': 'magenta',
  'O+': 'green', 'O-': 'cyan',
}

const CSV_COLUMNS = [
  { label: 'Adm No',       key: 'admissionNo'  },
  { label: 'Student Name', key: 'studentName'  },
  { label: 'Class',        key: 'className'    },
  { label: 'Section',      key: 'sectionName'  },
  { label: 'Blood Group',  key: 'bloodGroup'   },
  { label: 'Gender',       key: 'gender'       },
  { label: 'Father Name',  key: 'fatherName'   },
  { label: 'Mobile',       key: 'fatherMobile' },
]

export default function BloodGroupSearchPage() {
  const { message } = AntApp.useApp()
  const schoolName  = useBrandingStore(s => s.branding.displayName)

  const [bloodGroup, setBloodGroup] = useState(null)
  const [data,       setData]       = useState(null)
  const [loading,    setLoading]    = useState(false)

  const load = async () => {
    if (!bloodGroup) { message.warning('Select a blood group.'); return }
    setLoading(true)
    try {
      const r = await api.get(`/school/students?bloodGroup=${encodeURIComponent(bloodGroup)}&status=Active`)
      setData(r.data?.data || [])
    } catch (e) {
      message.error(apiError(e, 'Failed to load students.'))
    } finally {
      setLoading(false)
    }
  }

  const columns = [
    { title: '#',            key: 'idx',         width: 50,  render: (_, __, i) => i + 1 },
    { title: 'Adm No',       dataIndex: 'admissionNo',  width: 110 },
    { title: 'Student Name', dataIndex: 'studentName',  ellipsis: true },
    { title: 'Class',        dataIndex: 'className',    width: 100 },
    { title: 'Section',      dataIndex: 'sectionName',  width: 80,
      render: v => v ? <Tag>{v}</Tag> : <Text type="secondary">—</Text> },
    { title: 'Blood Group',  dataIndex: 'bloodGroup',   width: 110,
      render: v => v
        ? <Tag color={BLOOD_GROUP_COLORS[v] || 'default'}><strong>{v}</strong></Tag>
        : <Text type="secondary">—</Text> },
    { title: 'Gender',       dataIndex: 'gender',       width: 90 },
    { title: 'Father Name',  dataIndex: 'fatherName',   ellipsis: true },
    { title: 'Mobile',       dataIndex: 'fatherMobile', width: 125 },
  ]

  const exportToPdf = () => {
    if (!data?.length) return
    exportPdf({
      title:     `Blood Group: ${bloodGroup}`,
      schoolName,
      columns:   CSV_COLUMNS.map(c => c.label),
      rows:      data.map(r => CSV_COLUMNS.map(c => r[c.key] ?? '')),
      landscape: true,
    })
  }

  const exportToCsv = () => {
    if (!data?.length) return
    exportCsv({
      columns:  CSV_COLUMNS,
      rows:     data,
      filename: `blood_group_${bloodGroup.replace('+', 'pos').replace('-', 'neg')}`,
    })
  }

  return (
    <div>
      <Title level={4} style={{ marginBottom: 16 }}>Blood Group Search</Title>

      <Card style={{ marginBottom: 16 }}>
        <Row gutter={12} align="bottom">
          <Col>
            <Text strong>Blood Group</Text>
            <Select
              style={{ display: 'block', width: 160, marginTop: 4 }}
              placeholder="Select blood group"
              value={bloodGroup}
              onChange={v => { setBloodGroup(v); setData(null) }}
              options={BLOOD_GROUPS.map(g => ({
                value: g,
                label: (
                  <span>
                    <Tag color={BLOOD_GROUP_COLORS[g]} style={{ marginRight: 4 }}>{g}</Tag>
                  </span>
                ),
              }))}
            />
          </Col>
          <Col style={{ marginTop: 20 }}>
            <Button type="primary" icon={<SearchOutlined />} loading={loading} onClick={load}>
              Search
            </Button>
          </Col>
        </Row>
      </Card>

      {data && (
        <>
          {data.length > 0 && (
            <Row justify="end" style={{ marginBottom: 8 }}>
              <Space>
                <Text type="secondary">{data.length} students</Text>
                <Button size="small" icon={<FilePdfOutlined />} onClick={exportToPdf}>PDF</Button>
                <Button size="small" icon={<FileExcelOutlined />} onClick={exportToCsv}>CSV</Button>
              </Space>
            </Row>
          )}
          <Card>
            {data.length === 0
              ? <Text type="secondary">No active students found with blood group <strong>{bloodGroup}</strong>.</Text>
              : (
                <Table
                  size="small"
                  rowKey="studentId"
                  columns={columns}
                  dataSource={data}
                  pagination={{ pageSize: 50, showTotal: t => `${t} students` }}
                />
              )
            }
          </Card>
        </>
      )}
    </div>
  )
}
