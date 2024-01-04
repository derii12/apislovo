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
        [HttpGet("Phone_number_checking")]  // checking for user by phone number, and sanding confirm call if user is alredy in database
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
                        string ip = " "; // getting ip adres of unique user

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

                        var confirmed_user = await GetConfirmAndSend(phone_number, ip); //sending phone number and ip adress, creating confirm call

                        dynamic request_sms_array = JsonConvert.DeserializeObject(confirmed_user);
                        string confirm_code_creating = request_sms_array["code"].ToString(); //getting confirm code

                        List<User> confirm_users = UsersDTO.CreateConfirm(phone_number, confirm_code_creating); //insert new confirm for this user in database

                        return "found";
                    }
                    catch
                    {
                        return "error"; // something went wrong
                    }
                }
                else
                {
                    return "notfound"; // user with this phonenumber is not in database
                }
            }
            else
            {
                return "bad_phone_number"; //wrong formate of user phone
            }
        }

        // GET: SlovoController/Registration/5
        private static Random random = new Random();


        async Task<string> GetConfirmAndSend(string phone, string ip)//sending new confirm call for user
        {
            string res = "";

            HttpClient client = new HttpClient();

            string Url = "https://sms.ru/code/call?phone=" + phone + "&api_id=96D8F30A-2F80-CBCB-9096-8F38959EA997"; //sending url for call and send service

            Uri uri = new Uri(string.Format(Url));

            HttpResponseMessage response = await client.GetAsync(uri);

            if (response.IsSuccessStatusCode)
            {
                string inserted_user = await response.Content.ReadAsStringAsync();

                res = inserted_user;

            }
            else
            {
                res = "error(something went wrong when we tried to insert a user)"; // error of sms service
            }

            return res;
        }


        public static string RandomString(int length) // generating random string some lenght
        {
            const string chars = "ABCDEFGHIJKLMNPQRSTUVWXYZ123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }


        [HttpGet("GetUserIp")]
        async public Task<string> GetUserIp() //getting user ipadress 
        {
            string ip = "none";
            var headers = Request.Headers.ToList();
            foreach (var h in headers)
            {
                string hname = h.Key;
                string hvalue = h.Value.ToString();

                if (hname == "X-Forwarded-For")
                {
                    return hvalue.Split(',')[0];
                }

            }
            return ip;
        }

        [HttpGet("Registration")]
        async public Task<string> Registration(string phone_number, string username, string invitecode) // new user insertion
        {
            bool isAlpha_phone = phone_number.All(Char.IsDigit); /*проверка данных на соотвествие*/
            bool isAlpha_username = Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$"); //checking username for format
            bool isAlpha_username_digit = username.All(Char.IsDigit); /*проверка данных на соотвествие*/
            if (isAlpha_phone && isAlpha_username && isAlpha_username_digit == false) //checking data for wrong format
            {
                string newinviter = RandomString(10); //generating new invite_code

                string confirm_code_creating = "register";
                List<User> users = UsersDTO.InsertUser(phone_number, username, invitecode, newinviter, confirm_code_creating); //пытаемся записать нового пользователя и получить его id

                string inserted_user = users[0].id;
                if (Convert.ToInt64(inserted_user) > 0) //если пользователь внесен в бд, и код приглашения существует, то отправляем смс для подтверждения номера
                {
                    try
                    {
                        string ip = GetUserIp().Result; //ip of registered user if registation was successful


                        var confirmed_user = await GetConfirmAndSend(phone_number, ip);
                        dynamic request_sms_array = JsonConvert.DeserializeObject(confirmed_user); //getting json answer 
                        string new_confirm_code_creating = request_sms_array["code"].ToString();//selecting confirmation string code

                        List<User> confirm_users = UsersDTO.CreateConfirm(phone_number, new_confirm_code_creating); //insert new confirm code in database for this user

                        return "success";
                    }
                    catch
                    {
                        string ip = "";
                        var confirmed_user = await GetConfirmAndSend(phone_number, ip);
                        return "error"; //error of sending confirmation code for user
                    }
                }
                else
                {

                    return inserted_user.ToString(); // returns error code, if creating new user was no successful
                }

            }
            else
            {
                return "bad_username"; //wrong format of username(if user was not tried to hack our mobile app)
            }
        }
        [HttpGet("Confirmation")]
        public string Confirmation(string phone_number, string confirm_code) //checking entered confirm code for actuality
        {
            bool isAlpha_phone = phone_number.All(Char.IsDigit); /*проверка данных на соотвествие*/
            bool isAlpha_confirmation = confirm_code.All(Char.IsDigit);
            if (isAlpha_phone && isAlpha_confirmation) // checkin data format
            {

                string user_ip = GetUserIp().Result; //getting user ipadress

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
                    return inserted_user.ToString(); //getting errorcode of  confirmation
                }
            }
            else
            {
                return "bad_confirm_code";
            }
        }

        [HttpGet("username_get")] // getting info about this user(username,invitecode,streaks)
        public string username_get(string token)
        {
            try
            {
                string encoded_token = Tokens.GetName(token); //get encoded token (user id)
                User person = UsersDTO.GetUsersById(encoded_token); //geting info about user
                User pesoncode = UsersDTO.LoadUserInvite(encoded_token); //getting invite code for this user
                return person.username + ";" + pesoncode.invite_code;
                // int got_username = Convert.ToInt32(person.username);
            }
            catch
            {

                return "-1"; //wrong token error code
            }
        }


        [HttpGet("new_post")]
        public string new_post(string token, string post_text) //creating new post for this user
        {
            try
            {
                string encoded_token = Tokens.GetName(token); //get encoded token (user id)
                Post new_post = UsersDTO.NewUserPost(encoded_token, post_text); //creating new post for this user (new post text)
                return "success"; // post created succesfull
                // int got_username = Convert.ToInt32(person.username);
            }
            catch
            {
                return "-1"; //wrong token error code
            }
        }

        [HttpGet("refresh")]
        public string refresh() //procedure of checking database for posts, which time is left
        {
            UsersDTO.Refresh();
            return "success"; 
        }

        public static string PostLeftSecods(string post_time) //getting time in seconds, how much post will be actual
        {
            DateTimeOffset dtnow = new DateTimeOffset(DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc), TimeSpan.Zero); //getting now server time
            DateTimeOffset dtpost = new DateTimeOffset(DateTime.SpecifyKind(Convert.ToDateTime(post_time), DateTimeKind.Utc), TimeSpan.Zero); //getting time, when post has been created
            string post_time_left_seconds = (86400 - dtnow.ToUnixTimeSeconds() + dtpost.ToUnixTimeSeconds()).ToString(); //counting how much seconds post will be alive
            return post_time_left_seconds;
        }

        [HttpGet("load_post")]
        public string load_post(string token) //load autor's post
        {
                string a = refresh();
                string encoded_token = Tokens.GetName(token); // get encoded token(user id)
                Post post = UsersDTO.LoadUserPost(encoded_token); // load post info by uesr id
                return "{"+post.post_text + ";" + PostLeftSecods(post.post_time) + "}"; //sending request about user post information
                // int got_username = Convert.ToInt32(person.username);
        }

        [HttpGet("load_friends_posts")]
        public string load_friends_posts(string token) //getting friends posts for this user
        {
            try
            {
                string encoded_token = Tokens.GetName(token); //get encoded token(id)
                List<User> find_friends = UsersDTO.LoadFriends(encoded_token); //getting all friends of this user
                Post friendpost = UsersDTO.LoadUserPost(find_friends[0].id);//trying to get post of first friend in the list
                string find_post_res_info = ""; //information of post string(post text, author, streak)
                string find_post_res_time = ""; // information of post time (how much seconds post will be alive)
                for (int i = 0; i < find_friends.Count; i++)//checking for another friends post 
                {
                    Post friendpostother = UsersDTO.LoadUserPost(find_friends[i].id); //gettin i's friend post by friend id
                    if (friendpostother.id != null) 
                    {
                        find_post_res_info = find_post_res_info + "~" + friendpostother.post_text + "|" + UsersDTO.GetUsersById(friendpostother.id).username; //getting information about post and adding it to string
                        find_post_res_time = find_post_res_time + "|" + PostLeftSecods(friendpostother.post_time); //getting seconds, how much post will be alive
                    }
                    else
                    {

                    }
                }
                if (find_post_res_info == "")
                {
                    return "notfound"; //no one from user's friends have no posts
                }
                else //user's friends have some posts
                {
                    refresh(); 
                    find_post_res_info = find_post_res_info.Remove(0, 1);
                    find_post_res_time = find_post_res_time.Remove(0, 1);
                    return find_post_res_info + "•" + find_post_res_time; //creating answer which consist all information about user's friends posts
                }
            }
            catch
            {
                return "error"; //wrong token, or user have no friends
            }

        }

        [HttpGet("add_friend")] // adding new friend for user 
        public string add_friend(string token, string username)
        {
            try
            {
                string encoded_token = Tokens.GetName(token); //get encoded token (user id)
                List<User> find_users = UsersDTO.AddFriend(encoded_token, username); //adding friend with this username
                if (find_users[0].id != "-2" && find_users[0].id != "-1") //checking for adding errrors
                {
                    return "success";
                }
                else
                {
                    return "notfound"; //there no user in database with this username
                }
            }
            catch
            {
                return "error"; // wrong token
            }
        }

        [HttpGet("edit_username")]
        public string edit_username(string token, string new_username) //editing username of this user
        {
            bool isAlpha_username = Regex.IsMatch(new_username, @"^[a-zA-Z0-9_]+$"); //checking new username format
            bool isAlpha_newusername= new_username.All(Char.IsDigit); // checking if new username consist only digits
            if (isAlpha_username && new_username.Length>3 && isAlpha_newusername == false) //checking new usernmae for format
            {
                try
                {
                    string encoded_token = Tokens.GetName(token); // get uncoded token(user id)
                    List<User> edit_username = UsersDTO.EditUsername(encoded_token, new_username);//editing username to new
                    return edit_username[0].id; // editing status code
                }
                catch
                {
                    return "error"; //wrong token
                }
            }
            else
            {
                return "bad_username"; //wrong new username format
            }
        }


        [HttpGet("delete_friend")]
        public string delete_friend(string token, string username) //delete friend of this user
        {
            try
            {
                string encoded_token = Tokens.GetName(token); //get encoded token (user id)
                List<User> find_users = UsersDTO.DeleteFriend(encoded_token, username); //deleting friend by username
                if (find_users[0].id != "-2" && find_users[0].id != "-1") //if there is no error code
                {
                    return "success";
                }
                else
                {
                    return "notfound"; //there is no user with this username in database, or user is not friend of this user
                }
            }
            catch
            {
                return "error"; //wrong token
            }
        }

        [HttpGet("accept_friend")]
        public string accept_friend(string token, string username) // accepting friendship request for this user 
        {
            try
            {
                string encoded_token = Tokens.GetName(token); //get encoded token(user id)
                List<User> find_users = UsersDTO.AcceptFriend(encoded_token, username); //accepting friend with gotten username for this user
                if (find_users[0].id != "-1" && find_users[0].id != "-2")//username friend in database, users are not friends yet
                {
                    return "success";
                }
                else
                {
                    return "notfound"; //friend username is not in database or users are alredy friends
                }
            }
            catch
            {
                return "error"; // wrong token
            }
        }


        [HttpGet("search_user")]
        public string search_user(string token, string search_stroke)//searching users by str, which username consist this str
        {
            if (search_stroke.Contains(" ") || search_stroke.Length < 3) //checking searh stroke for format
            {
                return "error";
            }
            else
            {
                try
                {
                    string encoded_token = Tokens.GetName(token); //get uncodedf token(user id)
                    List<User> find_users = UsersDTO.SearchPeople(encoded_token, search_stroke); //getting users, that consist entered str
                    if (find_users[0].id != "-7") //if there is some users that consists str in username
                    {
                        string find_res = find_users[0].username; //first username,of found users
                        for (int i = 1; i < find_users.Count; i++)
                        {
                            find_res = find_res + ";" + find_users[i].username; //adding other found users
                        }
                        return "{" + find_res + "}";//stroke of found usernasmes
                    }
                    else
                    {
                        return "notfound"; //no users consists str in username
                    }
                }
                catch
                {
                    return "error"; //wrong token
                }
            }
        }


        [HttpGet("load_friends")]
        public string load_friends(string token) //getting all friends usernames of this user
        {
            try
            {
                string encoded_token = Tokens.GetName(token); //get encoded token (user id)
                List<User> find_friends = UsersDTO.LoadFriends(encoded_token);//getting all user's friends id

                if (find_friends[0].id != "-1") //if user have some friends
                {
                    string find_res = UsersDTO.GetUsersById(find_friends[0].id).username;
                    for (int i = 1; i < find_friends.Count; i++)
                    {
                        find_res = find_res + ";" + UsersDTO.GetUsersById(find_friends[i].id).username; //generating usernames list
                    }
                    return "{" + find_res + "}"; //creating answer string
                }
                else
                {
                    return "notfound"; //if user have no friends
                }
            }
            catch
            {
                return "error"; // wrong token
            }
        }

        [HttpGet("load_requests")]
        public string load_requests(string token) //getting all user incoming friendship requests
        {
            try
            {
                string encoded_token = Tokens.GetName(token); // get encoded token(user id)
                List<User> find_friends = UsersDTO.LoadFriendsRequests(encoded_token); //getting all users that sent request for this user
                if (find_friends[0].id != "-1") //there is some user requests
                {
                    string find_res = UsersDTO.GetUsersById(find_friends[0].id).username;
                    for (int i = 1; i < find_friends.Count; i++)
                    {
                        find_res = find_res + ";" + UsersDTO.GetUsersById(find_friends[i].id).username; //add incoming request username
                    }
                    return "{" + find_res + "}"; //answer string of usernames incoming requests
                }
                else
                {
                    return "notfound"; //there is no requests
                }
            }
            catch
            {
                return "error"; //wrong token
            }
        }

    }
}

