using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Data;
using static WebAPI_ASM.Model.User;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebAPI_ASM.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        public UserController(IConfiguration configuration, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _env = env;
        }

        // GET: api/<LoginController>
        [HttpGet("getAllUsers")]
        public JsonResult Get()
        {
            string query = @"SELECT * FROM `user`";

            DataTable table = new DataTable();
            string sqlDataSource = _configuration.GetConnectionString("ASMotorCon");
            MySqlDataReader myReader;
            using (MySqlConnection mycon = new MySqlConnection(sqlDataSource))
            {
                mycon.Open();
                using (MySqlCommand myCommand = new MySqlCommand(query, mycon))
                {
                    myReader = myCommand.ExecuteReader();
                    table.Load(myReader);

                    myReader.Close();
                    mycon.Close();
                }
            }

            return new JsonResult(table);
        }

        [HttpPost("login")]
        public IActionResult LoginPost([FromBody] LoginCredentials credentials)
        {
            string sqlDataSource = _configuration.GetConnectionString("ASMotorCon");
            using (MySqlConnection mycon = new MySqlConnection(sqlDataSource))
            {
                mycon.Open();

                string userCheckQuery = "SELECT COUNT(*) FROM user WHERE user_name = @Username";
                using (MySqlCommand userCheckCommand = new MySqlCommand(userCheckQuery, mycon))
                {
                    userCheckCommand.Parameters.AddWithValue("@Username", credentials.user_name);
                    int userCount = Convert.ToInt32(userCheckCommand.ExecuteScalar());

                    if (userCount == 0)
                    {
                        // User does not exist
                        return BadRequest(new { status = "error", message = "User not found." });
                    }
                }

                string passwordQuery = "SELECT user_id,fullname,user_password,role,staff_id,designation_id FROM user WHERE user_name=@Username";
                using (MySqlCommand passwordCommand = new MySqlCommand(passwordQuery, mycon))
                {
                    passwordCommand.Parameters.AddWithValue("@Username", credentials.user_name);
                    using (MySqlDataReader passwordReader = passwordCommand.ExecuteReader())
                    {
                        if (passwordReader.Read())
                        {
                            string fullName = passwordReader["fullname"].ToString();
                            string role = passwordReader["role"].ToString();
                            string hashedPassword = passwordReader["user_password"].ToString();
                            string userId = passwordReader["user_id"].ToString();
                            string staffId = passwordReader["staff_id"].ToString();
                            string designationId = passwordReader["designation_id"].ToString();

                            // Close the passwordReader before using staffReader
                            passwordReader.Close();

                            // Execute the StaffQuery to fetch staff-related data
                            string StaffQuery = "SELECT company_id, department, emp_code, reporting FROM `staff_creation` WHERE staff_id=@StaffID";
                            using (MySqlCommand staffCommand = new MySqlCommand(StaffQuery, mycon))
                            {
                                staffCommand.Parameters.AddWithValue("@StaffID", staffId);
                                using (MySqlDataReader staffReader = staffCommand.ExecuteReader())
                                {
                                    if (staffReader.Read())
                                    {
                                        string companyId = staffReader["company_id"].ToString();
                                        string departmentId = staffReader["department"].ToString();
                                        string staffCode = staffReader["emp_code"].ToString();
                                        string reporting = staffReader["reporting"].ToString();

                                        if (credentials.user_password == hashedPassword)
                                        {
                                            var data = new
                                            {
                                                Name = fullName,
                                                Role = role,
                                                staff_id = staffId,
                                                designation_id = designationId,
                                                company_id = companyId,
                                                department_id = departmentId,
                                                staff_code = staffCode,
                                                Reporting = reporting
                                            };
                                            return Ok(new { status = "success", message = "access permitted", data });
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Return a generic error response for unexpected cases
                return StatusCode(500, new { status = "error", message = "An unexpected error occurred.", data = new { } });
            }
        }



        [HttpPost("createaccount")]
        public IActionResult CreateAccountPost([FromBody] CreateAccountCredentials cacredentials)
        {
            if (string.IsNullOrWhiteSpace(cacredentials.fullname) ||
        string.IsNullOrWhiteSpace(cacredentials.emailid) ||
        string.IsNullOrWhiteSpace(cacredentials.user_name) ||
        string.IsNullOrWhiteSpace(cacredentials.user_password))
            {
                return BadRequest(new { status = "error", message = "All fields are required." });
            }

            string sqlDataSource = _configuration.GetConnectionString("ASMotorCon");
            using (MySqlConnection mycon = new MySqlConnection(sqlDataSource))
            {
                mycon.Open();

                string userEmailCheckQuery = "SELECT COUNT(*) FROM user WHERE emailid = @emailid";
                using (MySqlCommand userEmailCheckCommand = new MySqlCommand(userEmailCheckQuery, mycon))
                {
                    userEmailCheckCommand.Parameters.AddWithValue("@emailid", cacredentials.emailid);
                    int userCount = Convert.ToInt32(userEmailCheckCommand.ExecuteScalar());

                    if (userCount > 0)
                    {
                        // User does not exist
                        return BadRequest(new { status = "error", message = "User Email Id already exists." });
                    }
                }

                string userCheckQuery = "SELECT COUNT(*) FROM user WHERE user_name = @Username";
                using (MySqlCommand userCheckCommand = new MySqlCommand(userCheckQuery, mycon))
                {
                    userCheckCommand.Parameters.AddWithValue("@Username", cacredentials.user_name);
                    int userCount = Convert.ToInt32(userCheckCommand.ExecuteScalar());

                    if (userCount > 0)
                    {
                        // User does not exist
                        return BadRequest(new { status = "error", message = "User name already Exists." });
                    }
                }

                // Insert the new user into the database
                string insertQuery = "INSERT INTO user (fullname, emailid, user_name, user_password) VALUES (@Fullname, @Email, @Username, @Password)";
                using (MySqlCommand insertCommand = new MySqlCommand(insertQuery, mycon))
                {
                    insertCommand.Parameters.AddWithValue("@Fullname", cacredentials.fullname);
                    insertCommand.Parameters.AddWithValue("@Email", cacredentials.emailid);
                    insertCommand.Parameters.AddWithValue("@Username", cacredentials.user_name);
                    insertCommand.Parameters.AddWithValue("@Password", cacredentials.user_password);

                    int rowsAffected = insertCommand.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        // User successfully inserted
                        return Ok(new { status = "success", message = "User account created." });
                    }
                    else
                    {
                        // Insert failed
                        return BadRequest(new { status = "error", message = "User account creation failed." });
                    }
                }

            }


        }
    }
}
