using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace WebApi
{

    public class User
    {
       
        public string id { get; set; }

       
        public string username { get; set; }

        public string phone { get; set; }

        public string confirmation_code { get; set; }

        public string invite_code { get; set; }
    }

    public class Post
    {

        public string id { get; set; }


        public string post_text { get; set; }

        public string post_time { get; set; }



    }


    public class AuthOptions
    {
        public const string ISSUER = "SlovoServer"; // издатель токена
        public const string AUDIENCE = "SlovoClient"; // потребитель токена
        public const string KEY = "svdiufbiweubfiuweiufgbwieugiowueoioinowiheiufguqwyfywqtcyewtcdfiwjbiowubeiofnwioeuguygviwjbfoeineiouibioubweoifncncnejegheywyweuejhfbejfiweuiow3uejejdyuedbdfiweuebfw";   // ключ для шифрации
        public const Int64 LIFETIME = 1000000; // время жизни токена
        public static SymmetricSecurityKey GetSymmetricSecurityKey()
        {
            return new SymmetricSecurityKey(Encoding.ASCII.GetBytes(KEY));
        }
    }
    public static class Tokens
    {


        public static string GetToken(string uid) //создаем новый токен на основе введенных параметров
        {

            //  string res =      API.SendLogin(username, password);


            var identity = GetIdentity(uid);
            if (identity == null)
            {
                return null;
            }

            var now = DateTime.UtcNow;
            // создаем JWT-токен
            var jwt = new JwtSecurityToken(
                    issuer: AuthOptions.ISSUER,
                    audience: AuthOptions.AUDIENCE,
                    notBefore: now,
                    claims: identity.Claims, //получаем список claims из identity
                    expires: now.Add(TimeSpan.FromMinutes(AuthOptions.LIFETIME)),
                    signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));

            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt); // готовый токен

            var response = new //список состоящий из токена и имени пользователя
            {
                access_token = encodedJwt,
                username = identity.Name
            };

            return encodedJwt.ToString(); //возвращает строку токена
        }



        public static string GetName(string token) //получаем информацию из зашифрованного токена
        {
            string secret = AuthOptions.KEY; //достаем ключ расщифровки
            var key = Encoding.ASCII.GetBytes(secret); //преобразуем ключ в байты
            var handler = new JwtSecurityTokenHandler();
            var validations = new TokenValidationParameters //какие-то параметры токена
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false
            };


            if (token == null || token == "") //проверка токена на то, не является ли он пустым
            {
                return "-1";
            }

            var claims = handler.ValidateToken(token, validations, out var tokenSecure); //расшифрованный токен

            string[] Creds = claims.Identity.Name.Split('/'); //разбиваем расшифрованную строку на список из двух нужных нам параметров

            //далее просто используем полученную из токена информацию:::::


            string login = Creds[0];
         

            return login;
        }



        private static ClaimsIdentity GetIdentity(string uid)
        {

            int percount = 0;

            percount = Convert.ToInt32(uid); //получаем id пользователя с данными логином 


            if (percount > 0)
            {

                User person = WebApi.UsersDTO.GetUsersById(percount.ToString()); //создаем новый элемент класса юзер с полученным id (то есть получаем всю нужную информацию о пользователе на основе имеющихся данных логина и пароля)


                var claims = new List<Claim> //создаем список из параметров пользователя
                {
                    new Claim(ClaimsIdentity.DefaultNameClaimType, person.id + "/" + person.phone),
                    new Claim(ClaimsIdentity.DefaultRoleClaimType, person.phone)
                };

                ClaimsIdentity claimsIdentity = // тоже самое что identity
                new ClaimsIdentity(claims, "Token", ClaimsIdentity.DefaultNameClaimType,
                    ClaimsIdentity.DefaultRoleClaimType);
                return claimsIdentity; //возвращаем список параметров пользователя
            }

            // если пользователя не найдено
            return null;
        }



    }
    public static class UsersDTO
    {

        public static List<User> GetUsers(string phone_number)
        {
            List<User> users = new List<User>();

            string constr = @"workstation id=ms-sql-9.in-solve.ru;packet size=4096;user id=1gb_zevent2;pwd=24zea49egh;data source=ms-sql-9.in-solve.ru;persist security info=False;initial catalog=1gb_mindshare;Connection Timeout=300";
            string query = "exec dbo.USER_CHECK @phonenumb = '"+ phone_number  +  "';";

            using (SqlConnection con = new SqlConnection(constr))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {

                    cmd.Connection = con;
                    con.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        while (sdr.Read())
                        {
                            users.Add(new User
                            {
                                id = sdr["uid"].ToString(),
                            }); ;
                        }
                    }
                    con.Close();
                }
            }


            return users;
        }

        public static List<User> CreateConfirm(string phone_number, string confirm_code_creating)
        {
            List<User> users = new List<User>();

            string constr = @"workstation id=ms-sql-9.in-solve.ru;packet size=4096;user id=1gb_zevent2;pwd=24zea49egh;data source=ms-sql-9.in-solve.ru;persist security info=False;initial catalog=1gb_mindshare;Connection Timeout=300";
            string query = "exec dbo.USER_CREATE_CONFIRM @phonenumb = '" + phone_number + "', @confirm_code_creating = '" + confirm_code_creating + "';";

            using (SqlConnection con = new SqlConnection(constr))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {

                    cmd.Connection = con;
                    con.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        while (sdr.Read())
                        {
                            users.Add(new User
                            {
                                id = sdr["uid"].ToString(),
                            }); ;
                        }
                    }
                    con.Close();
                }
            }


            return users;
        }

        public static User GetUsersById(string uid)
        {
            User res = new User();
            string constr = @"workstation id=ms-sql-9.in-solve.ru;packet size=4096;user id=1gb_zevent2;pwd=24zea49egh;data source=ms-sql-9.in-solve.ru;persist security info=False;initial catalog=1gb_mindshare;Connection Timeout=300";
            string query = "select * from users where uid=" + uid + ";";

            using (SqlConnection con = new SqlConnection(constr))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {

                    cmd.Connection = con;
                    con.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        while (sdr.Read())
                        {
                            res.id = sdr["uid"].ToString();
                            res.username = sdr["username"].ToString();
                            res.phone = sdr["phone"].ToString();
                            res.confirmation_code = sdr["confirm_code"].ToString();
                        }
                    }
                    con.Close();
                }
            }


            return res;
        }

        public static List<User> ConfirmUser(string phone_number, string confirm_code, string user_ip)
        {
            List<User> users = new List<User>();

            string constr = @"workstation id=ms-sql-9.in-solve.ru;packet size=4096;user id=1gb_zevent2;pwd=24zea49egh;data source=ms-sql-9.in-solve.ru;persist security info=False;initial catalog=1gb_mindshare;Connection Timeout=300";
            string query = "exec dbo.USER_CONFIRM @phonenumb = '" + phone_number + "', @confirm_code = '" + confirm_code + "', @user_ip = '" + user_ip + "';";

            using (SqlConnection con = new SqlConnection(constr))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {

                    cmd.Connection = con;
                    con.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        while (sdr.Read())
                        {
                            users.Add(new User
                            {
                                id = sdr["uid"].ToString(),
                            }); ;
                        }
                    }
                    con.Close();
                }
            }


            return users;
        }


        public static List<User> SearchPeople(string uid,string search_stroke)
        {
            List<User> users = new List<User>();

            string constr = @"workstation id=ms-sql-9.in-solve.ru;packet size=4096;user id=1gb_zevent2;pwd=24zea49egh;data source=ms-sql-9.in-solve.ru;persist security info=False;initial catalog=1gb_mindshare;Connection Timeout=300";
            string query = "exec dbo.SEARCH_PEOPLE @uid = " + uid + ", @search_stroke = '" + search_stroke + "';";

            using (SqlConnection con = new SqlConnection(constr))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {

                    cmd.Connection = con;
                    con.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        while (sdr.Read())
                        {
                            users.Add(new User
                            {
                                id = sdr["uid"].ToString(),
                                username = sdr["username"].ToString()
                            }); ;
                        }
                    }
                    con.Close();
                }
            }


            return users;
        }

        public static List<User> LoadFriends(string uid)
        {
            List<User> users = new List<User>();

            string constr = @"workstation id=ms-sql-9.in-solve.ru;packet size=4096;user id=1gb_zevent2;pwd=24zea49egh;data source=ms-sql-9.in-solve.ru;persist security info=False;initial catalog=1gb_mindshare;Connection Timeout=300";
            string query = "exec dbo.LOAD_FRIENDS @uid = " + uid + ";";

            using (SqlConnection con = new SqlConnection(constr))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {

                    cmd.Connection = con;
                    con.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        while (sdr.Read())
                        {
                            users.Add(new User
                            {
                                id = sdr["idrcv"].ToString()
                            }); ;
                        }
                    }
                    con.Close();
                }
            }


            return users;
        }


        public static List<User> LoadFriendsRequests(string uid)
        {
            List<User> users = new List<User>();

            string constr = @"workstation id=ms-sql-9.in-solve.ru;packet size=4096;user id=1gb_zevent2;pwd=24zea49egh;data source=ms-sql-9.in-solve.ru;persist security info=False;initial catalog=1gb_mindshare;Connection Timeout=300";
            string query = "exec dbo.LOAD_REQUESTS_FRIENDS @uid = " + uid + ";";

            using (SqlConnection con = new SqlConnection(constr))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {

                    cmd.Connection = con;
                    con.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        while (sdr.Read())
                        {
                            users.Add(new User
                            {
                                id = sdr["idsnd"].ToString()
                            }); ;
                        }
                    }
                    con.Close();
                }
            }


            return users;
        }

        public static List<User> AddFriend(string uid, string username)
        {
            List<User> users = new List<User>();

            string constr = @"workstation id=ms-sql-9.in-solve.ru;packet size=4096;user id=1gb_zevent2;pwd=24zea49egh;data source=ms-sql-9.in-solve.ru;persist security info=False;initial catalog=1gb_mindshare;Connection Timeout=300";
            string query = "exec dbo.ADD_FRIEND @uid = " + uid + ", @fr_username = '" + username + "';";

            using (SqlConnection con = new SqlConnection(constr))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {

                    cmd.Connection = con;
                    con.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        while (sdr.Read())
                        {
                            users.Add(new User
                            {
                                id = sdr["idsnd"].ToString(),
                            }); ;
                        }
                    }
                    con.Close();
                }
            }


            return users;
        }


        public static List<User> EditUsername(string uid, string new_username)
        {
            List<User> users = new List<User>();

            string constr = @"workstation id=ms-sql-9.in-solve.ru;packet size=4096;user id=1gb_zevent2;pwd=24zea49egh;data source=ms-sql-9.in-solve.ru;persist security info=False;initial catalog=1gb_mindshare;Connection Timeout=300";
            string query = "exec dbo.NEW_USER_USERNAME @uid = " + uid + ", @new_username = '" + new_username + "';";

            using (SqlConnection con = new SqlConnection(constr))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {

                    cmd.Connection = con;
                    con.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        while (sdr.Read())
                        {
                            users.Add(new User
                            {
                                id = sdr["uid"].ToString(),
                            }); ;
                        }
                    }
                    con.Close();
                }
            }


            return users;
        }


        public static List<User> Refresh()
        {
            List<User> users = new List<User>();

            string constr = @"workstation id=ms-sql-9.in-solve.ru;packet size=4096;user id=1gb_zevent2;pwd=24zea49egh;data source=ms-sql-9.in-solve.ru;persist security info=False;initial catalog=1gb_mindshare;Connection Timeout=300";
            string query = "exec dbo.REFRESH;";

            using (SqlConnection con = new SqlConnection(constr))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {

                    cmd.Connection = con;
                    con.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        while (sdr.Read())
                        {
                            users.Add(new User
                            {
                                id = sdr["uid"].ToString(),
                            }); ;
                        }
                    }
                    con.Close();
                }
            }


            return users;
        }
        public static List<User> DeleteFriend(string uid, string username)
        {
            List<User> users = new List<User>();

            string constr = @"workstation id=ms-sql-9.in-solve.ru;packet size=4096;user id=1gb_zevent2;pwd=24zea49egh;data source=ms-sql-9.in-solve.ru;persist security info=False;initial catalog=1gb_mindshare;Connection Timeout=300";
            string query = "exec dbo.DELETE_FRIEND @uid = " + uid + ", @fr_username = '" + username + "';";

            using (SqlConnection con = new SqlConnection(constr))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {

                    cmd.Connection = con;
                    con.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        while (sdr.Read())
                        {
                            users.Add(new User
                            {
                                id = sdr["idsnd"].ToString(),
                            }); ;
                        }
                    }
                    con.Close();
                }
            }


            return users;
        }

        public static List<User> AcceptFriend(string uid, string request_username)
        {
            List<User> users = new List<User>();

            string constr = @"workstation id=ms-sql-9.in-solve.ru;packet size=4096;user id=1gb_zevent2;pwd=24zea49egh;data source=ms-sql-9.in-solve.ru;persist security info=False;initial catalog=1gb_mindshare;Connection Timeout=300";
            string query = "exec dbo.ACCEPT_REQUEST_FRIEND @uid = " + uid + ",@accept_friend = '" + request_username + "';";

            using (SqlConnection con = new SqlConnection(constr))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {

                    cmd.Connection = con;
                    con.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        while (sdr.Read())
                        {
                            users.Add(new User
                            {
                                id = sdr["idrcv"].ToString(),
                            }); ;
                        }
                    }
                    con.Close();
                }
            }


            return users;
        }


        public static Post NewUserPost(string uid, string post_text)
        {
            Post res = new Post();
            string post_time = DateTime.Now.ToString();
            string constr = @"workstation id=ms-sql-9.in-solve.ru;packet size=4096;user id=1gb_zevent2;pwd=24zea49egh;data source=ms-sql-9.in-solve.ru;persist security info=False;initial catalog=1gb_mindshare;Connection Timeout=300";
            string query = "exec dbo.NEW_USER_POST @uid = '" + uid + "', @post_text = '" + post_text + "', @post_time = '" + post_time + "';";

            using (SqlConnection con = new SqlConnection(constr))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {

                    cmd.Connection = con;
                    con.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        while (sdr.Read())
                        {
                            res.id = sdr["id"].ToString();
                        }
                    }
                    con.Close();
                }
            }


            return res;
        }


        public static User LoadUserInvite(string uid)
        {
            User res = new User();
            string constr = @"workstation id=ms-sql-9.in-solve.ru;packet size=4096;user id=1gb_zevent2;pwd=24zea49egh;data source=ms-sql-9.in-solve.ru;persist security info=False;initial catalog=1gb_mindshare;Connection Timeout=300";
            string query = "exec dbo.USER_GET_INVITE @id = " + uid + ";";

            using (SqlConnection con = new SqlConnection(constr))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {

                    cmd.Connection = con;
                    con.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        while (sdr.Read())
                        {
                            res.invite_code = sdr["invite"].ToString();
                        }
                    }
                    con.Close();
                }
            }


            return res;
        }



        public static Post LoadUserPost(string uid)
        {
            Post res = new Post();
            string post_time = DateTime.Now.ToString();
            string constr = @"workstation id=ms-sql-9.in-solve.ru;packet size=4096;user id=1gb_zevent2;pwd=24zea49egh;data source=ms-sql-9.in-solve.ru;persist security info=False;initial catalog=1gb_mindshare;Connection Timeout=300";
            string query = "exec dbo.LOAD_USER_POST @uid="+uid+";";

            using (SqlConnection con = new SqlConnection(constr))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {

                    cmd.Connection = con;
                    con.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        while (sdr.Read())
                        {
                            res.id = sdr["id"].ToString();
                            res.post_text = sdr["text"].ToString();
                            res.post_time = sdr["time"].ToString();
                        }
                    }
                    con.Close();
                }
            }


            return res;
        }




        public static List<User> InsertUser(string phone_number, string username, string confirm_code, string newinviter, string confirm_code_creating)
        {
            List<User> users = new List<User>();

            string constr = @"workstation id=ms-sql-9.in-solve.ru;packet size=4096;user id=1gb_zevent2;pwd=24zea49egh;data source=ms-sql-9.in-solve.ru;persist security info=False;initial catalog=1gb_mindshare;Connection Timeout=300";
            string query = "exec dbo.USER_INSERT @phone = '" + phone_number + "', @username = '"+ username + "', @invitecode = '" + confirm_code + "', @newinviter = '" + newinviter + "', @confirm_code_creating = '" + confirm_code_creating + "';";

            using (SqlConnection con = new SqlConnection(constr))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {

                    cmd.Connection = con;
                    con.Open();
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        while (sdr.Read())
                        {
                            users.Add(new User
                            {
                                id = sdr["uid"].ToString(),
                            }); ;
                        }
                    }
                    con.Close();
                }
            }


            return users;
        }

    }
}
