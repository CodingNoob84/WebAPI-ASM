namespace WebAPI_ASM.Model
{
    public class User
    {
        public class LoginCredentials
        {
            public string user_name { get; set; }
            public string user_password { get; set; }
        }

        public class CreateAccountCredentials
        {
            public string fullname { get; set; }
            public string emailid { get; set; }
            public string user_name { get; set; }
            public string user_password { get; set; }

        }

    }
}
