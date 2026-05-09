# Standardized Schema Proposal
> Converted from VB6 SAS_* tables → clean web/mobile app schema
> All IDs changed to INT (auto-increment) primary keys. AcademicYear string → FK to academic_years table.
> snake_case naming throughout.

---

## Table Name Mapping (Old → New)

| Old Table | New Table | Notes |
|---|---|---|
| *(missing)* | `school_groups` | New — needed for SchGrpID |
| `SAS_LicenceDet` | `schools` | |
| `SAS_TransLogData` | `audit_logs` | |
| `SAS_UserMaster` | `users` | |
| `SAS_AcdYear` | `academic_years` | Added surrogate PK |
| `SAS_SoftwareSettings` | `school_settings` | SchID becomes PK |
| `SAS_ClassGrps` | `class_groups` | |
| `SAS_ClassNames` | `classes` | ClsGrpID FK confirmed |
| `SAS_FeeCategory` | `fee_categories` | |
| `SAS_FeeTypes` | `fee_types` | |
| `SAS_TermMonthData` | `terms` | New table (provided in answers) |
| `SAS_Subjects` | `subjects` | |
| `SAS_PaymentMods` | `payment_modes` | New table (provided in answers) |
| `SAS_FeeMaster` | `fee_structures` | ClassNam→class_id FK confirmed |
| `SAS_BussData` | `buses` | |
| `SAS_BusRoutes` | `bus_routes` | |
| `SAS_BusMaster` | `bus_fee_structures` | Confirmed: bus fee per route/term |
| `SAS_StudentMaster` | `students` | StuID float→BIGINT |

---

## 1. school_groups *(NEW)*
> Needed to support SchGrpID — chain of schools under one group

| New Column | Type | Notes |
|---|---|---|
| **group_id** | INT PK AUTO_INCREMENT | |
| group_name | VARCHAR(100) | |
| description | VARCHAR(200) | |
| status | VARCHAR(10) | Active/Inactive |
| created_by | VARCHAR(25) | |
| created_at | DATETIME | |

---

## 2. schools *(SAS_LicenceDet)*

| New Column | Old Column | Type | Notes |
|---|---|---|---|
| **school_id** | SchID | INT PK AUTO_INCREMENT | Was VARCHAR |
| group_id | SchGrpID | INT | **FK → school_groups.group_id** |
| school_name | SchName | VARCHAR(150) | |
| school_caption | SchCapsn | VARCHAR(250) | |
| address | SchAddr | VARCHAR(205) | |
| city | SchCity | VARCHAR(55) | |
| district | SchDistrct | VARCHAR(55) | |
| state | SchState | VARCHAR(55) | |
| pin_code | SchPinCd | VARCHAR(15) | |
| landline | SchLandLno | VARCHAR(25) | |
| mobile | SchMbl | VARCHAR(35) | |
| day_end_notification_mobiles | DayClsMbls | VARCHAR(66) | Day-end SMS to admins |
| state_reg_no | SchStateRegNo | VARCHAR(150) | |
| central_reg_no | SchCentrlRegNo | VARCHAR(150) | |
| email | SchMailID | VARCHAR(150) | |
| website | SchWebNam | VARCHAR(100) | |
| organization_type | OrgnisanTyp | VARCHAR(20) | |
| license_purchase_date | PurDat | DATE | |
| license_renewal_date | RenwDat | DATE | |
| student_strength | StuStrngth | INT | Was FLOAT |
| staff_strength | StafStrngh | INT | Was FLOAT |
| status | SchStatus | VARCHAR(10) | |
| audit_log_id | ModLogID | INT | **FK → audit_logs.log_id** |
| created_by | CrtBy | VARCHAR(25) | |
| created_at | CrtDat | DATETIME | |
| machine_id | MachID | VARCHAR(20) | Can be dropped in web app |

---

## 3. audit_logs *(SAS_TransLogData)*

| New Column | Old Column | Type | Notes |
|---|---|---|---|
| **log_id** | TraID | INT PK AUTO_INCREMENT | Was VARCHAR |
| reference_id | RefID | VARCHAR(10) | Generic ref to any record |
| reference_type | RefTyp | VARCHAR(15) | Table/entity name |
| transaction_date | TraDat | DATE | |
| transaction_type | TraTyp | VARCHAR(15) | Insert/Update/Delete |
| transaction_time | TraTim | DATETIME | |
| user_id | UsrID | INT | **FK → users.user_id** |
| machine_id | MachID | VARCHAR(10) | |
| system_ip | SysIP | VARCHAR(15) | |
| form_name | TraFrm | VARCHAR(15) | Screen/module name |
| school_id | SchID | INT | **FK → schools.school_id** |

---

## 4. users *(SAS_UserMaster)*

| New Column | Old Column | Type | Notes |
|---|---|---|---|
| **user_id** | UserID | INT PK AUTO_INCREMENT | Was VARCHAR |
| employee_id | EMPID | INT | FK → employee table (TBD) |
| username | UserName | VARCHAR(50) | |
| password_hash | Userpwd | VARCHAR(255) | Must be hashed in web app |
| access_type | AccessTyp | VARCHAR(20) | |
| status | UsrStatus | VARCHAR(10) | |
| school_id | SchID | INT | **FK → schools.school_id** |
| audit_log_id | ModLogID | INT | **FK → audit_logs.log_id** |
| created_by | CrtBy | VARCHAR(25) | |
| created_at | CrtDat | DATETIME | |
| machine_id | MachID | VARCHAR(20) | |

---

## 5. academic_years *(SAS_AcdYear)*

| New Column | Old Column | Type | Notes |
|---|---|---|---|
| **academic_year_id** | *(new)* | INT PK AUTO_INCREMENT | Surrogate PK added |
| academic_year | AcademicYear | VARCHAR(55) | "2025-2026" or "25-29 B-Pharm" |
| start_month | StartMonth | VARCHAR(15) | |
| end_month | EndMonth | VARCHAR(15) | |
| status | AcdStatus | VARCHAR(5) | |
| registration_fee_frequency | RegFeeType | VARCHAR(10) | Yearly/Monthly/Termwise |
| transport_fee_frequency | TransFeeTyp | VARCHAR(10) | Yearly/Monthly/Termwise |
| hostel_fee_frequency | HostlFeeType | VARCHAR(10) | Yearly/Monthly/Termwise |
| boarding_type | StuBoardingTyp | VARCHAR(10) | Day School/Hostel |
| new_admissions_enabled | NewAdminsStat | VARCHAR(10) | Enable/Disable flag |
| school_id | SchID | INT | **FK → schools.school_id** |
| audit_log_id | ModLogID | INT | **FK → audit_logs.log_id** |
| created_by | CrtBy | VARCHAR(25) | |
| created_at | CrtDat | DATETIME | |
| machine_id | MachID | VARCHAR(20) | |

---

## 6. school_settings *(SAS_SoftwareSettings)*

| New Column | Old Column | Type | Notes |
|---|---|---|---|
| **school_id** | SchID | INT PK | **FK → schools.school_id** (single row per school) |
| fee_receipt_lock | FeeReceiptLock | INT | |
| fee_receipt_print | FeeReceiptPrint | VARCHAR(10) | |
| print_mode | PrintModl | VARCHAR(25) | |
| admission_no_type | AdminNoTyp | VARCHAR(15) | |
| receipt_printer | ReceiptPrntr | VARCHAR(30) | |
| reports_printer | ReportsPrntr | VARCHAR(30) | |
| category_wise_admissions | CatWisAdmns | VARCHAR(5) | Y/N flag |
| transport_fee_included | TransFeeInclud | VARCHAR(5) | Y/N flag |
| fine_enabled | FineStat | VARCHAR(5) | Y/N flag |
| receipt_fee_type_separator | RcptFeetypSprtr | VARCHAR(5) | |
| new_student_entry_mode | NSEtype | VARCHAR(10) | Fast Entry/Normal Entry |
| misc_print_mode | MiscPrntModl | VARCHAR(25) | |
| backup_drive | BackupDriv | VARCHAR(25) | May not apply to web app |
| receipt_print_copies | RecptNofPrnts | INT | |
| progress_report_type | PrgRepTyp | VARCHAR(25) | |
| billing_status | BillingStat | VARCHAR(10) | |
| other_subjects_type | OthrSubjtsTyp | VARCHAR(10) | |
| pre_primary_admission_prefix | PrPrmryAdmnNos | VARCHAR(15) | Prefix for auto-gen admission no |
| primary_admission_prefix | PrimryAdmnNos | VARCHAR(15) | |
| high_school_admission_prefix | HghSchlAdmnNos | VARCHAR(15) | |
| general_admission_prefix | GenralAdmnNos | VARCHAR(15) | |
| bill_no_series_type | BilNosStrts | VARCHAR(25) | New Year/Academic Year/Financial Year/Continues/Cash-Bank/Category-wise |
| webcam_name | WebCamNam | VARCHAR(25) | |
| institution_head_signature | InstutHedSigntr | BLOB/TEXT | Store as file path in web app |
| institution_head_name | InstutHedNam | VARCHAR(20) | |
| fee_message_to_teacher | FeeMsgToTechr | VARCHAR(5) | Y/N flag |
| student_concession_enabled | StudntConcsnMod | VARCHAR(25) | Enable/Disable discount for clerk |
| audit_log_id | ModLogID | INT | **FK → audit_logs.log_id** |
| created_by | CrtBy | VARCHAR(25) | |
| created_at | CrtDat | DATETIME | |
| machine_id | MachID | VARCHAR(20) | |

---

## 7. class_groups *(SAS_ClassGrps)*
> Examples: Pre-Primary, Primary, High-School, General

| New Column | Old Column | Type | Notes |
|---|---|---|---|
| **class_group_id** | ClsGrpID | INT PK AUTO_INCREMENT | Was VARCHAR |
| group_name | GrpNam | VARCHAR(30) | Pre-Primary/Primary/High-School/General |
| description | GrpDesc | VARCHAR(50) | |
| prefix | GrpPrfx | VARCHAR(5) | |
| status | GrpStat | VARCHAR(5) | |
| school_id | SchID | INT | **FK → schools.school_id** |
| audit_log_id | ModLogID | INT | **FK → audit_logs.log_id** |
| created_by | CrtBy | VARCHAR(25) | |
| created_at | CrtDat | DATETIME | |
| machine_id | MachID | VARCHAR(20) | |

---

## 8. classes *(SAS_ClassNames)*
> ClassID FK confirmed in clarification answers

| New Column | Old Column | Type | Notes |
|---|---|---|---|
| **class_id** | ClassID | INT PK AUTO_INCREMENT | Was NCHAR |
| class_name | ClassNam | VARCHAR(55) | e.g., "1 Class", "XI th Grade" |
| branch_name | BranchNam | VARCHAR(55) | Stream: Science/Arts/Commerce |
| description | ClassDesc | VARCHAR(50) | |
| status | ClassStat | VARCHAR(5) | |
| class_group_id | ClsGrpID | INT | **FK → class_groups.class_group_id** |
| fee_category_id | CategoryTyp | INT | **FK → fee_categories.fee_category_id** (e.g., General/Staff Child/VTPS) |
| prefix_enabled | PrefixStat | VARCHAR(5) | Y/N |
| prefix_code | PrefixCod | VARCHAR(5) | |
| sequence_no | SeqN | INT | Display order |
| school_id | SchID | INT | **FK → schools.school_id** |
| audit_log_id | ModLogID | INT | **FK → audit_logs.log_id** |
| created_by | CrtBy | VARCHAR(25) | |
| created_at | CrtDat | DATETIME | |
| machine_id | MachID | VARCHAR(20) | |

---

## 9. fee_categories *(SAS_FeeCategory)*
> e.g., General Students Fee, Staff Child Fee, VTPS Staff Fee

| New Column | Old Column | Type | Notes |
|---|---|---|---|
| **fee_category_id** | FeeCatgID | INT PK AUTO_INCREMENT | Was VARCHAR |
| category_name | FeeCategoryNam | VARCHAR(25) | |
| academic_year_id | AcademicYear | INT | **FK → academic_years.academic_year_id** |
| description | Descrpt | VARCHAR(30) | |
| status | CategoryStatus | VARCHAR(5) | |
| school_id | SchID | INT | **FK → schools.school_id** |
| created_by | CrtBy | VARCHAR(25) | |
| created_at | CrtDat | DATETIME | |
| machine_id | MachID | VARCHAR(20) | |
| deleted_by | DeleteByData | VARCHAR(60) | Consider splitting to deleted_by + deleted_at |

---

## 10. fee_types *(SAS_FeeTypes)*

| New Column | Old Column | Type | Notes |
|---|---|---|---|
| **fee_type_id** | FeeTypID | INT PK AUTO_INCREMENT | Was VARCHAR |
| fee_type_name | FeeTyp | VARCHAR(25) | |
| academic_year_id | AcademicYear | INT | **FK → academic_years.academic_year_id** |
| term_name | TermNam | VARCHAR(25) | Informational (see terms table) |
| sequence_no | SeqNo | INT | |
| description | FeeDesc | VARCHAR(50) | |
| status | FeeTypStatus | VARCHAR(3) | |
| school_id | SchID | INT | **FK → schools.school_id** |
| created_by | CrtBy | VARCHAR(25) | |
| created_at | CrtDat | DATETIME | |
| machine_id | MachID | VARCHAR(20) | |
| deleted_by | DelbyData | VARCHAR(60) | |

---

## 11. terms *(SAS_TermMonthData)* — NEW TABLE (from answers)
> Defines terms/months per academic year: "1st Term", "January-2025", etc.

| New Column | Old Column | Type | Notes |
|---|---|---|---|
| **term_id** | TrmID | INT PK AUTO_INCREMENT | Was VARCHAR |
| term_name | TrmMnthData | VARCHAR(20) | "1st Term" / "January-2025" |
| year_name | YearNam | VARCHAR(10) | |
| order_no | OrdrNo | INT | Display order |
| description | Descrpt | VARCHAR(30) | |
| academic_year_id | AcademicYear | INT | **FK → academic_years.academic_year_id** |
| status | TraStatus | VARCHAR(5) | |
| school_id | SchID | INT | **FK → schools.school_id** |
| created_by | CrtBy | VARCHAR(25) | |
| created_at | CrtDate | DATETIME | |
| machine_id | MachID | VARCHAR(20) | |
| deleted_by | DeletBy | VARCHAR(60) | |

---

## 12. subjects *(SAS_Subjects)*

| New Column | Old Column | Type | Notes |
|---|---|---|---|
| **subject_id** | SubJectID | INT PK AUTO_INCREMENT | Was NCHAR |
| subject_name | SubjectNam | VARCHAR(30) | |
| short_name | ShortNam | VARCHAR(15) | |
| subject_type | SubjectTyp | VARCHAR(20) | Theory/Language/General/Practical/Others |
| description | Descrpt | VARCHAR(50) | |
| remarks | RemarksDet | VARCHAR(50) | |
| academic_year_id | AcdYear | INT | **FK → academic_years.academic_year_id** |
| status | SubjectStatus | VARCHAR(5) | |
| school_id | SchID | INT | **FK → schools.school_id** |
| created_by | CrtBy | VARCHAR(25) | |
| created_at | CrtDat | DATETIME | |
| machine_id | MachID | VARCHAR(20) | |
| deleted_by | DelbyData | VARCHAR(60) | |

---

## 13. payment_modes *(SAS_PaymentMods)* — NEW TABLE (from answers)
> Cash / Cheque / Online — configurable per school

| New Column | Old Column | Type | Notes |
|---|---|---|---|
| **payment_mode_id** | MOPID | INT PK AUTO_INCREMENT | Was VARCHAR |
| mode_name | MOPTyp | VARCHAR(15) | Cash/Cheque/Online/etc. |
| description | TraDesc | VARCHAR(25) | |
| status | TraStatus | VARCHAR(5) | |
| school_id | SchID | INT | **FK → schools.school_id** |
| created_by | CrtBy | VARCHAR(25) | |
| created_at | CrtDate | DATETIME | |
| machine_id | MachID | VARCHAR(20) | |
| deleted_by | DeletBy | VARCHAR(60) | |

---

## 14. fee_structures *(SAS_FeeMaster)*
> Fee amount per class + fee category + fee type + term

| New Column | Old Column | Type | Notes |
|---|---|---|---|
| **fee_structure_id** | FeeMasterID | INT PK AUTO_INCREMENT | Was VARCHAR |
| fee_category_id | FeeCatgID | INT | **FK → fee_categories.fee_category_id** |
| class_id | ClassNam/ClassID | INT | **FK → classes.class_id** |
| fee_type_id | FeeTypID | INT | **FK → fee_types.fee_type_id** |
| term_id | TrmID | INT | **FK → terms.term_id** |
| amount | Amt | DECIMAL(12,2) | Was MONEY |
| description | Descrpt | VARCHAR(50) | |
| status | FeeMasterStat | VARCHAR(5) | |
| academic_year_id | AcademicYear | INT | **FK → academic_years.academic_year_id** |
| payment_mode_id | PymntTyp | INT | **FK → payment_modes.payment_mode_id** |
| admission_type | AdmsnTyp | VARCHAR(5) | New/Old Student |
| school_id | SchID | INT | **FK → schools.school_id** |
| created_by | CrtBy | VARCHAR(25) | |
| created_at | CrtDat | DATETIME | |
| machine_id | MachID | VARCHAR(20) | |
| fee_type_name | FeeTyp | VARCHAR(25) | Nullable — UI sends null |
| deleted_by | DelbyData | VARCHAR(60) | |

---

## 15. buses *(SAS_BussData)*

| New Column | Old Column | Type | Notes |
|---|---|---|---|
| **bus_id** | BusID | INT PK AUTO_INCREMENT | Was VARCHAR |
| bus_name | BusNam | VARCHAR(20) | Bus name or number |
| model | BusModelDet | VARCHAR(25) | |
| driver_id | DriverID | INT | FK → employee table (TBD) |
| capacity | BusCapacity | INT | Was VARCHAR |
| description | Descrpt | VARCHAR(50) | |
| status | BusStat | VARCHAR(5) | |
| purchase_date | PurDat | DATE | |
| registration_no | BusRegNo | VARCHAR(25) | |
| school_id | SchID | INT | **FK → schools.school_id** |
| created_by | CrtBy | VARCHAR(25) | |
| created_at | CrtDate | DATETIME | |
| machine_id | MachID | VARCHAR(20) | |
| trip_data | TripData | VARCHAR(10) | Nullable — UI sends null |
| cleaner_name | CleanerNam | VARCHAR(10) | Nullable — UI sends null |
| owner_data | OwnerData | VARCHAR(60) | Nullable — UI sends null |
| route_name | RouteNam | VARCHAR(25) | Nullable — UI sends null |
| deleted_by | DeletBy | VARCHAR(60) | |

---

## 16. bus_routes *(SAS_BusRoutes)*

| New Column | Old Column | Type | Notes |
|---|---|---|---|
| **route_id** | RouteID | INT PK AUTO_INCREMENT | Was VARCHAR |
| route_name | RouteNam | VARCHAR(55) | |
| route_code | RouteCod | VARCHAR(10) | |
| route_category | RouteCategory | VARCHAR(20) | |
| description | Descrpt | VARCHAR(50) | |
| status | RouteStatus | VARCHAR(5) | |
| school_id | SchID | INT | **FK → schools.school_id** |
| created_by | CrtBy | VARCHAR(25) | |
| created_at | CrtDate | DATETIME | |
| machine_id | MachID | VARCHAR(20) | |
| bus_no_data | BusNoData | VARCHAR(15) | Nullable — UI sends null |
| deleted_by | DeletBy | VARCHAR(60) | |

---

## 17. bus_fee_structures *(SAS_BusMaster)*
> Bus fee per route per term per academic year

| New Column | Old Column | Type | Notes |
|---|---|---|---|
| **bus_fee_structure_id** | BusMasterID | INT PK AUTO_INCREMENT | Was VARCHAR |
| route_id | RouteID | INT | **FK → bus_routes.route_id** |
| term_id | TermNam | INT | **FK → terms.term_id** |
| amount | Amt | DECIMAL(12,2) | Was MONEY |
| academic_year_id | AcademicYear | INT | **FK → academic_years.academic_year_id** |
| status | TraStatus | VARCHAR(5) | |
| school_id | SchID | INT | **FK → schools.school_id** |
| created_by | CrtBy | VARCHAR(25) | |
| created_at | CrtDate | DATETIME | |
| machine_id | MachID | VARCHAR(20) | |
| bus_name | BusNam | VARCHAR(15) | Nullable — UI sends null |
| category_name | CategoryNam | VARCHAR(20) | Nullable — UI sends null |
| deleted_by | DeletBy | VARCHAR(60) | |

---

## 18. students *(SAS_StudentMaster)*

| New Column | Old Column | Type | Notes |
|---|---|---|---|
| **student_id** | StuID | BIGINT PK AUTO_INCREMENT | Was FLOAT — critical fix |
| admission_no | StuAmnNo | VARCHAR(20) | School-issued human-readable ID |
| school_unique_id | StuUnqID | VARCHAR(20) | School-assigned student unique ID |
| student_name | StuName | VARCHAR(105) | |
| short_name | StuShortNam | VARCHAR(55) | |
| join_type | StuJoinTyp | VARCHAR(10) | New/Transfer |
| guardian_type | StuDepndntTyp | VARCHAR(10) | Parents/Guardian |
| father_name | StuFatherName | VARCHAR(50) | |
| father_qualification | StuFatherQualf | VARCHAR(50) | |
| father_occupation | StuFatherOccup | VARCHAR(50) | |
| father_employment_type | StuFatherEmpTyp | VARCHAR(50) | |
| father_mobile | StuFatherMobile | VARCHAR(20) | |
| mother_name | StuMotherName | VARCHAR(50) | |
| mother_qualification | StuMotherQualf | VARCHAR(50) | |
| mother_occupation | StuMotherOccup | VARCHAR(50) | |
| mother_mobile | StuMotherMobile | VARCHAR(20) | |
| date_of_birth | StuDOB | DATE | |
| date_of_joining | StuDOJ | DATE | |
| academic_year_id | AcademicYear | INT | **FK → academic_years.academic_year_id** |
| fee_category_id | StuFeeCategoryID | INT | **FK → fee_categories.fee_category_id** |
| gender | StuGender | VARCHAR(10) | |
| class_id | StuClassNam | INT | **FK → classes.class_id** |
| branch_name | StuClassBranch | VARCHAR(20) | Stream (Science/Arts/Commerce) |
| section | StuSect | VARCHAR(20) | |
| roll_no | StuClassRolNo | VARCHAR(10) | |
| caste | StuCaste | VARCHAR(25) | |
| caste_code | StuCasteCode | VARCHAR(10) | |
| religion | StuReligion | VARCHAR(20) | |
| nationality | StuNationlity | VARCHAR(10) | |
| door_no | StuDoorNo | VARCHAR(50) | |
| address_area | StuAddrArea | VARCHAR(50) | |
| address_city | StuAddrCity | VARCHAR(50) | |
| address_state | StuAddrState | VARCHAR(50) | |
| permanent_address | StuAddrPerminent | VARCHAR(150) | |
| email | StuMailID | VARCHAR(50) | |
| annual_income | StuAnnualIncome | DECIMAL(12,2) | Was MONEY |
| family_children_count | StuFamilyChildres | INT | No. of kids in family |
| dob_proof_submitted | StuDOBStat | VARCHAR(10) | Flag: Submitted/Not Submitted |
| aadhar_no | StuAdarNo | VARCHAR(25) | |
| caste_cert_submitted | StuCasteStat | VARCHAR(10) | Flag: Submitted/Not Submitted |
| other_certificates | StuOtherCertificates | VARCHAR(50) | |
| transport_type | StuTransportTyp | VARCHAR(10) | Bus/Walking/etc. |
| bus_route_id | StuBusRoute | INT | **FK → bus_routes.route_id** |
| joining_class | StuJoingClass | VARCHAR(20) | Class at first admission (history) |
| remarks | StuRemarks | VARCHAR(50) | |
| photo_path | StuImgPath | VARCHAR(50) | Store file path or cloud URL |
| admission_date | AdminDate | DATE | |
| disability_status | DisabilityStatus | VARCHAR(5) | Y/N |
| disability_type | DisabilityTyp | VARCHAR(25) | |
| reference_name | StuRefNam | VARCHAR(30) | |
| student_type | StudentType | VARCHAR(15) | DayScholar/Hosteler |
| scholarship_status | StuScholarshipStatus | VARCHAR(10) | |
| scholarship_description | StuScholarshipDescript | VARCHAR(30) | |
| blood_group | BloodGrp | VARCHAR(10) | |
| bus_id | BusID | INT | **FK → buses.bus_id** |
| bus_trip | BusTrip | VARCHAR(10) | 1st Trip/2nd Trip/3rd Trip (static) |
| join_term | JoinTerm | VARCHAR(20) | Term in which student joined |
| hostel_name | StuHosNam | VARCHAR(10) | NULL for now — hostel table TBD |
| biometric_id | BioMatrcID | VARCHAR(5) | Biometric device enrollment ID |
| mother_tongue | MothrLang | VARCHAR(25) | |
| first_language | StuFirstLang | VARCHAR(25) | |
| second_language | StuSecndLang | VARCHAR(10) | |
| third_language | StuThirdLang | VARCHAR(25) | |
| udise_no | StuUdiseNo | VARCHAR(35) | Govt UDISE number |
| spare_field_1 | Descrpt | VARCHAR(50) | Reserved for future use |
| spare_field_2 | Descrpt1 | VARCHAR(50) | Reserved for future use |
| status | StuStatus | VARCHAR(10) | |
| school_id | SchID | INT | **FK → schools.school_id** |
| created_by | CrtBy | VARCHAR(25) | |
| created_at | CrtDat | DATETIME | |
| machine_id | MachID | VARCHAR(25) | |
| deleted_by | DeleteByData | VARCHAR(60) | |

---

## Foreign Key Summary

| Table | Column | References |
|---|---|---|
| schools | group_id | school_groups.group_id |
| schools | audit_log_id | audit_logs.log_id |
| audit_logs | user_id | users.user_id |
| audit_logs | school_id | schools.school_id |
| users | school_id | schools.school_id |
| users | audit_log_id | audit_logs.log_id |
| academic_years | school_id | schools.school_id |
| school_settings | school_id | schools.school_id |
| class_groups | school_id | schools.school_id |
| classes | class_group_id | class_groups.class_group_id |
| classes | fee_category_id | fee_categories.fee_category_id |
| classes | school_id | schools.school_id |
| fee_categories | academic_year_id | academic_years.academic_year_id |
| fee_categories | school_id | schools.school_id |
| fee_types | academic_year_id | academic_years.academic_year_id |
| fee_types | school_id | schools.school_id |
| terms | academic_year_id | academic_years.academic_year_id |
| terms | school_id | schools.school_id |
| subjects | academic_year_id | academic_years.academic_year_id |
| subjects | school_id | schools.school_id |
| payment_modes | school_id | schools.school_id |
| fee_structures | fee_category_id | fee_categories.fee_category_id |
| fee_structures | class_id | classes.class_id |
| fee_structures | fee_type_id | fee_types.fee_type_id |
| fee_structures | term_id | terms.term_id |
| fee_structures | payment_mode_id | payment_modes.payment_mode_id |
| fee_structures | academic_year_id | academic_years.academic_year_id |
| fee_structures | school_id | schools.school_id |
| buses | school_id | schools.school_id |
| bus_routes | school_id | schools.school_id |
| bus_fee_structures | route_id | bus_routes.route_id |
| bus_fee_structures | term_id | terms.term_id |
| bus_fee_structures | academic_year_id | academic_years.academic_year_id |
| bus_fee_structures | school_id | schools.school_id |
| students | academic_year_id | academic_years.academic_year_id |
| students | fee_category_id | fee_categories.fee_category_id |
| students | class_id | classes.class_id |
| students | bus_route_id | bus_routes.route_id |
| students | bus_id | buses.bus_id |
| students | school_id | schools.school_id |

---

## Key Design Changes & Notes

1. **All VARCHAR IDs → INT AUTO_INCREMENT** for performance and proper relational integrity
2. **StuID FLOAT → BIGINT** — critical bug fix, float should never be a PK
3. **AcademicYear string removed** from all tables — replaced with `academic_year_id` FK
4. **school_groups table added** — needed for chain school support (SchGrpID was orphaned)
5. **terms table added** (SAS_TermMonthData) — properly normalizes term references
6. **payment_modes table added** (SAS_PaymentMods) — normalizes payment mode references
7. **Nullable columns retained** — `fee_type_name` in fee_structures, `trip_data`/`cleaner_name`/`owner_data`/`route_name` in buses, `bus_no_data` in bus_routes, `bus_name`/`category_name` in bus_fee_structures — all kept, UI will send null
8. **MONEY type → DECIMAL(12,2)** — MONEY is SQL Server specific; DECIMAL is portable
9. **image type for signature → store file path** — binary blobs in DB are not suitable for web/mobile
10. **DeleteByData / DeletBy** — these were combined strings; recommend splitting to `deleted_by` (user) + `deleted_at` (timestamp) columns in final schema
11. **machine_id** — desktop-era concept; can be retained for migration but may not be populated in web app
12. **hostel_name in students** — NULL for now; hostel table to be designed separately
13. **employee/driver table** — driver_id in buses and employee_id in users reference a missing employee table; to be designed separately
