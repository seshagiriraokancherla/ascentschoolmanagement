import { Card, Tabs } from 'antd'
import AcademicYearsTab  from './AcademicYearsTab'
import ClassGroupsTab    from './ClassGroupsTab'
import FeeCategoriesTab  from './FeeCategoriesTab'
import ClassesTab        from './ClassesTab'
import SectionsTab       from './SectionsTab'
import FeeTypesTab       from './FeeTypesTab'
import TermsTab          from './TermsTab'
import SubjectsTab       from './SubjectsTab'
import PaymentModesTab   from './PaymentModesTab'
import FeePeriodsTab     from './FeePeriodsTab'
import ExamTypesTab      from './ExamTypesTab'
import ClassTeachersTab  from './ClassTeachersTab'

const items = [
  { key: 'academic-years',  label: 'Academic Years',  children: <AcademicYearsTab /> },
  { key: 'class-groups',    label: 'Class Groups',    children: <ClassGroupsTab /> },
  { key: 'fee-categories',  label: 'Fee Categories',  children: <FeeCategoriesTab /> },
  { key: 'classes',         label: 'Classes',         children: <ClassesTab /> },
  { key: 'sections',        label: 'Sections',        children: <SectionsTab /> },
  { key: 'class-teachers',  label: 'Class Teachers',  children: <ClassTeachersTab /> },
  { key: 'fee-types',       label: 'Fee Types',       children: <FeeTypesTab /> },
  { key: 'terms',           label: 'Terms',           children: <TermsTab /> },
  { key: 'fee-periods',     label: 'Fee Periods',     children: <FeePeriodsTab /> },
  { key: 'subjects',        label: 'Subjects',        children: <SubjectsTab /> },
  { key: 'exam-types',      label: 'Exam Types',      children: <ExamTypesTab /> },
  { key: 'payment-modes',   label: 'Payment Modes',   children: <PaymentModesTab /> },
]

export default function MasterDataPage() {
  return (
    <Card title="Master Data" styles={{ body: { padding: '0 16px 16px' } }}>
      <Tabs defaultActiveKey="academic-years" items={items} />
    </Card>
  )
}
