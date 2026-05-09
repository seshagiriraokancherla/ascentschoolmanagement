import { useState, useRef } from 'react'
import {
  Card, Button, Table, Typography, Upload, App as AntApp,
  Alert, Progress, Space, Tag, Divider,
} from 'antd'
import {
  UploadOutlined, DownloadOutlined, CheckCircleOutlined, CloseCircleOutlined,
} from '@ant-design/icons'
import { useNavigate } from 'react-router-dom'
import Papa from 'papaparse'
import api from '../../api/axiosInstance'

const { Title, Text, Link } = Typography

// ── Example data shown to the user ───────────────────────────────────────────
const EXAMPLE_ROWS = [
  {
    AdmissionNo: 'STU001', StudentName: 'Ravi Kumar', Gender: 'Male',
    DateOfBirth: '15/08/2012', AcademicYear: '2024-25', ClassName: 'Class 6',
    SectionName: 'A', RollNo: '1', FeeCategory: 'General',
    FatherName: 'Suresh Kumar', MotherName: 'Lakshmi Kumar',
    FatherMobile: '9876543210', MotherMobile: '9876543211',
    AadharNo: '1234-5678-9012', Caste: 'OC', CasteCode: 'OC',
    Religion: 'Hindu', JoiningClass: 'LKG', MotherTongue: 'Telugu',
    Status: 'Active',
  },
  {
    AdmissionNo: 'STU002', StudentName: 'Priya Sharma', Gender: 'Female',
    DateOfBirth: '22/03/2013', AcademicYear: '2024-25', ClassName: 'Class 5',
    SectionName: 'B', RollNo: '2', FeeCategory: 'Staff Child',
    FatherName: 'Mohan Sharma', MotherName: 'Sita Sharma',
    FatherMobile: '9123456789', MotherMobile: '',
    AadharNo: '', Caste: 'BC-B', CasteCode: 'BC-B',
    Religion: 'Hindu', JoiningClass: 'Class 1', MotherTongue: 'Hindi',
    Status: 'Active',
  },
]

const COLUMNS_META = [
  { key: 'AdmissionNo',   required: true,  note: 'Unique per school' },
  { key: 'StudentName',   required: true,  note: '' },
  { key: 'Gender',        required: false, note: 'Male / Female / Other' },
  { key: 'DateOfBirth',   required: false, note: 'dd/MM/yyyy' },
  { key: 'AcademicYear',  required: false, note: 'Must match master data, e.g. 2024-25' },
  { key: 'ClassName',     required: false, note: 'Must match master data, e.g. Class 6' },
  { key: 'SectionName',   required: false, note: 'Must match section under the class, e.g. A' },
  { key: 'RollNo',        required: false, note: '' },
  { key: 'FeeCategory',   required: false, note: 'Must match master data, e.g. General' },
  { key: 'FatherName',    required: false, note: '' },
  { key: 'MotherName',    required: false, note: '' },
  { key: 'FatherMobile',  required: false, note: '10-digit mobile number' },
  { key: 'MotherMobile',  required: false, note: '10-digit mobile number' },
  { key: 'AadharNo',      required: false, note: 'Aadhar number' },
  { key: 'Caste',         required: false, note: 'e.g. OC / BC-B / SC / ST' },
  { key: 'CasteCode',     required: false, note: 'e.g. OC / BC-B / SC / ST' },
  { key: 'Religion',      required: false, note: 'e.g. Hindu / Muslim / Christian' },
  { key: 'JoiningClass',  required: false, note: 'Class at first admission, e.g. LKG' },
  { key: 'MotherTongue',  required: false, note: 'e.g. Telugu / Hindi / English' },
  { key: 'Status',        required: false, note: 'Active (default) / Inactive' },
]

const CSV_HEADERS = COLUMNS_META.map((c) => c.key)

const exampleColumns = [
  ...COLUMNS_META.map((c) => ({
    title: (
      <span>
        {c.key}
        {c.required && <span style={{ color: 'red' }}> *</span>}
      </span>
    ),
    dataIndex: c.key,
    key:       c.key,
    width:     140,
    render:    (v) => <Text style={{ fontSize: 12 }}>{v}</Text>,
  })),
]

const errorColumns = [
  { title: 'Row',          dataIndex: 'row',         key: 'row',         width: 70 },
  { title: 'Admission No', dataIndex: 'admissionNo',  key: 'admissionNo', width: 120 },
  { title: 'Reason',       dataIndex: 'reason',       key: 'reason' },
]

// ── Template download ─────────────────────────────────────────────────────────
function downloadTemplate() {
  const header = CSV_HEADERS.join(',')
  const row1   = EXAMPLE_ROWS.map((r) => CSV_HEADERS.map((h) => r[h] ?? '').join(',')).join('\n')
  const blob   = new Blob([header + '\n' + row1], { type: 'text/csv' })
  const url    = URL.createObjectURL(blob)
  const a      = document.createElement('a')
  a.href       = url
  a.download   = 'students_import_template.csv'
  a.click()
  URL.revokeObjectURL(url)
}

// ── Error CSV download ────────────────────────────────────────────────────────
function downloadErrors(errors) {
  const header = 'Row,AdmissionNo,Reason'
  const rows   = errors.map((e) => `${e.row},"${e.admissionNo || ''}","${e.reason}"`).join('\n')
  const blob   = new Blob([header + '\n' + rows], { type: 'text/csv' })
  const url    = URL.createObjectURL(blob)
  const a      = document.createElement('a')
  a.href       = url
  a.download   = 'students_import_errors.csv'
  a.click()
  URL.revokeObjectURL(url)
}

export default function StudentsImportPage() {
  const { message } = AntApp.useApp()
  const navigate    = useNavigate()

  const [preview,  setPreview]  = useState(null)   // parsed CSV rows
  const [fileName, setFileName] = useState(null)
  const [result,   setResult]   = useState(null)   // BulkImportResult from API
  const [loading,  setLoading]  = useState(false)
  const fileRef = useRef(null)

  const handleFile = (file) => {
    setResult(null)
    Papa.parse(file, {
      header:        true,
      skipEmptyLines: true,
      complete: ({ data, meta }) => {
        // Validate that required columns exist
        const missing = ['AdmissionNo', 'StudentName'].filter((c) => !meta.fields.includes(c))
        if (missing.length) {
          message.error(`CSV is missing required columns: ${missing.join(', ')}`)
          return
        }
        if (data.length > 500) {
          message.error('File has more than 500 rows. Please split and upload in batches.')
          return
        }
        setPreview(data)
        setFileName(file.name)
      },
      error: () => message.error('Failed to parse CSV file.'),
    })
    return false // prevent default upload behaviour
  }

  const handleImport = async () => {
    if (!preview || preview.length === 0) return
    setLoading(true)
    try {
      const r = await api.post('/school/students/bulk', { rows: preview })
      setResult(r.data.data)
    } catch (e) {
      message.error(e.message || 'Import failed.')
    } finally {
      setLoading(false)
    }
  }

  const reset = () => {
    setPreview(null)
    setFileName(null)
    setResult(null)
    if (fileRef.current) fileRef.current.value = ''
  }

  return (
    <div>
      <Title level={4} style={{ marginBottom: 16 }}>Bulk Student Import</Title>

      {/* ── Column reference table ──────────────────────────────────────────── */}
      <Card title="CSV Column Reference" style={{ marginBottom: 16 }}>
        <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 13 }}>
          <thead>
            <tr style={{ background: '#fafafa' }}>
              <th style={th}>Column Name</th>
              <th style={th}>Required</th>
              <th style={th}>Notes</th>
            </tr>
          </thead>
          <tbody>
            {COLUMNS_META.map((c) => (
              <tr key={c.key}>
                <td style={td}><code>{c.key}</code></td>
                <td style={td}>{c.required ? <Tag color="red">Required</Tag> : <Tag>Optional</Tag>}</td>
                <td style={td}>{c.note}</td>
              </tr>
            ))}
          </tbody>
        </table>

        <Divider style={{ margin: '12px 0' }} />
        <Text type="secondary" style={{ fontSize: 12 }}>Example data (first two rows of template):</Text>
        <div style={{ overflowX: 'auto', marginTop: 8 }}>
          <Table
            dataSource={EXAMPLE_ROWS}
            columns={exampleColumns}
            rowKey="AdmissionNo"
            pagination={false}
            size="small"
            scroll={{ x: 'max-content' }}
            style={{ fontSize: 12 }}
          />
        </div>

        <div style={{ marginTop: 12 }}>
          <Button icon={<DownloadOutlined />} onClick={downloadTemplate}>
            Download Template CSV
          </Button>
        </div>
      </Card>

      {/* ── Upload card ─────────────────────────────────────────────────────── */}
      <Card title="Upload CSV" style={{ marginBottom: 16 }}>
        <Space direction="vertical" style={{ width: '100%' }}>
          <Upload
            accept=".csv"
            showUploadList={false}
            beforeUpload={handleFile}
          >
            <Button icon={<UploadOutlined />}>Choose CSV file</Button>
          </Upload>

          {fileName && !result && (
            <Text type="secondary">Selected: <strong>{fileName}</strong> — {preview?.length} rows</Text>
          )}

          {preview && !result && (
            <Alert
              type="info"
              showIcon
              message={`${preview.length} rows ready to import. Review the preview below, then click Import.`}
            />
          )}

          {preview && !result && (
            <Space>
              <Button type="primary" loading={loading} onClick={handleImport}>
                Import {preview.length} Students
              </Button>
              <Button onClick={reset}>Clear</Button>
            </Space>
          )}
        </Space>
      </Card>

      {/* ── Preview table ────────────────────────────────────────────────────── */}
      {preview && !result && (
        <Card title={`Preview — ${preview.length} rows`} style={{ marginBottom: 16 }}>
          <div style={{ overflowX: 'auto' }}>
            <Table
              dataSource={preview.slice(0, 10)}
              columns={CSV_HEADERS.map((h) => ({ title: h, dataIndex: h, key: h, width: 130, ellipsis: true }))}
              rowKey={(_, i) => i}
              pagination={false}
              size="small"
              scroll={{ x: 'max-content' }}
              footer={preview.length > 10 ? () => `… and ${preview.length - 10} more rows (not shown)` : undefined}
            />
          </div>
        </Card>
      )}

      {/* ── Result ──────────────────────────────────────────────────────────── */}
      {result && (
        <Card
          title="Import Result"
          extra={<Button onClick={reset}>Import Another File</Button>}
          style={{ marginBottom: 16 }}
        >
          <Space size="large" style={{ marginBottom: 16 }}>
            <Text><strong>Total:</strong> {result.total}</Text>
            <Text style={{ color: '#52c41a' }}>
              <CheckCircleOutlined /> Imported: {result.imported}
            </Text>
            <Text style={{ color: '#ff4d4f' }}>
              <CloseCircleOutlined /> Failed: {result.failed}
            </Text>
          </Space>

          <Progress
            percent={Math.round((result.imported / result.total) * 100)}
            status={result.failed > 0 ? 'exception' : 'success'}
            style={{ maxWidth: 400 }}
          />

          {result.errors && result.errors.length > 0 && (
            <div style={{ marginTop: 16 }}>
              <Space style={{ marginBottom: 8 }}>
                <Text strong>Failed Rows</Text>
                <Button size="small" icon={<DownloadOutlined />} onClick={() => downloadErrors(result.errors)}>
                  Download Error Report
                </Button>
              </Space>
              <Table
                dataSource={result.errors}
                columns={errorColumns}
                rowKey={(_, i) => i}
                pagination={{ pageSize: 10, size: 'small' }}
                size="small"
              />
            </div>
          )}

          {result.imported > 0 && (
            <div style={{ marginTop: 16 }}>
              <Button type="primary" onClick={() => navigate('/students')}>
                View Students
              </Button>
            </div>
          )}
        </Card>
      )}
    </div>
  )
}

const th = { padding: '6px 12px', border: '1px solid #f0f0f0', textAlign: 'left', fontWeight: 600 }
const td = { padding: '6px 12px', border: '1px solid #f0f0f0' }
