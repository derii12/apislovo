using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;

namespace WebApi
{

    public class User
    {
       
        public string id { get; set; }

       
        public string username { get; set; }

        public string phone { get; set; }

        public string confirmation_code { get; set; }

        public string invite_code { get; set; }

        public string streak { get; set; }

        public string friend_status { get; set; }

        public string unique_user_code { get; set; }    

        public string user_public_key { get; set; }
    }




    public class Post
    {

        public string id { get; set; }


        public string post_text { get; set; }

        public string post_time { get; set; }


    }

    public class Reaction
    {

        public string id { get; set; }

        public string author_id { get; set; }

        public string post_id { get; set; }

        public string reaction_text { get; set; }

        public string reaction_datetime { get; set; }


    }


    public class PrivatePost
    {
        public string private_post_id { get; set; }

        public string post_unique_key { get; set; }

        public string visibility { get; set; }
    }


    public class AuthOptions
    {
        public const string ISSUER = "SlovoServer"; // издатель токена
        public const string AUDIENCE = "SlovoClient"; // потребитель токена
        public const string KEY = "svdiufbiweubfiuweiufgbwieugiowueoioinowiheiufguqwyfywqtcyewtcdfiwjbiowubeiofnwioeuguygviwjbfoeineiouibioubweoifncncnejegheywyweuejhfbejfiweuiow3uejejdyuedbdfiweuebfw";   // ключ для шифрации
        public const Int64 LIFETIME = 30; // время жизни токена
        public static SymmetricSecurityKey GetSymmetricSecurityKey()
        {
            return new SymmetricSecurityKey(Encoding.ASCII.GetBytes(KEY));
        }
    }

    public class RefreshAuthOptions
    {
        public const string ISSUER = "SlovoRefreshServer"; // издатель токена
        public const string AUDIENCE = "SlovoRefreshClient"; // потребитель токена
        public const string KEY = "swjbfoeineiouibioubweoifncncnejegw3uejejdyvdiufbiweubfiuwheywyweuejhfbejfiwdyvdiufbiweubfiuweiufgbwieugifklejkru8i9oej4w589cjeuw8rh783u4578tcvye4895ut9ws8er0tyoetd0yupftphjbfltoolivdkeriiufguqwyfywqtcyewtcdfiwjbiowubeiofnwioeuguygviuedbdfiweuebfw";   // ключ для шифрации
        public const Int64 LIFETIME = 1000000; // время жизни токена
        public static SymmetricSecurityKey GetSymmetricSecurityKey()
        {
            return new SymmetricSecurityKey(Encoding.ASCII.GetBytes(KEY));
        }
    }

    public class PostOptions
    {
        public const string ISSUER = "SlovoPostServer"; // издатель токена
        public const string AUDIENCE = "SlovoPostClient"; // потребитель токена
        public const string KEY = "svdiufbsadjfiejuwehjruh489u2893u489u2ie3jxdi2j498yu7843ty8347yuf58y23id4u9283udij3irsiorjtoseirt90ewiort034o90rit03945j983j458dj3894yertgdrfgegerg53445wewsethdjgfggdferrt3ty4yutyurtdyhjfjkgujdgfdfyutgyyjyujfvfghfghdfxdger2455y5iopirt545ytw";   // ключ для шифрации
        public const Int64 LIFETIME = 100000; // время жизни токена
        public static SymmetricSecurityKey GetSymmetricSecurityKey()
        {
            return new SymmetricSecurityKey(Encoding.ASCII.GetBytes(KEY));
        }
    }
    public static class Tokens
    {


        public static string GetToken(string uid, string operation_type) //создаем новый токен на основе введенных параметров
        {

            //  string res =      API.SendLogin(username, password);


           var identity = GetIdentity(uid);
          if (identity == null)
            {
                return null;
            }

            var now = DateTime.UtcNow;
            // создаем JWT-токен
            var jwt = new JwtSecurityToken();
            if (operation_type == "auth")
            {
                jwt = new JwtSecurityToken(
                        issuer: AuthOptions.ISSUER,
                        audience: AuthOptions.AUDIENCE,
                        notBefore: now,
                        claims: identity.Claims, //получаем список claims из identity
                        expires: now.Add(TimeSpan.FromMinutes(AuthOptions.LIFETIME)),
                        signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));
            }

            if (operation_type == "refresh_auth")
            {
                jwt = new JwtSecurityToken(
                        issuer: RefreshAuthOptions.ISSUER,
                        audience: RefreshAuthOptions.AUDIENCE,
                        notBefore: now,
                        claims: identity.Claims, //получаем список claims из identity
                        expires: now.Add(TimeSpan.FromMinutes(RefreshAuthOptions.LIFETIME)),
                        signingCredentials: new SigningCredentials(RefreshAuthOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));
            }

            if (operation_type!="auth" && operation_type != "refresh_auth")
            {
                jwt = new JwtSecurityToken(
                        issuer: PostOptions.ISSUER,
                        audience: PostOptions.AUDIENCE,
                        notBefore: now,
                        claims: identity.Claims, //получаем список claims из identity
                        expires: now.Add(TimeSpan.FromMinutes(PostOptions.LIFETIME)),
                        signingCredentials: new SigningCredentials(PostOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));
            }
            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt); // готовый токен

            var response = new //список состоящий из токена и имени пользователя
            {
                access_token = encodedJwt
            //    username = identity.Name
            };

            return encodedJwt.ToString(); //возвращает строку токена
        }



        public static string GetName(string token, string operation_type) //получаем информацию из зашифрованного токена
        {
            string secret = "";
            if (operation_type == "auth")
            {
                secret = AuthOptions.KEY; //достаем ключ расщифровки
            }
            if (operation_type == "refresh_auth")
            {
                secret = RefreshAuthOptions.KEY;
            }
            if(operation_type != "auth" && operation_type != "refresh_auth")
            {
                secret = PostOptions.KEY; //достаем ключ расщифровки
            }

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

            //string[] Creds = claims.Identity.Name.Split('/'); //разбиваем расшифрованную строку на список из двух нужных нам параметров

            //далее просто используем полученную из токена информацию:::::


            string login;
            if (operation_type == "auth")
            {
                login = claims.Identity.Name.Split('/')[0];
            }
            else
            {
                login = claims.Identity.Name;
            }
         

            return login;
        }



        private static ClaimsIdentity GetIdentity(string uid)
        {

      //      int percount = 0;
      //
         //   percount = Convert.ToInt32(uid); //получаем id пользователя с данными логином 


         //   if (percount > 0)
         //   {

               // User person = WebApi.UsersDTO.GetUsersById(percount.ToString()); //создаем новый элемент класса юзер с полученным id (то есть получаем всю нужную информацию о пользователе на основе имеющихся данных логина и пароля)


                var claims = new List<Claim> //создаем список из параметров пользователя
                {
                    new Claim(ClaimsIdentity.DefaultNameClaimType, uid)
                };

                ClaimsIdentity claimsIdentity = // тоже самое что identity
                new ClaimsIdentity(claims, "Token", ClaimsIdentity.DefaultNameClaimType,
                    ClaimsIdentity.DefaultRoleClaimType);
                return claimsIdentity; //возвращаем список параметров пользователя
           // }

            // если пользователя не найдено
            //return null;
        }



    }
    public static class UsersDTO
    {

        public static User UpdateAutentificator(string previous_refresh_code, string new_refresh_code, string device_code)
        {
            User res = new User();
            string constr = @"workstation id=ms-sql-9.in-solve.ru;packet size=4096;user id=1gb_zevent2;pwd=24zea49egh;data source=ms-sql-9.in-solve.ru;persist security info=False;initial catalog=1gb_mindshare;Connection Timeout=300";
            string query = "exec dbo.USER_UPDATE_AUTENTIFICATOR @previous_refresh_code='" + previous_refresh_code + "', @new_refresh_code='"+ new_refresh_code + "', @new_refr_datetime='"+ DateTime.Now.ToString()+"', @device_token='"+ device_code + "';";

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

                        }
                    }
                    con.Close();
                }
            }


            return res;
        }


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
                                id = sdr["uid"].ToString()
                            }); ;
                        }
                    }
                    con.Close();
                }
            }


            return users;
        }
        public static Reaction NewPostReaction(string author_id, string post_id, string react_txt)
        {
            Reaction res = new Reaction();
            string post_time = DateTime.Now.ToString();
            string constr = @"workstation id=ms-sql-9.in-solve.ru;packet size=4096;user id=1gb_zevent2;pwd=24zea49egh;data source=ms-sql-9.in-solve.ru;persist security info=False;initial catalog=1gb_mindshare;Connection Timeout=300";
            string query = "exec dbo.NEW_USER_REACTION @uid = " + author_id + ", @post_id = " + post_id + ", @react_txt = '" + react_txt + "', @react_datetime='"+post_time+ "';";

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
                            res.id = sdr["reactid"].ToString();
                        }
                    }
                    con.Close();
                }
            }


            return res;
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
            string query = "exec dbo.USER_GET @id=" + uid + ";";

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
                            res.unique_user_code = sdr["personal_code"].ToString();
                        }
                    }
                    con.Close();
                }
            }


            return res;
        }

        public static User GetUsersByUniqueUserCode(string usercode)
        {
            User res = new User();
            string constr = @"workstation id=ms-sql-9.in-solve.ru;packet size=4096;user id=1gb_zevent2;pwd=24zea49egh;data source=ms-sql-9.in-solve.ru;persist security info=False;initial catalog=1gb_mindshare;Connection Timeout=300";
            string query = "exec dbo.USER_GET_BY_UNIQUE_CODE @usercode='" + usercode + "';";

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
                            res.unique_user_code = sdr["personal_code"].ToString();
                        }
                    }
                    con.Close();
                }
            }


            return res;
        }

        public static User GetUsersStreak(string uid)
        {
            User res = new User();
            string constr = @"workstation id=ms-sql-9.in-solve.ru;packet size=4096;user id=1gb_zevent2;pwd=24zea49egh;data source=ms-sql-9.in-solve.ru;persist security info=False;initial catalog=1gb_mindshare;Connection Timeout=300";
            string query = "exec dbo.GET_USER_STREAK @uid=" + uid + ";";

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
                            res.streak = sdr["streak_count"].ToString();
                           
                        }
                    }
                    con.Close();
                }
            }


            return res;
        }

        public static List<User> ConfirmUser(string phone_number, string confirm_status, string user_ip, string user_device, string new_refresh_code, string public_modulus)
        {
            List<User> users = new List<User>();
            string datelog= DateTime.Now.ToString();
            string constr = @"workstation id=ms-sql-9.in-solve.ru;packet size=4096;user id=1gb_zevent2;pwd=24zea49egh;data source=ms-sql-9.in-solve.ru;persist security info=False;initial catalog=1gb_mindshare;Connection Timeout=300";
            string query = "exec dbo.USER_CONFIRM @phonenumb = '" + phone_number + "', @confirm_status = '" + confirm_status + "', @user_ip = '" + user_ip + "', @user_device = '"+ user_device + "', @datelog = '"+ datelog + "', @new_refresh_code='"+ new_refresh_code + "', @public_key='"+ public_modulus + "';";

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


        public static User FriendStatus(string uid,string fid)
        {
            User res = new User();
            string constr = @"workstation id=ms-sql-9.in-solve.ru;packet size=4096;user id=1gb_zevent2;pwd=24zea49egh;data source=ms-sql-9.in-solve.ru;persist security info=False;initial catalog=1gb_mindshare;Connection Timeout=300";
            string query = "exec dbo.GET_FRIENDSHIP_STATUS @aid=" + uid + ", @bid=" + fid + ";";

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
                            res.friend_status = sdr["status"].ToString();

                        }
                    }
                    con.Close();
                }
            }


            return res;
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
                                username = sdr["username"].ToString(),
                                unique_user_code = sdr["personal_code"].ToString()
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

        public static List<User> LoadFriendsKeys(string uid)
        {
            List<User> users = new List<User>();

            string constr = @"workstation id=ms-sql-9.in-solve.ru;packet size=4096;user id=1gb_zevent2;pwd=24zea49egh;data source=ms-sql-9.in-solve.ru;persist security info=False;initial catalog=1gb_mindshare;Connection Timeout=300";
            string query = "exec dbo.LOAD_FRIENDS_KEYS @uid = " + uid + ";";

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
                                friend_status = sdr["status"].ToString()
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

        public static List<User> LoadFriendPublicKey(string uid)
        {
            List<User> users = new List<User>();

            string constr = @"workstation id=ms-sql-9.in-solve.ru;packet size=4096;user id=1gb_zevent2;pwd=24zea49egh;data source=ms-sql-9.in-solve.ru;persist security info=False;initial catalog=1gb_mindshare;Connection Timeout=300";
            string query = "exec dbo.LOAD_USER_PUBLICKEY @uid = " + uid + ";";

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
                                user_public_key = sdr["public_key"].ToString()
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


        public static PrivatePost GetPrivatePost(string post_id, string reader_id)
        {
            PrivatePost posts = new PrivatePost();

            string constr = @"workstation id=ms-sql-9.in-solve.ru;packet size=4096;user id=1gb_zevent2;pwd=24zea49egh;data source=ms-sql-9.in-solve.ru;persist security info=False;initial catalog=1gb_mindshare;Connection Timeout=300";
            string query = "exec dbo.GET_PRIVATE_POST @post_id = " + post_id + ", @reader_id = " + reader_id + ";";

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
                            posts.private_post_id = sdr["private_post_id"].ToString();
                            posts.post_unique_key = sdr["encoder_key"].ToString();
                            posts.visibility = sdr["status"].ToString();
                        }
                    }
                    con.Close();
                }
            }


            return posts;
        }

        public static PrivatePost AddPrivatePost(string post_id, string reader_id, string encoder_key, string status)
        {
            PrivatePost posts = new PrivatePost();

            string constr = @"workstation id=ms-sql-9.in-solve.ru;packet size=4096;user id=1gb_zevent2;pwd=24zea49egh;data source=ms-sql-9.in-solve.ru;persist security info=False;initial catalog=1gb_mindshare;Connection Timeout=300";
            string query = "exec dbo.ADD_PRIVATE_POST @post_id = " + post_id + ", @reader_id = " + reader_id + ", @encoder_key = '" + encoder_key + "', @status = " + status + ";";

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
                            posts.private_post_id = sdr["private_post_id"].ToString();
                        }
                    }
                    con.Close();
                }
            }


            return posts;
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


        public static List<Reaction> LoadPostReactions(string post_id)
        {
            List<Reaction> reacts = new List<Reaction>();

            string constr = @"workstation id=ms-sql-9.in-solve.ru;packet size=4096;user id=1gb_zevent2;pwd=24zea49egh;data source=ms-sql-9.in-solve.ru;persist security info=False;initial catalog=1gb_mindshare;Connection Timeout=300";
            string query = "exec dbo.LOAD_POST_REACTION @post_id =" + post_id;

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
                            reacts.Add(new Reaction
                            {
                                id = sdr["reactid"].ToString(),
                                post_id = sdr["post_id"].ToString(),
                                author_id = sdr["author_id"].ToString(),
                                reaction_text = sdr["react_txt"].ToString(),
                                reaction_datetime = sdr["react_datetime"].ToString()
                            }); ;
                        }
                    }
                    con.Close();
                }
            }


            return reacts;
        }

        public static List<User> InsertUser(string phone_number, string username, string confirm_code, string newinviter, string confirm_code_creating, string unique_user_code)
        {
            List<User> users = new List<User>();

            string constr = @"workstation id=ms-sql-9.in-solve.ru;packet size=4096;user id=1gb_zevent2;pwd=24zea49egh;data source=ms-sql-9.in-solve.ru;persist security info=False;initial catalog=1gb_mindshare;Connection Timeout=300";
            string query = "exec dbo.USER_INSERT @phone = '" + phone_number + "', @username = '"+ username + "', @invitecode = '" + confirm_code + "', @newinviter = '" + newinviter + "', @confirm_code_creating = '" + confirm_code_creating + "', @user_code = '"+ unique_user_code + "';";

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
