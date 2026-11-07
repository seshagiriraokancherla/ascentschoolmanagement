import { Card, Typography } from 'antd'
import {
  OrderedListOutlined, BookOutlined, TagsOutlined, GoldOutlined, ProfileOutlined,
  IdcardOutlined, SolutionOutlined, TrophyOutlined, CloseCircleOutlined,
} from '@ant-design/icons'
import ExamTypesTab              from '../master/ExamTypesTab'
import ClassSubjectsTab          from '../master/ClassSubjectsTab'
import GradeTypesTab             from '../master/GradeTypesTab'
import MarksGradeMasterTab       from '../master/MarksGradeMasterTab'
import ExamMasterTab             from '../master/ExamMasterTab'
import ExamHallTicketReport      from '../reports/ExamHallTicketReport'
import MarksCardReport           from '../reports/MarksCardReport'
import ExamToppersReport         from '../reports/ExamToppersReport'
import ClassToppersReport        from '../reports/ClassToppersReport'
import AcademicYearToppersReport from '../reports/AcademicYearToppersReport'
import FailedStudentsReport      from '../reports/FailedStudentsReport'

const { Text } = Typography

// The Exams menu reuses the existing Master Data tabs and Reports screens unchanged —
// each page here only gives one of them its own route and a titled card.
function ExamPage({ icon, title, description, children }) {
  return (
    <Card
      title={<span>{icon}<span style={{ marginLeft: 8 }}>{title}</span></span>}
      extra={description && <Text type="secondary" style={{ fontSize: 12 }}>{description}</Text>}
    >
      {children}
    </Card>
  )
}

export const ExamTypesPage = () => (
  <ExamPage icon={<OrderedListOutlined />} title="Exam Types"
    description="The exams held in each academic year (FA 1, SA 1, …)">
    <ExamTypesTab />
  </ExamPage>
)

export const ClassSubjectsPage = () => (
  <ExamPage icon={<BookOutlined />} title="Class Subjects"
    description="Which subjects each class studies — drives marks entry and the marks card">
    <ClassSubjectsTab />
  </ExamPage>
)

export const SubjectGradesPage = () => (
  <ExamPage icon={<TagsOutlined />} title="Subjects Grade Master"
    description="Grade bands for each subject's marks">
    <GradeTypesTab />
  </ExamPage>
)

export const TotalGradesPage = () => (
  <ExamPage icon={<GoldOutlined />} title="Total Grade Master"
    description="Grading scales for a student's total marks">
    <MarksGradeMasterTab />
  </ExamPage>
)

export const ExamMasterPage = () => (
  <ExamPage icon={<ProfileOutlined />} title="Exam Master"
    description="Per class and subject: date, time, max marks and grading">
    <ExamMasterTab />
  </ExamPage>
)

export const HallTicketPage = () => (
  <ExamPage icon={<IdcardOutlined />} title="Exam Hall Ticket"
    description="Per-student hall tickets with the exam schedule — PDF only">
    <ExamHallTicketReport />
  </ExamPage>
)

export const MarksCardPage = () => (
  <ExamPage icon={<SolutionOutlined />} title="Marks Card"
    description="Per-student progress report — marks, grades and rank in the section">
    <MarksCardReport />
  </ExamPage>
)

export const ExamToppersPage = () => (
  <ExamPage icon={<TrophyOutlined />} title="Exam wise Toppers"
    description="Class-wise ranked toppers for an examination with subject-wise marks">
    <ExamToppersReport />
  </ExamPage>
)

export const ClassToppersPage = () => (
  <ExamPage icon={<TrophyOutlined />} title="Class wise Toppers"
    description="Exam-wise toppers for a selected class — all exams of the year in one view">
    <ClassToppersReport />
  </ExamPage>
)

export const AcademicYearToppersPage = () => (
  <ExamPage icon={<TrophyOutlined />} title="Academic Year Toppers"
    description="Cumulative toppers ranked by grand total across all exams in the year">
    <AcademicYearToppersReport />
  </ExamPage>
)

export const FailedStudentsPage = () => (
  <ExamPage icon={<CloseCircleOutlined />} title="Failed Students List"
    description="Students below the pass mark in one or more subjects for an examination">
    <FailedStudentsReport />
  </ExamPage>
)
