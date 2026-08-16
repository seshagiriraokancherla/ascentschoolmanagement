import { useEffect, useState } from 'react'
import {
  Form, Input, Select, DatePicker, InputNumber, Upload, Avatar,
  Button, Tabs, Card, Space, Row, Col, Divider, App as AntApp, Tag, Drawer, Table,
} from 'antd'
import {
  ArrowLeftOutlined, SaveOutlined, UserOutlined, CameraOutlined, HistoryOutlined,
} from '@ant-design/icons'
import { useNavigate, useParams } from 'react-router-dom'
import dayjs from 'dayjs'
import api, { apiError } from '../../api/axiosInstance'
import { uploadToR2, MAX_IMAGE_BYTES, IMAGE_TYPES } from '../../api/r2Upload'
import { useAuthStore } from '../../store/authStore'

const API_BASE = import.meta.env.VITE_API_BASE_URL || 'http://localhost:62845'

const STATUS_OPTIONS       = [{ value: 'Active', label: 'Active' }, { value: 'Inactive', label: 'Inactive' }, { value: 'TC', label: 'TC Issued' }]
const GENDER_OPTIONS       = [{ value: 'Male', label: 'Male' }, { value: 'Female', label: 'Female' }, { value: 'Other', label: 'Other' }]
const BLOOD_GROUP_OPTIONS  = ['A+','A-','B+','B-','AB+','AB-','O+','O-'].map((v) => ({ value: v, label: v }))
const JOIN_TYPE_OPTIONS    = [{ value: 'New', label: 'New Admission' }, { value: 'Transfer', label: 'Transfer' }]
const GUARDIAN_OPTIONS     = [{ value: 'Parents', label: 'Parents' }, { value: 'Guardian', label: 'Guardian' }]
const TRANSPORT_OPTIONS    = [{ value: 'Bus', label: 'Bus' }, { value: 'Walking', label: 'Walking' }, { value: 'Van', label: 'Van' }, { value: 'Other', label: 'Other' }]
const STUDENT_TYPE_OPTIONS = [{ value: 'DayScholar', label: 'Day Scholar' }, { value: 'Hosteler', label: 'Hosteler' }]
const CERT_OPTIONS         = [{ value: 'Submitted', label: 'Submitted' }, { value: 'Not Submitted', label: 'Not Submitted' }]
const BUS_TRIP_OPTIONS     = [{ value: '1st Trip', label: '1st Trip' }, { value: '2nd Trip', label: '2nd Trip' }, { value: '3rd Trip', label: '3rd Trip' }]
const SCHOLARSHIP_OPTIONS  = [{ value: 'Yes', label: 'Yes' }, { value: 'No', label: 'No' }]

function toDate(val) {
  if (!val) return null
  const d = dayjs(val)
  return d.isValid() ? d : null
}

export default function StudentFormPage() {
  const { id }   = useParams()
  const navigate = useNavigate()
  const { message } = AntApp.useApp()
  const isEdit   = Boolean(id)

  const [form]          = Form.useForm()
  const [saving,        setSaving]        = useState(false)
  const [loading,       setLoading]       = useState(false)
  const [photoPreview,  setPhotoPreview]  = useState(null)
  const [pendingPhoto,  setPendingPhoto]  = useState(null)   // File object waiting to upload
  const [student,       setStudent]       = useState(null)
  const [historyOpen,   setHistoryOpen]   = useState(false)
  const [historyRows,   setHistoryRows]   = useState([])
  const [historyLoading,setHistoryLoading]= useState(false)

  // Lookup data
  const [academicYears,  setAcademicYears]  = useState([])
  const [classes,        setClasses]        = useState([])
  const [sections,       setSections]       = useState([])
  const [feeCategories,  setFeeCategories]  = useState([])
  const [busRoutes,      setBusRoutes]      = useState([])
  const [buses,          setBuses]          = useState([])
  const [hostels,        setHostels]        = useState([])

  // Load lookups
  useEffect(() => {
    Promise.all([
      api.get('/school/master/academic-years?activeOnly=true'),
      api.get('/school/master/classes'),
      api.get('/school/master/fee-categories'),
    ]).then(([yr, cl, fc]) => {
      const years = yr.data?.data || []
      setAcademicYears(years)
      setClasses(cl.data?.data || [])
      setFeeCategories(fc.data?.data || [])
      if (!isEdit) {
        const current = years.find(y => y.isCurrent)
        if (current) form.setFieldValue('academicYearId', current.academicYearId)
      }
    }).catch(() => {})

    // Bus routes and buses are needed only for transport tab
    api.get('/school/master/bus-routes').then((r) => setBusRoutes(r.data?.data || [])).catch(() => {})
    api.get('/school/master/buses').then((r) => setBuses(r.data?.data || [])).catch(() => {})
    api.get('/school/hostel').then((r) => setHostels(r.data?.data || [])).catch(() => {})
  }, [])

  // Load student if editing
  useEffect(() => {
    if (!isEdit) {
      form.setFieldsValue({ status: 'Active', joinType: 'New' })
      return
    }
    setLoading(true)
    api.get(`/school/students/${id}`)
      .then((res) => {
        const s = res.data?.data
        setStudent(s)
        if (s.photoPath) setPhotoPreview(/^https?:\/\//.test(s.photoPath) ? s.photoPath : `${API_BASE}${s.photoPath}`)
        form.setFieldsValue({
          // Basic
          admissionNo:    s.admissionNo,
          studentName:    s.studentName,
          shortName:      s.shortName,
          joinType:       s.joinType,
          gender:         s.gender,
          dateOfBirth:    toDate(s.dateOfBirth),
          bloodGroup:     s.bloodGroup,
          status:         s.status,
          // Academic
          academicYearId: s.academicYearId,
          classId:        s.classId,
          branchName:     s.branchName,
          sectionId:      s.sectionId,
          rollNo:         s.rollNo,
          dateOfJoining:  toDate(s.dateOfJoining),
          admissionDate:  toDate(s.admissionDate),
          feeCategoryId:  s.feeCategoryId,
          joiningClass:   s.joiningClass,
          joinTerm:       s.joinTerm,
          // Family
          guardianType:          s.guardianType,
          fatherName:            s.fatherName,
          fatherQualification:   s.fatherQualification,
          fatherOccupation:      s.fatherOccupation,
          fatherEmploymentType:  s.fatherEmploymentType,
          fatherMobile:          s.fatherMobile,
          motherName:            s.motherName,
          motherQualification:   s.motherQualification,
          motherOccupation:      s.motherOccupation,
          motherMobile:          s.motherMobile,
          annualIncome:          s.annualIncome,
          familyChildrenCount:   s.familyChildrenCount,
          // Address
          doorNo:           s.doorNo,
          addressArea:      s.addressArea,
          addressCity:      s.addressCity,
          addressState:     s.addressState,
          permanentAddress: s.permanentAddress,
          email:            s.email,
          // Identity
          caste:               s.caste,
          casteCode:           s.casteCode,
          religion:            s.religion,
          nationality:         s.nationality,
          aadharNo:            s.aadharNo,
          udiseNo:             s.udiseNo,
          dobProofSubmitted:   s.dobProofSubmitted,
          casteCertSubmitted:  s.casteCertSubmitted,
          otherCertificates:   s.otherCertificates,
          disabilityStatus:    s.disabilityStatus,
          disabilityType:      s.disabilityType,
          biometricId:         s.biometricId,
          // Transport
          transportType: s.transportType,
          busRouteId:    s.busRouteId,
          busId:         s.busId,
          busTrip:       s.busTrip,
          // Other
          studentType:           s.studentType,
          hostelId:              s.hostelId,
          scholarshipStatus:     s.scholarshipStatus,
          scholarshipDescription:s.scholarshipDescription,
          motherTongue:          s.motherTongue,
          firstLanguage:         s.firstLanguage,
          secondLanguage:        s.secondLanguage,
          thirdLanguage:         s.thirdLanguage,
          referenceName:         s.referenceName,
          remarks:               s.remarks,
          spareField1:           s.spareField1,
          spareField2:           s.spareField2,
        })
        if (s.classId) loadSections(s.classId)
      })
      .catch((e) => message.error(apiError(e, 'Failed to load student.')))
      .finally(() => setLoading(false))
  }, [id])

  const handleSave = async () => {
    const values = await form.validateFields()
    setSaving(true)
    try {
      const body = {
        AdmissionNo:            values.admissionNo,
        StudentName:            values.studentName,
        ShortName:              values.shortName,
        JoinType:               values.joinType,
        Gender:                 values.gender,
        DateOfBirth:            values.dateOfBirth?.toISOString() || null,
        BloodGroup:             values.bloodGroup,
        Status:                 values.status,
        AcademicYearId:         values.academicYearId  || null,
        ClassId:                values.classId         || null,
        BranchName:             values.branchName,
        SectionId:              values.sectionId       || null,
        RollNo:                 values.rollNo,
        DateOfJoining:          values.dateOfJoining?.toISOString()  || null,
        AdmissionDate:          values.admissionDate?.toISOString()   || null,
        FeeCategoryId:          values.feeCategoryId   || null,
        JoiningClass:           values.joiningClass,
        JoinTerm:               values.joinTerm,
        GuardianType:           values.guardianType,
        FatherName:             values.fatherName,
        FatherQualification:    values.fatherQualification,
        FatherOccupation:       values.fatherOccupation,
        FatherEmploymentType:   values.fatherEmploymentType,
        FatherMobile:           values.fatherMobile,
        MotherName:             values.motherName,
        MotherQualification:    values.motherQualification,
        MotherOccupation:       values.motherOccupation,
        MotherMobile:           values.motherMobile,
        AnnualIncome:           values.annualIncome    || null,
        FamilyChildrenCount:    values.familyChildrenCount || null,
        DoorNo:                 values.doorNo,
        AddressArea:            values.addressArea,
        AddressCity:            values.addressCity,
        AddressState:           values.addressState,
        PermanentAddress:       values.permanentAddress,
        Email:                  values.email,
        Caste:                  values.caste,
        CasteCode:              values.casteCode,
        Religion:               values.religion,
        Nationality:            values.nationality,
        AadharNo:               values.aadharNo,
        UdiseNo:                values.udiseNo,
        DobProofSubmitted:      values.dobProofSubmitted,
        CasteCertSubmitted:     values.casteCertSubmitted,
        OtherCertificates:      values.otherCertificates,
        DisabilityStatus:       values.disabilityStatus,
        DisabilityType:         values.disabilityType,
        BiometricId:            values.biometricId,
        TransportType:          values.transportType,
        BusRouteId:             values.busRouteId  || null,
        BusId:                  values.busId       || null,
        BusTrip:                values.busTrip,
        StudentType:            values.studentType,
        HostelId:               values.hostelId || null,
        ScholarshipStatus:      values.scholarshipStatus,
        ScholarshipDescription: values.scholarshipDescription,
        MotherTongue:           values.motherTongue,
        FirstLanguage:          values.firstLanguage,
        SecondLanguage:         values.secondLanguage,
        ThirdLanguage:          values.thirdLanguage,
        ReferenceName:          values.referenceName,
        Remarks:                values.remarks,
        SpareField1:            values.spareField1,
        SpareField2:            values.spareField2,
      }

      let savedId = id
      if (isEdit) {
        await api.put(`/school/students/${id}`, body)
      } else {
        const res = await api.post('/school/students', body)
        savedId   = res.data?.data?.studentId
      }

      // Upload pending photo to R2 (student-images/{AdmissionNo}_{academicYear}.ext), then save its URL
      if (pendingPhoto && savedId) {
        try {
          const photoUrl = await uploadToR2({ purpose: 'student-photo', entityId: savedId, file: pendingPhoto })
          await api.put(`/school/students/${savedId}/photo`, { photoUrl })
        } catch (e) {
          message.warning(`Student saved, but photo upload failed: ${e.message}`)
        }
      }

      message.success(isEdit ? 'Student updated.' : 'Student registered.')
      navigate('/students')
    } catch (err) {
      if (!err?.errorFields) message.error(apiError(err, 'Failed to save student.'))
    } finally {
      setSaving(false)
    }
  }

  // Photo selection (no auto-upload); validate type + size (≤ 1 MB image)
  const handlePhotoSelect = (file) => {
    if (!IMAGE_TYPES.includes(file.type)) {
      message.error('Photo must be a JPG, PNG or WebP image.')
      return false
    }
    if (file.size > MAX_IMAGE_BYTES) {
      message.error('Photo must be 1 MB or smaller.')
      return false
    }
    setPendingPhoto(file)
    const url = URL.createObjectURL(file)
    setPhotoPreview(url)
    return false  // prevent antd auto-upload
  }

  async function loadSections(cId) {
    if (!cId) { setSections([]); return }
    try {
      const res = await api.get(`/school/master/sections?classId=${cId}`)
      setSections(res.data?.data || [])
    } catch { setSections([]) }
  }

  function onClassChange(val) {
    form.setFieldsValue({ sectionId: null })
    loadSections(val)
  }

  const yearOptions     = academicYears.map((y) => ({ value: y.academicYearId, label: y.academicYear }))
  const classOptions    = classes.map((c)        => ({ value: c.classId,        label: c.className }))
  const sectionOptions  = sections.map((s)       => ({ value: s.sectionId,      label: s.sectionName }))
  const catOptions      = feeCategories.map((c)  => ({ value: c.feeCategoryId,  label: c.categoryName }))
  const routeOptions    = busRoutes.map((r)       => ({ value: r.routeId,        label: r.routeName }))
  const busOptions      = buses.map((b)           => ({ value: b.busId,          label: b.busName || `Bus ${b.busId}` }))

  // ── Tab contents ────────────────────────────────────────────────────────

  const tabBasic = (
    <Row gutter={16}>
      <Col xs={24} md={4} style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 12 }}>
        <Avatar
          size={100}
          src={photoPreview}
          icon={!photoPreview && <UserOutlined />}
          style={{ border: '1px solid #d9d9d9' }}
        />
        <Upload showUploadList={false} beforeUpload={handlePhotoSelect} accept="image/*">
          <Button icon={<CameraOutlined />} size="small">Change Photo</Button>
        </Upload>
        {pendingPhoto && <Tag color="blue">Pending upload</Tag>}
      </Col>

      <Col xs={24} md={20}>
        <Row gutter={16}>
          <Col xs={24} md={8}>
            <Form.Item name="studentName" label="Student Name" rules={[{ required: true }]}>
              <Input />
            </Form.Item>
          </Col>
          <Col xs={24} md={8}>
            <Form.Item name="shortName" label="Short Name">
              <Input />
            </Form.Item>
          </Col>
          <Col xs={24} md={8}>
            <Form.Item name="admissionNo" label="Admission No">
              <Input />
            </Form.Item>
          </Col>
          <Col xs={24} md={8}>
            <Form.Item name="gender" label="Gender">
              <Select options={GENDER_OPTIONS} allowClear />
            </Form.Item>
          </Col>
          <Col xs={24} md={8}>
            <Form.Item name="dateOfBirth" label="Date of Birth">
              <DatePicker style={{ width: '100%' }} format="DD-MM-YYYY" />
            </Form.Item>
          </Col>
          <Col xs={24} md={8}>
            <Form.Item name="bloodGroup" label="Blood Group">
              <Select options={BLOOD_GROUP_OPTIONS} allowClear />
            </Form.Item>
          </Col>
          <Col xs={24} md={8}>
            <Form.Item name="joinType" label="Join Type">
              <Select options={JOIN_TYPE_OPTIONS} allowClear />
            </Form.Item>
          </Col>
          <Col xs={24} md={8}>
            <Form.Item name="status" label="Status">
              <Select options={STATUS_OPTIONS} />
            </Form.Item>
          </Col>
        </Row>
      </Col>
    </Row>
  )

  const tabAcademic = (
    <Row gutter={16}>
      <Col xs={24} md={8}>
        <Form.Item name="academicYearId" label="Academic Year">
          <Select options={yearOptions} allowClear placeholder="Select year" />
        </Form.Item>
      </Col>
      <Col xs={24} md={8}>
        <Form.Item name="classId" label="Class">
          <Select options={classOptions} allowClear placeholder="Select class" onChange={onClassChange} />
        </Form.Item>
      </Col>
      <Col xs={24} md={8}>
        <Form.Item name="sectionId" label="Section">
          <Select
            options={sectionOptions}
            allowClear
            placeholder="Select section"
            disabled={sectionOptions.length === 0}
          />
        </Form.Item>
      </Col>
      <Col xs={24} md={8}>
        <Form.Item name="branchName" label="Branch / Stream">
          <Input placeholder="e.g. Science" />
        </Form.Item>
      </Col>
      <Col xs={24} md={8}>
        <Form.Item name="rollNo" label="Roll No">
          <Input />
        </Form.Item>
      </Col>
      <Col xs={24} md={8}>
        <Form.Item name="feeCategoryId" label="Fee Category">
          <Select options={catOptions} allowClear placeholder="Select category" />
        </Form.Item>
      </Col>
      <Col xs={24} md={8}>
        <Form.Item name="dateOfJoining" label="Date of Joining">
          <DatePicker style={{ width: '100%' }} format="DD-MM-YYYY" />
        </Form.Item>
      </Col>
      <Col xs={24} md={8}>
        <Form.Item name="admissionDate" label="Admission Date">
          <DatePicker style={{ width: '100%' }} format="DD-MM-YYYY" />
        </Form.Item>
      </Col>
      <Col xs={24} md={8}>
        <Form.Item name="joiningClass" label="Joining Class">
          <Input placeholder="Class at first admission" />
        </Form.Item>
      </Col>
      <Col xs={24} md={8}>
        <Form.Item name="joinTerm" label="Join Term">
          <Input />
        </Form.Item>
      </Col>
    </Row>
  )

  const tabFamily = (
    <>
      <Form.Item name="guardianType" label="Guardian Type" style={{ maxWidth: 240 }}>
        <Select options={GUARDIAN_OPTIONS} allowClear />
      </Form.Item>

      <Divider orientation="left" plain>Father Details</Divider>
      <Row gutter={16}>
        <Col xs={24} md={8}>
          <Form.Item name="fatherName" label="Father Name">
            <Input />
          </Form.Item>
        </Col>
        <Col xs={24} md={8}>
          <Form.Item name="fatherOccupation" label="Occupation">
            <Input />
          </Form.Item>
        </Col>
        <Col xs={24} md={8}>
          <Form.Item name="fatherQualification" label="Qualification">
            <Input />
          </Form.Item>
        </Col>
        <Col xs={24} md={8}>
          <Form.Item name="fatherEmploymentType" label="Employment Type">
            <Input placeholder="e.g. Government / Private" />
          </Form.Item>
        </Col>
        <Col xs={24} md={8}>
          <Form.Item name="fatherMobile" label="Mobile">
            <Input />
          </Form.Item>
        </Col>
      </Row>

      <Divider orientation="left" plain>Mother Details</Divider>
      <Row gutter={16}>
        <Col xs={24} md={8}>
          <Form.Item name="motherName" label="Mother Name">
            <Input />
          </Form.Item>
        </Col>
        <Col xs={24} md={8}>
          <Form.Item name="motherOccupation" label="Occupation">
            <Input />
          </Form.Item>
        </Col>
        <Col xs={24} md={8}>
          <Form.Item name="motherQualification" label="Qualification">
            <Input />
          </Form.Item>
        </Col>
        <Col xs={24} md={8}>
          <Form.Item name="motherMobile" label="Mobile">
            <Input />
          </Form.Item>
        </Col>
      </Row>

      <Divider orientation="left" plain>Income</Divider>
      <Row gutter={16}>
        <Col xs={24} md={8}>
          <Form.Item name="annualIncome" label="Annual Income (₹)">
            <InputNumber style={{ width: '100%' }} min={0} />
          </Form.Item>
        </Col>
        <Col xs={24} md={8}>
          <Form.Item name="familyChildrenCount" label="No. of Children">
            <InputNumber style={{ width: '100%' }} min={0} />
          </Form.Item>
        </Col>
      </Row>
    </>
  )

  const tabAddress = (
    <Row gutter={16}>
      <Col xs={24} md={8}>
        <Form.Item name="doorNo" label="Door No / Plot No">
          <Input />
        </Form.Item>
      </Col>
      <Col xs={24} md={8}>
        <Form.Item name="addressArea" label="Area / Locality">
          <Input />
        </Form.Item>
      </Col>
      <Col xs={24} md={8}>
        <Form.Item name="addressCity" label="City">
          <Input />
        </Form.Item>
      </Col>
      <Col xs={24} md={8}>
        <Form.Item name="addressState" label="State">
          <Input />
        </Form.Item>
      </Col>
      <Col xs={24} md={8}>
        <Form.Item name="email" label="Email">
          <Input type="email" />
        </Form.Item>
      </Col>
      <Col xs={24} md={16}>
        <Form.Item name="permanentAddress" label="Permanent Address">
          <Input.TextArea rows={2} />
        </Form.Item>
      </Col>
    </Row>
  )

  const tabIdentity = (
    <Row gutter={16}>
      <Col xs={24} md={8}>
        <Form.Item name="caste" label="Caste">
          <Input />
        </Form.Item>
      </Col>
      <Col xs={24} md={8}>
        <Form.Item name="casteCode" label="Caste Code">
          <Input />
        </Form.Item>
      </Col>
      <Col xs={24} md={8}>
        <Form.Item name="religion" label="Religion">
          <Input />
        </Form.Item>
      </Col>
      <Col xs={24} md={8}>
        <Form.Item name="nationality" label="Nationality">
          <Input />
        </Form.Item>
      </Col>
      <Col xs={24} md={8}>
        <Form.Item name="aadharNo" label="Aadhar No">
          <Input />
        </Form.Item>
      </Col>
      <Col xs={24} md={8}>
        <Form.Item name="udiseNo" label="UDISE No">
          <Input />
        </Form.Item>
      </Col>
      <Col xs={24} md={8}>
        <Form.Item name="dobProofSubmitted" label="DOB Proof">
          <Select options={CERT_OPTIONS} allowClear />
        </Form.Item>
      </Col>
      <Col xs={24} md={8}>
        <Form.Item name="casteCertSubmitted" label="Caste Certificate">
          <Select options={CERT_OPTIONS} allowClear />
        </Form.Item>
      </Col>
      <Col xs={24} md={8}>
        <Form.Item name="otherCertificates" label="Other Certificates">
          <Input />
        </Form.Item>
      </Col>
      <Col xs={24} md={8}>
        <Form.Item name="disabilityStatus" label="Disability">
          <Select options={[{ value: 'Y', label: 'Yes' }, { value: 'N', label: 'No' }]} allowClear />
        </Form.Item>
      </Col>
      <Col xs={24} md={8}>
        <Form.Item name="disabilityType" label="Disability Type">
          <Input />
        </Form.Item>
      </Col>
      <Col xs={24} md={8}>
        <Form.Item name="biometricId" label="Biometric ID">
          <Input />
        </Form.Item>
      </Col>
    </Row>
  )

  const tabTransport = (
    <Row gutter={16}>
      <Col xs={24} md={8}>
        <Form.Item name="transportType" label="Transport Type">
          <Select options={TRANSPORT_OPTIONS} allowClear />
        </Form.Item>
      </Col>
      <Col xs={24} md={8}>
        <Form.Item name="busRouteId" label="Bus Route">
          <Select options={routeOptions} allowClear placeholder="Select route" />
        </Form.Item>
      </Col>
      <Col xs={24} md={8}>
        <Form.Item name="busId" label="Bus">
          <Select options={busOptions} allowClear placeholder="Select bus" />
        </Form.Item>
      </Col>
      <Col xs={24} md={8}>
        <Form.Item name="busTrip" label="Bus Trip">
          <Select options={BUS_TRIP_OPTIONS} allowClear />
        </Form.Item>
      </Col>
    </Row>
  )

  const tabOther = (
    <Row gutter={16}>
      <Col xs={24} md={8}>
        <Form.Item name="studentType" label="Student Type">
          <Select options={STUDENT_TYPE_OPTIONS} allowClear />
        </Form.Item>
      </Col>
      <Col xs={24} md={8}>
        <Form.Item name="hostelId" label="Hostel">
          <Select
            allowClear
            placeholder="Select hostel"
            options={hostels.map((h) => ({ value: h.hostelId, label: h.hostelName }))}
          />
        </Form.Item>
      </Col>
      <Col xs={24} md={8}>
        <Form.Item name="scholarshipStatus" label="Scholarship">
          <Select options={SCHOLARSHIP_OPTIONS} allowClear />
        </Form.Item>
      </Col>
      <Col xs={24} md={8}>
        <Form.Item name="scholarshipDescription" label="Scholarship Details">
          <Input />
        </Form.Item>
      </Col>
      <Col xs={24} md={8}>
        <Form.Item name="motherTongue" label="Mother Tongue">
          <Input />
        </Form.Item>
      </Col>
      <Col xs={24} md={8}>
        <Form.Item name="firstLanguage" label="1st Language">
          <Input />
        </Form.Item>
      </Col>
      <Col xs={24} md={8}>
        <Form.Item name="secondLanguage" label="2nd Language">
          <Input />
        </Form.Item>
      </Col>
      <Col xs={24} md={8}>
        <Form.Item name="thirdLanguage" label="3rd Language">
          <Input />
        </Form.Item>
      </Col>
      <Col xs={24} md={8}>
        <Form.Item name="referenceName" label="Reference">
          <Input />
        </Form.Item>
      </Col>
      <Col xs={24} md={16}>
        <Form.Item name="remarks" label="Remarks">
          <Input.TextArea rows={2} />
        </Form.Item>
      </Col>
      <Col xs={24} md={8}>
        <Form.Item name="spareField1" label="Spare Field 1">
          <Input />
        </Form.Item>
      </Col>
      <Col xs={24} md={8}>
        <Form.Item name="spareField2" label="Spare Field 2">
          <Input />
        </Form.Item>
      </Col>
    </Row>
  )

  const tabItems = [
    { key: '1', label: 'Basic Info',          children: tabBasic,     forceRender: true },
    { key: '2', label: 'Academic Details',    children: tabAcademic,  forceRender: true },
    { key: '3', label: 'Family Details',      children: tabFamily,    forceRender: true },
    { key: '4', label: 'Address & Contact',   children: tabAddress,   forceRender: true },
    { key: '5', label: 'Identity & Docs',     children: tabIdentity,  forceRender: true },
    { key: '6', label: 'Transport',           children: tabTransport, forceRender: true },
    { key: '7', label: 'Other Info',          children: tabOther,     forceRender: true },
  ]

  const openHistory = async () => {
    setHistoryOpen(true)
    setHistoryLoading(true)
    try {
      const res = await api.get(`/school/students/${id}/history`)
      setHistoryRows(res.data?.data || [])
    } catch (e) {
      message.error(apiError(e, 'Failed to load history.'))
    } finally {
      setHistoryLoading(false)
    }
  }

  const historyColumns = [
    { title: 'Changed On', dataIndex: 'validFrom', width: 150,
      render: v => dayjs(v).format('DD MMM YYYY, hh:mm A') },
    { title: 'By', width: 120, render: (_, r) => r.updatedBy || r.createdBy || '—' },
    { title: 'Name', dataIndex: 'studentName' },
    { title: 'Adm No', dataIndex: 'admissionNo', width: 100 },
    { title: 'Class / Sec', width: 130, render: (_, r) => r.className ? `${r.className}${r.sectionName ? ' - ' + r.sectionName : ''}` : '—' },
    { title: 'Status', dataIndex: 'status', width: 90, render: v => v ? <Tag>{v}</Tag> : '—' },
    { title: 'Father Mobile', dataIndex: 'fatherMobile', width: 120, render: v => v || '—' },
    { title: 'Email', dataIndex: 'email', render: v => v || '—' },
  ]

  return (
    <div>
      {/* Page header */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 16 }}>
        <Button icon={<ArrowLeftOutlined />} onClick={() => navigate('/students')}>
          Back
        </Button>
        <h2 style={{ margin: 0 }}>
          {isEdit
            ? `Edit Student${student ? ` — ${student.studentName}` : ''}`
            : 'Register New Student'}
        </h2>
      </div>

      <Card loading={loading}>
        <Form form={form} layout="vertical">
          <Tabs items={tabItems} />
        </Form>

        <div style={{ marginTop: 24, display: 'flex', justifyContent: 'flex-end', gap: 8 }}>
          <Button onClick={() => navigate('/students')}>Cancel</Button>
          <Button
            type="primary"
            icon={<SaveOutlined />}
            loading={saving}
            onClick={handleSave}
          >
            {isEdit ? 'Save Changes' : 'Register Student'}
          </Button>
        </div>

        {isEdit && student && (
          <div style={{ marginTop: 16, paddingTop: 12, borderTop: '1px solid #f0f0f0',
                        display: 'flex', justifyContent: 'space-between', alignItems: 'center',
                        fontSize: 12, color: '#8c8c8c' }}>
            <Button size="small" icon={<HistoryOutlined />} onClick={openHistory}>
              View change history
            </Button>
            <div style={{ textAlign: 'right' }}>
              {student.createdBy && (
                <span>Created by <b>{student.createdBy}</b>
                  {student.createdAt ? ` on ${dayjs(student.createdAt).format('DD MMM YYYY, hh:mm A')}` : ''}</span>
              )}
              {student.updatedBy && (
                <span style={{ marginLeft: 16 }}>Last updated by <b>{student.updatedBy}</b>
                  {student.updatedAt ? ` on ${dayjs(student.updatedAt).format('DD MMM YYYY, hh:mm A')}` : ''}</span>
              )}
            </div>
          </div>
        )}
      </Card>

      <Drawer
        title={`Change History${student ? ` — ${student.studentName}` : ''}`}
        open={historyOpen}
        onClose={() => setHistoryOpen(false)}
        width={900}
      >
        <div style={{ marginBottom: 8, fontSize: 12, color: '#8c8c8c' }}>
          Each row is a saved version (newest first). Times are UTC.
        </div>
        <Table
          rowKey={(_, i) => i}
          size="small"
          loading={historyLoading}
          dataSource={historyRows}
          columns={historyColumns}
          pagination={false}
          scroll={{ x: 'max-content' }}
          locale={{ emptyText: 'No history yet (or the temporal migration has not been applied).' }}
        />
      </Drawer>
    </div>
  )
}
