using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Relational;
using System.Data;
using WebAPI_ASM.Model;
using static WebAPI_ASM.Model.User;

namespace WebAPI_ASM.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CalendarController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        public CalendarController(IConfiguration configuration, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _env = env;
        }

        [HttpPost("getcalendardetails")]
        public JsonResult GetCalendarDetails([FromBody] GetCalendar calendar)
        {
            if (string.IsNullOrEmpty(calendar.CurrentDate))
            {
                DateTime currentDate = DateTime.Now;
                calendar.CurrentDate = currentDate.ToString("yyyy-MM-dd");
            }

            string Todoquery = "";
            if (calendar.Role == 4)
            {
                Todoquery = @"SELECT * FROM todo_creation WHERE status = 0 AND ((work_status = 3 AND MONTH(@CurrentDate) BETWEEN month(from_date) AND month(to_date)) OR work_status IN (0, 1, 2)) AND FIND_IN_SET(@StaffId, assign_to) > 0 order by priority desc ";
            }
            else if (calendar.Role == 3)
            {
                Todoquery = @"SELECT * FROM todo_creation WHERE status = 0 AND ((work_status = 3 AND MONTH(@CurrentDate) BETWEEN month(from_date) AND month(to_date)) OR work_status IN (0, 1, 2)) AND FIND_IN_SET(@StaffId, created_id) > 0 order by priority desc ";
            }
            else if (calendar.Role == 1)
            {
                Todoquery = @"SELECT * FROM todo_creation WHERE status = 0 AND ((work_status = 3 AND MONTH(@CurrentDate) BETWEEN month(from_date) AND month(to_date)) OR work_status IN (0, 1, 2))  order by priority desc ";
            }

            string AssignWorkquery = @"SELECT * FROM assign_work_ref WHERE status = 0 AND ((work_status = 3 AND MONTH(@CurrentDate) BETWEEN MONTH(from_date) AND MONTH(to_date)) OR work_status IN (0, 1, 2)) AND designation_id = @DesignationId order by priority desc;";

            DataTable todoTable = new DataTable();
            DataTable assignWorkTable = new DataTable();
            string sqlDataSource = _configuration.GetConnectionString("ASMotorCon");

            using (MySqlConnection mycon = new MySqlConnection(sqlDataSource))
            {
                mycon.Open();

                // Run Todo Query
                using (MySqlCommand todoCommand = new MySqlCommand(Todoquery, mycon))
                {
                    todoCommand.Parameters.AddWithValue("@StaffId", calendar.StaffId);
                    todoCommand.Parameters.AddWithValue("@CurrentDate", calendar.CurrentDate);
                    using (MySqlDataReader todoReader = todoCommand.ExecuteReader())
                    {
                        todoTable.Load(todoReader);
                    }
                }

                // Run Assign Work Query
                using (MySqlCommand assignWorkCommand = new MySqlCommand(AssignWorkquery, mycon))
                {
                    assignWorkCommand.Parameters.AddWithValue("@DesignationId", calendar.DesignationId);
                    assignWorkCommand.Parameters.AddWithValue("@CurrentDate", calendar.CurrentDate);
                    using (MySqlDataReader assignWorkReader = assignWorkCommand.ExecuteReader())
                    {
                        assignWorkTable.Load(assignWorkReader);
                    }
                }

                mycon.Close();
            }

            // Create a JSON object to return both tables
            var result = new
            {
                TodoTable = todoTable,
                AssignWorkTable = assignWorkTable
            };

            return new JsonResult(result);
        }

    }


}