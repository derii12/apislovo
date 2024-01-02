using AutoMapper;
using Identity.Models;
using Identity.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Models.DTOs.Account;
using Models.Enums;
using Models.ResponseModels;
using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using WebApi.Attributes;
using System.Net.Http;
using Json.Net;
using Newtonsoft.Json;
using AutoMapper;
using Caching;
using Core;
using GraphiQl;
using HealthChecks.UI.Client;
using Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Services.Interfaces;
using WebApi.Extensions;
using WebApi.GraphQL;
using WebApi.Helpers;
using WebApi.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http;
using System.Net.Sockets;
using Microsoft.AspNetCore.Http.Extensions;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SlovoController : ControllerBase
    {
        private readonly IMapper _mapper;
        public SlovoController(IMapper mapper)
        {

            _mapper = mapper;
        }

        // GET: SlovoController/Details/5
        [HttpGet("Phone_number_checking")]
        async public Task<string> Phone_number_checking(string phone_number)
        {
            bool isAlpha = phone_number.All(Char.IsDigit);
            if (isAlpha)
            {
                List<User> users = UsersDTO.GetUsers(phone_number);

                string find_user = users[0].id;
                if (Convert.ToInt64(find_user) > 0)
                {
                    try
                    {
                        string ip = " ";

                        var headers = Request.Headers.ToList();
                        foreach (var h in headers)
                        {
                            string hname = h.Key;
                            string hvalue = h.Value.ToString();

                            if (hname == "X-Forwarded-For")
                            {
                                ip = hvalue.Split(',')[0];
                            }

                        }

                        var confirmed_user = await GetConfirmAndSend(phone_number, ip);

                        dynamic request_sms_array = JsonConvert.DeserializeObject(confirmed_user);
                        string confirm_code_creating = request_sms_array["code"].ToString();

                        List<User> confirm_users = UsersDTO.CreateConfirm(phone_number, confirm_code_creating);

                        return "found";
                    }
                    catch
                    {
                        return "error";
                    }
                }
                else
                {
                    return "notfound";
                }
            }
            else
            {
                return "bad_phone_number";
            }
        }

        // GET: SlovoController/Registration/5
        private static Random random = new Random();
        async Task<string> GetConfirmAndSend(string phone, string ip)
        {
            string res = "";

            HttpClient client = new HttpClient();

            string Url = "https://sms.ru/code/call?phone=" + phone + "&api_id=96D8F30A-2F80-CBCB-9096-8F38959EA997";

            Uri uri = new Uri(string.Format(Url));

            HttpResponseMessage response = await client.GetAsync(uri);

            if (response.IsSuccessStatusCode)
            {
                string inserted_user = await response.Content.ReadAsStringAsync();

                res = inserted_user;

            }
            else
            {
                res = "error(something went wrong when we tried to insert a user)";
            }

            return res;
        }
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNPQRSTUVWXYZ123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        public static string RandomStringDigit(int length)
        {
            const string chars = "1234567890";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        [HttpGet("Registration")]
        async public Task<string> Registration(string phone_number, string username, string invitecode)
        {
            bool isAlpha_phone = phone_number.All(Char.IsDigit); /*проверка данных на соотвествие*/
            bool isAlpha_username = Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$");
            bool isAlpha_username_digit = username.All(Char.IsDigit); /*проверка данных на соотвествие*/
            if (isAlpha_phone && isAlpha_username && isAlpha_username_digit == false)
            {
                string newinviter = RandomString(10);

                string confirm_code_creating = "register";
                List<User> users = UsersDTO.InsertUser(phone_number, username, invitecode, newinviter, confirm_code_creating); //пытаемся записать нового пользователя и получить его id

                string inserted_user = users[0].id;
                if (Convert.ToInt64(inserted_user) > 0) //если пользователь внесен в бд, и код приглашения существует, то отправляем смс для подтверждения номера
                {
                    try
                    {
                        string ip = " ";

                        var headers = Request.Headers.ToList();
                        foreach (var h in headers)
                        {
                            string hname = h.Key;
                            string hvalue = h.Value.ToString();

                            if (hname == "X-Forwarded-For")
                            {
                                ip = hvalue.Split(',')[0];
                            }

                        }


                        var confirmed_user = await GetConfirmAndSend(phone_number, ip);
                        dynamic request_sms_array = JsonConvert.DeserializeObject(confirmed_user);
                        string new_confirm_code_creating = request_sms_array["code"].ToString();

                        List<User> confirm_users = UsersDTO.CreateConfirm(phone_number, new_confirm_code_creating);

                        return "success";
                    }
                    catch
                    {
                        string ip = "";
                        var confirmed_user = await GetConfirmAndSend(phone_number, ip);
                        return confirmed_user;
                    }
                }
                else
                {
                    return inserted_user.ToString();
                }

            }
            else
            {
                return "bad_username";
            }
        }
        [HttpGet("Confirmation")]
        public string Confirmation(string phone_number, string confirm_code)
        {
            bool isAlpha_phone = phone_number.All(Char.IsDigit); /*проверка данных на соотвествие*/
            bool isAlpha_confirmation = confirm_code.All(Char.IsDigit);
            if (isAlpha_phone && isAlpha_confirmation)
            {

                string user_ip = " ";

                var headers = Request.Headers.ToList();
                foreach (var h in headers)
                {
                    string hname = h.Key;
                    string hvalue = h.Value.ToString();

                    if (hname == "X-Forwarded-For")
                    {
                        user_ip = hvalue.Split(',')[0];
                    }

                }
                List<User> users = UsersDTO.ConfirmUser(phone_number, confirm_code, user_ip);



                string inserted_user = users[0].id;
                if (Convert.ToInt64(inserted_user) > 0)
                {
                    string token = Tokens.GetToken(inserted_user); //создание токена для этого пользователя

                    // string encoded_token = Tokens.GetName(token); расшифровка токена для этого пользователя
                    return token;
                }
                else
                {
                    return inserted_user.ToString();
                }
            }
            else
            {
                return "bad_confirm_code";
            }
        }

        [HttpGet("username_get")]
        public string username_get(string token)
        {
            try
            {
                string encoded_token = Tokens.GetName(token);
                User person = UsersDTO.GetUsersById(encoded_token);
                User pesoncode = UsersDTO.LoadUserInvite(encoded_token);
                return person.username + ";" + pesoncode.invite_code;
                // int got_username = Convert.ToInt32(person.username);
            }
            catch
            {

                return "-1";
            }
        }


        [HttpGet("new_post")]
        public string new_post(string token, string post_text)
        {
            try
            {
                string encoded_token = Tokens.GetName(token);
                Post new_post = UsersDTO.NewUserPost(encoded_token, post_text);
                return "success";
                // int got_username = Convert.ToInt32(person.username);
            }
            catch
            {

                return "-1";
            }
        }

        [HttpGet("refresh")]
        public string refresh()
        {
            UsersDTO.Refresh();
            return "success";
        }

        public static string PostLeftSecods(string post_time)
        {
            DateTimeOffset dtnow = new DateTimeOffset(DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc), TimeSpan.Zero);
            DateTimeOffset dtpost = new DateTimeOffset(DateTime.SpecifyKind(Convert.ToDateTime(post_time), DateTimeKind.Utc), TimeSpan.Zero);
            string post_time_left_seconds = (86400 - dtnow.ToUnixTimeSeconds() + dtpost.ToUnixTimeSeconds()).ToString();
            return post_time_left_seconds;
        }

        [HttpGet("load_post")]
        public string load_post(string token)
        {
                string a = refresh();
                string encoded_token = Tokens.GetName(token);
                Post post = UsersDTO.LoadUserPost(encoded_token);
                return "{"+post.post_text + ";" + PostLeftSecods(post.post_time) + "}";
                // int got_username = Convert.ToInt32(person.username);
        }

        [HttpGet("load_friends_posts")]
        public string load_friends_posts(string token)
        {
            try
            {
                string encoded_token = Tokens.GetName(token);
                List<User> find_friends = UsersDTO.LoadFriends(encoded_token);
                Post friendpost = UsersDTO.LoadUserPost(find_friends[0].id);
                string find_post_res_info = "";
                string find_post_res_time = "";
                for (int i = 0; i < find_friends.Count; i++)
                {
                    Post friendpostother = UsersDTO.LoadUserPost(find_friends[i].id);
                    if (friendpostother.id != null)
                    {
                        find_post_res_info = find_post_res_info + "~" + friendpostother.post_text + "|" + UsersDTO.GetUsersById(friendpostother.id).username; //генерация списка информации о постах
                        find_post_res_time = find_post_res_time + "|" + PostLeftSecods(friendpostother.post_time);
                    }
                    else
                    {

                    }
                }
                if (find_post_res_info == "")
                {
                    return "notfound";
                }
                else
                {
                    refresh();
                    find_post_res_info = find_post_res_info.Remove(0, 1);
                    find_post_res_time = find_post_res_time.Remove(0, 1);
                    return find_post_res_info + "•" + find_post_res_time;
                }
            }
            catch
            {
                return "error";
            }

        }

        [HttpGet("myip")]
        public string myip()
        {
            string aa = Request.Path.ToString();

            string ip = " ";

            var headers = Request.Headers.ToList();
            foreach (var h in headers)
            {
                string hname = h.Key;
                string hvalue = h.Value.ToString();

                if (hname == "X-Forwarded-For")
                {
                    ip = hvalue;
                }

            }

            return ip;
        }

        [HttpGet("add_friend")]
        public string add_friend(string token, string username)
        {
            try
            {
                string encoded_token = Tokens.GetName(token);
                List<User> find_users = UsersDTO.AddFriend(encoded_token, username);
                if (find_users[0].id != "-2" && find_users[0].id != "-1")
                {
                    return "success";
                }
                else
                {
                    return "notfound";
                }
            }
            catch
            {
                return "error";
            }
        }

        [HttpGet("edit_username")]
        public string edit_username(string token, string new_username)
        {
            bool isAlpha_username = Regex.IsMatch(new_username, @"^[a-zA-Z0-9_]+$");
            bool isAlpha_newusername= new_username.All(Char.IsDigit);
            if (isAlpha_username && new_username.Length>3 && isAlpha_newusername == false)
            {
                try
                {
                    string encoded_token = Tokens.GetName(token);
                    List<User> edit_username = UsersDTO.EditUsername(encoded_token, new_username);
                    return edit_username[0].id;
                }
                catch
                {
                    return "error";
                }
            }
            else
            {
                return "bad_username";
            }
        }


        [HttpGet("delete_friend")]
        public string delete_friend(string token, string username)
        {
            try
            {
                string encoded_token = Tokens.GetName(token);
                List<User> find_users = UsersDTO.DeleteFriend(encoded_token, username);
                if (find_users[0].id != "-2" && find_users[0].id != "-1")
                {
                    return "success";
                }
                else
                {
                    return "notfound";
                }
            }
            catch
            {
                return "error";
            }
        }

        [HttpGet("accept_friend")]
        public string accept_friend(string token, string username)
        {
            try
            {
                string encoded_token = Tokens.GetName(token);
                List<User> find_users = UsersDTO.AcceptFriend(encoded_token, username);
                if (find_users[0].id != "-1")
                {
                    return "success";
                }
                else
                {
                    return "notfound";
                }
            }
            catch
            {
                return "error";
            }
        }


        [HttpGet("search_user")]
        public string search_user(string token, string search_stroke)
        {
            if (search_stroke.Contains(" ") || search_stroke.Length < 3)
            {
                return "error";
            }
            else
            {
                try
                {
                    string encoded_token = Tokens.GetName(token);
                    List<User> find_users = UsersDTO.SearchPeople(encoded_token, search_stroke);
                    if (find_users[0].id != "-7")
                    {
                        string find_res = find_users[0].username;
                        for (int i = 1; i < find_users.Count; i++)
                        {
                            find_res = find_res + ";" + find_users[i].username;
                        }
                        return "{" + find_res + "}";
                    }
                    else
                    {
                        return "notfound";
                    }
                }
                catch
                {
                    return "error";
                }
            }
        }


        [HttpGet("load_friends")]
        public string load_friends(string token)
        {
            try
            {
                string encoded_token = Tokens.GetName(token);
                List<User> find_friends = UsersDTO.LoadFriends(encoded_token);

                if (find_friends[0].id != "-1")
                {
                    string find_res = UsersDTO.GetUsersById(find_friends[0].id).username;
                    for (int i = 1; i < find_friends.Count; i++)
                    {
                        find_res = find_res + ";" + UsersDTO.GetUsersById(find_friends[i].id).username;
                    }
                    return "{" + find_res + "}";
                }
                else
                {
                    return "notfound";
                }
            }
            catch
            {
                return "error";
            }
        }

        [HttpGet("load_requests")]
        public string load_requests(string token)
        {
            try
            {
                string encoded_token = Tokens.GetName(token);
                List<User> find_friends = UsersDTO.LoadFriendsRequests(encoded_token);
                if (find_friends[0].id != "-1")
                {
                    string find_res = UsersDTO.GetUsersById(find_friends[0].id).username;
                    for (int i = 1; i < find_friends.Count; i++)
                    {
                        find_res = find_res + ";" + UsersDTO.GetUsersById(find_friends[i].id).username;
                    }
                    return "{" + find_res + "}";
                }
                else
                {
                    return "notfound";
                }
            }
            catch
            {
                return "error";
            }
        }

    }
}

