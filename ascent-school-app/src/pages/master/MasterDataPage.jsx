import { useEffect, useState } from 'react'
import { Card, Tabs } from 'antd'
import api from '../../api/axiosInstance'
import AcademicYearsTab  from './AcademicYearsTab'
import ClassGroupsTab    from './ClassGroupsTab'
import FeeCategoriesTab  from './FeeCategoriesTab'
import ClassesTab        from './ClassesTab'
import SectionsTab       from './SectionsTab'
import FeeTypesTab       from './FeeTypesTab'
import TermsTab          from './TermsTab'
import SubjectsTab       from './SubjectsTab'
import PaymentModesTab   from './PaymentModesTab'

export default function MasterDataPage() {
  // Shared lookup data passed as props to dependent tabs
  const [academicYears,  setAcademicYears]  = useState([])
  const [classGroups,    setClassGroups]    = useState([])
  const [feeCategories,  setFeeCategories]  = useState([])
  const [classes,        setClasses]        = useState([])

  // Load shared lookups once on mount
  useEffect(() => {
    api.get('/school/master/academic-years')
       .then((r) => setAcademicYears(r.data?.data || []))
       .catch(() => {})

    api.get('/school/master/class-groups')
       .then((r) => setClassGroups(r.data?.data || []))
       .catch(() => {})

    api.get('/school/master/fee-categories')
       .then((r) => setFeeCategories(r.data?.data || []))
       .catch(() => {})

    api.get('/school/master/classes')
       .then((r) => setClasses(r.data?.data || []))
       .catch(() => {})
  }, [])

  const items = [
    {
      key:      'academic-years',
      label:    'Academic Years',
      children: <AcademicYearsTab />,
    },
    {
      key:      'class-groups',
      label:    'Class Groups',
      children: <ClassGroupsTab />,
    },
    {
      key:      'fee-categories',
      label:    'Fee Categories',
      children: <FeeCategoriesTab academicYears={academicYears} />,
    },
    {
      key:      'classes',
      label:    'Classes',
      children: <ClassesTab classGroups={classGroups} feeCategories={feeCategories} />,
    },
    {
      key:      'sections',
      label:    'Sections',
      children: <SectionsTab classes={classes} />,
    },
    {
      key:      'fee-types',
      label:    'Fee Types',
      children: <FeeTypesTab academicYears={academicYears} />,
    },
    {
      key:      'terms',
      label:    'Terms',
      children: <TermsTab academicYears={academicYears} />,
    },
    {
      key:      'subjects',
      label:    'Subjects',
      children: <SubjectsTab academicYears={academicYears} />,
    },
    {
      key:      'payment-modes',
      label:    'Payment Modes',
      children: <PaymentModesTab />,
    },
  ]

  return (
    <Card title="Master Data" styles={{ body: { padding: '0 16px 16px' } }}>
      <Tabs defaultActiveKey="academic-years" items={items} />
    </Card>
  )
}
