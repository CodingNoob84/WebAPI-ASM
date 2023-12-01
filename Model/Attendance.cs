namespace WebAPI_ASM.Model
{
    public class ApplyLeave
    {
        public string CompanyId { get; set; }
        public string DepartmentId { get; set; }
        public string StaffId { get; set; }
        public string StaffCode { get; set; }
        public string Reporting { get; set; }
        public string Reason { get; set; }
        public string PermissionFromTime { get; set; }
        public string PermissionToTime { get; set; }
        public string PermissionDate { get; set; }
        public string OnDutyPlace { get; set; }
        public string LeaveDate { get; set; }
        public string LeaveReason { get; set; }
        public string InsertLoginId { get; set; }
    }

    public class UpdateLeaveStatus
    {
        public int Id { get; set; } 
        public string LeaveStatus { get; set; }
        public string RejectReason { get; set; }    
    }
}
