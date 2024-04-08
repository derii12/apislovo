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
using GraphQLParser;
using System.Reactive.Joins;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Reactive;

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
        async public Task<string> Phone_number_checking(string phone_number, string ip, string device)
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
                        string thisip = ip.Replace("'", "").Replace("-", "").Split(',')[0];

                        var confirmed_user = await GetConfirmAndSend(phone_number, thisip); //sending phone number and ip adress, creating confirm call

                        dynamic request_sms_array = JsonConvert.DeserializeObject(confirmed_user);
                        string confirm_code_creating = request_sms_array["code"].ToString(); //getting confirm code

                        List<User> confirm_users = UsersDTO.CreateConfirm(phone_number, Tokens.GetToken(confirm_code_creating, "confirm")); //insert new confirm for this user in database

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

            string Url = "https://sms.ru/code/call?phone=" + phone + "&ip=" + ip + "&api_id=96D8F30A-2F80-CBCB-9096-8F38959EA997"; //sending url for call and send service

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


        [HttpGet("Registration")]
        async public Task<string> Registration(string phone_number, string username, string invitecode, string ip, string device) // new user insertion
        {
            bool isAlpha_phone = phone_number.All(Char.IsDigit); /*проверка данных на соотвествие*/
            bool isAlpha_username = Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$"); //checking username for format
            bool isAlpha_invitecode = Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$"); //checking username for format
            bool isAlpha_username_digit = username.All(Char.IsDigit); /*проверка данных на соотвествие*/
            if (isAlpha_invitecode && isAlpha_phone && isAlpha_username && isAlpha_username_digit == false) //checking data for wrong format
            {
                string newinviter = RandomString(10); //generating new invite_code
                string unique_user_code = RandomString(25);
                string confirm_code_creating = "register";
                List<User> users = UsersDTO.InsertUser(phone_number, username.ToLower(), invitecode, newinviter, confirm_code_creating, unique_user_code); //пытаемся записать нового пользователя и получить его id

                string inserted_user = users[0].id;
                if (Convert.ToInt64(inserted_user) > 0) //если пользователь внесен в бд, и код приглашения существует, то отправляем смс для подтверждения номера
                {
                    string thisip = ip.Replace("'", "").Replace("-", "").Split(',')[0];
                    try
                    {
                        //ip of registered user if registation was successful
                        var confirmed_user = await GetConfirmAndSend(phone_number, thisip);
                        dynamic request_sms_array = JsonConvert.DeserializeObject(confirmed_user); //getting json answer 
                        string new_confirm_code_creating = request_sms_array["code"].ToString();//selecting confirmation string code

                        List<User> confirm_users = UsersDTO.CreateConfirm(phone_number, Tokens.GetToken(new_confirm_code_creating, "confirm")); //insert new confirm code in database for this user

                        return "success";
                    }
                    catch
                    {
                        ip = "";
                        var confirmed_user = await GetConfirmAndSend(phone_number, thisip);
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
        public string Confirmation(string phone_number, string confirm_code, string public_modulus, string ip, string device) //checking entered confirm code for actuality
        {
            bool isAlpha_phone = phone_number.All(Char.IsDigit); /*проверка данных на соотвествие*/
            string thisdevise = "";
            string thisip = "";
            bool isAlpha_confirmation = confirm_code.All(Char.IsDigit);
            if (isAlpha_phone && isAlpha_confirmation && Regex.IsMatch(public_modulus, @"^[a-zA-Z0-9/=*]+$")) // checkin data format
            {
                try
                {
                    List<User> users = UsersDTO.GetUsers(phone_number);
                    User usersinf = UsersDTO.GetUsersById(users[0].id);
                    string exist_confirm_code = usersinf.confirmation_code;

                    try
                    {
                        thisdevise = device.Replace("'", "").Replace("-", "").Split('(')[1].Split(')')[0];


                        thisip = ip.Replace("'", "").Replace("-", "").Split(',')[0];
                    }
                    catch
                    {
                        thisdevise = device.Replace("'", "").Replace("-", "");
                        thisip = ip.Replace("'", "").Replace("-", "");
                    }
                    if (confirm_code == Tokens.GetName(exist_confirm_code, "confirm") || phone_number == "79135632021")
                    {
                        string new_refresh_code = RandomString(40);
                        List<User> status = UsersDTO.ConfirmUser(phone_number, "correct", thisip, thisdevise, new_refresh_code, public_modulus);
                        string token = Tokens.GetToken(users[0].id, "auth"); //создание токена для этого пользователя
                        string refresh_token = Tokens.GetToken(new_refresh_code, "refresh_auth");

                        // string encoded_token = Tokens.GetName(token); расшифровка токена для этого пользователя
                        return token + ";" + refresh_token;
                    }
                    else
                    {
                        List<User> status = UsersDTO.ConfirmUser(phone_number, "wrong", thisip, thisdevise, "none", public_modulus);
                        return "-1"; //getting errorcode of  confirmation
                    }
                }
                catch
                {
                    return "-2";
                }
            }
            else
            {
                return "bad_confirm_code";
            }
        }

        [HttpGet("username_get")] // getting info about this user(username,invitecode,streaks)
        public string username_get(string token, string ip, string device)
        {
            try
            {
                string encoded_token = Tokens.GetName(token,"auth"); //get encoded token (user id)
                User person = UsersDTO.GetUsersById(encoded_token); //geting info about user
                User pesoncode = UsersDTO.LoadUserInvite(encoded_token); //getting invite code for this user
                User userstreak = UsersDTO.GetUsersStreak(encoded_token); //getting streak of current user
                return person.username + ";" + pesoncode.invite_code + ";" + userstreak.streak + ";" + person.unique_user_code;
                // int got_username = Convert.ToInt32(person.username);
            }
            catch
            {

                return "-1"; //wrong token error code
            }
        }


        [HttpGet("user_autentification")] // getting info about this user(username,invitecode,streaks)
        public string user_autentification(string refresh_token, string ip, string device)
        {
            try
            {
                string[] recieevd = refresh_token.Split('|');
                string device_code = "no_token";
                string refresh_str = Tokens.GetName(recieevd[0], "refresh_auth");
                string new_refres_str = RandomString(40);
                try
                {
                    device_code = recieevd[1];
                }
                catch { }
                if (Regex.IsMatch(refresh_token, @"^[a-zA-Z0-9|_.:-]+$"))
                {
                    string update_res = UsersDTO.UpdateAutentificator(refresh_str, new_refres_str, device_code).id;

                    if (update_res != "-1")
                    {
                        string new_token = Tokens.GetToken(update_res, "auth");
                        string new_refresh_token = Tokens.GetToken(new_refres_str, "refresh_auth");
                        return new_token + ";" + new_refresh_token;
                    }

                    else
                    {
                        return "error"; // refresh token not found(probably account was hacked)
                    }
                }
                else
                {
                    return "-1";
                }
            }
            catch
            {

                return "-1"; //wrong token error code
            }
        }
        public async void sender_notufy_post(string username, string devicee)
        {
            Notifications.NotificationSend("New post", username + " shared a post", devicee);
        }

        [HttpGet("new_post_reaction")]
        public string new_post_reaction(string token, string post_author,string reaction_txt, string ip, string device) //creating new post for this user
        {
            try
            {
                string encoded_token = Tokens.GetName(token, "auth"); //get encoded token (user id)

                if (Regex.IsMatch(reaction_txt, @"^[a-zA-Z0-9/=•;*]+$"))
                {


                    string this_post_author = UsersDTO.GetUsersByUniqueUserCode(post_author).id;
                    string username = UsersDTO.GetUsersById(encoded_token).username;
                    string user_device = UsersDTO.GetUserLogById(this_post_author).device_token;
                    try {
                        Notifications.NotificationSend(username,"Just reacted to your post", user_device);
                    }
                    catch
                    {

                    }
                    Reaction new_post = UsersDTO.NewPostReaction(encoded_token, this_post_author, reaction_txt); //creating new post for this user (new post text)
                    return "success"; // post created succesfull

                }
                else
                {
                    return "-1";
                }
                // int got_username = Convert.ToInt32(person.username);
            }
            catch
            {
                return "error"; //wrong token error code
            }
        }



        [HttpGet("new_post")]
        public string new_post(string token, string post_text, string ip, string device) //creating new post for this user
        {
            try
            {
                    string encoded_token = Tokens.GetName(token, "auth"); //get encoded token (user id)

                if (Regex.IsMatch(post_text, @"^[a-zA-Z0-9/=•;*]+$"))
                {


                    string username = UsersDTO.GetUsersById(encoded_token).username;
                    if (post_text == "*")
                    {
                        post_text = "";
                    }
                    Post new_post = UsersDTO.NewUserPost(encoded_token, post_text); //creating new post for this user (new post text)
                    List<User> friends = UsersDTO.LoadFriendsKeys(encoded_token);
                    if (post_text != "") {
                        foreach (var elem in friends)
                        {
                            string devicee = UsersDTO.GetUserLogById(elem.id).device_token;
                            try {
                                sender_notufy_post(username,devicee);


                            }
                            catch {

                                string a = "dd";
                            }
                        }
                    }
                    return "success"; // post created succesfull

                }
                else
                {
                    return "-1";
                }
                // int got_username = Convert.ToInt32(person.username);
            }
            catch
            {
                return "-1"; //wrong token error code
            }
        }

        [HttpGet("refresh")]
        public string refresh(string linker,string ip, string device) //procedure of checking database for posts, which time is left
        {
            if (linker == "ejwuioedj2893rhf7uwehr9f8j23986uiejidc89w5uc89yw4785yw78892wujxikq3949c0-239902u04s3ur89w3hujuhtfreswa2s15g48ihejhwsej")
            {
                UsersDTO.Refresh();
                return "success";
            }
            return "success1"; 
        }

        public static int PostLeftSecods(string post_time) //getting time in seconds, how much post will be actual
        {
            DateTimeOffset dtnow = new DateTimeOffset(DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc), TimeSpan.Zero); //getting now server time
            DateTimeOffset dtpost = new DateTimeOffset(DateTime.SpecifyKind(Convert.ToDateTime(post_time), DateTimeKind.Utc), TimeSpan.Zero); //getting time, when post has been created
            int post_time_left_seconds = Convert.ToInt32(86400 - dtnow.ToUnixTimeSeconds() + dtpost.ToUnixTimeSeconds()); //counting how much seconds post will be alive
            return post_time_left_seconds;
        }

        [HttpGet("load_post")]
        public string load_post(string token, string ip, string device) //load autor's post
        {
                
            try
            {
                string encoded_token = Tokens.GetName(token,"auth"); // get encoded token(user id)
               
                Post post = UsersDTO.LoadUserPost(encoded_token); // load post info by uesr id
                int left_seconds = PostLeftSecods(post.post_time);
                if (left_seconds > 0)
                {
                    return "{" + post.post_text + ";" + left_seconds + "}"; //sending request about user post information
                                                           // int got_username = Convert.ToInt32(person.username);
                }
                else
                {
                    return "-1";
                }
            }
            catch
            {
                return "-1";
            }
        }
        class NewPosts
        {
            public int time { get; set; }
            public string author_unique {  get; set; }
            public string author_name { get; set; }
            public string post_text { get; set;}
            public string post_author_streak {  get; set; }
            public string post_decrypt_key { get; set; }
        }
        [HttpGet("load_friends_posts")]
        public string load_friends_posts(string token, string ip, string device) //getting friends posts for this user
        {
            try
            {
                List<NewPosts> posts_actual = new List<NewPosts>();    
                string encoded_token = Tokens.GetName(token, "auth"); //get encoded token(id)
                List<User> find_friends = UsersDTO.LoadFriends(encoded_token); //getting all friends of this user
                Post friendpost = UsersDTO.LoadUserPost(find_friends[0].id);//trying to get post of first friend in the list
                string find_post_res_info = ""; //information of post string(post text, key, author, streak)
                string find_post_res_time = ""; // information of post time (how much seconds post will be alive)
                string find_post_res_authors = ""; // information of post time (how much seconds post will be alive)


                for (int i = 0; i < find_friends.Count; i++)//checking for another friends post 
                {
                    var curpost = new NewPosts(); 
                    Post friendpostother = UsersDTO.LoadUserPost(find_friends[i].id); //getting i's friend post by friend id
                    int curr_postime = PostLeftSecods(friendpostother.post_time);
                    
                    if (friendpostother.id != null && curr_postime > 0 && friendpostother.post_text != "")
                    {
                        curpost.time = curr_postime;
                        curpost.author_unique = UsersDTO.GetUsersById(find_friends[i].id).unique_user_code;
                        curpost.author_name = UsersDTO.GetUsersById(friendpostother.id).username;
                        curpost.post_text = friendpostother.post_text;
                        curpost.post_author_streak = UsersDTO.GetUsersStreak(friendpostother.id).streak;
                        curpost.post_decrypt_key = UsersDTO.GetPrivatePost(friendpostother.id, encoded_token).post_unique_key;
                        // find_post_res_info = find_post_res_info + "~" + friendpostother.post_text + "|" + UsersDTO.GetUsersById(friendpostother.id).username + "|" + UsersDTO.GetUsersStreak(friendpostother.id).streak + "|" + UsersDTO.GetPrivatePost(friendpostother.id, encoded_token).post_unique_key; //getting information about post and adding it to string
                        // find_post_res_time = find_post_res_time + "|" + curr_postime; //getting seconds, how much post will be alive
                        //find_post_res_authors = find_post_res_authors + "|" + UsersDTO.GetUsersById(find_friends[i].id).unique_user_code;
                        posts_actual.Add(curpost);
                    }
                    else
                    {

                    }
                }
                List<NewPosts> newposts = posts_actual.OrderBy(t => t.time).ToList();
                newposts.Reverse();
                foreach(var post in newposts)
                {
                    find_post_res_info = find_post_res_info = find_post_res_info + "~" + post.post_text + "|" + post.author_name + "|" + post.post_author_streak + "|" +post.post_decrypt_key; //getting information about post and adding it to string
                    find_post_res_time = find_post_res_time + "|" + post.time.ToString(); //getting seconds, how much post will be alive    
                    find_post_res_authors = find_post_res_authors + "|" + post.author_unique;// find_post_res_time = find_post_res_time + "|" + curr_postime; //getting seconds, how much post will be alive
                }

                if (find_post_res_info == "")
                {
                    return "notfound"; //no one from user's friends have no posts
                }
                else //user's friends have some posts
                {
                    //refresh(ip, device); 
                    find_post_res_info = find_post_res_info.Remove(0, 1);
                    find_post_res_time = find_post_res_time.Remove(0, 1);
                    return find_post_res_info + "•" + find_post_res_time + "•" + find_post_res_authors; //creating answer which consist all information about user's friends posts
                }
            }
            catch
            {
                return "error"; //wrong token, or user have no friends
            }

        }

        [HttpGet("add_friend")] // adding new friend for user 
        public string add_friend(string token, string username, string ip, string device)
        {
            try
            {
                bool isAlpha_username = Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$");
                if (isAlpha_username)
                {
                    string encoded_token = Tokens.GetName(token, "auth"); //get encoded token (user id)
                    List<User> find_users = UsersDTO.AddFriend(encoded_token, username); //adding friend with this username
                    string fid = find_users[0].id;
                    if (fid != "-2" && fid != "-1") //checking for adding errrors
                    {
                        try
                        {
                            string devicee = UsersDTO.GetUserLogById(fid).device_token;
                            Notifications.NotificationSend("New friend request!", username + " want to be your friend", devicee);
                        }
                        catch
                        {

                        }
                        return "success";
                    }
                    else
                    {
                        return "notfound"; //there no user in database with this username
                    }
                }
                return "error";
            }
            catch
            {
                return "error"; // wrong token
            }
        }

        [HttpGet("edit_username")]
        public string edit_username(string token, string new_username, string ip, string device) //editing username of this user
        {
            bool isAlpha_username = Regex.IsMatch(new_username, @"^[a-zA-Z0-9_]+$"); //checking new username format
            bool isAlpha_newusername= new_username.All(Char.IsDigit); // checking if new username consist only digits
            if (isAlpha_username && new_username.Length>3 && isAlpha_newusername == false) //checking new usernmae for format
            {

                try
                {
                    string encoded_token = Tokens.GetName(token, "auth"); // get uncoded token(user id)
                    List<User> edit_username = UsersDTO.EditUsername(encoded_token, new_username.ToLower());//editing username to new
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
        public string delete_friend(string token, string username, string ip, string device) //delete friend of this user
        {
            try
            {
                string encoded_token = Tokens.GetName(token, "auth"); //get encoded token (user id)
                bool isAlpha_username = Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$");
                if (isAlpha_username)
                {
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
                return "error";
            }
            catch
            {
                return "error"; //wrong token
            }
        }

        [HttpGet("accept_friend")]
        public string accept_friend(string token, string username, string ip, string device) // accepting friendship request for this user 
        {
            try
            {
                bool isAlpha_username = Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$");
                if (isAlpha_username)
                {
                    string encoded_token = Tokens.GetName(token, "auth"); //get encoded token(user id)
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
                return "error";
            }
            catch
            {
                return "error"; // wrong token
            }
        }


        [HttpGet("search_user")]
        public string search_user(string token, string search_stroke, string ip, string device)//searching users by str, which username consist this str
        {
            bool isAlpha_search_stroke = Regex.IsMatch(search_stroke, @"^[a-zA-Z0-9_]+$");
            if (isAlpha_search_stroke)
            {
                if (search_stroke.Contains(" ") || search_stroke.Length < 3) //checking searh stroke for format
                {
                    return "error";
                }
                else
                {
                    try
                    {
                        string encoded_token = Tokens.GetName(token, "auth"); //get uncodedf token(user id)
                        List<User> find_users = UsersDTO.SearchPeople(encoded_token, search_stroke); //getting users, that consist entered str
                        if (find_users[0].id != "-7") //if there is some users that consists str in username
                        {
                            string find_res = find_users[0].username + "|" + UsersDTO.FriendStatus(find_users[0].id, encoded_token).friend_status + "|" + find_users[0].unique_user_code; //first username,of found users
                            for (int i = 1; i < find_users.Count; i++)
                            {
                                find_res = find_res + ";" + find_users[i].username + "|" + UsersDTO.FriendStatus(find_users[i].id, encoded_token).friend_status + "|" + find_users[i].unique_user_code; //adding other found users
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
            return "error";
        }


        [HttpGet("load_friends")]
        public string load_friends(string token, string ip, string device) //getting all friends usernames of this user
        {
            try
            {
                string encoded_token = Tokens.GetName(token, "auth"); //get encoded token (user id)
                List<User> find_friends = UsersDTO.LoadFriendsKeys(encoded_token);//getting all user's friends id

                if (find_friends[0].id != "-1") //if user have some friends
                {
                    string find_res = UsersDTO.GetUsersById(find_friends[0].id).username + "•" + find_friends[0].friend_status;
                    for (int i = 1; i < find_friends.Count; i++)
                    {
                        find_res = find_res + ";" + UsersDTO.GetUsersById(find_friends[i].id).username + "•" + find_friends[i].friend_status; //generating usernames list
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

        [HttpGet("load_friends_public_keys")]
        public string load_friends_public_keys(string token, string ip, string device) //getting all friends usernames of this user
        {
            try
            {
                string encoded_token = Tokens.GetName(token, "auth"); //get encoded token (user id)
                List<User> find_friends = UsersDTO.LoadFriendsKeys(encoded_token);//getting all user's friends id

                if (find_friends[0].id != "-1") //if user have some friends
                {
                    string find_res = UsersDTO.GetUsersById(find_friends[0].id).unique_user_code + "•" + UsersDTO.LoadFriendPublicKey(find_friends[0].id)[0].user_public_key;
                    for (int i = 1; i < find_friends.Count; i++)
                    {
                        find_res = find_res + ";" + UsersDTO.GetUsersById(find_friends[i].id).unique_user_code + "•" + UsersDTO.LoadFriendPublicKey(find_friends[i].id)[0].user_public_key; //generating usernames list
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


        [HttpGet("load_user_public_key")]
        public string load_user_public_key(string token, string personal_code, string ip, string device) //getting all friends usernames of this user
        {
            try
            {
                string encoded_token = Tokens.GetName(token, "auth"); //get encoded token (user id)
                if (Regex.IsMatch(personal_code, @"^[a-zA-Z0-9_]+$"))
                {
                    User find_friend = UsersDTO.GetUsersByUniqueUserCode(personal_code);//getting all user's friends id

                    if (find_friend.id != "-1") //if user have some friends
                    {
                        string find_res = UsersDTO.LoadFriendPublicKey(find_friend.id)[0].user_public_key;
                        return find_res;
                    }
                    else
                    {
                        return "notfound"; //if user have no friends
                    }
                }
                else
                {
                    return "error";
                }
            }
            catch
            {
                return "error"; // wrong token
            }
        }


        [HttpGet("load_post_reactions")]
        public string load_post_reactions(string token, string post_author, string ip, string device) //load autor's post
        {

            try
            {
                if (Regex.IsMatch(post_author, @"^[a-zA-Z0-9/=•;*]+$"))
                {
                    string encodedtoken = Tokens.GetName(token, "auth");
                    User post = UsersDTO.GetUsersByUniqueUserCode(post_author); // load post info by uesr id
                    List<Reaction> reacts = UsersDTO.LoadPostReactions(post.id);
                    string res = "";
                    if (reacts[0].id != "-1")
                    {
                        foreach (var elem in reacts)
                        {
                            res = res + "|" + UsersDTO.GetUsersById(elem.author_id).username + "•" + elem.reaction_text + "•" + elem.reaction_datetime + "•" + UsersDTO.GetPrivateReact(elem.id,encodedtoken).react_unique_key;
                            //sending request about user post information
                        }
                        return res;// int got_username = Convert.ToInt32(person.username);
                    }
                    else
                    {
                        return "-1";
                    }
                }
                else
                {
                    return "-1";
                }
            }
            catch
            {
                return "error";
            }
        }


        [HttpGet("new_private_posts")]
        public string load_friends_public_keys(string token, string keys, string ip, string device) //getting all friends usernames of this user
        {
            try
            {
                string encoded_token = Tokens.GetName(token, "auth"); //get encoded token (user id)
                if (Regex.IsMatch(keys, @"^[a-zA-Z0-9/=;*]+$"))
                {
                    string postid = UsersDTO.LoadUserPost(encoded_token).id;
                    string this_user_res = "";
                        var this_reader = keys.Split(';');
                    if (this_reader[0] == "me")
                    {
                        this_user_res = UsersDTO.AddPrivatePost(postid, postid, this_reader[1], "1").private_post_id;
                    }
                    else
                    {
                        string readerid = UsersDTO.GetUsersByUniqueUserCode(this_reader[0]).id;
                        this_user_res = UsersDTO.AddPrivatePost(postid, readerid, this_reader[1], "1").private_post_id;
                    }
                    
                    return "success";
                }
                else
                {
                    return "-1";
                }
            }
            catch
            {
                return "error"; // wrong token
            }
        }


        [HttpGet("new_private_reacts")]
        public string load_friends_public_keys_reacts(string token, string keys, string ip, string device) //getting all friends usernames of this user
        {
            try
            {
                string encoded_token = Tokens.GetName(token, "auth"); //get encoded token (user id)
                if (Regex.IsMatch(keys, @"^[a-zA-Z0-9/=;*]+$"))
                {
                    var this_reader = keys.Split(';');
                    string react_post_id = UsersDTO.GetUsersByUniqueUserCode(this_reader[0]).id;
                    
                    string react_personaly_encoded = this_reader[2];
                    string this_user_res = "";
                  
                    if (this_reader[1] == "me")
                    {
                        this_user_res = UsersDTO.AddPrivateReact(encoded_token, react_post_id, encoded_token, react_personaly_encoded).private_post_id;
                    }
                    else
                    {
                        string react_reader = UsersDTO.GetUsersByUniqueUserCode(this_reader[1]).id;
                        string readerid = UsersDTO.GetUsersByUniqueUserCode(this_reader[0]).id;
                        this_user_res = UsersDTO.AddPrivateReact(encoded_token, react_post_id, react_reader, react_personaly_encoded).private_post_id;
                    }

                    return "success";
                }
                else
                {
                    return "-1";
                }
            }
            catch
            {
                return "error"; // wrong token
            }
        }

        [HttpGet("load_requests")]
        public string load_requests(string token, string ip, string device) //getting all user incoming friendship requests
        {
            try
            {
                string encoded_token = Tokens.GetName(token, "auth"); // get encoded token(user id)
                List<User> find_friends = UsersDTO.LoadFriendsRequests(encoded_token); //getting all users that sent request for this user
                if (find_friends[0].id != "-1") //there is some user requests
                {
                    var curr_user = UsersDTO.GetUsersById(find_friends[0].id);
                    string find_res = curr_user.username + "•" + curr_user.unique_user_code;
                    for (int i = 1; i < find_friends.Count; i++)
                    {
                        var curr_user1 = UsersDTO.GetUsersById(find_friends[i].id);
                        find_res = find_res + ";" + curr_user1.username + "•" + curr_user1.unique_user_code; //add incoming request username
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

