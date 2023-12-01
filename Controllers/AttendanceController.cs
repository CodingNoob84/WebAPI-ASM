using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Data;
using WebAPI_ASM.Model;
using static WebAPI_ASM.Model.User;

namespace WebAPI_ASM.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AttendanceController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        public AttendanceController(IConfiguration configuration, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _env = env;
        }

        [HttpGet("getAttendanceCount")]
        public JsonResult GetCount(int StaffId,int Role)
        {
            string query = "";
            if (Role == 4)
            {
                query = @"
SELECT 
    IFNULL(SUM(CASE WHEN reason = 'Leave' THEN 1 ELSE 0 END), 0) AS LeaveCount,
    IFNULL(SUM(CASE WHEN reason = 'Permission' THEN 1 ELSE 0 END), 0) AS PermissionCount,
    IFNULL(SUM(CASE WHEN reason = 'On Duty' THEN 1 ELSE 0 END), 0) AS OnDutyCount
FROM `permission_or_on_duty`
WHERE staff_id = @StaffId;";

            }else if (Role == 3)
            {
                query = @"
SELECT 
    IFNULL(SUM(CASE WHEN reason = 'Leave' THEN 1 ELSE 0 END), 0) AS LeaveCount,
    IFNULL(SUM(CASE WHEN reason = 'Permission' THEN 1 ELSE 0 END), 0) AS PermissionCount,
    IFNULL(SUM(CASE WHEN reason = 'On Duty' THEN 1 ELSE 0 END), 0) AS OnDutyCount
FROM `permission_or_on_duty`
WHERE reporting = @StaffId;";
            }else if(Role == 1)
            {
                query = @"
SELECT 
    IFNULL(SUM(CASE WHEN reason = 'Leave' THEN 1 ELSE 0 END), 0) AS LeaveCount,
    IFNULL(SUM(CASE WHEN reason = 'Permission' THEN 1 ELSE 0 END), 0) AS PermissionCount,
    IFNULL(SUM(CASE WHEN reason = 'On Duty' THEN 1 ELSE 0 END), 0) AS OnDutyCount
FROM `permission_or_on_duty`;";

            }

            string sqlDataSource = _configuration.GetConnectionString("ASMotorCon");

            using (MySqlConnection mycon = new MySqlConnection(sqlDataSource))
            {
                mycon.Open();

                using (MySqlCommand myCommand = new MySqlCommand(query, mycon))
                {
                    myCommand.Parameters.AddWithValue("@StaffId", StaffId); // Add parameter for StaffId
                    using (MySqlDataReader myReader = myCommand.ExecuteReader())
                    {
                        if (myReader.Read())
                        {
                            var result = new
                            {
                                LeaveCount = Convert.ToInt32(myReader["LeaveCount"]),
                                PermissionCount = Convert.ToInt32(myReader["PermissionCount"]),
                                OnDutyCount = Convert.ToInt32(myReader["OnDutyCount"])
                            };
                            return new JsonResult(result);
                        }
                    }
                }

                mycon.Close();
            }

            // If no rows were found, return an empty JSON object
            return new JsonResult(new { });
        }

        // GET: api/<LoginController>
        [HttpGet("getAllDetails")]
        public JsonResult GetDetails(int StaffId, int Role)
        {
            string query = "";
            if(Role == 4)
            {
                query = @"
        SELECT *
        FROM `permission_or_on_duty`
        WHERE staff_id = @StaffId
        ORDER BY updated_date;";

            }else if(Role == 3)
            {
                query = @"
        SELECT *
        FROM `permission_or_on_duty`
        WHERE reporting = @StaffId
        ORDER BY updated_date;";

            }else if (Role == 1)
            {
                query = @"
        SELECT *
        FROM `permission_or_on_duty`
        ORDER BY updated_date;";

            }
            

            DataTable table = new DataTable();
            string sqlDataSource = _configuration.GetConnectionString("ASMotorCon");

            using (MySqlConnection mycon = new MySqlConnection(sqlDataSource))
            {
                mycon.Open();

                using (MySqlCommand myCommand = new MySqlCommand(query, mycon))
                {
                    myCommand.Parameters.AddWithValue("@StaffId", StaffId);
                    using (MySqlDataReader myReader = myCommand.ExecuteReader())
                    {
                        table.Load(myReader);
                    }
                }

                mycon.Close();
            }

            return new JsonResult(table);
        }

        [HttpPost("updatestatus")]
        public IActionResult LeaveStatusUpdate([FromBody] UpdateLeaveStatus leavestatus)
        {
            try
            {
                string query = @"
            UPDATE `permission_or_on_duty`
            SET leave_status = @LeaveStatus, reject_reason = @RejectReason
            WHERE permission_on_duty_id = @Id;";

                string sqlDataSource = _configuration.GetConnectionString("ASMotorCon");

                using (MySqlConnection mycon = new MySqlConnection(sqlDataSource))
                {
                    mycon.Open();

                    using (MySqlCommand myCommand = new MySqlCommand(query, mycon))
                    {
                        myCommand.Parameters.AddWithValue("@LeaveStatus", leavestatus.LeaveStatus);
                        myCommand.Parameters.AddWithValue("@RejectReason", leavestatus.RejectReason);
                        myCommand.Parameters.AddWithValue("@Id", leavestatus.Id);

                        int rowsUpdated = myCommand.ExecuteNonQuery();

                        mycon.Close();

                        if (rowsUpdated > 0)
                        {
                            return Ok("Leave status and reject reason updated successfully.");
                        }
                        else
                        {
                            return NotFound("No record found for the specified ID.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while updating leave status and reject reason: " + ex.Message);
            }
        }

        [HttpPost("applyleave")]
        public IActionResult InsertApplyLeave(ApplyLeave leave)
        {
            string sqlDataSource = _configuration.GetConnectionString("ASMotorCon");

            try
            {
                using (MySqlConnection mycon = new MySqlConnection(sqlDataSource))
                {
                    mycon.Open();

                    // Get the regularisation_id from the last row as a string
                    string regularisationIdQuery = @"SELECT regularisation_id 
                                             FROM permission_or_on_duty 
                                             ORDER BY permission_on_duty_id DESC 
                                             LIMIT 1;";

                    using (MySqlCommand regularisationIdCommand = new MySqlCommand(regularisationIdQuery, mycon))
                    {
                        string regularisationId = regularisationIdCommand.ExecuteScalar()?.ToString();

                        string numericPart = regularisationId.Substring(2);

                        if (int.TryParse(numericPart, out int numericValue))
                        {
                            // Increment numeric value
                            numericValue++;

                            // Format as "R-107" and return
                            regularisationId= "R-" + numericValue.ToString();
                        }

                      

                        //Insert the data along with the obtained regularisation_id
                            string insertQuery = @"INSERT INTO `permission_or_on_duty` (regularisation_id, company_id, department_id, staff_id, staff_code, reporting, reason, permission_from_time, permission_to_time, 
                        permission_date, on_duty_place, leave_date, leave_reason, insert_login_id) 
                        VALUES (@RegularisationId, @CompanyId, @DepartmentId, @StaffId, @StaffCode, @Reporting, @Reason, @PermissionFromTime, @PermissionToTime, @PermissionDate, @OnDutyPlace, @LeaveDate, @LeaveReason, @InsertLoginId)";

                        using (MySqlCommand myCommand = new MySqlCommand(insertQuery, mycon))
                        {
                            myCommand.Parameters.AddWithValue("@RegularisationId", regularisationId);
                            myCommand.Parameters.AddWithValue("@CompanyId", leave.CompanyId);
                            myCommand.Parameters.AddWithValue("@DepartmentId", leave.DepartmentId);
                            myCommand.Parameters.AddWithValue("@StaffId", leave.StaffId);
                            myCommand.Parameters.AddWithValue("@StaffCode", leave.StaffCode);
                            myCommand.Parameters.AddWithValue("@Reporting", leave.Reporting);
                            myCommand.Parameters.AddWithValue("@Reason", leave.Reason);
                            myCommand.Parameters.AddWithValue("@PermissionFromTime", leave.PermissionFromTime);
                            myCommand.Parameters.AddWithValue("@PermissionToTime", leave.PermissionToTime);
                            myCommand.Parameters.AddWithValue("@PermissionDate", leave.PermissionDate);
                            myCommand.Parameters.AddWithValue("@OnDutyPlace", leave.OnDutyPlace);
                            myCommand.Parameters.AddWithValue("@LeaveDate", leave.LeaveDate);
                            myCommand.Parameters.AddWithValue("@LeaveReason", leave.LeaveReason);
                            myCommand.Parameters.AddWithValue("@InsertLoginId", leave.InsertLoginId);

                            myCommand.ExecuteNonQuery();
                        }
                    }

                    mycon.Close();
                }
                
                // Insertion was successful, return a success message
                return Ok("Inserted successfully.");
            }
            catch (Exception ex)
            {
                // An error occurred during the insertion, return an error message
                return StatusCode(500, "An error occurred during insertion: " + ex.Message);
            }
        }


    }


}